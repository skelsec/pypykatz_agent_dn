using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace pypyagent
{
    enum PYPYCMDType {
        INIT = 0,
        FIND = 1,
        READ = 2,
        ERR = 3,
        OK = 4,
        END = 5
    };

    class PYPYCMD
    {
        public PYPYCMDType cmdtype;
        public List<byte[]> parameters = new List<byte[]>();

        static public PYPYCMD from_bytes(byte[] data)
        {
            int t = data[0];
            PYPYCMD cmd = new PYPYCMD();
            cmd.cmdtype = (PYPYCMDType)t;
            int plen = data[1];
            uint pos = 2;
            for(int i=0; i< plen; i++)
            {
                Byte[] vlen_b = new byte[4];
                Array.Copy(data, pos, vlen_b, 0, 4);
                Array.Reverse(vlen_b);
                uint vlen = BitConverter.ToUInt32(vlen_b, 0);

                Byte[] pdata = new byte[vlen];
                Array.Copy(data, pos + 4, pdata, 0, vlen);
                cmd.parameters.Add(pdata);
                pos += (uint)(vlen+4);
            }

            return cmd;
        }

        public byte[] to_bytes()
        {
            List<byte> buff = new List<byte>();
            buff.Add((byte)this.cmdtype);
            buff.Add((byte)this.parameters.Count);
            foreach(byte[] p in this.parameters)
            {
                byte[] len = BitConverter.GetBytes((uint)p.Length);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(len);
                foreach (byte b in len)
                    buff.Add(b);
                foreach(byte b in p)
                    buff.Add(b);
            }
            return buff.ToArray();
        }

        public override string ToString()
        {
            int i = 0;
            String t = "";
            t += "=== PYPYCMD ==\r\n";
            t += "cmdtype: " + this.cmdtype.ToString() + "\r\n";
            foreach (byte[] p in this.parameters)
            {
                t += "param " + i.ToString() + " : " + p.ToString();
            }

            return t;
        }
    }
}
