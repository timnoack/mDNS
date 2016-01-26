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
using mDNS.Logging;

namespace mDNS
{
	/// <summary> ServiceEvent.
	/// 
	/// </summary>
	/// <author>   Werner Randelshofer
	/// </author>
	/// <version>  	%I%, %G%
	/// </version>
	public class ServiceEvent : EventArgs
	{
		// TODO: check this
		/// <summary> Returns the JmDNS instance which originated the event.</summary>
		virtual public mDNS DNS
		{
			get
			{
				// TODO: check this
				return (mDNS) Source;
			}
			
		}
		/// <summary> Returns the fully qualified type of the service.</summary>
		virtual public string Type
		{
			get
			{
				return type;
			}
			
		}
		/// <summary> Returns the instance name of the service.
		/// Always returns null, if the event is sent to a service type listener.
		/// </summary>
		virtual public string Name
		{
			get
			{
				return name;
			}
			
		}
		/// <summary> Returns the service info record, or null if the service could not be
		/// resolved.
		/// Always returns null, if the event is sent to a service type listener.
		/// </summary>
		virtual public ServiceInfo Info
		{
			get
			{
				return info;
			}
			
		}

		public object Source
		{
			get
			{
				return source;
			}
		}

		private static ILog logger;
		/// <summary> The type name of the service.</summary>
		private string type;
		/// <summary> The instance name of the service. Or null, if the event was
		/// fired to a service type listener.
		/// </summary>
		private string name;
		/// <summary> The service info record, or null if the service could be be resolved.
		/// This is also null, if the event was fired to a service type listener.
		/// </summary>
		private ServiceInfo info;

		// added by bbuda
		private object source;
		
		/// <summary> Creates a new instance.
		/// 
		/// </summary>
		/// <param name="source">the JmDNS instance which originated the event.
		/// </param>
		/// <param name="type">the type name of the service.
		/// </param>
		/// <param name="name">the instance name of the service.
		/// </param>
		/// <param name="info">the service info record, or null if the service could be be resolved.
		/// </param>
		public ServiceEvent(mDNS source, string type, string name, ServiceInfo info):base()
		{
			this.source = source;
			this.type = type;
			this.name = name;
			this.info = info;
		}
		
		public override string ToString()
		{
			StringBuilder buf = new StringBuilder();
			buf.Append("<" + GetType().FullName + "> ");
			buf.Append(base.ToString());
			buf.Append(" name ");
			buf.Append(Name);
			buf.Append(" type ");
			buf.Append(Type);
			buf.Append(" info ");
			buf.Append(Info);
			return buf.ToString();
		}
		static ServiceEvent()
		{
			logger = LogManager.GetLogger(typeof(ServiceEvent).ToString());
		}
	}
}
