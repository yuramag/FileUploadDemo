using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using System.Windows.Input;
using FileUploadDemoClient.FileUploadService;

namespace FileUploadDemoClient
{
    public class MainWindowViewModel : Changeable
    {
        private int m_isProcessing;

        public bool IsProcessing
        {
            get { return m_isProcessing != 0; }
            private set
            {
                var initial = IsProcessing;

                if (value)
                    m_isProcessing++;
                else
                    m_isProcessing--;

                if (initial != IsProcessing)
                {
                    NotifyOfPropertyChange(() => IsProcessing);
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private ObservableCollection<FileViewModel> m_data;

        public ObservableCollection<FileViewModel> Data
        {
            get { return m_data; }
            private set
            {
                if (!ReferenceEquals(m_data, value))
                {
                    m_data = value;
                    NotifyOfPropertyChange(() => Data);
                }
            }
        }

        private FileViewModel m_selectedFile;

        public FileViewModel SelectedFile
        {
            get { return m_selectedFile; }
            set
            {
                if (m_selectedFile != value)
                {
                    m_selectedFile = value;
                    NotifyOfPropertyChange(() => SelectedFile);
                }
            }
        }

        private string m_message;

        public string Message
        {
            get { return m_message; }
            set
            {
                if (m_message != value)
                {
                    m_message = value;
                    NotifyOfPropertyChange(() => Message);
                }
            }
        }

        private ICommand m_refreshCommand;

        public ICommand RefreshCommand
        {
            get { return m_refreshCommand ?? (m_refreshCommand = new RelayCommand(() => RefreshData(), () => !IsProcessing)); }
        }

        private async Task RefreshData()
        {
            IsProcessing = true;
            try
            {
                Message = "Refreshing...";
                using (var service = new FileUploadServiceClient())
                {
                    var data = await service.GetBlobFilesAsync();
                    Data = new ObservableCollection<FileViewModel>(data.Select(x => new FileViewModel(x)));
                }
                Message = "Data Refreshed";
            }
            catch (Exception e)
            {
                Message = "Error";
                MessageBox.Show(e.Message, "Error");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private ICommand m_addCommand;

        public ICommand AddCommand
        {
            get { return m_addCommand ?? (m_addCommand = new RelayCommand(() => AddFile(), () => !IsProcessing)); }
        }

        private async Task AddFile()
        {
            const int chunkSize = 2*1024*1024; // 2MB

            IsProcessing = true;
            try
            {
                Message = "Uploading File...";

                var dlg = new Microsoft.Win32.OpenFileDialog {Title = "Select File to Upload"};

                if (dlg.ShowDialog() != true)
                    return;
                var fileName = dlg.FileName;
                if (string.IsNullOrEmpty(fileName))
                    return;

                var sw = Stopwatch.StartNew();

                using (var service = new FileUploadServiceClient())
                {
                    var fileId = await service.CreateBlobFileAsync(Path.GetFileName(fileName),
                        fileName, new FileInfo(fileName).Length, Environment.UserName);

                    using (var stream = File.OpenRead(fileName))
                    {
                        var edb = new ExecutionDataflowBlockOptions {BoundedCapacity = 5, MaxDegreeOfParallelism = 5};

                        var ab = new ActionBlock<Tuple<byte[], int>>(x => service.AddBlobFileChunkAsync(fileId, x.Item2, x.Item1), edb);

                        foreach (var item in stream.GetByteChunks(chunkSize).Select((x, i) => Tuple.Create(x, i)))
                            await ab.SendAsync(item);

                        ab.Complete();

                        await ab.Completion;
                    }
                }

                await RefreshData();

                Message = string.Format("Elapsed: {0} seconds", sw.Elapsed.TotalSeconds);
            }
            catch (Exception e)
            {
                Message = "Error";
                MessageBox.Show(e.Message, "Error");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private ICommand m_deleteCommand;

        public ICommand DeleteCommand
        {
            get { return m_deleteCommand ?? (m_deleteCommand = new RelayCommand(() => DeleteFile(), () => !IsProcessing && SelectedFile != null)); }
        }

        private async Task DeleteFile()
        {
            var file = SelectedFile;
            if (file == null)
                return;

            IsProcessing = true;
            try
            {
                Message = "Deleting File...";

                var sw = Stopwatch.StartNew();

                using (var service = new FileUploadServiceClient())
                    await service.DeleteBlobFileAsync(file.File.BlobFileId);

                await RefreshData();

                Message = string.Format("Elapsed: {0} seconds", sw.Elapsed.TotalSeconds);
            }
            catch (Exception e)
            {
                Message = "Error";
                MessageBox.Show(e.Message, "Error");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private ICommand m_processCommand;

        public ICommand ProcessCommand
        {
            get { return m_processCommand ?? (m_processCommand = new RelayCommand(() => ProcessFile(), () => !IsProcessing && SelectedFile != null)); }
        }

        private async Task ProcessFile()
        {
            var file = SelectedFile;
            if (file == null)
                return;

            IsProcessing = true;
            try
            {
                Message = "Calculating File Hash...";

                var sw = Stopwatch.StartNew();

                using (var service = new FileUploadServiceClient())
                {
                    var result = await service.ProcessFileAsync(file.File.BlobFileId);
                    MessageBox.Show(result, "RESULT");
                }

                Message = string.Format("Elapsed: {0} seconds", sw.Elapsed.TotalSeconds);
            }
            catch (Exception e)
            {
                Message = "Error";
                MessageBox.Show(e.Message, "Error");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private ICommand m_saveAsCommand;

        public ICommand SaveAsCommand
        {
            get { return m_saveAsCommand ?? (m_saveAsCommand = new RelayCommand(() => SaveAsFile(), () => !IsProcessing && SelectedFile != null)); }
        }

        private async Task SaveAsFile()
        {
            var file = SelectedFile;
            if (file == null)
                return;

            IsProcessing = true;
            try
            {
                Message = "Saving File...";

                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Select Destination File",
                    FileName = file.File.Name,
                    Filter = "All Files|*.*"
                };

                if (dlg.ShowDialog() != true)
                    return;
                var fileName = dlg.FileName;
                if (string.IsNullOrEmpty(fileName))
                    return;

                var sw = Stopwatch.StartNew();

                using (var service = new FileUploadServiceClient())
                {
                    await service.SaveFileAsAsync(file.File.BlobFileId, fileName);
                }

                Message = string.Format("Elapsed: {0} seconds", sw.Elapsed.TotalSeconds);
            }
            catch (Exception e)
            {
                Message = "Error";
                MessageBox.Show(e.Message, "Error");
            }
            finally
            {
                IsProcessing = false;
            }
        }
    }
}