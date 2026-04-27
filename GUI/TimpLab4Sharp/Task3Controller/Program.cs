using System.Runtime.InteropServices;

namespace Task3Controller
{
    static class Constants
    {
        public const string dllControllerName = "task3_controller.dll";
    }

    class Controller
    {
        private string _ipAddress { get; }
        private string _confPath { get; }
        private DataCallback? _callbackRef;
        private IntPtr _callbackPtr = IntPtr.Zero;
        private bool _started;

        public Controller(string IpAddress, string confPath)
        {
            _ipAddress = IpAddress;
            _confPath = confPath;
        }

        public void Start()
        {
            Console.WriteLine("Запуск контроллера");

            if (_started)
            {
                return;
            }

            _callbackRef = OnNativeStates;
            _callbackPtr = Marshal.GetFunctionPointerForDelegate(_callbackRef);

            var rc = StartController(ParsePort(_ipAddress), _confPath, _callbackPtr);
            if (rc != 0)
            {
                Console.WriteLine("Ошибка запуска контроллера");
                return;
            }

            _started = true;
            Console.WriteLine("Контроллер запущен");
        }

        public void Stop()
        {
            if (!_started)
            {
                return;
            }

            StopController();
            _started = false;
            _callbackPtr = IntPtr.Zero;
            _callbackRef = null;

            Console.WriteLine("Остановка контроллера");
        }

        public int GetUnitsCount()
        {
            return GetUnitCount();
        }

        public bool IsRunning()
        {
            return IsControllerRunning() == 1;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void DataCallback([MarshalAs(UnmanagedType.LPStr)] string states);

        private void OnNativeStates(string states)
        {
            Console.WriteLine($"Состояния: {states}");
        }

        [DllImport(Constants.dllControllerName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StartController")]
        private static extern int StartController([MarshalAs(UnmanagedType.LPStr)] string port, [MarshalAs(UnmanagedType.LPStr)] string configPath, IntPtr callback);

        [DllImport(Constants.dllControllerName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopController")]
        private static extern void StopController();

        [DllImport(Constants.dllControllerName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "GetUnitCount")]
        private static extern int GetUnitCount();

        [DllImport(Constants.dllControllerName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "IsControllerRunning")]
        private static extern int IsControllerRunning();

        private static string ParsePort(string address)
        {
            var split = address.Split(':');
            return split.Length > 1 ? split[^1] : address;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Controller controller = new Controller("127.0.0.1:9000", "config.txt");

            controller.Start();
            Console.ReadLine();
            controller.Stop();
        }
    }
}