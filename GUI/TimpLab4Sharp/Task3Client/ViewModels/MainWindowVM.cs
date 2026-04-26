using MVVMClassLibrary.Commands;
using MVVMClassLibrary.Services;
using MVVMClassLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Task3Client.Models;

namespace Task3Client.ViewModels
{
    public interface IMainWindowVM
    {
        public ObservableCollection<UnitItemViewModel> Units { get; }

        ICommand ConnectCommand { get; }

        ICommand DisconnectCommand { get; }

        ICommand ExitCommand { get; }

        string ControllerAddress { get; }

        string Status { get; }

        string Log { get; }

        bool IsConnected { get; }
    }

    public class MainWindowVM : BaseViewModel, IMainWindowVM
    {
        private readonly IDialogService _dialogService;

        private readonly IUnitDispatcherModel _dispatcherModel;

        private readonly RelayCommand _connectCommand;
        private readonly RelayCommand _disconnectCommand;
        private readonly RelayCommand _exitCommand;

        private string _controllerAddress = "127.0.0.1:8080";
        private string _status = "Не подключено.";
        private string _log = "Ожидание подключения к контроллеру.";
        private bool _isConnected;

        public MainWindowVM(IDialogService dialogService, IUnitDispatcherModel dispatcherModel)
        {
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            _dispatcherModel = dispatcherModel ?? throw new ArgumentNullException(nameof(dispatcherModel));
            _dispatcherModel.StatesReceived += OnStatesReceived;

            Units = new ObservableCollection<UnitItemViewModel>();

            _connectCommand = new RelayCommand(_ => Connect(), _ => CanConnect());
            _disconnectCommand = new RelayCommand(_ => Disconnect(), _ => CanDisconnect());
            _exitCommand = new RelayCommand(_ => dialogService.CloseWindow(this));
        }

        public ObservableCollection<UnitItemViewModel> Units { get; }

        public ICommand ConnectCommand => _connectCommand;

        public ICommand DisconnectCommand => _disconnectCommand;

        public ICommand ExitCommand => _exitCommand;

        public string ControllerAddress
        {
            get => _controllerAddress;
            set
            {
                if (Set(ref _controllerAddress, value))
                {
                    RefreshCommands();
                }
            }
        }

        public string Status
        {
            get => _status;
            private set => Set(ref _status, value);
        }

        public string Log
        {
            get => _log;
            private set => Set(ref _log, value);
        }

        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (Set(ref _isConnected, value))
                {
                    RefreshCommands();
                }
            }
        }

        private void Connect()
        {
            try
            {
                var count = _dispatcherModel.Connect(ControllerAddress);
                if (count > 0)
                {
                    IsConnected = true;
                    Status = "Подключено.";
                    CreateUnits(count);
                    AppendLog($"Подключение установлено. Количество установок: {count}.");
                    return;
                }

                Status = "Ошибка подключения.";
                AppendLog($"Не удалось подключиться к контроллеру (код {count}).");
            }
            catch (Exception ex)
            {
                Status = "Ошибка подключения.";
                AppendLog($"Ошибка подключения: {ex.Message}");
            }
        }

        private void Disconnect()
        {
            _dispatcherModel.Disconnect();
            IsConnected = false;
            Status = "Не подключено.";
            AppendLog("Соединение с контроллером закрыто.");
        }

        private bool CanConnect()
        {
            return !IsConnected && !string.IsNullOrWhiteSpace(ControllerAddress);
        }

        private bool CanDisconnect()
        {
            return IsConnected;
        }

        private void RefreshCommands()
        {
            _connectCommand.RaiseCanExecuteChanged();
            _disconnectCommand.RaiseCanExecuteChanged();
            _exitCommand.RaiseCanExecuteChanged();
        }


        private void AppendLog(string message)
        {
            Log = string.IsNullOrWhiteSpace(Log)
                ? message
                : $"{Log}{Environment.NewLine}{message}";
        }

        private void CreateUnits(int count)
        {
            Units.Clear();
            for (var i = 1; i <= count; i++)
            {
                Units.Add(new UnitItemViewModel(i, UnitState.Working));
            }
        }

        private void OnStatesReceived(IReadOnlyList<UnitState> states)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var limit = Math.Min(states.Count, Units.Count);
                for (var i = 0; i < limit; i++)
                {
                    Units[i].State = states[i];
                }
            });
        }
    }
}
