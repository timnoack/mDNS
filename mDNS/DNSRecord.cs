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
	
	/// <summary> DNS record
	/// 
	/// </summary>
	/// <author> 	Arthur van Hoff, Rick Blair, Werner Randelshofer, Pierre Frisch
	/// </author>
	/// <version>  	%I%, %G%
	/// </version>
	abstract public class DNSRecord:DNSEntry
	{
		private static ILog logger;
		internal int ttl;
		private long created;
		
		/// <summary> Create a DNSRecord with a name, type, clazz, and ttl.</summary>
		internal DNSRecord(string name, int type, int clazz, int ttl) : base(name, type, clazz)
		{
			this.ttl = ttl;
			this.created = (DateTime.Now.Ticks - 621355968000000000) / 10000;
		}
		
		/// <summary> True if this record is the same as some other record.</summary>
		public  override bool Equals(object other)
		{
			return (other is DNSRecord) && SameAs((DNSRecord) other);
		}
		
		/// <summary> True if this record is the same as some other record.</summary>
		internal virtual bool SameAs(DNSRecord other)
		{
			return base.Equals(other) && SameValue((DNSRecord) other);
		}
		
		/// <summary> True if this record has the same value as some other record.</summary>
		internal abstract bool SameValue(DNSRecord other);
		
		/// <summary> True if this record has the same type as some other record.</summary>
		internal virtual bool SameType(DNSRecord other)
		{
			return type == other.type;
		}
		
		/// <summary> Handles a query represented by this record.
		/// 
		/// </summary>
		/// <returns> Returns true if a conflict with one of the services registered
		/// with JmDNS or with the hostname occured.
		/// </returns>
		internal abstract bool HandleQuery(mDNS dns, long expirationTime);
		/// <summary> Handles a responserepresented by this record.
		/// 
		/// </summary>
		/// <returns> Returns true if a conflict with one of the services registered
		/// with JmDNS or with the hostname occured.
		/// </returns>
		internal abstract bool HandleResponse(mDNS dns);
		
		/// <summary> Adds this as an answer to the provided outgoing datagram.</summary>
		internal abstract DNSOutgoing AddAnswer(mDNS dns, DNSIncoming in_Renamed, IPAddress addr, int port, DNSOutgoing out_Renamed);
		
		/// <summary> True if this record is suppressed by the answers in a message.</summary>
		internal virtual bool SuppressedBy(DNSIncoming msg)
		{
			try
			{
				for (int i = msg.numAnswers; i-- > 0; )
				{
					if (SuppressedBy((DNSRecord) msg.answers[i]))
					{
						return true;
					}
				}
				return false;
			}
			catch (IndexOutOfRangeException e)
			{
				logger.Warn("suppressedBy() message " + msg + " exception ", e);
				// msg.print(true);
				return false;
			}
		}
		
		/// <summary> True if this record would be supressed by an answer.
		/// This is the case if this record would not have a
		/// significantly longer TTL.
		/// </summary>
		internal virtual bool SuppressedBy(DNSRecord other)
		{
			if (SameAs(other) && (other.ttl > ttl / 2))
			{
				return true;
			}
			return false;
		}
		
		/// <summary> Get the expiration time of this record.</summary>
		internal virtual long GetExpirationTime(int percent)
		{
			return created + (percent * ttl * 10L);
		}
		
		/// <summary> Get the remaining TTL for this record.</summary>
		internal virtual int GetRemainingTTL(long now)
		{
			return (int) Math.Max(0, (GetExpirationTime(100) - now) / 1000);
		}
		
		/// <summary> Check if the record is expired.</summary>
		internal virtual bool IsExpired(long now)
		{
			return GetExpirationTime(100) <= now;
		}
		
		/// <summary> Check if the record is stale, ie it has outlived
		/// more than half of its TTL.
		/// </summary>
		internal virtual bool IsStale(long now)
		{
			return GetExpirationTime(50) <= now;
		}
		
		/// <summary> Reset the TTL of a record. This avoids having to
		/// update the entire record in the cache.
		/// </summary>
		internal virtual void ResetTTL(DNSRecord other)
		{
			created = other.created;
			ttl = other.ttl;
		}
		
		/// <summary> Write this record into an outgoing message.</summary>
		internal abstract void Write(DNSOutgoing out_Renamed);
		
		public virtual string toString(string other)
		{
			return toString("record", ttl + "/" + GetRemainingTTL((DateTime.Now.Ticks - 621355968000000000) / 10000) + "," + other);
		}
		static DNSRecord()
		{
			logger = LogManager.GetLogger(typeof(DNSRecord).ToString());
		}

		// TODO: check this
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
}
