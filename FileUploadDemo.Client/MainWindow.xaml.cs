using System.Windows;
using System.Windows.Input;

namespace FileUploadDemoClient
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var vm = DataContext as MainWindowViewModel;
            if (vm != null && vm.RefreshCommand.CanExecute(null))
                vm.RefreshCommand.Execute(null);
        }
    }
}
