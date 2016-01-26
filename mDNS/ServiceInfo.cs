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
using System.Text;
using System.Threading;
using mDNS.Logging;

namespace mDNS
{
	
	/// <summary> JmDNS service information.
	/// 
	/// </summary>
	/// <author> 	Arthur van Hoff, Jeff Sonstein, Werner Randelshofer
	/// </author>
	/// <version>  	%I%, %G%
	/// </version>
	public class ServiceInfo : IDNSListener
	{
		private void  InitBlock()
		{
			state = DNSState.PROBING_1;
		}
		/// <summary> Fully qualified service type name, such as <code>_http._tcp.local.</code> .</summary>
		virtual public string Type
		{
			get
			{
				return type;
			}
			
		}
		/// <summary> Fully qualified service name, such as <code>foobar._http._tcp.local.</code> .</summary>
		virtual public string QualifiedName
		{
			get
			{
				return name + "." + type;
			}
			
		}
		/// <summary> Get the name of the server.</summary>
		virtual public string Server
		{
			get
			{
				return server;
			}
			
		}
		/// <summary> Get the host address of the service (ie X.X.X.X).</summary>
		virtual public string HostAddress
		{
			get
			{
				return (addr != null?addr.ToString():"");
			}
			
		}
		virtual public IPAddress Address
		{
			get
			{
				return addr;
			}
			
		}
		/// <summary> Get the InetAddress of the service.</summary>
		virtual public IPAddress InetAddress
		{
			get
			{
				return addr;
			}
			
		}
		/// <summary> Get the port for the service.</summary>
		virtual public int Port
		{
			get
			{
				return port;
			}
			
		}
		/// <summary> Get the priority of the service.</summary>
		virtual public int Priority
		{
			get
			{
				return priority;
			}
			
		}
		/// <summary> Get the weight of the service.</summary>
		virtual public int Weight
		{
			get
			{
				return weight;
			}
			
		}
		/// <summary> Get the text for the serivce as raw bytes.</summary>
		virtual public sbyte[] TextBytes
		{
			get
			{
				return text;
			}
			
		}
		/// <summary> Get the text for the service. This will interpret the text bytes
		/// as a UTF8 encoded string. Will return null if the bytes are not
		/// a valid UTF8 encoded string.
		/// </summary>
		virtual public string TextString
		{
			get
			{
				if ((text == null) || (text.Length == 0) || ((text.Length == 1) && (text[0] == 0)))
				{
					return null;
				}
				return ReadUTF(text, 0, text.Length);
			}
			
		}
		/// <summary> Enumeration of the property names.</summary>
		virtual public IEnumerator PropertyNames
		{
			get
			{
				Hashtable props = Properties;
				return (props != null)?props.Keys.GetEnumerator():ArrayList.Synchronized(new ArrayList(10)).GetEnumerator();
			}
			
		}
		virtual internal Hashtable Properties
		{
			get
			{
				lock (this)
				{
					if ((props == null) && (text != null))
					{
						Hashtable newProps = Hashtable.Synchronized(new Hashtable());
						int off = 0;
						while (off < text.Length)
						{
							// length of the next key value pair
							int len = text[off++] & 0xFF;
							if ((len == 0) || (off + len > text.Length))
							{
								newProps.Clear();
								break;
							}
							// look for the '='
							int i = 0;
							for (; (i < len) && (text[off + i] != '='); i++)
								;
							
							// get the property name
							string name = ReadUTF(text, off, i);
							if (name == null)
							{
								newProps.Clear();
								break;
							}
							if (i == len)
							{
								newProps[name] = NO_VALUE;
							}
							else
							{
								sbyte[] value_Renamed = new sbyte[len - ++i];
								Array.Copy(text, off + i, value_Renamed, 0, len - i);
								newProps[name] = value_Renamed;
								off += len;
							}
						}
						this.props = newProps;
					}
					return props;
				}
			}
			
		}
		/// <summary> Returns the current state of this info.</summary>
		virtual internal DNSState State
		{
			get
			{
				return state;
			}
			
		}
		virtual public string NiceTextString
		{
			get
			{
				StringBuilder buf = new StringBuilder();
				for (int i = 0, len = text.Length; i < len; i++)
				{
					if (i >= 20)
					{
						buf.Append("...");
						break;
					}
					int ch = text[i] & 0xFF;
					if ((ch < ' ') || (ch > 127))
					{
						buf.Append("\\0");
						buf.Append(Convert.ToString(ch, 8));
					}
					else
					{
						buf.Append((char) ch);
					}
				}
				return buf.ToString();
			}
			
		}
		private static ILog logger;
		public static readonly sbyte[] NO_VALUE = new sbyte[0];
		internal mDNS dns;
		
		// State machine
		/// <summary> The state of this service info.
		/// This is used only for services announced by JmDNS.
		/// 
		/// For proper handling of concurrency, this variable must be
		/// changed only using methods advanceState(), revertState() and cancel().
		/// </summary>
		private DNSState state;
		
		/// <summary> Task associated to this service info.
		/// Possible tasks are JmDNS.Prober, JmDNS.Announcer, JmDNS.Responder,
		/// JmDNS.Canceler.
		/// </summary>
		// TODO: make this work
		internal object task;
		
		internal string type;
		private string name;
		internal string server;
		internal int port;
		internal int weight;
		internal int priority;
		internal sbyte[] text;
		internal Hashtable props;
		internal IPAddress addr;
		
		
		
		/// <summary> Construct a service description for registrating with JmDNS.</summary>
		/// <param name="type">fully qualified service type name, such as <code>_http._tcp.local.</code>.
		/// </param>
		/// <param name="name">unqualified service instance name, such as <code>foobar</code>
		/// </param>
		/// <param name="port">the local port on which the service runs
		/// </param>
		/// <param name="text">string describing the service
		/// </param>
		public ServiceInfo(string type, string name, int port, string text) : this(type, name, port, 0, 0, text)
		{
		}
		
		/// <summary> Construct a service description for registrating with JmDNS.</summary>
		/// <param name="type">fully qualified service type name, such as <code>_http._tcp.local.</code>.
		/// </param>
		/// <param name="name">unqualified service instance name, such as <code>foobar</code>
		/// </param>
		/// <param name="port">the local port on which the service runs
		/// </param>
		/// <param name="weight">weight of the service
		/// </param>
		/// <param name="priority">priority of the service
		/// </param>
		/// <param name="text">string describing the service
		/// </param>
		public ServiceInfo(string type, string name, int port, int weight, int priority, string text) : this(type, name, port, weight, priority, (sbyte[]) null)
		{
			try
			{
				MemoryStream out_Renamed = new MemoryStream(text.Length);
				WriteUTF(out_Renamed, text);
				this.text = SupportClass.ToSByteArray(out_Renamed.ToArray());
			}
			catch (IOException e)
			{
				throw new Exception("unexpected exception: " + e);
			}
		}
		
		/// <summary> Construct a service description for registrating with JmDNS. The properties hashtable must
		/// map property names to either Strings or byte arrays describing the property values.
		/// </summary>
		/// <param name="type">fully qualified service type name, such as <code>_http._tcp.local.</code>.
		/// </param>
		/// <param name="name">unqualified service instance name, such as <code>foobar</code>
		/// </param>
		/// <param name="port">the local port on which the service runs
		/// </param>
		/// <param name="weight">weight of the service
		/// </param>
		/// <param name="priority">priority of the service
		/// </param>
		/// <param name="props">properties describing the service
		/// </param>
		public ServiceInfo(string type, string name, int port, int weight, int priority, Hashtable props) : this(type, name, port, weight, priority, new sbyte[0])
		{
			if (props != null)
			{
				try
				{
					MemoryStream out_Renamed = new MemoryStream(256);
					foreach (string key in props.Keys)
					{
						object val = props[key];
						MemoryStream out2 = new MemoryStream(100);
						WriteUTF(out2, key);
						if (val is string)
						{
							out2.WriteByte((byte) '=');
							WriteUTF(out2, (string) val);
						}
						else if (val is sbyte[])
						{
							out2.WriteByte((byte) '=');
							sbyte[] bval = (sbyte[]) val;
							out2.Write(SupportClass.ToByteArray(bval), 0, bval.Length);
						}
						else if (val != NO_VALUE)
						{
							throw new ArgumentException("invalid property value: " + val);
						}
						sbyte[] data = SupportClass.ToSByteArray(out2.ToArray());
						out_Renamed.WriteByte((byte) data.Length);
						out_Renamed.Write(SupportClass.ToByteArray(data), 0, data.Length);
					}
					this.text = SupportClass.ToSByteArray(out_Renamed.ToArray());
				}
				catch (IOException e)
				{
					throw new Exception("unexpected exception: " + e);
				}
			}
		}
		
		/// <summary> Construct a service description for registrating with JmDNS.</summary>
		/// <param name="type">fully qualified service type name, such as <code>_http._tcp.local.</code>.
		/// </param>
		/// <param name="name">unqualified service instance name, such as <code>foobar</code>
		/// </param>
		/// <param name="port">the local port on which the service runs
		/// </param>
		/// <param name="weight">weight of the service
		/// </param>
		/// <param name="priority">priority of the service
		/// </param>
		/// <param name="text">bytes describing the service
		/// </param>
		public ServiceInfo(string type, string name, int port, int weight, int priority, sbyte[] text)
		{
			InitBlock();
			this.type = type;
			this.name = name;
			this.port = port;
			this.weight = weight;
			this.priority = priority;
			this.text = text;
		}
		
		/// <summary> Construct a service record during service discovery.</summary>
		internal ServiceInfo(string type, string name)
		{
			InitBlock();
			if (!type.EndsWith("."))
			{
				throw new ArgumentException("type must be fully qualified DNS name ending in '.': " + type);
			}
			
			this.type = type;
			this.name = name;
		}
		
		/// <summary> During recovery we need to duplicate service info to reregister them
		/// 
		/// </summary>
		internal ServiceInfo(ServiceInfo info)
		{
			InitBlock();
			if (info != null)
			{
				this.type = info.type;
				this.name = info.name;
				this.port = info.port;
				this.weight = info.weight;
				this.priority = info.priority;
				this.text = info.text;
			}
		}
		
		/// <summary> Unqualified service instance name, such as <code>foobar</code> .</summary>
		public virtual string getName()
		{
			return name;
		}
		
		/// <summary> Sets the service instance name.
		/// 
		/// </summary>
		/// <param name="name">unqualified service instance name, such as <code>foobar</code>
		/// </param>
		internal virtual void SetName(string name)
		{
			this.name = name;
		}
		
		/// <summary> Get the URL for this service. An http URL is created by
		/// combining the address, port, and path properties.
		/// </summary>
		public virtual string GetURL()
		{
			return GetURL("http");
		}
		
		/// <summary> Get the URL for this service. An URL is created by
		/// combining the protocol, address, port, and path properties.
		/// </summary>
		public virtual string GetURL(string protocol)
		{
			string url = protocol + "://" + Address + ":" + Port;
			string path = GetPropertyString("path");
			if (path != null)
			{
				if (path.IndexOf("://") >= 0)
				{
					url = path;
				}
				else
				{
					url += (path.StartsWith("/")?path:"/" + path);
				}
			}
			return url;
		}
		
		/// <summary> Get a property of the service. This involves decoding the
		/// text bytes into a property list. Returns null if the property
		/// is not found or the text data could not be decoded correctly.
		/// </summary>
		public virtual sbyte[] GetPropertyBytes(string name)
		{
			lock (this)
			{
				return (sbyte[]) Properties[name];
			}
		}
		
		/// <summary> Get a property of the service. This involves decoding the
		/// text bytes into a property list. Returns null if the property
		/// is not found, the text data could not be decoded correctly, or
		/// the resulting bytes are not a valid UTF8 string.
		/// </summary>
		public virtual string GetPropertyString(string name)
		{
			lock (this)
			{
				sbyte[] data = (sbyte[]) Properties[name];
				if (data == null)
				{
					return null;
				}
				if (data == NO_VALUE)
				{
					return "true";
				}
				return ReadUTF(data, 0, data.Length);
			}
		}
		
		/// <summary> Write a UTF string with a length to a stream.</summary>
		internal virtual void WriteUTF(Stream out_Renamed, string str)
		{
			for (int i = 0, len = str.Length; i < len; i++)
			{
				int c = str[i];
				if ((c >= 0x0001) && (c <= 0x007F))
				{
					out_Renamed.WriteByte((byte) c);
				}
				else if (c > 0x07FF)
				{
					out_Renamed.WriteByte((byte) (0xE0 | ((c >> 12) & 0x0F)));
					out_Renamed.WriteByte((byte) (0x80 | ((c >> 6) & 0x3F)));
					out_Renamed.WriteByte((byte) (0x80 | ((c >> 0) & 0x3F)));
				}
				else
				{
					out_Renamed.WriteByte((byte) (0xC0 | ((c >> 6) & 0x1F)));
					out_Renamed.WriteByte((byte) (0x80 | ((c >> 0) & 0x3F)));
				}
			}
		}
		
		/// <summary> Read data bytes as a UTF stream.</summary>
		internal virtual string ReadUTF(sbyte[] data, int off, int len)
		{
			StringBuilder buf = new StringBuilder();
			for (int end = off + len; off < end; )
			{
				int ch = data[off++] & 0xFF;
				switch (ch >> 4)
				{
					
					case 0: 
					case 1: 
					case 2: 
					case 3: 
					case 4: 
					case 5: 
					case 6: 
					case 7: 
						// 0xxxxxxx
						break;
					
					case 12: 
					case 13: 
						if (off >= len)
						{
							return null;
						}
						// 110x xxxx   10xx xxxx
						ch = ((ch & 0x1F) << 6) | (data[off++] & 0x3F);
						break;
					
					case 14: 
						if (off + 2 >= len)
						{
							return null;
						}
						// 1110 xxxx  10xx xxxx  10xx xxxx
						ch = ((ch & 0x0f) << 12) | ((data[off++] & 0x3F) << 6) | (data[off++] & 0x3F);
						break;
					
					default: 
						if (off + 1 >= len)
						{
							return null;
						}
						// 10xx xxxx,  1111 xxxx
						ch = ((ch & 0x3F) << 4) | (data[off++] & 0x0f);
						break;
					
				}
				buf.Append((char) ch);
			}
			return buf.ToString();
		}
		
		// REMIND: Oops, this shouldn't be public!
		/// <summary> JmDNS callback to update a DNS record.</summary>
		public virtual void UpdateRecord(mDNS jmdns, long now, DNSRecord rec)
		{
			if ((rec != null) && !rec.IsExpired(now))
			{
				switch (rec.type)
				{
					
					case DNSConstants.TYPE_A: 
					// IPv4
					case DNSConstants.TYPE_AAAA:  // IPv6 FIXME [PJYF Oct 14 2004] This has not been tested
						if (rec.name.Equals(server))
						{
							addr = ((Address) rec).IPAddress;
						}
						break;
					
					case DNSConstants.TYPE_SRV: 
						if (rec.name.Equals(QualifiedName))
						{
							Service srv = (Service) rec;
							server = srv.server;
							port = srv.port;
							weight = srv.weight;
							priority = srv.priority;
							addr = null;
							// changed to use getCache() instead - jeffs
							// updateRecord(jmdns, now, (DNSRecord)jmdns.cache.get(server, TYPE_A, CLASS_IN));
							UpdateRecord(jmdns, now, (DNSRecord) jmdns.Cache.get_Renamed(server, DNSConstants.TYPE_A, DNSConstants.CLASS_IN));
						}
						break;
					
					case DNSConstants.TYPE_TXT: 
						if (rec.name.Equals(QualifiedName))
						{
							Text txt = (Text) rec;
							text = txt.text;
						}
						break;
					}
				// Future Design Pattern
				// This is done, to notify the wait loop in method
				// JmDNS.getServiceInfo(type, name, timeout);
				if (HasData && dns != null)
				{
					dns.HandleServiceResolved(this);
					dns = null;
				}
				lock (this)
				{
					Monitor.PulseAll(this);
				}
			}
		}
		
		/// <summary> Returns true if the service info is filled with data.</summary>
		internal virtual bool HasData
		{
			get
			{
				return server != null && addr != null && text != null;
			}
		}
		
		
		// State machine
		/// <summary> Sets the state and notifies all objects that wait on the ServiceInfo.</summary>
		internal virtual void AdvanceState()
		{
			lock (this)
			{
				state = state.Advance();
				Monitor.PulseAll(this);
			}
		}
		/// <summary> Sets the state and notifies all objects that wait on the ServiceInfo.</summary>
		internal virtual void RevertState()
		{
			lock (this)
			{
				state = state.Revert();
				Monitor.PulseAll(this);
			}
		}
		/// <summary> Sets the state and notifies all objects that wait on the ServiceInfo.</summary>
		internal virtual void Cancel()
		{
			lock (this)
			{
				state = DNSState.CANCELED;
				Monitor.PulseAll(this);
			}
		}
		
		
		
		public override int GetHashCode()
		{
			return QualifiedName.GetHashCode();
		}
		
		public override bool Equals(object obj)
		{
			return (obj is ServiceInfo) && QualifiedName.Equals(((ServiceInfo) obj).QualifiedName);
		}
		
		public override string ToString()
		{
			StringBuilder buf = new StringBuilder();
			buf.Append("service[");
			buf.Append(QualifiedName);
			buf.Append(',');
			buf.Append(Address);
			buf.Append(':');
			buf.Append(port);
			buf.Append(',');
			buf.Append(NiceTextString);
			buf.Append(']');
			return buf.ToString();
		}
		static ServiceInfo()
		{
			logger = LogManager.GetLogger(typeof(ServiceInfo).ToString());
		}
	}
}
