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
	/// <summary> The Announcer sends an accumulated query of all announces, and advances
	/// the state of all serviceInfos, for which it has sent an announce.
	/// The Announcer also sends announcements and advances the state of JmDNS itself.
	/// 
	/// When the announcer has run two times, it finishes.
	/// </summary>
	// TODO: check this
	internal class Announcer /*: IThreadRunnable :TimerTask*/
	{
		private ILog logger = LogManager.GetLogger("Announcer");
			
		private void  InitBlock(mDNS enclosingInstance)
		{
			this.enclosingInstance = enclosingInstance;
			taskState = DNSState.ANNOUNCING_1;
		}
			
		private mDNS enclosingInstance;
			
		public mDNS Enclosing_Instance
		{
			get
			{
				return enclosingInstance;
			}
				
		}
			
		/// <summary>The state of the announcer. </summary>
		internal DNSState taskState;
			
		public Announcer(mDNS enclosingInstance)
		{
			InitBlock(enclosingInstance);
			// Associate host to this, if it needs announcing
			if (Enclosing_Instance.State == DNSState.ANNOUNCING_1)
			{
				Enclosing_Instance.Task = this;
			}
			// Associate services to this, if they need announcing
			lock (Enclosing_Instance)
			{
				foreach (ServiceInfo info in Enclosing_Instance.services.Values)
				{
					if (info.State == DNSState.ANNOUNCING_1)
					{
						info.task = this;
					}
				}
			}
		}
			
		public virtual void start()
		{
			// TODO: check this
			//Enclosing_Instance.timer.schedule(this, DNSConstants.ANNOUNCE_WAIT_INTERVAL, DNSConstants.ANNOUNCE_WAIT_INTERVAL);
			Enclosing_Instance.Timer = new Timer(new TimerCallback(this.Run), null, DNSConstants.ANNOUNCE_WAIT_INTERVAL, DNSConstants.ANNOUNCE_WAIT_INTERVAL);
		}
		public bool cancel()
		{
			// Remove association from host to this
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
				
			// TODO: check this - this is not completely correct
			//return base.cancel();
			return true;
		}
			
		public void Run(object state)
		{
			DNSOutgoing out_Renamed = null;
			try
			{
				// send probes for JmDNS itself
				if (Enclosing_Instance.State == taskState)
				{
					if (out_Renamed == null)
					{
						out_Renamed = new DNSOutgoing(DNSConstants.FLAGS_QR_RESPONSE | DNSConstants.FLAGS_AA);
					}
					DNSRecord answer = Enclosing_Instance.localHost.DNS4AddressRecord;
					if (answer != null)
						out_Renamed.AddAnswer(answer, 0);
					answer = Enclosing_Instance.localHost.DNS6AddressRecord;
					if (answer != null)
						out_Renamed.AddAnswer(answer, 0);
					Enclosing_Instance.AdvanceState();
				}
				// send announces for services
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
							logger.Debug("run() JmDNS announcing " + info.QualifiedName + " state " + info.State);
							if (out_Renamed == null)
							{
								out_Renamed = new DNSOutgoing(DNSConstants.FLAGS_QR_RESPONSE | DNSConstants.FLAGS_AA);
							}
							out_Renamed.AddAnswer(new Pointer(info.type, DNSConstants.TYPE_PTR, DNSConstants.CLASS_IN, DNSConstants.DNS_TTL, info.QualifiedName), 0);
							out_Renamed.AddAnswer(new Service(info.QualifiedName, DNSConstants.TYPE_SRV, DNSConstants.CLASS_IN, DNSConstants.DNS_TTL, info.priority, info.weight, info.port, Enclosing_Instance.localHost.Name), 0);
							out_Renamed.AddAnswer(new Text(info.QualifiedName, DNSConstants.TYPE_TXT, DNSConstants.CLASS_IN, DNSConstants.DNS_TTL, info.text), 0);
						}
					}
				}
				if (out_Renamed != null)
				{
					logger.Debug("run() JmDNS announcing #" + taskState);
					Enclosing_Instance.Send(out_Renamed);
				}
				else
				{
					// If we have nothing to send, another timer taskState ahead
					// of us has done the job for us. We can cancel.
					cancel();
				}
			}
			catch (Exception e)
			{
				logger.Warn("run() exception ", e);
				Enclosing_Instance.Recover();
			}
				
			taskState = taskState.Advance();
			if (!taskState.Announcing)
			{
				cancel();
					
				new Renewer(enclosingInstance).start();
			}
		}
	}
}
