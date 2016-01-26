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

        public event Windows.Foundation.TypedEventHandler<DatagramSocket, DatagramSocketMessageReceivedEventArgs> MessageReceived;

        public UdpClient(int mDNS_PORT)
        {
            socket = new DatagramSocket();
            this.mDNS_PORT = mDNS_PORT;
            socket.Control.MulticastOnly = true;
        }

        private void Socket_MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            if (MessageReceived != null)
                MessageReceived(sender, args);
        }

        public async Task Bind()
        {
            await socket.BindServiceNameAsync((mDNS_PORT.ToString()));
            socket.MessageReceived += Socket_MessageReceived;
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
            var waitHandle = socket.GetOutputStreamAsync(new Windows.Networking.HostName(iPEndPoint.Address.ToString()), iPEndPoint.Port.ToString());

            byte[] buffer = new byte[length];
            Array.Copy(data, buffer, length);
            var iBuffer = buffer.AsBuffer();

            var outputBuffer = await waitHandle;
            await outputBuffer.WriteAsync(iBuffer);
            await outputBuffer.FlushAsync();
        }

        public void Dispose()
        {
            if (socket != null)
                socket.Dispose();
        }
    }
}