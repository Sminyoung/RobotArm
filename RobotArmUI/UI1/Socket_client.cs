using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RobotArmUI
{
    public partial class Socket_client
    {
        static public void Sendsock(string msg)
        {
            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                client.Connect(new IPEndPoint(IPAddress.Parse("192.168.0.13"), 9999));

                var data = Encoding.UTF8.GetBytes(msg);

                client.Send(BitConverter.GetBytes(data.Length));
                client.Send(data);

                data = new byte[4];
                client.Receive(data, data.Length, SocketFlags.None);

                Array.Reverse(data);
                data = new Byte[BitConverter.ToInt32(data, 0)];

                client.Receive(data, data.Length, SocketFlags.None);
            }
        }
    }
}
