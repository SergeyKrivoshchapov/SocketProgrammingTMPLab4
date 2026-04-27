using MVVMClassLibrary.Services;
using System.Configuration;
using System.Data;
using System.Windows;
using Task3Client.Models;
using Task3Client.ViewModels;
using Task3Client.Views;

namespace Task3Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            IDialogService dialogService = new DialogService();

            IUnitDispatcherModel unitDispatcherModel = new UnitDispatcherModel();

            IMainWindowVM vm = new MainWindowVM(dialogService, unitDispatcherModel);
            MainWindow mainWindow = new MainWindow(vm);

            dialogService.RegisterWindow(vm, mainWindow);

            mainWindow.Show();
        }
    }

}
