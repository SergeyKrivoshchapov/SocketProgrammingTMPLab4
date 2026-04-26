using System;
using System.Collections.Generic;
using System.Text;

namespace Task1GUI.Models
{
    public interface IServerTransferModel
    {
        event EventHandler<string> UpdateLogs;

        bool StartServer();

        bool StopServer();

        bool TransferToClient();
    }

    public class ServerMockModel : IServerTransferModel
    {
        public event EventHandler<string> UpdateLogs;

        public bool StartServer()
        {
            UpdateLogs?.Invoke(this, "Сервер запущен");

            return true;
        }

        public bool StopServer()
        {
            UpdateLogs?.Invoke(this, "Сервер остановлен");

            return true;
        }

        public bool TransferToClient()
        {
            UpdateLogs?.Invoke(this, "Отправлен список логических устройств");

            return true;
        }
    }
}
