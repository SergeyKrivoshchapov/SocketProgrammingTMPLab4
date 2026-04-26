using System;
using System.Collections.Generic;
using System.Text;

namespace Task1GUI.Models
{
    public interface IClientTransferModel
    {
        event EventHandler<string> UpdateLogs;

        bool ConnectToServer(string IpAddress);

        bool DisconnectToServer();

        bool TransferToServer(string path);
    }
    
    public class ClientMockModel : IClientTransferModel
    {
        public event EventHandler<string> UpdateLogs;

        public bool ConnectToServer(string IpAddress)
        {
            UpdateLogs?.Invoke(this, $"Подключен к серверу {IpAddress}");

            return true;
        }

        public bool DisconnectToServer()
        {
            UpdateLogs?.Invoke(this, "Отключен от сервера");

            return true;
        }

        public bool TransferToServer(string path)
        {
            UpdateLogs?.Invoke(this, "Отправлены данные серверу");

            return true;
        }
    }
}
