using System;
using System.Collections.Generic;
using System.Text;
using Task3Client.ViewModels;

namespace Task3Client.Models
{
    public interface IUnitDispatcherModel
    {
        bool IsConnected { get; }

        event Action<IReadOnlyList<UnitState>>? StatesReceived;

        int Connect(string address);

        void Disconnect();
    }

    public class UnitDispatcherMockModel : IUnitDispatcherModel
    {
        public bool IsConnected { get; private set; } = false;

        public event Action<IReadOnlyList<UnitState>>? StatesReceived;

        public int Connect(string address)
        {
            IsConnected = true;
            return 10;
        }

        public void Disconnect()
        {
            IsConnected = false;
        }
    }
}
