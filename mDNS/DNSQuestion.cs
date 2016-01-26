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
using mDNS.Logging;

namespace mDNS
{
	
	/// <summary> A DNS question.
	/// 
	/// </summary>
	/// <author> 	Arthur van Hoff
	/// </author>
	/// <version>  	%I%, %G%
	/// </version>
	sealed class DNSQuestion : DNSEntry
	{
		private static ILog logger;
		/// <summary> Create a question.</summary>
		internal DNSQuestion(string name, int type, int clazz):base(name, type, clazz)
		{
		}
		
		/// <summary> Check if this question is answered by a given DNS record.</summary>
		internal bool IsAnsweredBy(DNSRecord rec)
		{
			return (clazz == rec.clazz) && ((type == rec.type) || (type == DNSConstants.TYPE_ANY)) && name.Equals(rec.name);
		}
		
		/// <summary> For debugging only.</summary>
		public override string ToString()
		{
			return toString("question", null);
		}
		static DNSQuestion()
		{
			logger = LogManager.GetLogger(typeof(DNSQuestion).ToString());
		}
	}
}
