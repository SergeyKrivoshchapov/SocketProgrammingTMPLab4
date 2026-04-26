namespace Task3Controller
{
    class Controller
    {
        private string _ipAddress { get; }
        private string _confPath { get; }

        public Controller(string IpAddress, string confPath)
        {
            _ipAddress = IpAddress;
            _confPath = confPath;
        }

        public void Start()
        {
            Console.WriteLine("Запуск контроллера");

            Console.WriteLine("Остановка контроллера");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Controller controller = new Controller("127.0.0.1:8008", "");

            controller.Start();
        }
    }
}