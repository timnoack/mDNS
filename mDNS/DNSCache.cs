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
using System.Text;
using mDNS.Logging;

namespace mDNS
{
	
	/// <summary> A table of DNS entries. This is a hash table which
	/// can handle multiple entries with the same name.
	/// <p>
	/// Storing multiple entries with the same name is implemented using a
	/// linked list of <code>CacheNode</code>'s.
	/// <p>
	/// The current implementation of the API of DNSCache does expose the
	/// cache nodes to clients. Clients must explicitly deal with the nodes
	/// when iterating over entries in the cache. Here's how to iterate over
	/// all entries in the cache:
	/// <pre>
	/// for (Iterator i=dnscache.iterator(); i.hasNext(); ) {
	/// for (DNSCache.CacheNode n = (DNSCache.CacheNode) i.next(); n != null; n.next()) {
	/// DNSEntry entry = n.getValue();
	/// ...do something with entry...
	/// }
	/// }
	/// </pre>
	/// <p>
	/// And here's how to iterate over all entries having a given name:
	/// <pre>
	/// for (DNSCache.CacheNode n = (DNSCache.CacheNode) dnscache.find(name); n != null; n.next()) {
	/// DNSEntry entry = n.getValue();
	/// ...do something with entry...
	/// }
	/// </pre>
	/// 
	/// </summary>
	class DNSCache : IEnumerable
	{
		private static ILog logger;
		// Implementation note:
		// We might completely hide the existence of CacheNode's in a future version
		// of DNSCache. But this will require to implement two (inner) classes for
		// the  iterators that will be returned by method <code>iterator()</code> and
		// method <code>find(name)</code>.
		// Since DNSCache is not a public class, it does not seem worth the effort
		// to clean its API up that much.
		
		// [PJYF Oct 15 2004] This should implements Collections that would be amuch cleaner implementation
		
		/// <summary> The number of DNSEntry's in the cache.</summary>
		private int size;
		
		/// <summary> The hashtable used internally to store the entries of the cache.
		/// Keys are instances of String. The String contains an unqualified service
		/// name.
		/// Values are linked lists of CacheNode instances.
		/// </summary>
		private Hashtable hashtable;
		
		/// <summary> Cache nodes are used to implement storage of multiple DNSEntry's of the
		/// same name in the cache.
		/// </summary>
		public class CacheNode
		{
			virtual public DNSEntry Value
			{
				get
				{
					return value;
				}
				
			}
			public virtual CacheNode Next
			{
				get
				{
					return next;
				}
				set
				{
					next = value;
				}
			}
			private static ILog logger;
			private DNSEntry value;
			private CacheNode next;
			public CacheNode(DNSEntry value)
			{
				this.value = value;
			}
			static CacheNode()
			{
				logger = LogManager.GetLogger(typeof(CacheNode).ToString());
			}
		}
		
		
		/// <summary> Create a table with a given initial size.</summary>
		public DNSCache(int size)
		{
			hashtable = new Hashtable(size);
		}
		
		/// <summary> Clears the cache.</summary>
		public virtual void  clear()
		{
			lock (this)
			{
				hashtable.Clear();
				size = 0;
			}
		}
		
		/// <summary> Adds an entry to the table.</summary>
		public virtual void  add(DNSEntry entry)
		{
			lock (this)
			{
				//logger.log("DNSCache.add("+entry.getName()+")");
				CacheNode newValue = new CacheNode(entry);
				CacheNode node = (CacheNode) hashtable[entry.Name];
				if (node == null)
				{
					hashtable[entry.Name] = newValue;
				}
				else
				{
					newValue.Next = node.Next;
					node.Next = newValue;
				}
				size++;
			}
		}
		
		/// <summary> Remove a specific entry from the table. Returns true if the
		/// entry was found.
		/// </summary>
		public virtual bool remove(DNSEntry entry)
		{
			lock (this)
			{				
				CacheNode node = (CacheNode) hashtable[entry.Name];
				if (node != null)
				{
					if (node.Value == entry)
					{
						if (node.Next == null)
						{
							hashtable.Remove(entry.Name);
						}
						else
						{
							hashtable[entry.Name] = node.Next;
						}
						size--;
						return true;
					}
					
					CacheNode previous = node;
					node = node.Next;
					while (node != null)
					{
						if (node.Value == entry)
						{
							previous.Next = node.Next;
							size--;
							return true;
						}
						previous = node;
						node = node.Next;
					} ;
				}
				return false;
			}
		}
		
		/// <summary> Get a matching DNS entry from the table (using equals).
		/// Returns the entry that was found.
		/// </summary>
		public virtual DNSEntry get_Renamed(DNSEntry entry)
		{
			lock (this)
			{
				for (CacheNode node = find(entry.Name); node != null; node = node.Next)
				{
					if (node.Value.Equals(entry))
					{
						return node.Value;
					}
				}
				return null;
			}
		}
		
		/// <summary> Get a matching DNS entry from the table.</summary>
		public virtual DNSEntry get_Renamed(string name, int type, int clazz)
		{
			lock (this)
			{
				for (CacheNode node = find(name); node != null; node = node.Next)
				{
					if (node.Value.type == type && node.Value.clazz == clazz)
					{
						return node.Value;
					}
				}
				return null;
			}
		}
		
		/// <summary> Iterates over all cache nodes.
		/// The iterator returns instances of DNSCache.CacheNode.
		/// Each instance returned is the first node of a linked list.
		/// To retrieve all entries, one must iterate over this linked list. See
		/// code snippets in the header of the class.
		/// </summary>
		public virtual IEnumerator iterator()
		{
			return ArrayList.ReadOnly(new ArrayList(hashtable.Values)).GetEnumerator();
		}
		
		/// <summary> Iterate only over items with matching name.
		/// Returns an instance of DNSCache.CacheNode or null.
		/// If an instance is returned, it is the first node of a linked list.
		/// To retrieve all entries, one must iterate over this linked list.
		/// </summary>
		public virtual CacheNode find(string name)
		{
			lock (this)
			{
				return (CacheNode) hashtable[name];
			}
		}
		
		/// <summary> List all entries for debugging.</summary>
		public virtual void print()
		{
			lock (this)
			{
				foreach (CacheNode node in hashtable)
					for (CacheNode n = node; n != null; n = n.Next)
						Console.WriteLine(n.Value);
			}
		}
		
		public override string ToString()
		{
			StringBuilder aLog = new StringBuilder();
			aLog.Append("\t---- cache ----");
			foreach (CacheNode node in hashtable)
				for (CacheNode n = node; n != null; n = n.Next)
					aLog.Append("\n\t\t" + n.Value);
			return aLog.ToString();
		}

		static DNSCache()
		{
			logger = LogManager.GetLogger(typeof(DNSCache));
		}

		public class DNSCacheEnumerator : IEnumerator
		{
			private IDictionaryEnumerator innerEnumerator;
			// TODO: what should the accessibilty be on this constructor?
			public DNSCacheEnumerator(IDictionaryEnumerator innerCollection)
			{
				this.innerEnumerator = innerCollection;
			}

			public void Reset()
			{
				innerEnumerator.Reset();
			}

			public object Current
			{
				get
				{
					return innerEnumerator.Current;
				}
			}

			public bool MoveNext()
			{
				return innerEnumerator.MoveNext();
			}
		}

		public IEnumerator GetEnumerator()
		{
			return new DNSCacheEnumerator(this.hashtable.GetEnumerator());
		}
	}
}
