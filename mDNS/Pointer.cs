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
using mDNS.Logging;

namespace mDNS
{
	/// <summary> Pointer record.</summary>
	internal class Pointer : DNSRecord
	{
		virtual internal string Alias
		{
			get
			{
				return alias;
			}
				
		}
		private static ILog logger;
		internal string alias;
			
		internal Pointer(string name, int type, int clazz, int ttl, string alias) : base(name, type, clazz, ttl)
		{
			this.alias = alias;
		}
		internal override void Write(DNSOutgoing out_Renamed)
		{
			out_Renamed.WriteName(alias);
		}
		internal override bool SameValue(DNSRecord other)
		{
			return alias.Equals(((Pointer) other).alias);
		}
		internal override bool HandleQuery(mDNS dns, long expirationTime)
		{
			// Nothing to do (?)
			// I think there is no possibility for conflicts for this record type?
			return false;
		}
		internal override bool HandleResponse(mDNS dns)
		{
			// Nothing to do (?)
			// I think there is no possibility for conflicts for this record type?
			return false;
		}
		internal override DNSOutgoing AddAnswer(mDNS dns, DNSIncoming in_Renamed, IPAddress addr, int port, DNSOutgoing out_Renamed)
		{
			return out_Renamed;
		}
		public override string ToString()
		{
			return toString(alias);
		}
		static Pointer()
		{
			logger = LogManager.GetLogger(typeof(Pointer).ToString());
		}
	}
}
