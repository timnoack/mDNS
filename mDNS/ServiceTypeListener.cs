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

namespace mDNS
{
	/// <summary> Listener for service types.
	/// 
	/// </summary>
	/// <author> 	Arthur van Hoff, Werner Randelshofer
	/// </author>
	/// <version>  	%I%, %G%
	/// </version>
	public delegate void ServiceTypeListenerDelegate(Object sender, ServiceEvent ServiceTypeListenerDelegateParam);
	// TODO: make sure this still works
	public interface IServiceTypeListener /*:EventListener*/
	{
		/// <summary> A new service type was discovered. </summary>
		/// <param name="event">The service event providing the fully qualified type of
		/// the service.
		/// </param>
		void ServiceTypeAdded(object event_sender, ServiceEvent event_Renamed);
	}
}
