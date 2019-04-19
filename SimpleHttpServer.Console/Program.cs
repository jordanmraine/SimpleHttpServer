using SimpleHttpServer.Core;

namespace SimpleHttpServer.Console
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Server.Start();
            System.Console.ReadLine();
        }
    }
}
