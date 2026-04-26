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
using Task2Client.ViewModels;

namespace Task2Client.Views
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IMainWindowVM _viewModel;
    
        public MainWindow(IMainWindowVM viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;
            DataContext = _viewModel;

            ConfigurePlots();

            _viewModel.SamplesChanged += RedrawPlots;
        }

        private void ConfigurePlots()
        {
            TemperaturePlot.Plot.Title("Температура");
            TemperaturePlot.Plot.XLabel("Время, с");
            TemperaturePlot.Plot.YLabel("Температура, °C");

            PressurePlot.Plot.Title("Давление");
            PressurePlot.Plot.XLabel("Время, с");
            PressurePlot.Plot.YLabel("Давление, атм");

            RedrawPlots();
        }

        private void RedrawPlots()
        {
            var x = _viewModel.GetTimeAxisSeconds();
            var temperature = _viewModel.GetTemperatureValues();
            var pressure = _viewModel.GetPressureValues();

            TemperaturePlot.Plot.Clear();
            PressurePlot.Plot.Clear();

            if (x.Count > 0 && temperature.Count > 0)
            {
                TemperaturePlot.Plot.AddScatter(x.ToArray(), temperature.ToArray(), color: System.Drawing.Color.OrangeRed, markerSize: 3);
            }

            if (x.Count > 0 && pressure.Count > 0)
            {
                PressurePlot.Plot.AddScatter(x.ToArray(), pressure.ToArray(), color: System.Drawing.Color.RoyalBlue, markerSize: 3);
            }

            TemperaturePlot.Plot.Title("Температура");
            TemperaturePlot.Plot.XLabel("Время, с");
            TemperaturePlot.Plot.YLabel("Температура, °C");

            PressurePlot.Plot.Title("Давление");
            PressurePlot.Plot.XLabel("Время, с");
            PressurePlot.Plot.YLabel("Давление, атм");

            TemperaturePlot.Refresh();
            PressurePlot.Refresh();
        }
    }
}
