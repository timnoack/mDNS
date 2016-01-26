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
	/// <summary> Periodicaly removes expired entries from the cache.</summary>
	// TODO: check this
	internal class RecordReaper /*: IThreadRunnable /*: TimerTask*/
	{
		private ILog logger = LogManager.GetLogger("RecordReaper");
		public RecordReaper(mDNS enclosingInstance)
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
			// TODO: check this
			//Enclosing_Instance.timer.schedule(this, DNSConstants.RECORD_REAPER_INTERVAL, DNSConstants.RECORD_REAPER_INTERVAL);
			Enclosing_Instance.Timer = new Timer(new TimerCallback(this.Run), null, DNSConstants.RECORD_REAPER_INTERVAL, DNSConstants.RECORD_REAPER_INTERVAL);
		}
		public void Run(object state)
		{
			lock (Enclosing_Instance)
			{
				if (Enclosing_Instance.State == DNSState.CANCELED)
				{
					return ;
				}
				logger.Debug("run() JmDNS reaping cache");
					
				// Remove expired answers from the cache
				// -------------------------------------
				// To prevent race conditions, we defensively copy all cache
				// entries into a list.
				IList list = new ArrayList();
				lock (Enclosing_Instance.Cache)
				{
					foreach (DictionaryEntry entry in Enclosing_Instance.Cache)
					{
						DNSCache.CacheNode node = (DNSCache.CacheNode)entry.Value;
						for (DNSCache.CacheNode n = node; n != null; n = n.Next)
						{
							list.Add(n.Value);
						}
					}
				}
				// Now, we remove them.
				long now = (DateTime.Now.Ticks - 621355968000000000) / 10000;
				foreach (DNSRecord c in list)
				{
					if (c.IsExpired(now))
					{
						Enclosing_Instance.UpdateRecord(now, c);
						Enclosing_Instance.Cache.remove(c);
					}
				}
			}
		}
	}
}
