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
using System.Net;
using System.Text;
using mDNS.Logging;

namespace mDNS
{
	/// <summary> Service record.</summary>
	internal class Service : DNSRecord
	{
		private static ILog logger;
		internal int priority;
		internal int weight;
		internal int port;
		internal string server;
			
		internal Service(string name, int type, int clazz, int ttl, int priority, int weight, int port, string server) : base(name, type, clazz, ttl)
		{
			this.priority = priority;
			this.weight = weight;
			this.port = port;
			this.server = server;
		}
			
		internal override void Write(DNSOutgoing out_Renamed)
		{
			out_Renamed.WriteShort(priority);
			out_Renamed.WriteShort(weight);
			out_Renamed.WriteShort(port);
			out_Renamed.WriteName(server);
		}
			
		private sbyte[] toByteArray()
		{
			try
			{
				// TODO: check this
				MemoryStream bout = new MemoryStream();
				BinaryWriter dout = new BinaryWriter(bout);
				dout.Write(SupportClass.ToByteArray(SupportClass.ToSByteArray(Encoding.GetEncoding("UTF8").GetBytes(name))));
				dout.Write((Int16) type);
				dout.Write((Int16) clazz);
				//dout.writeInt(len);
				dout.Write((Int16) priority);
				dout.Write((Int16) weight);
				dout.Write((Int16) port);
				dout.Write(SupportClass.ToByteArray(SupportClass.ToSByteArray(Encoding.GetEncoding("UTF8").GetBytes(server))));
				dout.Dispose();
				return SupportClass.ToSByteArray(bout.ToArray());
			}
			catch
			{
				throw new Exception();
			}
		}
		private int lexCompare(Service that)
		{
			sbyte[] thisBytes = this.toByteArray();
			sbyte[] thatBytes = that.toByteArray();
			for (int i = 0, n = Math.Min(thisBytes.Length, thatBytes.Length); i < n; i++)
			{
				if (thisBytes[i] > thatBytes[i])
				{
					return 1;
				}
				else if (thisBytes[i] < thatBytes[i])
				{
					return - 1;
				}
			}
			return thisBytes.Length - thatBytes.Length;
		}
		internal override bool SameValue(DNSRecord other)
		{
			Service s = (Service) other;
			return (priority == s.priority) && (weight == s.weight) && (port == s.port) && server.Equals(s.server);
		}
		internal override bool HandleQuery(mDNS dns, long expirationTime)
		{
			ServiceInfo info = (ServiceInfo) dns.services[name.ToLower()];
			if (info != null && (port != info.port || !server.ToUpper().Equals(dns.LocalHost.Name.ToUpper())))
			{
				logger.Debug("handleQuery() Conflicting probe detected");
					
				// Tie breaker test
				if (info.State.Probing && lexCompare(new Service(info.QualifiedName, DNSConstants.TYPE_SRV, DNSConstants.CLASS_IN | DNSConstants.CLASS_UNIQUE, DNSConstants.DNS_TTL, info.priority, info.weight, info.port, dns.LocalHost.Name)) >= 0)
				{
					// We lost the tie break
					string oldName = info.QualifiedName.ToLower();
					info.SetName(dns.IncrementName(info.getName()));
					dns.services.Remove(oldName);
					dns.services[info.QualifiedName.ToLower()] = info;
					logger.Debug("handleQuery() Lost tie break: new unique name chosen:" + info.getName());
				}
				info.RevertState();
				return true;
			}
			return false;
		}
		internal override bool HandleResponse(mDNS dns)
		{
			ServiceInfo info = (ServiceInfo) dns.services[name.ToLower()];
			if (info != null && (port != info.port || !server.ToUpper().Equals(dns.LocalHost.Name.ToUpper())))
			{
				logger.Debug("handleResponse() Denial detected");
					
				if (info.State.Probing)
				{
					string oldName = info.QualifiedName.ToLower();
					info.SetName(dns.IncrementName(info.getName()));
					dns.services.Remove(oldName);
					dns.services[info.QualifiedName.ToLower()] = info;
					logger.Debug("handleResponse() New unique name chose:" + info.getName());
				}
				info.RevertState();
				return true;
			}
			return false;
		}
		internal override DNSOutgoing AddAnswer(mDNS dns, DNSIncoming in_Renamed, IPAddress addr, int port, DNSOutgoing out_Renamed)
		{
			ServiceInfo info = (ServiceInfo) dns.services[name.ToLower()];
			if (info != null)
			{
				if (this.port == info.port != server.Equals(dns.LocalHost.Name))
				{
					return dns.AddAnswer(in_Renamed, addr, port, out_Renamed, new Service(info.QualifiedName, DNSConstants.TYPE_SRV, DNSConstants.CLASS_IN | DNSConstants.CLASS_UNIQUE, DNSConstants.DNS_TTL, info.priority, info.weight, info.port, dns.LocalHost.Name));
				}
			}
			return out_Renamed;
		}
		public override string ToString()
		{
			return toString(server + ":" + port);
		}
		static Service()
		{
			logger = LogManager.GetLogger(typeof(Service).ToString());
		}
	}
}
