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
using System.Net;
using System.Threading;
using mDNS.Logging;

namespace mDNS
{
	/// <summary> The Responder sends a single answer for the specified service infos
	/// and for the host name.
	/// </summary>
	// TODO: check this
	internal class Responder /*: IThreadRunnable /*:TimerTask*/
	{
		private ILog logger = LogManager.GetLogger("Responder");
		private Random random = new Random();
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
		private DNSIncoming in_Renamed;
		private IPAddress addr;
		private int port;
		public Responder(mDNS enclosingInstance, DNSIncoming in_Renamed, IPAddress addr, int port)
		{
			InitBlock(enclosingInstance);
			this.in_Renamed = in_Renamed;
			this.addr = addr;
			this.port = port;
		}
		public virtual void start()
		{
			// According to draft-cheshire-dnsext-multicastdns.txt
			// chapter "8 Responding":
			// We respond immediately if we know for sure, that we are
			// the only one who can respond to the query.
			// In all other cases, we respond within 20-120 ms.
			//
			// According to draft-cheshire-dnsext-multicastdns.txt
			// chapter "7.2 Multi-Packet Known Answer Suppression":
			// We respond after 20-120 ms if the query is truncated.
				
			bool iAmTheOnlyOne = true;
			foreach (DNSEntry entry in in_Renamed.questions)
			{
				if (entry is DNSQuestion)
				{
					DNSQuestion q = (DNSQuestion) entry;
					logger.Debug("start() question=" + q);
					iAmTheOnlyOne &= (q.type == DNSConstants.TYPE_SRV || q.type == DNSConstants.TYPE_TXT || q.type == DNSConstants.TYPE_A || q.type == DNSConstants.TYPE_AAAA || Enclosing_Instance.localHost.Name.ToUpper().Equals(q.name.ToUpper()) || Enclosing_Instance.services.Contains(q.name.ToLower()));
					if (!iAmTheOnlyOne)
					{
						break;
					}
				}
			}
			int delay = (iAmTheOnlyOne && !in_Renamed.Truncated)?0:DNSConstants.RESPONSE_MIN_WAIT_INTERVAL + random.Next(DNSConstants.RESPONSE_MAX_WAIT_INTERVAL - DNSConstants.RESPONSE_MIN_WAIT_INTERVAL + 1) - in_Renamed.ElapsedSinceArrival();
			if (delay < 0)
				delay = 0;
			logger.Debug("start() Responder chosen delay=" + delay);
			// TODO: check this
			Enclosing_Instance.Timer = new Timer(new TimerCallback(this.Run), null, delay, 0);
		}
		public void Run(object state)
		{
			lock (Enclosing_Instance.IOLock)
			{
				if (Enclosing_Instance.PlannedAnswer == in_Renamed)
				{
					Enclosing_Instance.PlannedAnswer = null;
				}
					
				// We use these sets to prevent duplicate records
				// FIXME - This should be moved into DNSOutgoing
				// TODO: check these for compatibility
				SupportClass.HashSetSupport questions = new SupportClass.HashSetSupport();
				SupportClass.HashSetSupport answers = new SupportClass.HashSetSupport();
					
					
				if (Enclosing_Instance.State == DNSState.ANNOUNCED)
				{
					try
					{
						long now = (DateTime.Now.Ticks - 621355968000000000) / 10000;
						long expirationTime = now + 1; //=now+DNSConstants.KNOWN_ANSWER_TTL;
						bool isUnicast = (port != DNSConstants.MDNS_PORT);
							
							
						// Answer questions
						foreach (DNSEntry entry in in_Renamed.questions)
						{
							if (entry is DNSQuestion)
							{
								DNSQuestion q = (DNSQuestion) entry;
									
								// for unicast responses the question must be included
								if (isUnicast)
								{
									//out.addQuestion(q);
									questions.Add(q);
								}
									
								int type = q.type;
								if (type == DNSConstants.TYPE_ANY || type == DNSConstants.TYPE_SRV)
								{
									// I ama not sure of why there is a special case here [PJYF Oct 15 2004]
									if (Enclosing_Instance.localHost.Name.ToUpper().Equals(q.Name.ToUpper()))
									{
										// type = DNSConstants.TYPE_A;
										DNSRecord answer = Enclosing_Instance.localHost.DNS4AddressRecord;
										if (answer != null)
											answers.Add(answer);
										answer = Enclosing_Instance.localHost.DNS6AddressRecord;
										if (answer != null)
											answers.Add(answer);
										type = DNSConstants.TYPE_IGNORE;
									}
									else if (Enclosing_Instance.serviceTypes.Contains(q.Name.ToLower()))
									{
										type = DNSConstants.TYPE_PTR;
									}
								}
									
								switch (type)
								{
										
									case DNSConstants.TYPE_A:  
									{
										// Answer a query for a domain name
										//out = addAnswer( in, addr, port, out, host );
										DNSRecord answer = Enclosing_Instance.localHost.DNS4AddressRecord;
										if (answer != null)
											answers.Add(answer);
										break;
									}
										
									case DNSConstants.TYPE_AAAA:  
									{
										// Answer a query for a domain name
										DNSRecord answer = Enclosing_Instance.localHost.DNS6AddressRecord;
										if (answer != null)
											answers.Add(answer);
										break;
									}
										
									case DNSConstants.TYPE_PTR:  
									{
										// Answer a query for services of a given type
												
										// find matching services
										foreach (ServiceInfo info in Enclosing_Instance.services.Values)
										{
											if (info.State == DNSState.ANNOUNCED)
											{
												if (q.name.ToUpper().Equals(info.type.ToUpper()))
												{
													DNSRecord answer = Enclosing_Instance.localHost.DNS4AddressRecord;
													if (answer != null)
														answers.Add(answer);
													answer = Enclosing_Instance.localHost.DNS6AddressRecord;
													if (answer != null)
														answers.Add(answer);
													answers.Add(new Pointer(info.type, DNSConstants.TYPE_PTR, DNSConstants.CLASS_IN, DNSConstants.DNS_TTL, info.QualifiedName));
													answers.Add(new Service(info.QualifiedName, DNSConstants.TYPE_SRV, DNSConstants.CLASS_IN | DNSConstants.CLASS_UNIQUE, DNSConstants.DNS_TTL, info.priority, info.weight, info.port, Enclosing_Instance.localHost.Name));
													answers.Add(new Text(info.QualifiedName, DNSConstants.TYPE_TXT, DNSConstants.CLASS_IN | DNSConstants.CLASS_UNIQUE, DNSConstants.DNS_TTL, info.text));
												}
											}
										}
										if (q.name.ToUpper().Equals("_services._mdns._udp.local.".ToUpper()))
										{
											foreach (String s in Enclosing_Instance.serviceTypes.Values)
											{
												answers.Add(new Pointer("_services._mdns._udp.local.", DNSConstants.TYPE_PTR, DNSConstants.CLASS_IN, DNSConstants.DNS_TTL, s));
											}
										}
										break;
									}
										
									case DNSConstants.TYPE_SRV: 
									case DNSConstants.TYPE_ANY: 
									case DNSConstants.TYPE_TXT:  
									{
										ServiceInfo info = (ServiceInfo) Enclosing_Instance.services[q.name.ToLower()];
										if (info != null && info.State == DNSState.ANNOUNCED)
										{
											DNSRecord answer = Enclosing_Instance.localHost.DNS4AddressRecord;
											if (answer != null)
												answers.Add(answer);
											answer = Enclosing_Instance.localHost.DNS6AddressRecord;
											if (answer != null)
												answers.Add(answer);
											answers.Add(new Pointer(info.type, DNSConstants.TYPE_PTR, DNSConstants.CLASS_IN, DNSConstants.DNS_TTL, info.QualifiedName));
											answers.Add(new Service(info.QualifiedName, DNSConstants.TYPE_SRV, DNSConstants.CLASS_IN | DNSConstants.CLASS_UNIQUE, DNSConstants.DNS_TTL, info.priority, info.weight, info.port, Enclosing_Instance.localHost.Name));
											answers.Add(new Text(info.QualifiedName, DNSConstants.TYPE_TXT, DNSConstants.CLASS_IN | DNSConstants.CLASS_UNIQUE, DNSConstants.DNS_TTL, info.text));
										}
										break;
									}
										
									default:  
									{
										//Console.WriteLine("JmDNSResponder.unhandled query:"+q);
										break;
									}
										
								}
							}
						}
							
							
						// remove known answers, if the ttl is at least half of
						// the correct value. (See Draft Cheshire chapter 7.1.).
						foreach (DNSRecord knownAnswer in in_Renamed.answers)
						{
							bool tempBoolean;
							tempBoolean = answers.Contains(knownAnswer);
							answers.Remove(knownAnswer);
							if (knownAnswer.ttl > DNSConstants.DNS_TTL / 2 && tempBoolean)
							{
								logger.Debug("JmDNS Responder Known Answer Removed");
							}
						}
							
							
						// responde if we have answers
						if (answers.Count != 0)
						{
							logger.Debug("run() JmDNS responding");
							DNSOutgoing out_Renamed = null;
							if (isUnicast)
							{
								out_Renamed = new DNSOutgoing(DNSConstants.FLAGS_QR_RESPONSE | DNSConstants.FLAGS_AA, false);
							}
								
							foreach (DNSQuestion question in questions)
							{
								out_Renamed.AddQuestion(question);
							}
							foreach (DNSRecord answer in answers)
							{
								out_Renamed = Enclosing_Instance.AddAnswer(in_Renamed, addr, port, out_Renamed, answer);
							}
							Enclosing_Instance.Send(out_Renamed);
						}
						// TODO: do we need this?
						//cancel();
					}
					catch (Exception e)
					{
						logger.Warn("run() exception ", e);
						Enclosing_Instance.Close();
					}
				}
			}
		}
	}
}
