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
using System.Threading;
using mDNS.Logging;

namespace mDNS
{
	/// <summary> The Prober sends three consecutive probes for all service infos
	/// that needs probing as well as for the host name.
	/// The state of each service info of the host name is advanced, when a probe has
	/// been sent for it.
	/// When the prober has run three times, it launches an Announcer.
	/// 
	/// If a conflict during probes occurs, the affected service infos (and affected
	/// host name) are taken away from the prober. This eventually causes the prober
	/// tho cancel itself.
	/// </summary>
	// TODO: check this
	internal class Prober /*IThreadRunnable*/ /*:TimerTask*/
	{
		private ILog logger = LogManager.GetLogger("Prober");
		private Random random = new Random();
		private void  InitBlock(mDNS enclosingInstance)
		{
			this.enclosingInstance = enclosingInstance;
			taskState = DNSState.PROBING_1;
		}
		private mDNS enclosingInstance;
		public mDNS Enclosing_Instance
		{
			get
			{
				return enclosingInstance;
			}
				
		}
		/// <summary>The state of the prober. </summary>
		internal DNSState taskState;
			
		public Prober(mDNS enclosingInstance)
		{
			InitBlock(enclosingInstance);
			// Associate the host name to this, if it needs probing
			if (Enclosing_Instance.State == DNSState.PROBING_1)
			{
				Enclosing_Instance.Task = this;
			}
			// Associate services to this, if they need probing
			lock (Enclosing_Instance)
			{
				foreach (ServiceInfo info in Enclosing_Instance.services.Values)
				{
					if (info.State == DNSState.PROBING_1)
					{
						info.task = this;
					}
				}
			}
		}
			
			
		public virtual void  start()
		{
			long now = (DateTime.Now.Ticks - 621355968000000000) / 10000;
			if (now - Enclosing_Instance.LastThrottleIncrement < DNSConstants.PROBE_THROTTLE_COUNT_INTERVAL)
			{
				Enclosing_Instance.Throttle++;
			}
			else
			{
				Enclosing_Instance.Throttle = 1;
			}
			Enclosing_Instance.LastThrottleIncrement = now;
				
			if (Enclosing_Instance.State == DNSState.ANNOUNCED && Enclosing_Instance.Throttle < DNSConstants.PROBE_THROTTLE_COUNT)
			{
				//TODO: check this
				//Enclosing_Instance.timer.schedule(this, javax.jmdns.JmDNS.random.Next(1 + DNSConstants.PROBE_WAIT_INTERVAL), DNSConstants.PROBE_WAIT_INTERVAL);
				Enclosing_Instance.Timer = new Timer(new TimerCallback(this.Run), null, random.Next(1 + DNSConstants.PROBE_WAIT_INTERVAL), DNSConstants.PROBE_WAIT_INTERVAL);
			}
			else
			{
				// TODO: check this
				//Enclosing_Instance.timer.schedule(this, DNSConstants.PROBE_CONFLICT_INTERVAL, DNSConstants.PROBE_CONFLICT_INTERVAL);
				TimerCallback thisCallback = new TimerCallback(this.Run);
				Enclosing_Instance.Timer = new Timer(thisCallback, null, DNSConstants.PROBE_CONFLICT_INTERVAL, DNSConstants.PROBE_CONFLICT_INTERVAL);
			}
		}
			
		public bool cancel()
		{
			// Remove association from host name to this
			if (Enclosing_Instance.Task == this)
				Enclosing_Instance.Task = null;
				
			// Remove associations from services to this
			lock (Enclosing_Instance)
			{
				foreach (ServiceInfo info in Enclosing_Instance.services.Values)
				{
					if (info.task == this)
					{
						info.task = null;
					}
				}
			}
				
			// TODO: check this - not completely correct
			//return base.cancel();
			return true;
		}
			
		public void Run(object state)
		{
			lock (Enclosing_Instance.IOLock)
			{
				DNSOutgoing out_Renamed = null;
				try
				{
					// send probes for JmDNS itself
					if (Enclosing_Instance.State == taskState && Enclosing_Instance.Task == this)
					{
						if (out_Renamed == null)
						{
							out_Renamed = new DNSOutgoing(DNSConstants.FLAGS_QR_QUERY);
						}
						out_Renamed.AddQuestion(new DNSQuestion(Enclosing_Instance.localHost.Name, DNSConstants.TYPE_ANY, DNSConstants.CLASS_IN));
						DNSRecord answer = Enclosing_Instance.localHost.DNS4AddressRecord;
						if (answer != null)
							out_Renamed.AddAuthorativeAnswer(answer);
						answer = Enclosing_Instance.localHost.DNS6AddressRecord;
						if (answer != null)
							out_Renamed.AddAuthorativeAnswer(answer);
						Enclosing_Instance.AdvanceState();
					}
					// send probes for services
					// Defensively copy the services into a local list,
					// to prevent race conditions with methods registerService
					// and unregisterService.
					IList list;
					lock (Enclosing_Instance)
					{
						list = new ArrayList(Enclosing_Instance.services.Values);
					}
					foreach (ServiceInfo info in list)
					{
						lock (info)
						{
							if (info.State == taskState && info.task == this)
							{
								info.AdvanceState();
								logger.Info("run() JmDNS probing " + info.QualifiedName + " state " + info.State);
								if (out_Renamed == null)
								{
									out_Renamed = new DNSOutgoing(DNSConstants.FLAGS_QR_QUERY);
									out_Renamed.AddQuestion(new DNSQuestion(info.QualifiedName, DNSConstants.TYPE_ANY, DNSConstants.CLASS_IN));
								}
								out_Renamed.AddAuthorativeAnswer(new Service(info.QualifiedName, DNSConstants.TYPE_SRV, DNSConstants.CLASS_IN, DNSConstants.DNS_TTL, info.priority, info.weight, info.port, info.server));
							}
						}
					}
					if (out_Renamed != null)
					{
						logger.Debug("run() JmDNS probing #" + taskState);
						Enclosing_Instance.Send(out_Renamed);
					}
					else
					{
						// If we have nothing to send, another timer taskState ahead
						// of us has done the job for us. We can cancel.
						cancel();
						return ;
					}
				}
				catch (Exception e)
				{
					logger.Warn("run() exception ", e);
					Enclosing_Instance.Recover();
				}
					
				taskState = taskState.Advance();
				if (!taskState.Probing)
				{
					cancel();
						
					new Announcer(enclosingInstance).start();
				}
			}
		}
	}
}
