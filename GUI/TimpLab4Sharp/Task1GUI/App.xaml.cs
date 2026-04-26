using System.Configuration;
using System.Data;
using System.Windows;
using Task1GUI.Models;
using Task1GUI.ViewModels;
using Task1GUI.Views;
using MVVMClassLibrary.Services;

namespace Task1GUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            IDialogService dialogService = new DialogService();

            IDiskDriver diskDriver = new DiskDriver();
            IClientTransferModel transferModel = new ClientMockModel();
            IServerTransferModel serverTransferModel = new ServerMockModel();

            IMainWindowVM vm = new MainWindowVM(dialogService, diskDriver, transferModel, serverTransferModel);
            MainWindow mainWindow = new MainWindow(vm);

            dialogService.RegisterWindow(vm, mainWindow);

            mainWindow.Show();
        }
    }

}
