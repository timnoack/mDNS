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
	/// <summary> The ServiceInfoResolver queries up to three times consecutively for
	/// a service info, and then removes itself from the timer.
	/// 
	/// The ServiceInfoResolver will run only if JmDNS is in state ANNOUNCED.
	/// REMIND: Prevent having multiple service resolvers for the same info in the
	/// timer queue.
	/// </summary>
	// TODO: check this
	internal class ServiceInfoResolver /*: IThreadRunnable /*:TimerTask*/
	{
		private ILog logger = LogManager.GetLogger("ServiceInfoResolver");
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
		private ServiceInfo info;
		public ServiceInfoResolver(mDNS enclosingInstance, ServiceInfo info)
		{
			InitBlock(enclosingInstance);
			this.info = info;
			info.dns = Enclosing_Instance;
			Enclosing_Instance.AddListener(info, new DNSQuestion(info.QualifiedName, DNSConstants.TYPE_ANY, DNSConstants.CLASS_IN));
		}
		public virtual void  start()
		{
			//TODO: check this
			Enclosing_Instance.Timer = new Timer(new TimerCallback(this.Run), null, DNSConstants.QUERY_WAIT_INTERVAL, DNSConstants.QUERY_WAIT_INTERVAL);
		}
			
		public void Run(object state)
		{
			try
			{
				if (Enclosing_Instance.State == DNSState.ANNOUNCED)
				{
					if (count++ < 3 && !info.HasData)
					{
						long now = (DateTime.Now.Ticks - 621355968000000000) / 10000;
						DNSOutgoing out_Renamed = new DNSOutgoing(DNSConstants.FLAGS_QR_QUERY);
						out_Renamed.AddQuestion(new DNSQuestion(info.QualifiedName, DNSConstants.TYPE_SRV, DNSConstants.CLASS_IN));
						out_Renamed.AddQuestion(new DNSQuestion(info.QualifiedName, DNSConstants.TYPE_TXT, DNSConstants.CLASS_IN));
						if (info.server != null)
						{
							out_Renamed.AddQuestion(new DNSQuestion(info.server, DNSConstants.TYPE_A, DNSConstants.CLASS_IN));
						}
						out_Renamed.AddAnswer((DNSRecord) Enclosing_Instance.Cache.get_Renamed(info.QualifiedName, DNSConstants.TYPE_SRV, DNSConstants.CLASS_IN), now);
						out_Renamed.AddAnswer((DNSRecord) Enclosing_Instance.Cache.get_Renamed(info.QualifiedName, DNSConstants.TYPE_TXT, DNSConstants.CLASS_IN), now);
						if (info.server != null)
						{
							out_Renamed.AddAnswer((DNSRecord) Enclosing_Instance.Cache.get_Renamed(info.server, DNSConstants.TYPE_A, DNSConstants.CLASS_IN), now);
						}
						Enclosing_Instance.Send(out_Renamed);
					}
					else
					{
						// After three queries, we can quit.
						// TODO: can omit cancel()?
						//cancel();
						Enclosing_Instance.RemoveListener(info);
					}
					;
				}
				else if (Enclosing_Instance.State == DNSState.CANCELED)
				{
					// TODO: can omit cancel??
					//cancel();
					Enclosing_Instance.RemoveListener(info);
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
