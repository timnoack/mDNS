// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

using System;
using System.IO;
using mDNS.Logging;
using Windows.Foundation;

namespace mDNS
{
	/// <summary> Listen for multicast packets.</summary>
	internal class SocketListener /*: IThreadRunnable*/
	{
		private ILog logger = LogManager.GetLogger("SocketListener");
			
		public SocketListener(mDNS enclosingInstance)
		{
			InitBlock(enclosingInstance);
		}
		private void  InitBlock(mDNS enclosingInstance)
		{
			this.enclosingInstance = enclosingInstance;
		}
		private mDNS enclosingInstance;
		public mDNS Enclosing_Instance
		{
			get
			{
				return enclosingInstance;
			}
				
		}
		public virtual void Run(IAsyncAction action)
		{
            Enclosing_Instance.Socket.MessageReceived += Socket_MessageReceived;
			try
			{
				sbyte[] buf = new sbyte[DNSConstants.MAX_MSG_ABSOLUTE];
				SupportClass.PacketSupport packet = new SupportClass.PacketSupport(SupportClass.ToByteArray(buf), buf.Length);
				while (Enclosing_Instance.State != DNSState.CANCELED)
				{
					
				}
			}
			catch (IOException e)
			{
				if (Enclosing_Instance.State != DNSState.CANCELED)
				{
					logger.Warn("run() exception ", e);
					Enclosing_Instance.Recover();
				}
			}
		}

        private void Socket_MessageReceived(Windows.Networking.Sockets.DatagramSocket sender, Windows.Networking.Sockets.DatagramSocketMessageReceivedEventArgs args)
        {
            byte[] result;

            using (var streamReader = new MemoryStream())
            {
                args.GetDataStream().AsStreamForRead().CopyTo(streamReader);
                result = streamReader.ToArray();
            }
            SupportClass.PacketSupport packet = new SupportClass.PacketSupport(result, result.Length, new System.Net.IPEndPoint(System.Net.IPAddress.Parse(args.RemoteAddress.CanonicalName), int.Parse(args.RemotePort)));
            try
            {
                if (Enclosing_Instance.localHost.ShouldIgnorePacket(packet))
                    return;

                DNSIncoming msg = new DNSIncoming(packet);
                logger.Debug("SocketListener.run() JmDNS in:" + msg.Print(true));

                lock (Enclosing_Instance.IOLock)
                {
                    if (msg.Query)
                    {
                        if (packet.Port != DNSConstants.MDNS_PORT)
                        {
                            Enclosing_Instance.HandleQuery(msg, packet.Address, packet.Port);
                        }
                        Enclosing_Instance.HandleQuery(msg, Enclosing_Instance.Group, DNSConstants.MDNS_PORT);
                    }
                    else
                    {
                        Enclosing_Instance.HandleResponse(msg);
                    }
                }
            }
            catch (IOException e)
            {
                logger.Warn("run() exception ", e);
            }
        }
    }
}
