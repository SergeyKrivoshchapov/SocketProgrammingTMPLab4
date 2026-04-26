using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace MVVMClassLibrary.Services
{
    public interface IDialogService
    {
        void Register<TViewModel>(Func<TViewModel, Window> windowFactory) where TViewModel : class;
        void RegisterWindow(object viewModel, Window window);

        bool? ShowDialog(object viewModel);
        bool? ShowDialog<TViewModel>() where TViewModel : class, new();
        void ShowWindow(object viewModel);
        void ShowWindow<TViewModel>() where TViewModel : class, new();

        void CloseWindow(object viewModel);

        void ShowMessageBox(string message);
    }

    public class DialogService : IDialogService
    {
        private readonly Dictionary<object, Window> _openedWindows = new Dictionary<object, Window>();
        private readonly Dictionary<Type, Func<object, Window>> _windowFactories = new Dictionary<Type, Func<object, Window>>();

        public void Register<TViewModel>(Func<TViewModel, Window> windowFactory) where TViewModel : class
        {
            _windowFactories[typeof(TViewModel)] = vm => windowFactory((TViewModel)vm);
        }

        public void RegisterWindow(object viewModel, Window window)
        {
            TrackOpenedWindow(viewModel, window);
        }

        public bool? ShowDialog(object viewModel)
        {
            Window window = GetOrCreateWindow(viewModel);
            if (window == null)
            {
                return false;
            }

            return window.ShowDialog();
        }

        public bool? ShowDialog<TViewModel>() where TViewModel : class, new()
        {
            return ShowDialog(new TViewModel());
        }

        public void ShowWindow(object viewModel)
        {
            Window window = GetOrCreateWindow(viewModel);
            window?.Show();
        }

        public void ShowWindow<TViewModel>() where TViewModel : class, new()
        {
            ShowWindow(new TViewModel());
        }

        public void CloseWindow(object viewModel)
        {
            if (_openedWindows.TryGetValue(viewModel, out Window window))
            {
                window.Close();
                _openedWindows.Remove(viewModel);
            }
        }

        private Window GetOrCreateWindow(object viewModel)
        {
            if (_openedWindows.TryGetValue(viewModel, out Window existedWindow))
            {
                return existedWindow;
            }

            Type viewModelType = viewModel.GetType();
            if (_windowFactories.TryGetValue(viewModelType, out Func<object, Window> factory))
            {
                Window createdWindow = factory(viewModel);
                if (createdWindow.DataContext == null)
                {
                    createdWindow.DataContext = viewModel;
                }

                TrackOpenedWindow(viewModel, createdWindow);
                return createdWindow;
            }

            return null;
        }

        private void TrackOpenedWindow(object viewModel, Window window)
        {
            _openedWindows[viewModel] = window;
            window.Closed += (_, __) => _openedWindows.Remove(viewModel);
        }

        public void ShowMessageBox(string message)
        {
            MessageBox.Show(message);
        }
    }
}
