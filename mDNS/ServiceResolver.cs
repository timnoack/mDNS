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
	/// <summary> The ServiceResolver queries three times consecutively for services of
	/// a given type, and then removes itself from the timer.
	/// 
	/// The ServiceResolver will run only if JmDNS is in state ANNOUNCED.
	/// REMIND: Prevent having multiple service resolvers for the same type in the
	/// timer queue.
	/// </summary>
	// TODO: check this
	internal class ServiceResolver /*: IThreadRunnable /*:TimerTask */
	{
		private ILog logger = LogManager.GetLogger("ServiceResolver");
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
		/// <summary> Counts the number of queries being sent.</summary>
		internal int count = 0;
		private string type;
		public ServiceResolver(mDNS enclosingInstance, string type)
		{
			InitBlock(enclosingInstance);
			this.type = type;
		}
		public virtual void  start()
		{
			// TODO: check this
			Enclosing_Instance.Timer = new Timer(new TimerCallback(this.Run), null, DNSConstants.QUERY_WAIT_INTERVAL, DNSConstants.QUERY_WAIT_INTERVAL);
		}
			
		public void Run(object state)
		{
			try
			{
				if (Enclosing_Instance.State == DNSState.ANNOUNCED)
				{
					if (count++ < 3)
					{
						logger.Debug("run() JmDNS querying service");
						long now = (DateTime.Now.Ticks - 621355968000000000) / 10000;
						DNSOutgoing out_Renamed = new DNSOutgoing(DNSConstants.FLAGS_QR_QUERY);
						out_Renamed.AddQuestion(new DNSQuestion(type, DNSConstants.TYPE_PTR, DNSConstants.CLASS_IN));
						foreach (ServiceInfo info in Enclosing_Instance.services.Values)
						{
							try
							{
								out_Renamed.AddAnswer(new Pointer(info.type, DNSConstants.TYPE_PTR, DNSConstants.CLASS_IN, DNSConstants.DNS_TTL, info.QualifiedName), now);
							}
							catch
							{
								break;
							}
						}
						Enclosing_Instance.Send(out_Renamed);
					}
					else
					{
						// After three queries, we can quit.
						// TODO: can omit?
						//cancel();
					}
					;
				}
				else if (Enclosing_Instance.State == DNSState.CANCELED)
				{
					// TODO: can omit?
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
