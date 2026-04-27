using System.Runtime.InteropServices;

namespace Task2Controller
{
    static class Constants
    {
        public const string dllControllerName = "task2_controller.dll";
    }

    class Controller
    {
        private string _ipAddress { get; }
        private DataCallback? _callbackRef;
        private IntPtr _callbackPtr = IntPtr.Zero;
        private bool _connected;

        public Controller(string IpAddress)
        {
            _ipAddress = IpAddress;
        }

        public void Start()
        {
            Console.WriteLine("Запуск контроллера");

            if (_connected)
            {
                return;
            }

            _callbackRef = OnNativeData;
            _callbackPtr = Marshal.GetFunctionPointerForDelegate(_callbackRef);

            var rc = StartController(_ipAddress.Split(":")[1], _callbackPtr);
            if (rc != 0)
            {
                Console.WriteLine("Ошибка подключения к контроллеру");
                return;
            }

            _connected = true;
            Console.WriteLine("Контроллер подключен");
        }

        public void Stop()
        {
            if (!_connected)
            {
                return;
            }

            StopController();
            _connected = false;
            _callbackPtr = IntPtr.Zero;
            _callbackRef = null;

            Console.WriteLine("Остановка контроллера");
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void DataCallback(double temperature, double pressure);

        private void OnNativeData(double temperature, double pressure)
        {
            Console.WriteLine($"Данные: T={temperature:F2}, P={pressure:F2}");
        }

        [DllImport(Constants.dllControllerName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StartController")]
        private static extern int StartController([MarshalAs(UnmanagedType.LPStr)] string port, IntPtr callback);

        [DllImport(Constants.dllControllerName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "StopController")]
        private static extern void StopController();
    }

    class Program
    {
        static void Main(string[] args)
        {
            Controller controller = new Controller("127.0.0.1:9000");

            controller.Start();
            Console.ReadLine();
            controller.Stop();
        }
    }
}