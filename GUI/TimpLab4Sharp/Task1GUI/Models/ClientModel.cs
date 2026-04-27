using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Task1GUI.Common;

namespace Task1GUI.Models
{
    public interface IClientTransferModel
    {
        event EventHandler<string> UpdateLogs;

        (bool, List<string>) ConnectToServer(string IpAddress);

        bool DisconnectToServer();

        bool TransferToServer(string path);
    }
    
    //public class ClientMockModel : IClientTransferModel
    //{
    //    public event EventHandler<string> UpdateLogs;

    //    public (bool, string) ConnectToServer(string IpAddress)
    //    {
    //        UpdateLogs?.Invoke(this, $"Подключен к серверу {IpAddress}");

    //        return (true, "");
    //    }

    //    public bool DisconnectToServer()
    //    {
    //        UpdateLogs?.Invoke(this, "Отключен от сервера");

    //        return true;
    //    }

    //    public bool TransferToServer(string path)
    //    {
    //        UpdateLogs?.Invoke(this, "Отправлены данные серверу");

    //        return true;
    //    }
    //}

    public class ClientModel : IClientTransferModel
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct Reply
        {
            public int status;
            public IntPtr msg;
        }

        public event EventHandler<string> UpdateLogs;

        public (bool, List<string>) ConnectToServer(string IpAddress)
        {
            IntPtr replyPtr = Connect(IpAddress + ":9000");
            if (replyPtr == IntPtr.Zero)
            {
                return (false, []);
            }

            try
            {
                Reply reply = Marshal.PtrToStructure<Reply>(replyPtr);

                bool status = reply.status == 0;
                string raw = Marshal.PtrToStringUTF8(reply.msg) ?? string.Empty;
                string drives = raw.StartsWith("DRIVES:") ? raw.Substring("DRIVES:".Length) : raw;
                List<string> drivesList = string.IsNullOrWhiteSpace(drives)
                    ? []
                    : drives.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => x.Trim())
                            .Where(x => x.Length > 0)
                            .ToList();

                return (status, drivesList);
            }
            finally
            {
                FreeReply(replyPtr);
            }
        }

        public bool DisconnectToServer()
        {
            Disconnect();

            return true;
        }

        public bool TransferToServer(string path)
        {
            UpdateLogs?.Invoke(this, "Отправлены данные серверу");

            return true;
        }
        
        [DllImport(Constants.dllTask1ClientName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ConnectToServer")]
        private static extern IntPtr Connect([MarshalAs(UnmanagedType.LPUTF8Str)] string address);

        [DllImport(Constants.dllTask1ClientName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "DisconnectFromServer")]
        private static extern void Disconnect();

        [DllImport(Constants.dllTask1ClientName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "FreeReply")]
        private static extern void FreeReply(IntPtr reply);
    }
}
