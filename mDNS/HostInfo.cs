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
using System.Net;
using System.Net.Sockets;
using System.Text;
using mDNS.Logging;

namespace mDNS
{
	
	/// <summary> HostInfo information on the local host to be able to cope with change of addresses.
	/// 
	/// </summary>
	/// <author> 	Pierre Frisch, Werner Randelshofer
	/// </author>
	/// <version>  	%I%, %G%
	/// </version>
	public class HostInfo
	{
		virtual public string Name
		{
			get
			{
				return name;
			}
			
		}
		virtual public IPAddress Address
		{
			get
			{
				return address;
			}
			
		}
		// TODO: do we need this?
//		virtual public NetworkInterface Interface
//		{
//			get
//			{
//				return interfaze;
//			}
//			
//		}
		virtual internal Address DNS4AddressRecord
		{
			get
			{
				// TODO: simplify this statement
				// i removed ipv6 stuff
				if ((Address != null) && (Address.AddressFamily == AddressFamily.InterNetwork))
				{
					return new Address(Name, DNSConstants.TYPE_A, DNSConstants.CLASS_IN, DNSConstants.DNS_TTL, Address);
				}
				return null;
			}
			
		}
		virtual internal Address DNS6AddressRecord
		{
			get
			{
				if ((Address != null) && (Address.AddressFamily == AddressFamily.InterNetworkV6))
				{
					return new Address(Name, DNSConstants.TYPE_AAAA, DNSConstants.CLASS_IN, DNSConstants.DNS_TTL, Address);
				}
				return null;
			}
			
		}
		private static ILog logger;
		protected internal string name;
		protected internal IPAddress address;
		// TODO: do we need this?
//		protected internal NetworkInterface interfaze;
		/// <summary> This is used to create a unique name for the host name.</summary>
		private int hostNameCount;
		
		public HostInfo(IPAddress address, string name) : base()
		{
			this.address = address;
			this.name = name;
			// TODO: do we need this?
//			if (address != null)
//			{
//				try
//				{
//					interfaze = NetworkInterface.getByInetAddress(address);
//				}
//				catch (Exception exception)
//				{
//					// FIXME Shouldn't we take an action here?
//					logger.Warn("LocalHostInfo() exception ", exception);
//				}
//			}
		}
		
		internal virtual string IncrementHostName()
		{
			lock (this)
			{
				hostNameCount++;
				int plocal = name.IndexOf(".local.");
				int punder = name.LastIndexOf("-");
				name = name.Substring(0, ((punder == - 1?plocal:punder)) - (0)) + "-" + hostNameCount + ".local.";
				return name;
			}
		}
		
		internal virtual bool ShouldIgnorePacket(SupportClass.PacketSupport packet)
		{
			bool result = false;
			if (Address != null)
			{
				IPAddress from = packet.Address;
				if (from != null)
				{
					// TODO: how to replace this?
//					if (from.isLinkLocalAddress() && (!Address.isLinkLocalAddress()))
//					{
//						// Ignore linklocal packets on regular interfaces, unless this is
//						// also a linklocal interface. This is to avoid duplicates. This is
//						// a terrible hack caused by the lack of an API to get the address
//						// of the interface on which the packet was received.
//						result = true;
//					}
					if (IPAddress.IsLoopback(from) && (!IPAddress.IsLoopback(Address)))
					{
						// Ignore loopback packets on a regular interface unless this is
						// also a loopback interface.
						result = true;
					}
				}
			}
			return result;
		}
		
		internal virtual Address GetDNSAddressRecord(Address address)
		{
			return (DNSConstants.TYPE_AAAA == address.type?DNS6AddressRecord:DNS4AddressRecord);
		}
		
		public override string ToString()
		{
			StringBuilder buf = new StringBuilder();
			buf.Append("local host info[");
			buf.Append(Name != null?Name:"no name");
			buf.Append(", ");
			//buf.Append(Interface != null?Interface.getDisplayName():"???");
			buf.Append(":");
			buf.Append(Address != null?Address.ToString():"no address");
			buf.Append("]");
			return buf.ToString();
		}
		static HostInfo()
		{
			logger = LogManager.GetLogger(typeof(HostInfo).ToString());
		}
	}
}
