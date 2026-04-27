using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Task3Client.Common;
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

    public class UnitDispatcherModel : IUnitDispatcherModel
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void StatesCallback(IntPtr statesPtr);

        [DllImport(Constants.dllControllerName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ConnectToController")]
        private static extern int ConnectToController([MarshalAs(UnmanagedType.LPStr)] string address, IntPtr callback);

        [DllImport(Constants.dllControllerName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "DisconnectFromController")]
        private static extern void DisconnectFromController();

        [DllImport(Constants.dllControllerName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "GetConnectedUnitCount")]
        private static extern int GetConnectedUnitCount();

        public bool IsConnected { get; private set; } = false;

        public event Action<IReadOnlyList<UnitState>>? StatesReceived;

        private StatesCallback? _callbackRef;
        private IntPtr _callbackPtr = IntPtr.Zero;
        private int _connectedUnitCount;

        public int Connect(string address)
        {
            if (IsConnected)
            {
                return _connectedUnitCount > 0 ? _connectedUnitCount : GetConnectedUnitCount();
            }

            _callbackRef = OnNativeStates;
            _callbackPtr = Marshal.GetFunctionPointerForDelegate(_callbackRef);

            var count = ConnectToController(address, _callbackPtr);
            if (count <= 0)
            {
                _callbackPtr = IntPtr.Zero;
                _callbackRef = null;
                return count;
            }

            IsConnected = true;
            _connectedUnitCount = count;
            return count;
        }

        public void Disconnect()
        {
            if (!IsConnected)
            {
                return;
            }

            DisconnectFromController();
            IsConnected = false;
            _connectedUnitCount = 0;
            _callbackPtr = IntPtr.Zero;
            _callbackRef = null;
        }

        private void OnNativeStates(IntPtr statesPtr)
        {
            var statesLine = Marshal.PtrToStringUTF8(statesPtr) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(statesLine))
            {
                StatesReceived?.Invoke(Array.Empty<UnitState>());
                return;
            }

            var tokens = statesLine.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var states = new List<UnitState>(tokens.Length);

            foreach (var token in tokens)
            {
                if (!int.TryParse(token, out var value) || !Enum.IsDefined(typeof(UnitState), value))
                {
                    states.Add(UnitState.Maintenance);
                    continue;
                }

                states.Add((UnitState)value);
            }

            StatesReceived?.Invoke(states);
        }
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
