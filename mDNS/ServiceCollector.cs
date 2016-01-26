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
using System.Text;
using System.Collections;
using mDNS.Logging;

namespace mDNS
{
	/// <summary> Instances of ServiceCollector are used internally to speed up the
	/// performance of method <code>list(type)</code>.
	/// 
	/// </summary>
	/// <seealso cref="#list">
	/// </seealso>
	internal class ServiceCollector : IServiceListener
	{
		private static ILog logger;
		/// <summary> A set of collected service instance names.</summary>
		private IDictionary infos = Hashtable.Synchronized(new Hashtable());
			
		public string type;
		public ServiceCollector(string type)
		{
			this.type = type;
		}
			
		/// <summary>A service has been added. </summary>
		public virtual void ServiceAdded(object event_sender, ServiceEvent event_Renamed)
		{
			lock (infos.SyncRoot)
			{
				event_Renamed.DNS.RequestServiceInfo(event_Renamed.Type, event_Renamed.Name, 0);
			}
		}
			
		/// <summary>A service has been removed. </summary>
		public virtual void ServiceRemoved(object event_sender, ServiceEvent event_Renamed)
		{
			lock (infos.SyncRoot)
			{
				infos.Remove(event_Renamed.Name);
			}
		}
			
		/// <summary> A service hase been resolved. Its details are now available in the
		/// ServiceInfo record.
		/// </summary>
		public virtual void ServiceResolved(object event_sender, ServiceEvent event_Renamed)
		{
			lock (infos.SyncRoot)
			{
				infos[event_Renamed.Name] = event_Renamed.Info;
			}
		}
			
		/// <summary> Returns an array of all service infos which have been collected by this
		/// ServiceCollector.
		/// </summary>
		public virtual ServiceInfo[] list()
		{
			lock (infos.SyncRoot)
			{
				return (ServiceInfo[]) SupportClass.ICollectionSupport.ToArray(infos.Values, new ServiceInfo[infos.Count]);
			}
		}
			
		public override string ToString()
		{
			StringBuilder aLog = new StringBuilder();
			lock (infos.SyncRoot)
			{
				foreach (object key in infos.Keys)
				{
					aLog.Append("\n\t\tService: " + key + ": " + infos[key]);
				}
			}
			return aLog.ToString();
		}
		static ServiceCollector()
		{
			logger = LogManager.GetLogger(typeof(ServiceCollector).ToString());
		}
	}
}
