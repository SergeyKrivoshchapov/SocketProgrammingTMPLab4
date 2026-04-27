using System.Configuration;
using System.Data;
using System.Windows;
using Task1GUI.Models;
using Task1GUI.ViewModels;
using Task1GUI.Views;
using MVVMClassLibrary.Services;
using Task1GUI.Services;

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

            IDiskDriverService diskDriverService = new DiskDriverService(disks => new DiskDriver(disks));

            IClientTransferModel transferModel = new ClientModel();
            IServerTransferModel serverTransferModel = new ServerModel();

            IMainWindowVM vm = new MainWindowVM(dialogService, diskDriverService, transferModel, serverTransferModel);
            MainWindow mainWindow = new MainWindow(vm);

            dialogService.RegisterWindow(vm, mainWindow);

            mainWindow.Show();
        }
    }

}
