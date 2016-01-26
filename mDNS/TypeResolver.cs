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
using System.Threading;
using mDNS.Logging;

namespace mDNS
{
	/// <summary> Helper class to resolve service types.
	/// 
	/// The TypeResolver queries three times consecutively for service types, and then
	/// removes itself from the timer.
	/// 
	/// The TypeResolver will run only if JmDNS is in state ANNOUNCED.
	/// </summary>
	// TODO: check this
	internal class TypeResolver /*: IThreadRunnable /*:TimerTask*/
	{
		private ILog logger = LogManager.GetLogger("TypeResolver");
		public TypeResolver(mDNS enclosingInstance)
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
		public virtual void  start()
		{
			//TODO: check this
			Enclosing_Instance.Timer = new Timer(new TimerCallback(this.Run), null, DNSConstants.QUERY_WAIT_INTERVAL, DNSConstants.QUERY_WAIT_INTERVAL);
		}
		/// <summary>Counts the number of queries that were sent. </summary>
		internal int count = 0;
			
		public void Run(object state)
		{
			try
			{
				if (Enclosing_Instance.State == DNSState.ANNOUNCED)
				{
					if (++count < 3)
					{
						logger.Debug("run() JmDNS querying type");
						DNSOutgoing out_Renamed = new DNSOutgoing(DNSConstants.FLAGS_QR_QUERY);
						out_Renamed.AddQuestion(new DNSQuestion("_services._mdns._udp.local.", DNSConstants.TYPE_PTR, DNSConstants.CLASS_IN));
						foreach (string s in Enclosing_Instance.serviceTypes.Values)
						{
							out_Renamed.AddAnswer(new Pointer("_services._mdns._udp.local.", DNSConstants.TYPE_PTR, DNSConstants.CLASS_IN, DNSConstants.DNS_TTL, s), 0);
						}
						Enclosing_Instance.Send(out_Renamed);
					}
					else
					{
						// After three queries, we can quit.
						// TODO: can we omit this?
						//cancel();
					}
					;
				}
				else if (Enclosing_Instance.State == DNSState.CANCELED)
				{
					// TODO: can omit this?
					//cancel();
				}
			}
			catch (Exception e)
			{
				logger.Warn("run() exception ", e);
				Enclosing_Instance.Recover();
			}
		}
	}
}
