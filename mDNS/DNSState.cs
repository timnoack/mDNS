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
using System.Collections;
using mDNS.Logging;

namespace mDNS
{
	/// <summary> DNSState defines the possible states for services registered with JmDNS.
	/// 
	/// </summary>
	/// <author>   Werner Randelshofer
	/// </author>
	/// <version>  1.0  May 23, 2004  Created.
	/// </version>
	public class DNSState : IComparable
	{
		/// <summary> Returns true, if this is a probing state.</summary>
		virtual public bool Probing
		{
			get
			{
				return CompareTo(PROBING_1) >= 0 && CompareTo(PROBING_3) <= 0;
			}
			
		}
		/// <summary> Returns true, if this is an announcing state.</summary>
		virtual public bool Announcing
		{
			get
			{
				return CompareTo(ANNOUNCING_1) >= 0 && CompareTo(ANNOUNCING_2) <= 0;
			}
			
		}
		/// <summary> Returns true, if this is an announced state.</summary>
		virtual public bool Announced
		{
			get
			{
				return CompareTo(ANNOUNCED) == 0;
			}
			
		}
		private static ILog logger;
		
		private string name;
		
		/// <summary>Ordinal of next state to be created. </summary>
		private static int nextOrdinal = 0;
		/// <summary>Assign an ordinal to this state. </summary>
		private int ordinal = nextOrdinal++;
		/// <summary> Logical sequence of states.
		/// The sequence is consistent with the ordinal of a state.
		/// This is used for advancing through states.
		/// </summary>
		private static readonly ArrayList sequence = new ArrayList();
		
		private DNSState(string name)
		{
			this.name = name;
			sequence.Add(this);
		}
		public override string ToString()
		{
			return name;
		}
		
		public static readonly DNSState PROBING_1 = new DNSState("probing 1");
		public static readonly DNSState PROBING_2 = new DNSState("probing 2");
		public static readonly DNSState PROBING_3 = new DNSState("probing 3");
		public static readonly DNSState ANNOUNCING_1 = new DNSState("announcing 1");
		public static readonly DNSState ANNOUNCING_2 = new DNSState("announcing 2");
		public static readonly DNSState ANNOUNCED = new DNSState("announced");
		public static readonly DNSState CANCELED = new DNSState("canceled");
		
		/// <summary> Returns the next advanced state.
		/// In general, this advances one step in the following sequence: PROBING_1,
		/// PROBING_2, PROBING_3, ANNOUNCING_1, ANNOUNCING_2, ANNOUNCED.
		/// Does not advance for ANNOUNCED and CANCELED state.
		/// </summary>
		public DNSState Advance()
		{
			return (Probing || Announcing)?(DNSState) sequence[ordinal + 1]:this;
		}
		
		/// <summary> Returns to the next reverted state.
		/// All states except CANCELED revert to PROBING_1.
		/// Status CANCELED does not revert.
		/// </summary>
		public DNSState Revert()
		{
			return (this == CANCELED)?this:PROBING_1;
		}
		
		/// <summary> Compares two states.
		/// The states compare as follows:
		/// PROBING_1 &lt; PROBING_2 &lt; PROBING_3 &lt; ANNOUNCING_1 &lt;
		/// ANNOUNCING_2 &lt; RESPONDING &lt; ANNOUNCED &lt; CANCELED.
		/// </summary>
		public virtual int CompareTo(object o)
		{
			return ordinal - ((DNSState) o).ordinal;
		}
		static DNSState()
		{
			logger = LogManager.GetLogger(typeof(DNSState).ToString());
		}
	}
}
