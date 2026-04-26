using MVVMClassLibrary.Commands;
using MVVMClassLibrary.Services;
using MVVMClassLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Task2Client.Models;
using Task2Client.Samples;

namespace Task2Client.ViewModels
{
    public interface IMainWindowVM
    {
        event Action? SamplesChanged;
        ICommand ConnectCommand { get; }
        ICommand DisconnectCommand { get; }
        ICommand ClearCommand { get; }
        ICommand ExitCommand { get; }
        string ControllerAddress { get; set; }
        string Status { get; }
        string Log { get; }
        bool IsConnected { get; }

        IReadOnlyList<double> GetTimeAxisSeconds();

        IReadOnlyList<double> GetTemperatureValues();

        IReadOnlyList<double> GetPressureValues();
    }

    public class MainWindowVM : BaseViewModel, IMainWindowVM
    {
        private readonly IDialogService _dialogService;

        private readonly IDispatcherModel _dispatcherModel;

        private readonly List<DispatcherSample> _samples = new();

        private readonly RelayCommand _connectCommand;
        private readonly RelayCommand _disconnectCommand;
        private readonly RelayCommand _clearCommand;
        private readonly RelayCommand _exitCommand;

        private string _controllerAddress = "127.0.0.1:8080";
        private string _status = "Не подключено.";
        private string _log = "Ожидание подключения к контроллеру.";
        private bool _isConnected;

        public MainWindowVM(IDispatcherModel dispatcherModel, IDialogService dialogService)
        {
            _dialogService = dialogService;

            _dispatcherModel = dispatcherModel ?? throw new ArgumentNullException(nameof(dispatcherModel));
            _dispatcherModel.SampleReceived += OnSampleReceived;

            _connectCommand = new RelayCommand(_ => Connect(), _ => CanConnect());
            _disconnectCommand = new RelayCommand(_ => Disconnect(), _ => CanDisconnect());
            _clearCommand = new RelayCommand(_ => ClearData());
            _exitCommand = new RelayCommand(_ => _dialogService.CloseWindow(this));
        }

        public event Action? SamplesChanged;

        public ICommand ConnectCommand => _connectCommand;
        public ICommand DisconnectCommand => _disconnectCommand;
        public ICommand ClearCommand => _clearCommand;
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

        public IReadOnlyList<double> GetTimeAxisSeconds()
        {
            if (_samples.Count == 0)
            {
                return Array.Empty<double>();
            }

            var start = _samples[0].Time;
            return _samples.Select(s => (s.Time - start).TotalSeconds).ToArray();
        }

        public IReadOnlyList<double> GetTemperatureValues()
        {
            return _samples.Select(s => s.Temperature).ToArray();
        }

        public IReadOnlyList<double> GetPressureValues()
        {
            return _samples.Select(s => s.Pressure).ToArray();
        }

        private void Connect()
        {
            try
            {
                var code = _dispatcherModel.Connect(ControllerAddress);
                if (code == 0)
                {
                    IsConnected = true;
                    Status = "Подключено.";
                    AppendLog($"Подключение к контроллеру {ControllerAddress} установлено.");
                    return;
                }

                AppendLog($"Не удалось подключиться к контроллеру (код {code}).");
                Status = "Ошибка подключения.";
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

        private void ClearData()
        {
            _samples.Clear();
            SamplesChanged?.Invoke();
            AppendLog("Графики очищены.");
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
            _clearCommand.RaiseCanExecuteChanged();
            _exitCommand.RaiseCanExecuteChanged();
        }

        private void AppendLog(string message)
        {
            Log = string.IsNullOrWhiteSpace(Log)
                ? message
                : $"{Log}{Environment.NewLine}{message}";
        }

        private void OnSampleReceived(DispatcherSample sample)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _samples.Add(sample);
                SamplesChanged?.Invoke();
            });
        }
    }
}
