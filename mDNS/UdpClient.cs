using System;
using System.Net;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
namespace mDNS
{
    public class UdpClient : IDisposable
    {
        private DatagramSocket socket;
        private int mDNS_PORT;

        public DatagramSocket Socket {
            get { return socket; } }



        public UdpClient(int mDNS_PORT)
        {
            socket = new DatagramSocket();
            this.mDNS_PORT = mDNS_PORT;
        }


        public async Task Bind()
        {
            socket.Control.MulticastOnly = true;
            await socket.BindServiceNameAsync((mDNS_PORT.ToString()));
        }

        public void JoinMulticastGroup(IPAddress group, int v)
        {
            socket.JoinMulticastGroup(new Windows.Networking.HostName(group.ToString()));
        }

        public void DropMulticastGroup(IPAddress group)
        {
          // TODO: Implement
        }

        public void Close()
        {
            socket.Dispose();
        }


        public async Task Send(byte[] data, int length, IPEndPoint iPEndPoint)
        {
            var stream = await socket.GetOutputStreamAsync(new Windows.Networking.HostName(iPEndPoint.Address.ToString()), iPEndPoint.Port.ToString());

            DataWriter writer = new DataWriter(stream);
            for (int i = 0; i < length; i++)
                writer.WriteByte(data[i]);
      
            await writer.StoreAsync();
        }

        public void Dispose()
        {
            if (socket != null)
                socket.Dispose();
        }
    }
}