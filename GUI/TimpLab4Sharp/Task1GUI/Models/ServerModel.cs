using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Task1GUI.Common;

namespace Task1GUI.Models
{
    public interface IServerTransferModel
    {
        event EventHandler<string> UpdateLogs;

        bool StartServer();

        bool StopServer();

        (bool status, string drives) TransferToClient();
    }

    //public class ServerMockModel : IServerTransferModel
    //{
    //    public event EventHandler<string> UpdateLogs;

    //    public bool StartServer()
    //    {
    //        UpdateLogs?.Invoke(this, "Сервер запущен");

    //        return true;
    //    }

    //    public bool StopServer()
    //    {
    //        UpdateLogs?.Invoke(this, "Сервер остановлен");

    //        return true;
    //    }

    //    public bool TransferToClient()
    //    {
    //        UpdateLogs?.Invoke(this, "Отправлен список логических устройств");

    //        return true;
    //    }
    //}

    public class ServerModel : IServerTransferModel
    {
        public event EventHandler<string>? UpdateLogs;

        // C: typedef void (*DataCallback)(char* msg);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void DataCallback(IntPtr msg);

        [DllImport(Constants.dllTask1ServerName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StartServer", CharSet = CharSet.Ansi)]
        private static extern int StartServ([MarshalAs(UnmanagedType.LPStr)] string port, IntPtr callback);

        [DllImport(Constants.dllTask1ServerName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopServer")]
        private static extern void StopServ();

        // держим, чтобы GC не собрал делегат
        private DataCallback? _callbackRef;
        private IntPtr _callbackPtr = IntPtr.Zero;
        private bool _started;

        public bool StartServer()
        {
            if (_started) return true;

            _callbackRef = OnNativeLog;
            _callbackPtr = Marshal.GetFunctionPointerForDelegate(_callbackRef!);

            var rc = StartServ("9000", _callbackPtr);
            if (rc != 0)
            {
                return false;
            }

            _started = true;
            return true;
        }

        public bool StopServer()
        {
            if (!_started) return true;

            StopServ();
            _started = false;

            _callbackPtr = IntPtr.Zero;
            _callbackRef = null;

            return true;
        }

        public (bool status, string drives) TransferToClient()
        {
            if (!_started)
            {
                return (false, string.Empty);
            }

            var drives = string.Join(",", DriveInfo.GetDrives()
                .Select(d => d.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.EndsWith("\\") ? name.Substring(0, name.Length - 1) : name));

            UpdateLogs?.Invoke(this, $"Отправлен список логических устройств клиенту: {drives}");

            return (true, drives);

        }

        private void OnNativeLog(IntPtr msgPtr)
        {
            var msg = Marshal.PtrToStringUTF8(msgPtr) ?? string.Empty;
            UpdateLogs?.Invoke(this, msg);
        }
    }
}
