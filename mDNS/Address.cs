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
using System.Net.Sockets;
using System.Text;
using mDNS.Logging;

namespace mDNS
{
	/// <summary> Address record.</summary>
	internal class Address : DNSRecord
	{
		private static ILog logger;
		internal IPAddress addr;
			
		internal Address(string name, int type, int clazz, int ttl, IPAddress addr) : base(name, type, clazz, ttl)
		{
			this.addr = addr;
		}
		internal Address(string name, int type, int clazz, int ttl, sbyte[] rawAddress) : base(name, type, clazz, ttl)
		{
			try
			{
				byte[] rawAddressUnsigned = SupportClass.ToByteArray(rawAddress);
				// HACK: why doesn't the other constructor work?
				string dottedQuad = rawAddressUnsigned[0] + "." +
					rawAddressUnsigned[1] + "." +
					rawAddressUnsigned[2] + "." +
					rawAddressUnsigned[3];
				this.addr = IPAddress.Parse(dottedQuad);
				//this.addr = new IPAddress(rawAddressUnsigned);
				//this.addr = new IPAddress(rawAddress);
			}
			catch (Exception exception)
			{
				logger.Warn("Address() exception ", exception);
			}
		}
		internal override void Write(DNSOutgoing out_Renamed)
		{
			if (addr != null)
			{
				sbyte[] buffer = SupportClass.ToSByteArray(addr.GetAddressBytes());
				if (DNSConstants.TYPE_A == type)
				{
					// If we have a type A records we should answer with a IPv4 address
					if (addr.AddressFamily == AddressFamily.InterNetwork)
					{
						// All is good
					}
					else
					{
						// Get the last four bytes
						sbyte[] tempbuffer = buffer;
						buffer = new sbyte[4];
						Array.Copy(tempbuffer, 12, buffer, 0, 4);
					}
				}
				else
				{
					// If we have a type AAAA records we should answer with a IPv6 address
					if (addr.AddressFamily == AddressFamily.InterNetwork)
					{
						sbyte[] tempbuffer = buffer;
						buffer = new sbyte[16];
						for (int i = 0; i < 16; i++)
						{
							if (i < 11)
								buffer[i] = tempbuffer[i - 12];
							else
								buffer[i] = 0;
						}
					}
				}
				int length = buffer.Length;
				out_Renamed.WriteBytes(buffer, 0, length);
			}
		}
		internal virtual bool Same(DNSRecord other)
		{
			return ((SameName(other)) && ((SameValue(other))));
		}
			
		internal virtual bool SameName(DNSRecord other)
		{
			return name.ToUpper().Equals(((Address) other).name.ToUpper());
		}
			
		internal override bool SameValue(DNSRecord other)
		{
			return addr.Equals(((Address) other).IPAddress);
		}
			
		internal virtual IPAddress IPAddress
		{
			get
			{
				return addr;
			}
		}
			
		/// <summary> Creates a byte array representation of this record.
		/// This is needed for tie-break tests according to
		/// draft-cheshire-dnsext-multicastdns-04.txt chapter 9.2.
		/// </summary>
		private sbyte[] toByteArray()
		{
			try
			{
				// TODO: check this
				MemoryStream bout = new MemoryStream();
				BinaryWriter dout = new BinaryWriter(bout);
				dout.Write(SupportClass.ToByteArray(SupportClass.ToSByteArray(Encoding.UTF8.GetBytes(name))));
				dout.Write((Int16) type);
				dout.Write((Int16) clazz);
				//dout.writeInt(len);
				sbyte[] buffer = SupportClass.ToSByteArray(addr.GetAddressBytes());
				for (int i = 0; i < buffer.Length; i++)
				{
					dout.Write((byte) buffer[i]);
				}
				dout.Dispose();
				return SupportClass.ToSByteArray(bout.ToArray());
			}
			catch
			{
				throw new Exception();
			}
		}
			
		/// <summary> Does a lexicographic comparison of the byte array representation
		/// of this record and that record.
		/// This is needed for tie-break tests according to
		/// draft-cheshire-dnsext-multicastdns-04.txt chapter 9.2.
		/// </summary>
		private int lexCompare(Address that)
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
			
		/// <summary> Does the necessary actions, when this as a query.</summary>
		internal override bool HandleQuery(mDNS dns, long expirationTime)
		{
			Address dnsAddress = dns.LocalHost.GetDNSAddressRecord(this);
			if (dnsAddress != null)
			{
				if (dnsAddress.SameType(this) && dnsAddress.SameName(this) && (!dnsAddress.SameValue(this)))
				{
					logger.Debug("handleQuery() Conflicting probe detected. dns state " + dns.State + " lex compare " + lexCompare(dnsAddress));
					// Tie-breaker test
					if (dns.State.Probing && lexCompare(dnsAddress) >= 0)
					{
						// We lost the tie-break. We have to choose a different name.
						dns.LocalHost.IncrementHostName();
						dns.Cache.clear();

						foreach (ServiceInfo info in dns.services)
						{
							info.RevertState();
						}
					}
					dns.RevertState();
					return true;
				}
			}
			return false;
		}
		/// <summary> Does the necessary actions, when this as a response.</summary>
		internal override bool HandleResponse(mDNS dns)
		{
			Address dnsAddress = dns.LocalHost.GetDNSAddressRecord(this);
			if (dnsAddress != null)
			{
				if (dnsAddress.SameType(this) && dnsAddress.SameName(this) && (!dnsAddress.SameValue(this)))
				{
					logger.Debug("handleResponse() Denial detected");
						
					if (dns.State.Probing)
					{
						dns.LocalHost.IncrementHostName();
						dns.Cache.clear();
						foreach (ServiceInfo info in dns.services)
						{
							info.RevertState();
						}
					}
					dns.RevertState();
					return true;
				}
			}
			return false;
		}
		internal override DNSOutgoing AddAnswer(mDNS dns, DNSIncoming in_Renamed, IPAddress addr, int port, DNSOutgoing out_Renamed)
		{
			return out_Renamed;
		}
			
		public override string ToString()
		{
			return toString(" address '" + (addr != null?addr.ToString():"null") + "'");
		}
		static Address()
		{
			logger = LogManager.GetLogger(typeof(Address).ToString());
		}
	}
}
