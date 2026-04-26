using System;
using System.Collections.Generic;
using System.Text;
using Task2Client.Samples;

namespace Task2Client.Models
{
    public interface IDispatcherModel
    {
        bool IsConnected { get; }

        event Action<DispatcherSample>? SampleReceived;

        int Connect(string address);

        void Disconnect();
    }

    public class DispatcherMockModel : IDispatcherModel
    {
        public bool IsConnected => true;

        public event Action<DispatcherSample>? SampleReceived;

        public int Connect(string address)
        {
            return 0;
        }

        public void Disconnect()
        {

        }
    }
}
