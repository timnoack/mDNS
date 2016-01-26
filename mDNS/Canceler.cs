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
	/// <summary> The Canceler sends two announces with TTL=0 for the specified services.</summary>
	// TODO: check this
	internal class Canceler /*: IThreadRunnable :TimerTask*/
	{
		private ILog logger = LogManager.GetLogger("Canceler");
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
		/// <summary> Counts the number of announces being sent.</summary>
		internal int count = 0;
		/// <summary> The services that need cancelling.
		/// Note: We have to use a local variable here, because the services
		/// that are canceled, are removed immediately from variable JmDNS.services.
		/// </summary>
		private ServiceInfo[] infos;
		/// <summary> We call notifyAll() on the lock object, when we have canceled the
		/// service infos.
		/// This is used by method JmDNS.unregisterService() and
		/// JmDNS.unregisterAllServices, to ensure that the JmDNS
		/// socket stays open until the Canceler has canceled all services.
		/// 
		/// Note: We need this lock, because ServiceInfos do the transition from
		/// state ANNOUNCED to state CANCELED before we get here. We could get
		/// rid of this lock, if we added a state named CANCELLING to DNSState.
		/// </summary>
		private object lock_Renamed;
		internal int ttl = 0;
		public Canceler(mDNS enclosingInstance, ServiceInfo info, object lock_Renamed)
		{
			InitBlock(enclosingInstance);
			this.infos = new ServiceInfo[]{info};
			this.lock_Renamed = lock_Renamed;
			Enclosing_Instance.AddListener(info, new DNSQuestion(info.QualifiedName, DNSConstants.TYPE_ANY, DNSConstants.CLASS_IN));
		}
		public Canceler(mDNS enclosingInstance, ServiceInfo[] infos, object lock_Renamed)
		{
			InitBlock(enclosingInstance);
			this.infos = infos;
			this.lock_Renamed = lock_Renamed;
		}
		public Canceler(mDNS enclosingInstance, ICollection infos, object lock_Renamed)
		{
			InitBlock(enclosingInstance);
			this.infos = (ServiceInfo[]) SupportClass.ICollectionSupport.ToArray(infos, new ServiceInfo[infos.Count]);
			this.lock_Renamed = lock_Renamed;
		}
		public virtual void  start()
		{
			// TODO: check this
			Enclosing_Instance.Timer = new Timer(new TimerCallback(this.Run), null, 0, DNSConstants.ANNOUNCE_WAIT_INTERVAL);
		}
			
		public void Run(object state)
		{
			try
			{
				if (++count < 3)
				{
					logger.Debug("run() JmDNS canceling service");
					// announce the service
					DNSOutgoing out_Renamed = new DNSOutgoing(DNSConstants.FLAGS_QR_RESPONSE | DNSConstants.FLAGS_AA);
					for (int i = 0; i < infos.Length; i++)
					{
						ServiceInfo info = infos[i];
						out_Renamed.AddAnswer(new Pointer(info.type, DNSConstants.TYPE_PTR, DNSConstants.CLASS_IN, ttl, info.QualifiedName), 0);
						out_Renamed.AddAnswer(new Service(info.QualifiedName, DNSConstants.TYPE_SRV, DNSConstants.CLASS_IN, ttl, info.priority, info.weight, info.port, Enclosing_Instance.localHost.Name), 0);
						out_Renamed.AddAnswer(new Text(info.QualifiedName, DNSConstants.TYPE_TXT, DNSConstants.CLASS_IN, ttl, info.text), 0);
						DNSRecord answer = Enclosing_Instance.localHost.DNS4AddressRecord;
						if (answer != null)
							out_Renamed.AddAnswer(answer, 0);
						answer = Enclosing_Instance.localHost.DNS6AddressRecord;
						if (answer != null)
							out_Renamed.AddAnswer(answer, 0);
					}
					Enclosing_Instance.Send(out_Renamed);
				}
				else
				{
					// After three successful announcements, we are finished.
					lock (lock_Renamed)
					{
						Monitor.PulseAll(lock_Renamed);
					}
					// TODO: omit cancel?
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
