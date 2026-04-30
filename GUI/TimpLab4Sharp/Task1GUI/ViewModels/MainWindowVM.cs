using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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
        // Текущий полный путь
        string CurrentPath { get; }
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
        // Команда для навигации назад
        ICommand NavigateBackCommand { get; }

        // Обработка двойного клика на элементе
        void ItemDoubleClick(string? formattedItemName);

        // Навигация в родительскую папку
        void NavigateBack();
    }

    public class MainWindowVM : BaseViewModel, IMainWindowVM
    {
        private string _selectedDrive = string.Empty;
        private string? _selectedItem = null;
        private string _currentPath = string.Empty;
        private string _ipAddress = string.Empty;
        private string _serverLog = string.Empty;
        private string _clientLog = string.Empty;
        private bool _isServerRunning = false;
        private bool _isClientConnected = false;
        private string _lastSelectedDrive = string.Empty;  // Для сохранения диска при навигации назад

        private readonly RelayCommand _toggleServerCommand;
        private readonly RelayCommand _connectCommand;
        private readonly RelayCommand _disconnectCommand;
        private readonly RelayCommand _exitCommand;
        private readonly RelayCommand _sendToServerCommand;
        private readonly RelayCommand _sendToClientCommand;
        private readonly RelayCommand _navigateBackCommand;

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
                _lastSelectedDrive = _selectedDrive;

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
        public string CurrentPath
        {
            get => _currentPath;
            private set
            {
                Set(ref _currentPath, value);
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
        public ICommand NavigateBackCommand => _navigateBackCommand;

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
            _navigateBackCommand = new RelayCommand(_ => NavigateBack(), CanNavigateBack);

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

        private bool CanNavigateBack(object? _)
        {
            return IsClientConnected && _diskDriver != null && _diskDriver.CanNavigateBack();
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

                // Добавляем только реальные диски в ComboBox (без "...")
                Drives = new ObservableCollection<string>(_diskDriver.GetAllDisks());

                // Выбираем первый диск
                if (Drives.Count > 0)
                {
                    SelectedDrive = Drives[0];
                }
                else
                {
                    Items = new ObservableCollection<string>();
                }
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
                Items.Clear();
                SelectedItem = null;
                Drives.Clear();
                CurrentPath = string.Empty;
                if (_diskDriver != null)
                {
                    _diskDriver.ClearNavigation();
                }
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
            if (string.IsNullOrEmpty(SelectedDrive)) 
                return;

            try
            {
                if (_diskDriver == null)
                {
                    Items = new ObservableCollection<string>();
                    CurrentPath = string.Empty;
                    return;
                }

                var driveRoot = GetSelectedDriveRoot();
                _diskDriver.SetCurrentDrive(driveRoot);

                // Обновляем текущий путь
                CurrentPath = _diskDriver.GetCurrentPath();

                var rawContent = _diskDriver.GetDirectoryContent(driveRoot);
                var formattedItems = rawContent.Select(item => _diskDriver.GetFormattedItemName(item)).ToList();

                // Добавляем навигационные кнопки в начало списка
                var itemsWithNavButtons = new List<string> { ".", ".." };
                itemsWithNavButtons.AddRange(formattedItems);

                Items = new ObservableCollection<string>(itemsWithNavButtons);

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
            _navigateBackCommand.RaiseCanExecuteChanged();
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
            if (string.IsNullOrWhiteSpace(SelectedDrive) || SelectedDrive == "...")
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

        /// <summary>
        /// Обработка двойного клика на элементе
        /// </summary>
        public void ItemDoubleClick(string? formattedItemName)
        {
            if (!IsClientConnected || _diskDriver == null || string.IsNullOrEmpty(formattedItemName))
            {
                return;
            }

            try
            {
                // Обработка нажатия на "." (корень диска)
                if (formattedItemName == ".")
                {
                    // Возвращаемся в корень текущего диска
                    while (_diskDriver.CanNavigateBack())
                    {
                        _diskDriver.NavigateBack();
                    }
                    LoadItems();
                    return;
                }

                // Обработка нажатия на ".." (родительская папка)
                if (formattedItemName == "..")
                {
                    NavigateBack();
                    return;
                }

                var currentPath = _diskDriver.GetCurrentPath();
                var rawContent = _diskDriver.GetDirectoryContent(currentPath);
                var rawItem = rawContent.FirstOrDefault(item => _diskDriver.GetFormattedItemName(item) == formattedItemName);

                if (string.IsNullOrEmpty(rawItem))
                {
                    return;
                }

                if (rawItem[0] == 'D')
                {
                    if (_diskDriver.NavigateToFolder(formattedItemName))
                    {
                        try
                        {
                            var newPath = _diskDriver.GetCurrentPath();
                            CurrentPath = newPath;

                            var newContent = _diskDriver.GetDirectoryContent(newPath);
                            var formattedContent = newContent.Select(item => _diskDriver.GetFormattedItemName(item)).ToList();

                            // Добавляем навигационные кнопки в начало
                            var itemsWithNavButtons = new List<string> { ".", ".." };
                            itemsWithNavButtons.AddRange(formattedContent);

                            Items = new ObservableCollection<string>(itemsWithNavButtons);
                            SelectedItem = null;
                        }
                        catch (Exception ex)
                        {
                            _dialogService.ShowMessageBox($"Ошибка при загрузке содержимого папки: {ex.Message}");
                            _diskDriver.NavigateBack();
                        }
                    }
                    else
                    {
                        _dialogService.ShowMessageBox("Не удалось открыть папку");
                    }
                }
                else if (rawItem[0] == 'F')
                {
                    // Это файл - отправляем запрос на сервер
                    var filePath = CombineWindowsPath(currentPath, formattedItemName);
                    var fileContent = _diskDriver.GetFileContent(filePath);

                    if (!string.IsNullOrEmpty(fileContent))
                    {
                        ClientLog += $"Получен файл: {formattedItemName}\n";
                    }
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessageBox($"Ошибка при обработке элемента: {ex.Message}");
            }
        }

        /// <summary>
        /// Вернуться в родительскую папку
        /// </summary>
        public void NavigateBack()
        {
            if (!IsClientConnected || _diskDriver == null)
            {
                return;
            }

            try
            {
                if (_diskDriver.NavigateBack())
                {
                    var newPath = _diskDriver.GetCurrentPath();
                    CurrentPath = newPath;

                    var content = _diskDriver.GetDirectoryContent(newPath);
                    var formattedContent = content.Select(item => _diskDriver.GetFormattedItemName(item)).ToList();

                    // Добавляем навигационные кнопки в начало
                    var itemsWithNavButtons = new List<string> { ".", ".." };
                    itemsWithNavButtons.AddRange(formattedContent);

                    Items = new ObservableCollection<string>(itemsWithNavButtons);
                    SelectedItem = null;
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessageBox($"Ошибка при навигации: {ex.Message}");
            }
        }
    }
}
