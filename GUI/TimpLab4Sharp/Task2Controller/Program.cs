namespace Task2Controller
{
    class Controller
    {
        private string _ipAddress { get; }

        public Controller(string IpAddress)
        {
            _ipAddress = IpAddress;
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
            Controller controller = new Controller("127.0.0.1:8008");

            controller.Start();
        }
    }
}