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
	internal class Text : DNSRecord
	{
		private static ILog logger;
		internal sbyte[] text;
			
		internal Text(string name, int type, int clazz, int ttl, sbyte[] text) : base(name, type, clazz, ttl)
		{
			this.text = text;
		}
		internal override void Write(DNSOutgoing out_Renamed)
		{
			out_Renamed.WriteBytes(text, 0, text.Length);
		}
		internal override bool SameValue(DNSRecord other)
		{
			Text txt = (Text) other;
			if (txt.text.Length != text.Length)
			{
				return false;
			}
			for (int i = text.Length; i-- > 0; )
			{
				if (txt.text[i] != text[i])
				{
					return false;
				}
			}
			return true;
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
			// Shouldn't we care if we get a conflict at this level?
			/*
				ServiceInfo info = (ServiceInfo) dns.services.get(name.toLowerCase());
				if (info != null) {
				if (! Arrays.equals(text,info.text)) {
				info.revertState();
				return true;
				}
				}*/
			return false;
		}
		internal override DNSOutgoing AddAnswer(mDNS dns, DNSIncoming in_Renamed, IPAddress addr, int port, DNSOutgoing out_Renamed)
		{
			return out_Renamed;
		}
		public override string ToString()
		{
			// TODO this is a mess
			return toString((text.Length > 10)?new String(SupportClass.ToCharArray(text), 0, 7) + "...":new String(SupportClass.ToCharArray(text)));
		}
		static Text()
		{
			logger = LogManager.GetLogger(typeof(Text).ToString());
		}
	}
}
