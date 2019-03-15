using System;
using System.Text;

namespace pypyagent
{
    class PYPYagent
    {
        bool useConsole;
        PYPYreader reader;
        PYPYReverseSocketTransport transport;

        public PYPYagent(PYPYreader reader, PYPYReverseSocketTransport transport, bool useConsole = false)
        {
            this.reader = reader;
            this.transport = transport;
            this.useConsole = useConsole;
        }
        public void run()
        {
            try
            {
                if (useConsole)
                    Console.WriteLine("Trying to connect to server");
                transport.connect();
            }
            catch (Exception e)
            {
                if (useConsole)
                    Console.WriteLine("Failed to connect to server! Reason: " + e.ToString());
            }

            bool good = true;
            while (good)
            {
                PYPYCMD cmd = transport.readCMD();
                
                if (cmd.cmdtype == PYPYCMDType.END)
                {
                    if(this.useConsole)
                    {
                        string result;
                        if (cmd.parameters.Count > 0)
                        {
                            result = System.Text.Encoding.UTF8.GetString(cmd.parameters[0]);
                        }
                        else
                            result = "Server did not provide result data, check server for results!";
                        
                        Console.Write(result);
                    }
                    break;
                }
                    

                switch (cmd.cmdtype)
                {
                    case PYPYCMDType.INIT:
                        {
                            string data = "";
                            try
                            {
                                data = reader.get_info();
                                byte[] bytes = Encoding.Default.GetBytes(data);
                                transport.sendOK(bytes);

                            }
                            catch (Exception e)
                            {
                                data = "INIT Failed! Reason: " + e.ToString();
                                if (useConsole)
                                    Console.WriteLine(data);
                                byte[] bytes = Encoding.Default.GetBytes(data);
                                transport.sendERR(bytes);
                                good = false;
                            }

                            break;
                        }
                    case PYPYCMDType.FIND:
                        {
                            try
                            {
                                string module = System.Text.Encoding.UTF8.GetString(cmd.parameters[0]);
                                ulong pos = reader.find(module, cmd.parameters[1]);
                                byte[] data = BitConverter.GetBytes(pos);
                                if (BitConverter.IsLittleEndian)
                                    Array.Reverse(data);

                                transport.sendOK(data);
                            }
                            catch (Exception e)
                            {
                                string data = "FIND Failed! Reason: " + e.ToString();
                                if (useConsole)
                                    Console.WriteLine(data);
                                byte[] bytes = Encoding.Default.GetBytes(data);
                                transport.sendERR(bytes);
                                good = false;
                            }
                            break;
                        }
                    case PYPYCMDType.READ:
                        {
                            try
                            {
                                if (BitConverter.IsLittleEndian)
                                {
                                    Array.Reverse(cmd.parameters[0]);
                                    Array.Reverse(cmd.parameters[1]);
                                }

                                ulong pos = (ulong)BitConverter.ToUInt64(cmd.parameters[0], 0);
                                ulong length = (ulong)BitConverter.ToUInt64(cmd.parameters[1], 0);
                                byte[] data = reader.read(pos, length);
                                transport.sendOK(data);
                            }
                            catch (Exception e)
                            {
                                string data = "READ Failed! Reason: " + e.ToString();
                                if (useConsole)
                                    Console.WriteLine(data);
                                byte[] bytes = Encoding.Default.GetBytes(data);
                                transport.sendERR(bytes);
                                good = false;
                            }
                            break;
                        }
                    default:
                        {
                            if (useConsole)
                                Console.WriteLine("Unknown command!");
                            break;
                        }
                }
            }
            if (useConsole)
            {
                Console.WriteLine("Finished!");
            }
        }
    }
    
}
