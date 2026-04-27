using MVVMClassLibrary.Services;
using System.Configuration;
using System.Data;
using System.Windows;
using Task2Client.Models;
using Task2Client.ViewModels;
using Task2Client.Views;

namespace Task2Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            IDialogService dialogService = new DialogService();

            IDispatcherModel dispatcherModel = new DispatcherModel();

            IMainWindowVM vm = new MainWindowVM(dispatcherModel, dialogService);
            MainWindow mainWindow = new MainWindow(vm);

            dialogService.RegisterWindow(vm, mainWindow);

            mainWindow.Show();
        }
    }

}
