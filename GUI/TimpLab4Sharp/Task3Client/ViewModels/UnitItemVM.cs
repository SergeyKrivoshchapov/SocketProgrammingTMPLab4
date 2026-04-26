using MVVMClassLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Task3Client.ViewModels
{
    public enum UnitState
    {
        Working = 0,
        Emergency = 1,
        Maintenance = 2
    }

    public sealed class UnitItemViewModel : BaseViewModel
    {
        private UnitState _state;

        public UnitItemViewModel(int number, UnitState initialState)
        {
            Number = number;
            _state = initialState;
        }

        public int Number { get; }

        public UnitState State
        {
            get => _state;
            set
            {
                if (Set(ref _state, value))
                {
                    OnPropertyChanged(nameof(StateBrush));
                }
            }
        }

        public Brush StateBrush => State switch
        {
            UnitState.Working => Brushes.LimeGreen,
            UnitState.Emergency => Brushes.IndianRed,
            UnitState.Maintenance => Brushes.Gray,
            _ => Brushes.Gray
        };
    }
}
