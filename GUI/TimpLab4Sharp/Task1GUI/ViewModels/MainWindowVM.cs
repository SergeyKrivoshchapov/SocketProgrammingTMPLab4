using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Input;
using Task1GUI.Models;
using MVVMClassLibrary.Services;
using MVVMClassLibrary.Commands;
using MVVMClassLibrary.ViewModels;
using Task1GUI.Services;

namespace Task1GUI.ViewModels
{
    public interface IMainWindowVM
    {
        // Список дисков
        ObservableCollection<string> Drives { get; }
        // Каталоги и файлы в выбранном диске
        ObservableCollection<string> Items { get; }
        // Выбранный диск
        string SelectedDrive { get; set; }
        // Выбранный элемент (каталог или файл)
        string? SelectedItem { get; set;  }
        // Ip-адрес для подключения со стороны клиента
        string IpAddress { get; set; }
        // Вывод сервера
        string ServerLog { get; set; }
        // Вывод клиента
        string ClientLog { get; set; }
        // Включен ли сервер
        bool IsServerRunning { get; }
        // Подключился ли сервер к серверу по IpAddress
        bool IsClientConnected { get; }

        // Включение/выключение сервера
        ICommand ToggleServerCommand { get; }
        // Подключение клиента к серверу по IpAddress
        ICommand ConnectCommand { get; }
        // Отключение клиента от сервера
        ICommand DisconnectCommand { get; }
        // Команда для выхода из приложения
        ICommand ExitCommand { get; }
        // Команда для отправки выбранного элемента на сервер
        ICommand SendToServerCommand { get; }
        // Команда для отправки списка логических устройств на клиент
        ICommand SendToClientCommand { get; }
    }

    public class MainWindowVM : BaseViewModel, IMainWindowVM
    {
        private string _selectedDrive = string.Empty;
        private string? _selectedItem = null;
        private string _ipAddress = string.Empty;
        private string _serverLog = string.Empty;
        private string _clientLog = string.Empty;
        private bool _isServerRunning = false;
        private bool _isClientConnected = false;

        private readonly RelayCommand _toggleServerCommand;
        private readonly RelayCommand _connectCommand;
        private readonly RelayCommand _disconnectCommand;
        private readonly RelayCommand _exitCommand;
        private readonly RelayCommand _sendToServerCommand;
        private readonly RelayCommand _sendToClientCommand;

        public ObservableCollection<string> Drives { get; private set
            {
                field = value;
                OnPropertyChanged();
            } }
        public ObservableCollection<string> Items { get; private set {
                field = value;
                OnPropertyChanged();
            } }
        public string SelectedDrive {
            get => _selectedDrive;
            set
            {
                if (Set(ref _selectedDrive, value))
                {
                    LoadItems();
                }
            }
        }
        public string? SelectedItem {
            get => _selectedItem;
            set
            {
                if (Set(ref _selectedItem, value))
                {
                    RefreshCommands();
                }
            }
        }
        public string IpAddress
        {
            get => _ipAddress;
            set
            {
                if (Set(ref _ipAddress, value))
                {
                    RefreshCommands();
                }
            }
        }
        public string ServerLog {
            get => _serverLog;
            set
            {
                Set(ref _serverLog, value);
            }
        }
        public string ClientLog {
            get => _clientLog;
            set
            {
                Set(ref _clientLog, value);
            }
        }
        public bool IsServerRunning { 
            get 
            {
                return _isServerRunning;
            }
            private set
            {
                if (Set(ref _isServerRunning, value))
                {
                    RefreshCommands();
                }
            }
        }
        public bool IsClientConnected
        {
            get => _isClientConnected;
            private set
            {
                if (Set(ref _isClientConnected, value))
                {
                    RefreshCommands();
                }
            }
        }

        public ICommand ToggleServerCommand => _toggleServerCommand;
        public ICommand ConnectCommand => _connectCommand;
        public ICommand DisconnectCommand => _disconnectCommand;
        public ICommand ExitCommand => _exitCommand;
        public ICommand SendToServerCommand => _sendToServerCommand;
        public ICommand SendToClientCommand => _sendToClientCommand;

        private readonly IDialogService _dialogService;
        private readonly IDiskDriverService _diskDriverService;
        private readonly IClientTransferModel _clientTransferModel;
        private readonly IServerTransferModel _serverTransferModel;

        private IDiskDriver? _diskDriver = null;

        public MainWindowVM(IDialogService dialogService, IDiskDriverService diskDriverService, 
            IClientTransferModel clientTransferModel, IServerTransferModel serverTransferModel)
        {
            _dialogService = dialogService;
            _diskDriverService = diskDriverService;
            _clientTransferModel = clientTransferModel;
            _serverTransferModel = serverTransferModel;

            _toggleServerCommand = new RelayCommand(ToggleServer);
            _connectCommand = new RelayCommand(ClientConnect, CanClientConnect);
            _disconnectCommand = new RelayCommand(ClientDisconnect, CanClientDisconnect);
            _exitCommand = new RelayCommand(Exit);
            _sendToServerCommand = new RelayCommand(SendToServer, CanSendToServer);
            _sendToClientCommand = new RelayCommand(SendToClient, CanSendToClient);

            Drives = new ObservableCollection<string>();
            Items = new ObservableCollection<string>();

            _serverTransferModel.UpdateLogs += UpdateServerLog;
            _clientTransferModel.UpdateLogs += UpdateClientLog;

            RefreshCommands();
        }

        private bool CanClientConnect(object? _)
        {
            return !IsClientConnected && IPAddress.TryParse(GetTransferIpAddress(), out IPAddress? _);
        }

        private bool CanClientDisconnect(object? _)
        {
            return IsClientConnected;
        }

        private bool CanSendToServer(object? _)
        {
            return IsClientConnected && !string.IsNullOrEmpty(SelectedItem);
        }

        private bool CanSendToClient(object? _)
        {
            return IsServerRunning && IsClientConnected;
        }

        private void ToggleServer(object? sender)
        {
            if (IsServerRunning)
            {
                if (_serverTransferModel.StopServer())
                {
                    IsServerRunning = false;
                }
            }
            else
            {
                if (_serverTransferModel.StartServer())
                {
                    IsServerRunning = true;
                }
            }
        }

        private void ClientConnect(object? sender)
        {
            var transferIp = GetTransferIpAddress();

            bool status;
            List<string> disks;

            (status, disks) = _clientTransferModel.ConnectToServer(transferIp);

            if (status)
            {
                IsClientConnected = true;
                _diskDriver = _diskDriverService.GetDiskDriver(disks);
                Drives = new ObservableCollection<string>(_diskDriver.GetAllDisks());
            }
        }

        private string GetTransferIpAddress()
        {
            return IpAddress.Replace("_", string.Empty).Trim();
        }

        private void ClientDisconnect(object? sender)
        {
            if (_clientTransferModel.DisconnectToServer())
            {
                IsClientConnected = false;
            }
        }

        private void Exit(object? sender)
        {
            _dialogService.CloseWindow(this);
        }

        private void SendToServer(object? sender)
        {
            if (!IsClientConnected || _diskDriver == null || string.IsNullOrEmpty(SelectedItem))
            {
                return;
            }

            var selectedPath = CombineWindowsPath(GetSelectedDriveRoot(), SelectedItem.Substring(2));

            if (SelectedItem[0] == 'D')
            {
                _diskDriver.GetDirectoryContent(selectedPath);
            }
            else if (SelectedItem[0] == 'F')
            {
                _diskDriver.GetFileContent(selectedPath);
            }
        }

        private void SendToClient(object? sender)
        {
            if (!IsServerRunning || !IsClientConnected)
            {
                return;
            }

            _serverTransferModel.TransferToClient();
        }

        private void LoadItems()
        {
            if (string.IsNullOrEmpty(SelectedDrive)) return;

            try
            {
                if (_diskDriver == null)
                {
                    Items = new ObservableCollection<string>();
                    return;
                }
                else
                {
                    Items = new ObservableCollection<string>(_diskDriver.GetDirectoryContent(GetSelectedDriveRoot()));
                }

                SelectedItem = null;
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessageBox($"Не удалось подгрузить данные с {SelectedDrive}\n{ex.Message}");
            }
        }

        private void RefreshCommands()
        {
            _toggleServerCommand.RaiseCanExecuteChanged();
            _connectCommand.RaiseCanExecuteChanged();
            _disconnectCommand.RaiseCanExecuteChanged();
            _exitCommand.RaiseCanExecuteChanged();
            _sendToServerCommand.RaiseCanExecuteChanged();
            _sendToClientCommand.RaiseCanExecuteChanged();
        }

        private void UpdateServerLog(object? sender, string log)
        {
            ServerLog += log + "\n";
        }

        private void UpdateClientLog(object? sender, string log)
        {
            ClientLog += log + "\n";
        }

        private string GetSelectedDriveRoot()
        {
            if (string.IsNullOrWhiteSpace(SelectedDrive))
            {
                return string.Empty;
            }

            return SelectedDrive.EndsWith("\\") ? SelectedDrive : SelectedDrive + "\\";
        }

        private static string CombineWindowsPath(string basePath, string childName)
        {
            var normalizedBase = basePath.TrimEnd('\\');
            return normalizedBase + "\\" + childName;
        }
    }
}
