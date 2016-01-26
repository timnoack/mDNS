// %Z%%M%, %I%, %G%
//
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
	
	/// <summary>DNS constants.</summary>
	public class DNSConstants
	{
		
		// changed to final class - jeffs
		internal const string MDNS_GROUP = "224.0.0.251";
		internal const string MDNS_GROUP_IPV6 = "FF02::FB";
		internal const int MDNS_PORT = 5353;
		internal const int DNS_PORT = 53;
		internal const int DNS_TTL = 60 * 60; // default one hour TTL
		// final static int DNS_TTL		    = 120 * 60;	// two hour TTL (draft-cheshire-dnsext-multicastdns.txt ch 13)
		
		internal const int MAX_MSG_TYPICAL = 1460;
		internal const int MAX_MSG_ABSOLUTE = 8972;
		
		internal const int FLAGS_QR_MASK = 0x8000; // Query response mask
		internal const int FLAGS_QR_QUERY = 0x0000; // Query
		internal const int FLAGS_QR_RESPONSE = 0x8000; // Response
		
		internal const int FLAGS_AA = 0x0400; // Authorative answer
		internal const int FLAGS_TC = 0x0200; // Truncated
		internal const int FLAGS_RD = 0x0100; // Recursion desired
		internal const int FLAGS_RA = 0x8000; // Recursion available
		
		internal const int FLAGS_Z = 0x0040; // Zero
		internal const int FLAGS_AD = 0x0020; // Authentic data
		internal const int FLAGS_CD = 0x0010; // Checking disabled
		
		internal const int CLASS_IN = 1; // Final Static Internet
		internal const int CLASS_CS = 2; // CSNET
		internal const int CLASS_CH = 3; // CHAOS
		internal const int CLASS_HS = 4; // Hesiod
		internal const int CLASS_NONE = 254; // Used in DNS UPDATE [RFC 2136]
		internal const int CLASS_ANY = 255; // Not a DNS class, but a DNS query class, meaning "all classes"
		internal const int CLASS_MASK = 0x7FFF; // Multicast DNS uses the bottom 15 bits to identify the record class...
		internal const int CLASS_UNIQUE = 0x8000; // ... and the top bit indicates that all other cached records are now invalid
		
		internal const int TYPE_IGNORE = 0; // This is a hack to stop further processing
		internal const int TYPE_A = 1; // Address
		internal const int TYPE_NS = 2; // Name Server
		internal const int TYPE_MD = 3; // Mail Destination
		internal const int TYPE_MF = 4; // Mail Forwarder
		internal const int TYPE_CNAME = 5; // Canonical Name
		internal const int TYPE_SOA = 6; // Start of Authority
		internal const int TYPE_MB = 7; // Mailbox
		internal const int TYPE_MG = 8; // Mail Group
		internal const int TYPE_MR = 9; // Mail Rename
		internal const int TYPE_NULL = 10; // NULL RR
		internal const int TYPE_WKS = 11; // Well-known-service
		internal const int TYPE_PTR = 12; // Domain Name pofinal static inter
		internal const int TYPE_HINFO = 13; // Host information
		internal const int TYPE_MINFO = 14; // Mailbox information
		internal const int TYPE_MX = 15; // Mail exchanger
		internal const int TYPE_TXT = 16; // Arbitrary text string
		internal const int TYPE_RP = 17; // for Responsible Person                 [RFC1183]
		internal const int TYPE_AFSDB = 18; // for AFS Data Base location             [RFC1183]
		internal const int TYPE_X25 = 19; // for X.25 PSDN address                  [RFC1183]
		internal const int TYPE_ISDN = 20; // for ISDN address                       [RFC1183]
		internal const int TYPE_RT = 21; // for Route Through                      [RFC1183]
		internal const int TYPE_NSAP = 22; // for NSAP address, NSAP style A record  [RFC1706]
		internal const int TYPE_NSAP_PTR = 23; //
		internal const int TYPE_SIG = 24; // for security signature                 [RFC2931]
		internal const int TYPE_KEY = 25; // for security key                       [RFC2535]
		internal const int TYPE_PX = 26; // X.400 mail mapping information         [RFC2163]
		internal const int TYPE_GPOS = 27; // Geographical Position                  [RFC1712]
		internal const int TYPE_AAAA = 28; // IP6 Address                            [Thomson]
		internal const int TYPE_LOC = 29; // Location Information                   [Vixie]
		internal const int TYPE_NXT = 30; // Next Domain - OBSOLETE                 [RFC2535, RFC3755]
		internal const int TYPE_EID = 31; // Endpoint Identifier                    [Patton]
		internal const int TYPE_NIMLOC = 32; // Nimrod Locator                         [Patton]
		internal const int TYPE_SRV = 33; // Server Selection                       [RFC2782]
		internal const int TYPE_ATMA = 34; // ATM Address                            [Dobrowski]
		internal const int TYPE_NAPTR = 35; // Naming Authority Pointer               [RFC2168, RFC2915]
		internal const int TYPE_KX = 36; // Key Exchanger                          [RFC2230]
		internal const int TYPE_CERT = 37; // CERT                                   [RFC2538]
		internal const int TYPE_A6 = 38; // A6                                     [RFC2874]
		internal const int TYPE_DNAME = 39; // DNAME                                  [RFC2672]
		internal const int TYPE_SINK = 40; // SINK                                   [Eastlake]
		internal const int TYPE_OPT = 41; // OPT                                    [RFC2671]
		internal const int TYPE_APL = 42; // APL                                    [RFC3123]
		internal const int TYPE_DS = 43; // Delegation Signer                      [RFC3658]
		internal const int TYPE_SSHFP = 44; // SSH Key Fingerprint                    [RFC-ietf-secsh-dns-05.txt]
		internal const int TYPE_RRSIG = 46; // RRSIG                                  [RFC3755]
		internal const int TYPE_NSEC = 47; // NSEC                                   [RFC3755]
		internal const int TYPE_DNSKEY = 48; // DNSKEY                                 [RFC3755]
		internal const int TYPE_UINFO = 100; //									      [IANA-Reserved]
		internal const int TYPE_UID = 101; //                                        [IANA-Reserved]
		internal const int TYPE_GID = 102; //                                        [IANA-Reserved]
		internal const int TYPE_UNSPEC = 103; //                                        [IANA-Reserved]
		internal const int TYPE_TKEY = 249; // Transaction Key                        [RFC2930]
		internal const int TYPE_TSIG = 250; // Transaction Signature                  [RFC2845]
		internal const int TYPE_IXFR = 251; // Incremental transfer                   [RFC1995]
		internal const int TYPE_AXFR = 252; // Transfer of an entire zone             [RFC1035]
		internal const int TYPE_MAILA = 253; // Mailbox-related records (MB, MG or MR) [RFC1035]
		internal const int TYPE_MAILB = 254; // Mail agent RRs (Obsolete - see MX)     [RFC1035]
		internal const int TYPE_ANY = 255; // Request for all records	        	  [RFC1035]
		
		//Time Intervals for various functions
		
		internal const int SHARED_QUERY_TIME = 20; //milliseconds before send shared query
		internal const int QUERY_WAIT_INTERVAL = 225; //milliseconds between query loops.
		internal const int PROBE_WAIT_INTERVAL = 250; //milliseconds between probe loops.
		internal const int RESPONSE_MIN_WAIT_INTERVAL = 20; //minimal wait interval for response.
		internal const int RESPONSE_MAX_WAIT_INTERVAL = 115; //maximal wait interval for response
		internal const int PROBE_CONFLICT_INTERVAL = 1000; //milliseconds to wait after conflict.
		internal const int PROBE_THROTTLE_COUNT = 10; //After x tries go 1 time a sec. on probes.
		internal const int PROBE_THROTTLE_COUNT_INTERVAL = 5000; //We only increment the throttle count, if
		// the previous increment is inside this interval.
		internal const int ANNOUNCE_WAIT_INTERVAL = 1000; //milliseconds between Announce loops.
		internal const int RECORD_REAPER_INTERVAL = 10000; //milliseconds between cache cleanups.
		internal const int KNOWN_ANSWER_TTL = 120;
		internal static readonly int ANNOUNCED_RENEWAL_TTL_INTERVAL = DNS_TTL * 500; // 50% of the TTL in milliseconds
	}
}
