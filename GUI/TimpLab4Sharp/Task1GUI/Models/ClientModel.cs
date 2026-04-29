using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using Task1GUI.Common;

namespace Task1GUI.Models
{
    public interface IClientTransferModel
    {
        event EventHandler<string>? UpdateLogs;
        (bool, List<string>) ConnectToServer(string ipAddress);
        bool DisconnectToServer();
    }

    public class ClientModel : IClientTransferModel
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct Reply
        {
            public int status;
            public IntPtr msg;
        }

        public event EventHandler<string>? UpdateLogs;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void DataCallback(IntPtr msg);

        [DllImport(Constants.dllTask1ClientName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ConnectToServer")]
        private static extern IntPtr Connect([MarshalAs(UnmanagedType.LPUTF8Str)] string address, IntPtr callback);

        [DllImport(Constants.dllTask1ClientName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "DisconnectFromServer")]
        private static extern void Disconnect();

        [DllImport(Constants.dllTask1ClientName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "FreeReply")]
        private static extern void FreeReply(IntPtr reply);

        private DataCallback? _callbackRef;
        private IntPtr _callbackPtr = IntPtr.Zero;

        public (bool, List<string>) ConnectToServer(string ipAddress)
        {
            _callbackRef = OnNativeLog;
            _callbackPtr = Marshal.GetFunctionPointerForDelegate(_callbackRef!);

            IntPtr replyPtr = Connect(ipAddress + ":9000", _callbackPtr);
            if (replyPtr == IntPtr.Zero)
            {
                UpdateLogs?.Invoke(this, "Ошибка подключения");
                return (false, new List<string>());
            }

            try
            {
                Reply reply = Marshal.PtrToStructure<Reply>(replyPtr);
                bool status = reply.status == 0;

                if (!status)
                {
                    throw new Exception("Ошибка подключения");
                }

                string raw = Marshal.PtrToStringUTF8(reply.msg) ?? string.Empty;
                string drives = raw.StartsWith("DRIVES:") ? raw.Substring("DRIVES:".Length) : raw;
                List<string> drivesList = string.IsNullOrWhiteSpace(drives)
                    ? new List<string>()
                    : drives.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => x.Trim())
                            .Where(x => x.Length > 0)
                            .ToList();

                return (status, drivesList);
            }
            catch (Exception) { }
            finally
            {
                FreeReply(replyPtr);
            }

            UpdateLogs?.Invoke(this, "Ошибка подключения");
            return (false, new List<string>());
        }

        public bool DisconnectToServer()
        {
            Disconnect();
            _callbackPtr = IntPtr.Zero;
            _callbackRef = null;
            return true;
        }

        private void OnNativeLog(IntPtr msgPtr)
        {
            var msg = Marshal.PtrToStringUTF8(msgPtr) ?? string.Empty;
            UpdateLogs?.Invoke(this, msg);
        }
    }
}
