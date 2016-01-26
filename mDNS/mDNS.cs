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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using mDNS.Logging;
using Windows.System.Threading;
using Windows.Networking.Sockets;
using System.Threading.Tasks;

namespace mDNS
{
	
	// REMIND: multiple IP addresses
	
	/// <summary> mDNS implementation in Java.
	/// 
	/// </summary>
	/// <author> 	Arthur van Hoff, Rick Blair, Jeff Sonstein, Werner Randelshofer, Pierre Frisch
	/// </author>
	/// <version>  	%I%, %G%
	/// </version>
	public class mDNS
	{
		private void InitBlock()
		{
			state = DNSState.PROBING_1;
		}
		/// <summary> Returns the current state of this info.</summary>
		virtual internal DNSState State
		{
			get
			{
				return state;
			}
			
		}
		/// <summary> Return the DNSCache associated with the cache variable</summary>
		virtual internal DNSCache Cache
		{
			get
			{
				return cache;
			}
			
		}
		
		internal IPAddress Group
		{
			get
			{
				return group;
			}
		}
		
		/// <summary> Return the HostName associated with this JmDNS instance.
		/// Note: May not be the same as what started.  The host name is subject to
		/// negotiation.
		/// </summary>
		virtual public string HostName
		{
			get
			{
				return localHost.Name;
			}
			
		}
		virtual public HostInfo LocalHost
		{
			get
			{
				return localHost;
			}
			
		}
		/// <summary> Return the address of the interface to which this instance of JmDNS is
		/// bound.
		/// </summary>
		virtual public IPAddress Interface
		{
			get
			{
				// TODO: return something here
				throw new NotImplementedException("Not yet ported from jmDNS");
			}
			
		}
		
		internal UdpClient Socket
		{
			get
			{
				return socket;
			}
		}
		
		internal object IOLock
		{
			get
			{
				return ioLock;
			}
		}
		
		internal long LastThrottleIncrement
		{
			get
			{
				return lastThrottleIncrement;
			}
			set
			{
				lastThrottleIncrement = value;
			}
		}
		
		internal DNSIncoming PlannedAnswer
		{
			get
			{
				return plannedAnswer;
			}
			set
			{
				plannedAnswer = value;
			}
		}
		
		internal SupportClass.ThreadClass Shutdown
		{
			get
			{
				return shutdown;
			}
			set
			{
				shutdown = value;
			}
		}
		
		internal object Task
		{
			get
			{
				return task;
			}
			set
			{
				task = value;
			}
		}
		
		internal int Throttle
		{
			get
			{
				return throttle;
			}
			set
			{
				throttle = value;
			}
		}
		
		internal Timer Timer
		{
			get
			{
				return timer;
			}
			set
			{
				timer = value;
			}
		}
		
		private static ILog logger;
		/// <summary> The version of JmDNS.</summary>
		public static string VERSION = "0.0.1";
		
		/// <summary> This is the multicast group, we are listening to for multicast DNS messages.</summary>
		private IPAddress group;
		/// <summary> This is our multicast socket.</summary>
		private UdpClient socket;
		
		
		/// <summary> Holds instances of JmDNS.DNSListener.
		/// Must by a synchronized collection, because it is updated from
		/// concurrent threads.
		/// </summary>
		private IList listeners;
		/// <summary> Holds instances of ServiceListener's.
		/// Keys are Strings holding a fully qualified service type.
		/// Values are LinkedList's of ServiceListener's.
		/// </summary>
		private IDictionary serviceListeners;
		/// <summary> Holds instances of ServiceTypeListener's.</summary>
		private IList typeListeners;
		
		
		/// <summary> Cache for DNSEntrys.</summary>
		private DNSCache cache;
		
		/// <summary> This hashtable holds the services that have been registered.
		/// Keys are instances of String which hold an all lower-case version of the
		/// fully qualified service name.
		/// Values are instances of ServiceInfo.
		/// </summary>
		internal IDictionary services;
		
		/// <summary> This hashtable holds the service types that have been registered or
		/// that have been received in an incoming datagram.
		/// Keys are instances of String which hold an all lower-case version of the
		/// fully qualified service type.
		/// Values hold the fully qualified service type.
		/// </summary>
		internal IDictionary serviceTypes;
		/// <summary> This is the shutdown hook, we registered with the java runtime.</summary>
		private SupportClass.ThreadClass shutdown;
		
		/// <summary> Handle on the local host
		/// 
		/// </summary>
		internal HostInfo localHost;
		
		private SupportClass.ThreadClass incomingListener = null;
		
		/// <summary> Throttle count.
		/// This is used to count the overall number of probes sent by JmDNS.
		/// When the last throttle increment happened .
		/// </summary>
		private int throttle;
		
		/// <summary> Last throttle increment.</summary>
		private long lastThrottleIncrement;
		
		/// <summary> The timer is used to dispatch all outgoing messages of JmDNS.
		/// It is also used to dispatch maintenance tasks for the DNS cache.
		/// </summary>
		private Timer timer;
		
		/// <summary> The source for random values.
		/// This is used to introduce random delays in responses. This reduces the
		/// potential for collisions on the network.
		/// </summary>
		private static readonly Random random = new Random();
		
		/// <summary> This lock is used to coordinate processing of incoming and outgoing
		/// messages. This is needed, because the Rendezvous Conformance Test
		/// does not forgive race conditions.
		/// </summary>
		private object ioLock = new Object();
		
		/// <summary> If an incoming package which needs an answer is truncated, we store it
		/// here. We add more incoming DNSRecords to it, until the JmDNS.Responder
		/// timer picks it up.
		/// Remind: This does not work well with multiple planned answers for packages
		/// that came in from different clients.
		/// </summary>
		private DNSIncoming plannedAnswer;
		
		// State machine
		/// <summary> The state of JmDNS.
		/// 
		/// For proper handling of concurrency, this variable must be
		/// changed only using methods advanceState(), revertState() and cancel().
		/// </summary>
		private DNSState state;
		
		/// <summary> Timer task associated to the host name.
		/// This is used to prevent from having multiple tasks associated to the host
		/// name at the same time.
		/// </summary>
		// TODO: need to replace task
		private object task;
		
		/// <summary> This hashtable is used to maintain a list of service types being collected
		/// by this JmDNS instance.
		/// The key of the hashtable is a service type name, the value is an instance
		/// of JmDNS.ServiceCollector.
		/// 
		/// </summary>
		/// <seealso cref="#list">
		/// </seealso>
		private Hashtable serviceCollectors = new Hashtable();

        private IPAddress _cachedAddress;
        private string _cachedName;

		/// <summary> Create an instance of JmDNS.</summary>
		public mDNS()
		{
			InitBlock();
			logger.Debug("JmDNS instance created");
			try
			{
                // TODO: check these
                IPAddress addr = Dns.GetLocalIp();
                _cachedAddress = IPAddress.IsLoopback(addr) ? null : addr;
                _cachedName = Dns.GetLocalHostName();

			}
			catch
			{
                _cachedName = "computer";
			}
		}
		

		/// <summary> Create an instance of JmDNS and bind it to a
		/// specific network interface given its IP-address.
		/// </summary>
		public mDNS(IPAddress addr)
		{
			InitBlock();
			try
			{
                //Init(addr, Dns.GetHostByAddress(addr.ToString()).HostName);
                // TODO: get hostname from address
                _cachedAddress = addr;
                _cachedName = Dns.GetLocalHostName();
            }
			catch
			{
                _cachedName = "computer";
               
			}
		}
		
		/// <summary> Initialize everything.
		/// 
		/// </summary>
		public async Task Init()
		{
            IPAddress address = _cachedAddress;
            string name = _cachedName;
            // A host name with "." is illegal. so strip off everything and append .local.
            int idx = name.IndexOf(".");
			if (idx > 0)
				name = name.Substring(0, (idx) - (0));
			name += ".local.";
			// localHost to IP address binding
			localHost = new HostInfo(address, name);
			
			cache = new DNSCache(100);
			
			listeners = ArrayList.Synchronized(new ArrayList());
			serviceListeners = new Hashtable();
			typeListeners = new ArrayList();
			
			services = Hashtable.Synchronized(new Hashtable(20));
			serviceTypes = Hashtable.Synchronized(new Hashtable(20));
			
			// REMIND: If I could pass in a name for the Timer thread,
			//         I would pass 'JmDNS.Timer'.
			//timer = new Timer();
			new RecordReaper(this).start();
			shutdown = new SupportClass.ThreadClass(new WorkItemHandler(new Shutdown(this).Run));
			
			// TODO: make this run at shutdown
			//Process.GetCurrentProcess().addShutdownHook(shutdown.Instance);
			
			incomingListener = new SupportClass.ThreadClass(new WorkItemHandler(new SocketListener(this).Run));
			
			// Bind to multicast socket
			await OpenMulticastSocket(localHost);
			Start(services.Values);
		}
		
		private void Start(ICollection serviceInfos)
		{
			state = DNSState.PROBING_1;
			incomingListener.Start();
			new Prober(this).start();
			foreach (ServiceInfo info in serviceInfos)
			{
				try
				{
					RegisterService(new ServiceInfo(info));
				}	
				catch (Exception exception)
				{
					logger.Warn("start() Registration exception ", exception);
				}
			}
		}
		private async Task OpenMulticastSocket(HostInfo hostInfo)
		{
			if (group == null)
			{
				// TODO: not going to resolve this, just going to set it directly
				//group = Dns.Resolve(DNSConstants.MDNS_GROUP).AddressList[0];
				group = IPAddress.Parse(DNSConstants.MDNS_GROUP);
			}
			if (socket != null)
			{
				this.CloseMulticastSocket();
			}
			socket = new UdpClient(DNSConstants.MDNS_PORT);
            await socket.Bind();

			socket.JoinMulticastGroup((IPAddress) group, 255);
		}
		
		private async Task CloseMulticastSocket()
		{
			logger.Debug("closeMulticastSocket()");
			if (socket != null)
			{
				// close socket
				try
				{
					socket.DropMulticastGroup((IPAddress) group);
					socket.Close();
					if (incomingListener != null)
						await incomingListener.Join();
				}
				catch (Exception exception)
				{
					logger.Warn("closeMulticastSocket() Close socket exception ", exception);
				}
				socket = null;
			}
		}
		
		// State machine
		/// <summary> Sets the state and notifies all objects that wait on JmDNS.</summary>
		internal virtual void AdvanceState()
		{
			lock (this)
			{
				state = state.Advance();
				Monitor.PulseAll(this);
			}
		}
		/// <summary> Sets the state and notifies all objects that wait on JmDNS.</summary>
		internal virtual void RevertState()
		{
			lock (this)
			{
				state = state.Revert();
				Monitor.PulseAll(this);
			}
		}
		/// <summary> Sets the state and notifies all objects that wait on JmDNS.</summary>
		internal virtual void Cancel()
		{
			lock (this)
			{
				state = DNSState.CANCELED;
				Monitor.PulseAll(this);
			}
		}
		
		/// <summary> Get service information. If the information is not cached, the method
		/// will block until updated information is received.
		/// <p>
		/// Usage note: Do not call this method from the AWT event dispatcher thread.
		/// You will make the user interface unresponsive.
		/// 
		/// </summary>
		/// <param name="type">fully qualified service type, such as <code>_http._tcp.local.</code> .
		/// </param>
		/// <param name="name">unqualified service name, such as <code>foobar</code> .
		/// </param>
		/// <returns> null if the service information cannot be obtained
		/// </returns>
		public virtual ServiceInfo GetServiceInfo(string type, string name)
		{
			return GetServiceInfo(type, name, 3 * 1000);
		}
		
		/// <summary> Get service information. If the information is not cached, the method
		/// will block for the given timeout until updated information is received.
		/// <p>
		/// Usage note: If you call this method from the AWT event dispatcher thread,
		/// use a small timeout, or you will make the user interface unresponsive.
		/// 
		/// </summary>
		/// <param name="type">full qualified service type, such as <code>_http._tcp.local.</code> .
		/// </param>
		/// <param name="name">unqualified service name, such as <code>foobar</code> .
		/// </param>
		/// <param name="timeout">timeout in milliseconds
		/// </param>
		/// <returns> null if the service information cannot be obtained
		/// </returns>
		public virtual ServiceInfo GetServiceInfo(string type, string name, int timeout)
		{
			ServiceInfo info = new ServiceInfo(type, name);
			new ServiceInfoResolver(this, info).start();
			
			try
			{
				long end = (DateTime.Now.Ticks - 621355968000000000) / 10000 + timeout;
				long delay;
				lock (info)
				{
					while (!info.HasData && (delay = end - (DateTime.Now.Ticks - 621355968000000000) / 10000) > 0)
					{
						Monitor.Wait(info, TimeSpan.FromMilliseconds(delay));
					}
				}
			}
			catch
			{
				// empty
			}
			
			return (info.HasData)?info:null;
		}
		
		/// <summary> Request service information. The information about the service is
		/// requested and the ServiceListener.resolveService method is called as soon
		/// as it is available.
		/// <p>
		/// Usage note: Do not call this method from the AWT event dispatcher thread.
		/// You will make the user interface unresponsive.
		/// 
		/// </summary>
		/// <param name="type">full qualified service type, such as <code>_http._tcp.local.</code> .
		/// </param>
		/// <param name="name">unqualified service name, such as <code>foobar</code> .
		/// </param>
		public virtual void RequestServiceInfo(string type, string name)
		{
			RequestServiceInfo(type, name, 3 * 1000);
		}
		
		/// <summary> Request service information. The information about the service is requested
		/// and the ServiceListener.resolveService method is called as soon as it is available.
		/// 
		/// </summary>
		/// <param name="type">full qualified service type, such as <code>_http._tcp.local.</code> .
		/// </param>
		/// <param name="name">unqualified service name, such as <code>foobar</code> .
		/// </param>
		/// <param name="timeout">timeout in milliseconds
		/// </param>
		public virtual void RequestServiceInfo(string type, string name, int timeout)
		{
			RegisterServiceType(type);
			ServiceInfo info = new ServiceInfo(type, name);
			new ServiceInfoResolver(this, info).start();
			
			try
			{
				long end = (DateTime.Now.Ticks - 621355968000000000) / 10000 + timeout;
				long delay;
				lock (info)
				{
					while (!info.HasData && (delay = end - (DateTime.Now.Ticks - 621355968000000000) / 10000) > 0)
					{
						Monitor.Wait(info, TimeSpan.FromMilliseconds(delay));
					}
				}
			}
			catch
			{
				// empty
			}
		}
		
		internal virtual void HandleServiceResolved(ServiceInfo info)
		{
			IList list = (IList) serviceListeners[info.type.ToLower()];
			if (list != null)
			{
				ServiceEvent event_Renamed = new ServiceEvent(this, info.type, info.getName(), info);
				// Iterate on a copy in case listeners will modify it
				ArrayList listCopy = new ArrayList(list);
				foreach (IServiceListener listener in listCopy)
				{
					listener.ServiceResolved(this, event_Renamed);
				}
			}
		}
		
		/// <summary> Listen for service types.</summary>
		/// <param name="listener">listener for service types
		/// </param>
		public event ServiceTypeListenerDelegate ServiceTypeListenerDelegateVar;
		protected virtual void  OnServiceType(ServiceEvent eventParam)
		{
			if (ServiceTypeListenerDelegateVar != null)
				ServiceTypeListenerDelegateVar(this, eventParam);
		}
		public virtual void AddServiceTypeListener(IServiceTypeListener listener)
		{
			lock (this)
			{
				typeListeners.Remove(listener);
				typeListeners.Add(listener);
			}
			
			foreach (String s in serviceTypes.Values)
			{
				listener.ServiceTypeAdded(this, new ServiceEvent(this, s, null, null));
			}
			
			new TypeResolver(this).start();
		}
		
		/// <summary> Remove listener for service types.</summary>
		/// <param name="listener">listener for service types
		/// </param>
		public virtual void RemoveServiceTypeListener(IServiceTypeListener listener)
		{
			lock (this)
			{
				typeListeners.Remove(listener);
			}
		}
		
		/// <summary> Listen for services of a given type. The type has to be a fully qualified
		/// type name such as <code>_http._tcp.local.</code>.
		/// </summary>
		/// <param name="type">full qualified service type, such as <code>_http._tcp.local.</code>.
		/// </param>
		/// <param name="listener">listener for service updates
		/// </param>
		public virtual void AddServiceListener(string type, IServiceListener listener)
		{
			string lotype = type.ToLower();
			RemoveServiceListener(lotype, listener);
			IList list = null;
			lock (this)
			{
				list = (IList) serviceListeners[lotype];
				if (list == null)
				{
					list = ArrayList.Synchronized(new ArrayList());
					serviceListeners[lotype] = list;
				}
				list.Add(listener);
			}
			
			// report cached service types
			foreach (DictionaryEntry entry in cache)
			{
				for (DNSCache.CacheNode n = (DNSCache.CacheNode)entry.Value; n != null; n = n.Next)
				{
					DNSRecord rec = (DNSRecord) n.Value;
					if (rec.type == DNSConstants.TYPE_SRV)
					{
						if (rec.name.EndsWith(type))
						{
							listener.ServiceAdded(this, new ServiceEvent(this, type, ToUnqualifiedName(type, rec.name), null));
						}
					}
				}
			}
			new ServiceResolver(this, type).start();
		}
		
		/// <summary> Remove listener for services of a given type.</summary>
		/// <param name="listener">listener for service updates
		/// </param>
		public virtual void RemoveServiceListener(string type, IServiceListener listener)
		{
			type = type.ToLower();
			IList list = (IList) serviceListeners[type];
			if (list != null)
			{
				lock (this)
				{
					list.Remove(listener);
					if (list.Count == 0)
					{
						serviceListeners.Remove(type);
					}
				}
			}
		}
		
		/// <summary> Register a service. The service is registered for access by other jmdns clients.
		/// The name of the service may be changed to make it unique.
		/// </summary>
		public virtual void RegisterService(ServiceInfo info)
		{
			RegisterServiceType(info.type);
			
			// bind the service to this address
			info.server = localHost.Name;
			info.addr = localHost.Address;
			
			lock (this)
			{
				makeServiceNameUnique(info);
				services[info.QualifiedName.ToLower()] = info;
			}
			
			new Prober(this).start();
			
			logger.Info("registerService() JmDNS registered service as " + info);
		}
		
		/// <summary> Unregister a service. The service should have been registered.</summary>
		public virtual void UnregisterService(ServiceInfo info)
		{
			lock (this)
			{
				services.Remove(info.QualifiedName.ToLower());
			}
			info.Cancel();
			
			// Note: We use this lock object to synchronize on it.
			//       Synchronizing on another object (e.g. the ServiceInfo) does
			//       not make sense, because the sole purpose of the lock is to
			//       wait until the canceler has finished. If we synchronized on
			//       the ServiceInfo or on the Canceler, we would block all
			//       accesses to synchronized methods on that object. This is not
			//       what we want!
			object lock_Renamed = new Object();
			new Canceler(this, info, lock_Renamed).start();
			
			// Remind: We get a deadlock here, if the Canceler does not run!
			try
			{
				lock (lock_Renamed)
				{
					Monitor.Wait(lock_Renamed);
				}
			}
			catch
			{
				// empty
			}
		}
		
		/// <summary> Unregister all services.</summary>
		public virtual void UnregisterAllServices()
		{
			logger.Debug("unregisterAllServices()");
			if (services.Count == 0)
			{
				return ;
			}
			
			ICollection list;
			lock (this)
			{
				list = new ArrayList(services.Values);
				services.Clear();
			}
			foreach (ServiceInfo info in list)
			{
					info.Cancel();
			}
			object lock_Renamed = new Object();
			new Canceler(this, list, lock_Renamed).start();
			
			// Remind: We get a livelock here, if the Canceler does not run!
			try
			{
				lock (lock_Renamed)
				{
					Monitor.Wait(lock_Renamed);
				}
			}
			catch
			{
				// empty
			}
		}
		
		/// <summary> Register a service type. If this service type was not already known,
		/// all service listeners will be notified of the new service type. Service types
		/// are automatically registered as they are discovered.
		/// </summary>
		public virtual void RegisterServiceType(string type)
		{
			string name = type.ToLower();
			if (serviceTypes[name] == null)
			{
				if ((type.IndexOf("._mdns._udp.") < 0) && !type.EndsWith(".in-addr.arpa."))
				{
					ICollection list;
					lock (this)
					{
						serviceTypes[name] = type;
						list = new ArrayList(typeListeners);
					}

					foreach (IServiceTypeListener listener in list)
					{
						listener.ServiceTypeAdded(this, new ServiceEvent(this, type, null, null));
					}
				}
			}
		}
		
		/// <summary> Generate a possibly unique name for a host using the information we
		/// have in the cache.
		/// 
		/// </summary>
		/// <returns> returns true, if the name of the host had to be changed.
		/// </returns>
		private bool MakeHostNameUnique(Address host)
		{
			string originalName = host.Name;
			long now = (DateTime.Now.Ticks - 621355968000000000) / 10000;
			
			bool collision;
			do 
			{
				collision = false;
				
				// Check for collision in cache
				for (DNSCache.CacheNode j = cache.find(host.Name.ToLower()); j != null; j = j.Next)
				{
					DNSRecord a = (DNSRecord) j.Value;
					if (false) // TODO: huh?
					{
						host.name = IncrementName(host.Name);
						collision = true;
						break;
					}
				}
			}
			while (collision);
			
			if (originalName.Equals(host.Name))
			{
				return false;
			}
			else
			{
				return true;
			}
		}
		/// <summary> Generate a possibly unique name for a service using the information we
		/// have in the cache.
		/// 
		/// </summary>
		/// <returns> returns true, if the name of the service info had to be changed.
		/// </returns>
		private bool makeServiceNameUnique(ServiceInfo info)
		{
			string originalQualifiedName = info.QualifiedName;
			long now = (DateTime.Now.Ticks - 621355968000000000) / 10000;
			
			bool collision;
			do 
			{
				collision = false;
				
				// Check for collision in cache
				for (DNSCache.CacheNode j = cache.find(info.QualifiedName.ToLower()); j != null; j = j.Next)
				{
					DNSRecord a = (DNSRecord) j.Value;
					if ((a.type == DNSConstants.TYPE_SRV) && !a.IsExpired(now))
					{
						Service s = (Service) a;
						if (s.port != info.port || !s.server.Equals(localHost.Name))
						{
							logger.Debug("makeServiceNameUnique() JmDNS.makeServiceNameUnique srv collision:" + a + " s.server=" + s.server + " " + localHost.Name + " equals:" + (s.server.Equals(localHost.Name)));
							info.SetName(IncrementName(info.getName()));
							collision = true;
							break;
						}
					}
				}
				
				// Check for collision with other service infos published by JmDNS
				object selfService = services[info.QualifiedName.ToLower()];
				if (selfService != null && selfService != info)
				{
					info.SetName(IncrementName(info.getName()));
					collision = true;
				}
			}
			while (collision);
			
			return !(originalQualifiedName.Equals(info.QualifiedName));
		}
		internal virtual string IncrementName(string name)
		{
			try
			{
				int l = name.LastIndexOf('(');
				int r = name.LastIndexOf(')');
				if ((l >= 0) && (l < r))
				{
					name = name.Substring(0, (l) - (0)) + "(" + (Int32.Parse(name.Substring(l + 1, (r) - (l + 1))) + 1) + ")";
				}
				else
				{
					name += " (2)";
				}
			}
			catch
			{
				name += " (2)";
			}
			return name;
		}
		
		/// <summary> Add a listener for a question. The listener will receive updates
		/// of answers to the question as they arrive, or from the cache if they
		/// are already available.
		/// </summary>
		internal virtual void AddListener(IDNSListener listener, DNSQuestion question)
		{
			long now = (DateTime.Now.Ticks - 621355968000000000) / 10000;
			
			// add the new listener
			lock (this)
			{
				listeners.Add(listener);
			}
			
			// report existing matched records
			if (question != null)
			{
				for (DNSCache.CacheNode i = cache.find(question.name); i != null; i = i.Next)
				{
					DNSRecord c = (DNSRecord) i.Value;
					if (question.IsAnsweredBy(c) && !c.IsExpired(now))
					{
						listener.UpdateRecord(this, now, c);
					}
				}
			}
		}
		
		/// <summary> Remove a listener from all outstanding questions. The listener will no longer
		/// receive any updates.
		/// </summary>
		internal virtual void RemoveListener(IDNSListener listener)
		{
			lock (this)
			{
				listeners.Remove(listener);
			}
		}
		
		
		// Remind: Method updateRecord should receive a better name.
		/// <summary> Notify all listeners that a record was updated.</summary>
		internal virtual void UpdateRecord(long now, DNSRecord rec)
		{
			// We do not want to block the entire DNS while we are updating the record for each listener (service info)
			IList listenerList = null;
			lock (this)
			{
				listenerList = new ArrayList(listeners);
			}
			foreach (IDNSListener listener in listenerList)
			{
				listener.UpdateRecord(this, now, rec);
			}
			if (rec.type == DNSConstants.TYPE_PTR || rec.type == DNSConstants.TYPE_SRV)
			{
				IList serviceListenerList = null;
				lock (this)
				{
					serviceListenerList = (IList) serviceListeners[rec.name.ToLower()];
					// Iterate on a copy in case listeners will modify it
					if (serviceListenerList != null)
						serviceListenerList = new ArrayList(serviceListenerList);
				}
				if (serviceListenerList != null)
				{
					bool expired = rec.IsExpired(now);
					string type = rec.Name;
					string name = ((Pointer) rec).Alias;
					// DNSRecord old = (DNSRecord)services.get(name.toLowerCase());
					if (!expired)
					{
						// new record
						ServiceEvent event_Renamed = new ServiceEvent(this, type, ToUnqualifiedName(type, name), null);
						foreach (IServiceListener listener in serviceListenerList)
						{
							listener.ServiceAdded(this, event_Renamed);
						}
					}
					else
					{
						// expire record
						ServiceEvent event_Renamed = new ServiceEvent(this, type, ToUnqualifiedName(type, name), null);
						foreach (IServiceListener listener in serviceListenerList)
						{
							listener.ServiceRemoved(this, event_Renamed);
						}
					}
				}
			}
		}
		
		/// <summary> Handle an incoming response. Cache answers, and pass them on to
		/// the appropriate questions.
		/// </summary>
		internal void HandleResponse(DNSIncoming msg)
		{
			long now = (DateTime.Now.Ticks - 621355968000000000) / 10000;
			
			bool hostConflictDetected = false;
			bool serviceConflictDetected = false;
			
			for (int recIdx = 0; recIdx < msg.answers.Count; recIdx++)
			{
				DNSRecord rec = (DNSRecord) msg.answers[recIdx];
				bool isInformative = false;
				bool expired = rec.IsExpired(now);
				
				// update the cache
				DNSRecord c = (DNSRecord) cache.get_Renamed(rec);
				if (c != null)
				{
					if (expired)
					{
						isInformative = true;
						cache.remove(c);
					}
					else
					{
						c.ResetTTL(rec);
						rec = c;
						msg.answers[recIdx] = c; // put back into collection
					}
				}
				else
				{
					if (!expired)
					{
						isInformative = true;
						cache.add(rec);
					}
				}
				switch (rec.type)
				{
					
					case DNSConstants.TYPE_PTR: 
						// handle _mdns._udp records
						if (rec.Name.IndexOf("._mdns._udp.") >= 0)
						{
							if (!expired && rec.name.StartsWith("_services._mdns._udp."))
							{
								isInformative = true;
								RegisterServiceType(((Pointer) rec).alias);
							}
							continue;
						}
						RegisterServiceType(rec.name);
						break;
					}
				
				if ((rec.GetEntryType() == DNSConstants.TYPE_A) || (rec.GetEntryType() == DNSConstants.TYPE_AAAA))
				{
					hostConflictDetected |= rec.HandleResponse(this);
				}
				else
				{
					serviceConflictDetected |= rec.HandleResponse(this);
				}
				
				// notify the listeners
				if (isInformative)
				{
					UpdateRecord(now, rec);
				}
			}
			
			if (hostConflictDetected || serviceConflictDetected)
			{
				new Prober(this).start();
			}
		}
		
		/// <summary> Handle an incoming query. See if we can answer any part of it
		/// given our service infos.
		/// </summary>
		internal void HandleQuery(DNSIncoming in_Renamed, IPAddress addr, int port)
		{
			// Track known answers
			bool hostConflictDetected = false;
			bool serviceConflictDetected = false;
			long expirationTime = (DateTime.Now.Ticks - 621355968000000000) / 10000 + DNSConstants.KNOWN_ANSWER_TTL;
			foreach (DNSRecord answer in in_Renamed.answers)
			{
				if ((answer.GetEntryType() == DNSConstants.TYPE_A) || (answer.GetEntryType() == DNSConstants.TYPE_AAAA))
				{
					hostConflictDetected |= answer.HandleQuery(this, expirationTime);
				}
				else
				{
					serviceConflictDetected |= answer.HandleQuery(this, expirationTime);
				}
			}
			
			if (plannedAnswer != null)
			{
				plannedAnswer.Append(in_Renamed);
			}
			else
			{
				if (in_Renamed.Truncated)
				{
					plannedAnswer = in_Renamed;
				}
				
				new Responder(this, in_Renamed, addr, port).start();
			}
			
			if (hostConflictDetected || serviceConflictDetected)
			{
				new Prober(this).start();
			}
		}
		
		/// <summary> Add an answer to a question. Deal with the case when the
		/// outgoing packet overflows
		/// </summary>
		internal virtual DNSOutgoing AddAnswer(DNSIncoming in_Renamed, IPAddress addr, int port, DNSOutgoing out_Renamed, DNSRecord rec)
		{
			if (out_Renamed == null)
			{
				out_Renamed = new DNSOutgoing(DNSConstants.FLAGS_QR_RESPONSE | DNSConstants.FLAGS_AA);
			}
			try
			{
				out_Renamed.AddAnswer(in_Renamed, rec);
			}
			catch
			{
				out_Renamed.flags |= DNSConstants.FLAGS_TC;
				out_Renamed.id = in_Renamed.id;
				out_Renamed.Finish();
				Send(out_Renamed);
				
				out_Renamed = new DNSOutgoing(DNSConstants.FLAGS_QR_RESPONSE | DNSConstants.FLAGS_AA);
				out_Renamed.AddAnswer(in_Renamed, rec);
			}
			return out_Renamed;
		}
		
		
		/// <summary> Send an outgoing multicast DNS message.</summary>
		internal async Task Send(DNSOutgoing out_Renamed)
		{
			out_Renamed.Finish();
			if (!out_Renamed.Empty)
			{
				SupportClass.PacketSupport packet = new SupportClass.PacketSupport(SupportClass.ToByteArray(out_Renamed.data), out_Renamed.off, new IPEndPoint(group, DNSConstants.MDNS_PORT));
				
				try
				{
					DNSIncoming msg = new DNSIncoming(packet);
					logger.Debug("send() JmDNS out:" + msg.Print(true));
				}
				catch (IOException e)
				{
					logger.Error("send(DNSOutgoing) - JmDNS can not parse what it sends!!!", e);
				}
				await SupportClass.UdpClientSupport.Send(socket, packet);
			}
		}

		/// <summary> Recover jmdns when there is an error.</summary>
		protected internal virtual void Recover()
		{
			logger.Debug("recover()");
			// We have an IO error so lets try to recover if anything happens lets close it.
			// This should cover the case of the IP address changing under our feet
			if (DNSState.CANCELED != state)
			{
				lock (this)
				{
					// Synchronize only if we are not already in process to prevent dead locks
					//
					logger.Debug("recover() Cleanning up");
					// Stop JmDNS
					state = DNSState.CANCELED; // This protects against recursive calls
					
					// We need to keep a copy for reregistration
					ICollection oldServiceInfos = new ArrayList(services.Values);
					
					// Cancel all services
					UnregisterAllServices();
					DisposeServiceCollectors();
					//
					// close multicast socket
					CloseMulticastSocket();
					//
					cache.clear();
					logger.Debug("recover() All is clean");
					//
					// All is clear now start the services
					//
					try
					{
						OpenMulticastSocket(localHost);
						Start(oldServiceInfos);
					}
					catch (Exception exception)
					{
						logger.Warn("recover() Start services exception ", exception);
					}
					logger.Warn("recover() We are back!");
				}
			}
		}
		
		/// <summary> Close down jmdns. Release all resources and unregister all services.</summary>
		public virtual async Task Close()
		{
			if (state != DNSState.CANCELED)
			{
                Task t = CloseMulticastSocket();
                lock (this)
				{
					// Synchronize only if we are not already in process to prevent dead locks
					// Stop JmDNS
					state = DNSState.CANCELED; // This protects against recursive calls
					
					UnregisterAllServices();
					DisposeServiceCollectors();
					
					// close socket
					
					// Stop the timer
					// TODO: how to replace this?
					//timer.cancel();
               
					
					// remove the shutdown hook
					if (shutdown != null)
					{
						// TODO: need to replace this??
						//Process.GetCurrentProcess().removeShutdownHook(shutdown.Instance);
					}
				}
                await t;
			}
		}
		
		/// <summary> List cache entries, for debugging only.</summary>
		internal virtual void Print()
		{
			Console.WriteLine("---- cache ----");
			cache.print();
			Console.WriteLine();
		}
		/// <summary> List Services and serviceTypes.
		/// Debugging Only
		/// 
		/// </summary>
		
		public virtual void PrintServices()
		{
			// TODO : log this?
			Console.WriteLine(ToString());
		}
		
		public override string ToString()
		{
			StringBuilder aLog = new StringBuilder();
			aLog.Append("\t---- Services -----");
			if (services != null)
			{
				foreach (object key in services.Keys)
				{
					aLog.Append("\n\t\tService: " + key + ": " + services[key]);
				}
			}
			aLog.Append("\n");
			aLog.Append("\t---- Types ----");
			if (serviceTypes != null)
			{
				foreach (object key in serviceTypes.Keys)
				{
					aLog.Append("\n\t\tType: " + key + ": " + serviceTypes[key]);
				}
			}
			aLog.Append("\n");
			aLog.Append(cache.ToString());
			aLog.Append("\n");
			aLog.Append("\t---- Service Collectors ----");
			if (serviceCollectors != null)
			{
				lock (serviceCollectors.SyncRoot)
				{
					foreach (object key in serviceCollectors.Keys)
					{
						aLog.Append("\n\t\tService Collector: " + key + ": " + serviceCollectors[key]);
					}
					serviceCollectors.Clear();
				}
			}
			return aLog.ToString();
		}
		
		/// <summary> Returns a list of service infos of the specified type.
		/// 
		/// </summary>
		/// <param name="type">Service type name, such as <code>_http._tcp.local.</code>.
		/// </param>
		/// <returns> An array of service instance names.
		/// </returns>
		public async virtual System.Threading.Tasks.Task<ServiceInfo[]> List(string type)
		{
			// Implementation note: The first time a list for a given type is
			// requested, a ServiceCollector is created which collects service
			// infos. This greatly speeds up the performance of subsequent calls
			// to this method. The caveats are, that 1) the first call to this method
			// for a given type is slow, and 2) we spawn a ServiceCollector
			// instance for each service type which increases network traffic a
			// little.
			
			ServiceCollector collector;
			
			bool newCollectorCreated;
			lock (serviceCollectors.SyncRoot)
			{
				// TODO: check this
				collector = (ServiceCollector) serviceCollectors[type];
				if (collector == null)
				{
					collector = new ServiceCollector(type);
					serviceCollectors[type] = collector;
					AddServiceListener(type, collector);
					newCollectorCreated = true;
				}
				else
				{
					newCollectorCreated = false;
				}
			}
			
			// After creating a new ServiceCollector, we collect service infos for
			// 200 milliseconds. This should be enough time, to get some service
			// infos from the network.
			if (newCollectorCreated)
			{
				try
				{
                    await System.Threading.Tasks.Task.Delay(new TimeSpan((System.Int64)10000 * 200));
				}
				catch
				{
				}
			}
			
			return collector.list();
		}
		
		/// <summary> This method disposes all ServiceCollector instances which have been
		/// created by calls to method <code>list(type)</code>.
		/// 
		/// </summary>
		/// <seealso cref="#list">
		/// </seealso>
		private void  DisposeServiceCollectors()
		{
			logger.Debug("disposeServiceCollectors()");
			lock (serviceCollectors.SyncRoot)
			{
				foreach (ServiceCollector collector in serviceCollectors.Values)
				{
					RemoveServiceListener(collector.type, collector);
				}
				serviceCollectors.Clear();
			}
		}
		
		private static string ToUnqualifiedName(string type, string qualifiedName)
		{
			if (qualifiedName.EndsWith(type))
			{
				return qualifiedName.Substring(0, (qualifiedName.Length - type.Length - 1) - (0));
			}
			else
			{
				return qualifiedName;
			}
		}
		static mDNS()
		{
			logger = LogManager.GetLogger(typeof(mDNS).ToString());
		}
	}
}
