using System;
using System.Runtime.InteropServices;
using Task2Client.Common;
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

    public class DispatcherModel : IDispatcherModel
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void DataCallback(double temperature, double pressure);

        [DllImport(Constants.dllControllerName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ConnectToController")]
        private static extern int ConnectToController([MarshalAs(UnmanagedType.LPStr)] string address, IntPtr callback);

        [DllImport(Constants.dllControllerName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "DisconnectFromController")]
        private static extern void DisconnectFromController();

        private DataCallback? _callbackRef;
        private IntPtr _callbackPtr = IntPtr.Zero;
        private bool _connected;

        public bool IsConnected => _connected;

        public event Action<DispatcherSample>? SampleReceived;

        public int Connect(string address)
        {
            if (_connected)
            {
                return 0;
            }

            _callbackRef = OnNativeData;
            _callbackPtr = Marshal.GetFunctionPointerForDelegate(_callbackRef);

            var rc = ConnectToController(address, _callbackPtr);
            if (rc != 0)
            {
                _callbackPtr = IntPtr.Zero;
                _callbackRef = null;
                return rc;
            }

            _connected = true;
            return 0;
        }

        public void Disconnect()
        {
            if (!_connected)
            {
                return;
            }

            DisconnectFromController();
            _connected = false;
            _callbackPtr = IntPtr.Zero;
            _callbackRef = null;

        }

        private void OnNativeData(double temperature, double pressure)
        {
            SampleReceived?.Invoke(new DispatcherSample(DateTime.Now, temperature, pressure));
        }
    }
}
