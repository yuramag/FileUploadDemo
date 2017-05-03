using FileUploadDemoClient.FileUploadService;

namespace FileUploadDemoClient
{
    public sealed class FileViewModel : Changeable
    {
        public FileViewModel(BlobFileInfo file)
        {
            File = file;
        }

        public BlobFileInfo File { get; set; }
    }
}