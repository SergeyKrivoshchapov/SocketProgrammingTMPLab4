using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Task1GUI.ViewModels;

namespace Task1GUI.Views
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string IpMask = "___.___.___.___";
        private IMainWindowVM _viewModel;
        private bool _isUpdatingIpText;

        public MainWindow(IMainWindowVM viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;

            DataContext = _viewModel;

            IpAddressTextBox.Text = IpMask;
            IpAddressTextBox.CaretIndex = 0;
        }

        private void IpAddressTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingIpText || sender is not TextBox textBox)
            {
                return;
            }

            var oldCaret = textBox.CaretIndex;
            var normalized = NormalizeIpText(textBox.Text);
            if (textBox.Text == normalized)
            {
                return;
            }

            _isUpdatingIpText = true;
            textBox.Text = normalized;
            textBox.CaretIndex = GetNextCaretIndex(Math.Min(oldCaret, IpMask.Length));
            _isUpdatingIpText = false;
        }

        private void IpAddressTextBox_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is not TextBox textBox)
            {
                return;
            }

            if (string.IsNullOrEmpty(e.Text) || !char.IsDigit(e.Text[0]))
            {
                e.Handled = true;
                return;
            }

            var index = GetNextEditableIndex(textBox.CaretIndex);
            if (index < 0)
            {
                e.Handled = true;
                return;
            }

            var chars = textBox.Text.PadRight(IpMask.Length, '_').ToCharArray();
            chars[index] = e.Text[0];

            _isUpdatingIpText = true;
            textBox.Text = new string(chars);
            textBox.CaretIndex = GetNextCaretIndex(index + 1);
            _isUpdatingIpText = false;
            e.Handled = true;
        }

        private void IpAddressTextBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox textBox)
            {
                return;
            }

            if (e.Key == Key.Back)
            {
                var index = GetPreviousEditableIndex(textBox.CaretIndex - 1);
                if (index >= 0)
                {
                    var chars = textBox.Text.PadRight(IpMask.Length, '_').ToCharArray();
                    chars[index] = '_';
                    _isUpdatingIpText = true;
                    textBox.Text = new string(chars);
                    textBox.CaretIndex = index;
                    _isUpdatingIpText = false;
                }

                e.Handled = true;
                return;
            }

            if (e.Key == Key.Delete)
            {
                var index = GetNextEditableIndex(textBox.CaretIndex);
                if (index >= 0)
                {
                    var chars = textBox.Text.PadRight(IpMask.Length, '_').ToCharArray();
                    chars[index] = '_';
                    _isUpdatingIpText = true;
                    textBox.Text = new string(chars);
                    textBox.CaretIndex = index;
                    _isUpdatingIpText = false;
                }

                e.Handled = true;
                return;
            }

            if (e.Key == Key.Left)
            {
                textBox.CaretIndex = GetPreviousEditableIndex(textBox.CaretIndex - 1) switch
                {
                    >= 0 and var idx => idx,
                    _ => 0
                };
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Right)
            {
                textBox.CaretIndex = GetNextCaretIndex(textBox.CaretIndex + 1);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Space || e.Key == Key.OemPeriod || e.Key == Key.Decimal)
            {
                e.Handled = true;
            }
        }

        private static bool IsEditableIndex(int index)
        {
            return index >= 0 && index < IpMask.Length && IpMask[index] == '_';
        }

        private static int GetNextEditableIndex(int from)
        {
            for (var i = Math.Max(0, from); i < IpMask.Length; i++)
            {
                if (IsEditableIndex(i))
                {
                    return i;
                }
            }

            return -1;
        }

        private static int GetPreviousEditableIndex(int from)
        {
            for (var i = Math.Min(IpMask.Length - 1, from); i >= 0; i--)
            {
                if (IsEditableIndex(i))
                {
                    return i;
                }
            }

            return -1;
        }

        private static int GetNextCaretIndex(int from)
        {
            var next = GetNextEditableIndex(from);
            return next >= 0 ? next : IpMask.Length;
        }

        private static string NormalizeIpText(string? text)
        {
            var chars = IpMask.ToCharArray();
            if (string.IsNullOrEmpty(text))
            {
                return new string(chars);
            }

            var source = text.Length > IpMask.Length ? text[..IpMask.Length] : text;
            for (var i = 0; i < source.Length; i++)
            {
                if (IpMask[i] == '_' && char.IsDigit(source[i]))
                {
                    chars[i] = source[i];
                }
            }

            return new string(chars);
        }

        private void ListBox_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is not ListBox listBox)
            {
                return;
            }

            var selectedItem = listBox.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedItem))
            {
                return;
            }

            _viewModel?.ItemDoubleClick(selectedItem);
        }

        private void DrivesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // При выборе "..." закрываем выпадающий список и сбрасываем выбор
            if (sender is ComboBox comboBox && comboBox.SelectedItem as string == "...")
            {
                comboBox.IsDropDownOpen = false;
                // Задержка для корректного обновления UI
                Dispatcher.InvokeAsync(() => 
                {
                    comboBox.Focus();
                }, System.Windows.Threading.DispatcherPriority.Render);
            }
        }
    }
}
