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
using System.Text;
using mDNS.Logging;

namespace mDNS
{
	
	/// <summary> Parse an incoming DNS message into its components.
	/// 
	/// </summary>
	/// <author> 	Arthur van Hoff, Werner Randelshofer, Pierre Frisch
	/// </author>
	/// <version>  	%I%, %G%
	/// </version>
	sealed class DNSIncoming
	{
		/// <summary> Check if the message is a query.</summary>
		internal bool Query
		{
			get
			{
				return (flags & DNSConstants.FLAGS_QR_MASK) == DNSConstants.FLAGS_QR_QUERY;
			}
			
		}
		/// <summary> Check if the message is truncated.</summary>
		internal bool Truncated
		{
			get
			{
				return (flags & DNSConstants.FLAGS_TC) != 0;
			}
			
		}
		/// <summary> Check if the message is a response.</summary>
		internal bool Response
		{
			get
			{
				return (flags & DNSConstants.FLAGS_QR_MASK) == DNSConstants.FLAGS_QR_RESPONSE;
			}
			
		}

		private static ILog logger;
		// Implementation note: This vector should be immutable.
		// If a client of DNSIncoming changes the contents of this vector,
		// we get undesired results. To fix this, we have to migrate to
		// the Collections API of Java 1.2. i.e we replace Vector by List.
		// final static Vector EMPTY = new Vector();
		
		private SupportClass.PacketSupport packet;
		private int off;
		private int len;
		private sbyte[] data;
		
		internal int id;
		private int flags;
		private int numQuestions;
		internal int numAnswers;
		private int numAuthorities;
		private int numAdditionals;
		private long receivedTime;
		
		internal IList questions;
		internal IList answers;
		
		/// <summary> Parse a message from a datagram packet.</summary>
		internal DNSIncoming(SupportClass.PacketSupport packet)
		{
			this.packet = packet;
			this.data = SupportClass.ToSByteArray(packet.Data);
			this.len = packet.Length;
			// TODO: will this always be 0 in .NET??
			this.off = 0;
			this.questions = ArrayList.ReadOnly(new ArrayList());
			this.answers = ArrayList.ReadOnly(new ArrayList());
			this.receivedTime = (DateTime.Now.Ticks - 621355968000000000) / 10000;
			
			try
			{
				id = ReadUnsignedShort();
				flags = ReadUnsignedShort();
				numQuestions = ReadUnsignedShort();
				numAnswers = ReadUnsignedShort();
				numAuthorities = ReadUnsignedShort();
				numAdditionals = ReadUnsignedShort();
				
				// parse questions
				if (numQuestions > 0)
				{
					questions = ArrayList.Synchronized(new ArrayList(numQuestions));
					for (int i = 0; i < numQuestions; i++)
					{
						DNSQuestion question = new DNSQuestion(ReadName(), ReadUnsignedShort(), ReadUnsignedShort());
						questions.Add(question);
					}
				}
				
				// parse answers
				int n = numAnswers + numAuthorities + numAdditionals;
				if (n > 0)
				{
					answers = ArrayList.Synchronized(new ArrayList(n));
					for (int i = 0; i < n; i++)
					{
						string domain = ReadName();
						int type = ReadUnsignedShort();
						int clazz = ReadUnsignedShort();
						int ttl = ReadInt();
						int len = ReadUnsignedShort();
						int end = off + len;
						DNSRecord rec = null;
						
						switch (type)
						{
							
							case DNSConstants.TYPE_A: 
							// IPv4
							case DNSConstants.TYPE_AAAA:  // IPv6 FIXME [PJYF Oct 14 2004] This has not been tested
								rec = new Address(domain, type, clazz, ttl, ReadBytes(off, len));
								break;
							
							case DNSConstants.TYPE_CNAME: 
							case DNSConstants.TYPE_PTR: 
								rec = new Pointer(domain, type, clazz, ttl, ReadName());
								break;
							
							case DNSConstants.TYPE_TXT: 
								rec = new Text(domain, type, clazz, ttl, ReadBytes(off, len));
								break;
							
							case DNSConstants.TYPE_SRV: 
								rec = new Service(domain, type, clazz, ttl, ReadUnsignedShort(), ReadUnsignedShort(), ReadUnsignedShort(), ReadName());
								break;
							
							case DNSConstants.TYPE_HINFO: 
								// Maybe we should do something with those
								break;
							
							default: 
								logger.Debug("DNSIncoming() unknown type:" + type);
								break;
							
						}
						
						if (rec != null)
						{
							// Add a record, if we were able to create one.
							answers.Add(rec);
						}
						else
						{
							// Addjust the numbers for the skipped record
							if (answers.Count < numAnswers)
							{
								numAnswers--;
							}
							else if (answers.Count < numAnswers + numAuthorities)
							{
								numAuthorities--;
							}
							else if (answers.Count < numAnswers + numAuthorities + numAdditionals)
							{
								numAdditionals--;
							}
						}
						off = end;
					}
				}
			}
			catch (IOException e)
			{
				logger.Warn("DNSIncoming() dump " + Print(true) + "\n exception ", e);
				throw e;
			}
		}
		
		private int GetData(int off)
		{
			if ((off < 0) || (off >= len))
			{
				throw new IOException("parser error: offset=" + off);
			}
			return data[off] & 0xFF;
		}
		
		private int ReadUnsignedShort()
		{
			return (GetData(off++) << 8) + GetData(off++);
		}
		
		private int ReadInt()
		{
			return (ReadUnsignedShort() << 16) + ReadUnsignedShort();
		}
		
		private sbyte[] ReadBytes(int off, int len)
		{
			sbyte[] bytes = new sbyte[len];
			Array.Copy(data, off, bytes, 0, len);
			return bytes;
		}
		
		private void ReadUTF(StringBuilder buf, int off, int len)
		{
			for (int end = off + len; off < end; )
			{
				int ch = GetData(off++);
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
						// 110x xxxx   10xx xxxx
						ch = ((ch & 0x1F) << 6) | (GetData(off++) & 0x3F);
						break;
					
					case 14: 
						// 1110 xxxx  10xx xxxx  10xx xxxx
						ch = ((ch & 0x0f) << 12) | ((GetData(off++) & 0x3F) << 6) | (GetData(off++) & 0x3F);
						break;
					
					default: 
						// 10xx xxxx,  1111 xxxx
						ch = ((ch & 0x3F) << 4) | (GetData(off++) & 0x0f);
						break;
					
				}
				buf.Append((char) ch);
			}
		}
		
		private string ReadName()
		{
			StringBuilder buf = new StringBuilder();
			int off = this.off;
			int next = - 1;
			int first = off;
			
			while (true)
			{
				int len = GetData(off++);
				if (len == 0)
				{
					break;
				}
				switch (len & 0xC0)
				{
					
					case 0x00: 
						//buf.append("[" + off + "]");
						ReadUTF(buf, off, len);
						off += len;
						buf.Append('.');
						break;
					
					case 0xC0: 
						//buf.append("<" + (off - 1) + ">");
						if (next < 0)
						{
							next = off + 1;
						}
						off = ((len & 0x3F) << 8) | GetData(off++);
						if (off >= first)
						{
							throw new IOException("bad domain name: possible circular name detected");
						}
						first = off;
						break;
					
					default: 
						throw new IOException("bad domain name: '" + buf + "' at " + off);
					
				}
			}
			this.off = (next >= 0)?next:off;
			return buf.ToString();
		}
		
		/// <summary> Debugging.</summary>
		internal string Print(bool dump)
		{
			StringBuilder buf = new StringBuilder();
			buf.Append(ToString() + "\n");
			foreach (DNSQuestion question in questions)
				buf.Append("    ques:" + question + "\n");
			int count = 0;
			foreach (DNSRecord answer in answers)
			{
				if (count < numAnswers)
				{
					buf.Append("    answ:");
				}
				else if (count < numAnswers + numAuthorities)
				{
					buf.Append("    auth:");
				}
				else
				{
					buf.Append("    addi:");
				}
				buf.Append(answer.ToString() + "\n");
			}
			if (dump)
			{
				for (int off = 0, len = packet.Length; off < len; off += 32)
				{
					int n = Math.Min(32, len - off);
					if (off < 10)
					{
						buf.Append(' ');
					}
					if (off < 100)
					{
						buf.Append(' ');
					}
					buf.Append(off);
					buf.Append(':');
					for (int i = 0; i < n; i++)
					{
						if ((i % 8) == 0)
						{
							buf.Append(' ');
						}
						buf.Append(Convert.ToString((data[off + i] & 0xF0) >> 4, 16));
						buf.Append(Convert.ToString((data[off + i] & 0x0F) >> 0, 16));
					}
					buf.Append("\n");
					buf.Append("    ");
					for (int i = 0; i < n; i++)
					{
						if ((i % 8) == 0)
						{
							buf.Append(' ');
						}
						buf.Append(' ');
						int ch = data[off + i] & 0xFF;
						buf.Append(((ch > ' ') && (ch < 127))?(char) ch:'.');
					}
					buf.Append("\n");
					
					// limit message size
					if (off + 32 >= 64)
					{
						buf.Append("....\n");
						break;
					}
				}
			}
			return buf.ToString();
		}
		
		public override string ToString()
		{
			StringBuilder buf = new StringBuilder();
			buf.Append(Query?"dns[query,":"dns[response,");
			if (packet.Address != null)
				buf.Append(packet.Address.ToString());
			buf.Append(':');
			buf.Append(packet.Port);
			buf.Append(",len=");
			buf.Append(packet.Length);
			buf.Append(",id=0x");
			buf.Append(Convert.ToString(id, 16));
			if (flags != 0)
			{
				buf.Append(",flags=0x");
				buf.Append(Convert.ToString(flags, 16));
				if ((flags & DNSConstants.FLAGS_QR_RESPONSE) != 0)
				{
					buf.Append(":r");
				}
				if ((flags & DNSConstants.FLAGS_AA) != 0)
				{
					buf.Append(":aa");
				}
				if ((flags & DNSConstants.FLAGS_TC) != 0)
				{
					buf.Append(":tc");
				}
			}
			if (numQuestions > 0)
			{
				buf.Append(",questions=");
				buf.Append(numQuestions);
			}
			if (numAnswers > 0)
			{
				buf.Append(",answers=");
				buf.Append(numAnswers);
			}
			if (numAuthorities > 0)
			{
				buf.Append(",authorities=");
				buf.Append(numAuthorities);
			}
			if (numAdditionals > 0)
			{
				buf.Append(",additionals=");
				buf.Append(numAdditionals);
			}
			buf.Append("]");
			return buf.ToString();
		}
		
		/// <summary> Appends answers to this Incoming.</summary>
		/// <exception cref=""> InvalidArgumentException if this is not truncated, and
		/// that or this is not a query.
		/// </exception>
		internal void Append(DNSIncoming that)
		{
			if (this.Query && this.Truncated && that.Query)
			{
				SupportClass.ICollectionSupport.AddAll(this.questions, that.questions);
				this.numQuestions += that.numQuestions;
				
				if (SupportClass.EqualsSupport.Equals(ArrayList.ReadOnly(new ArrayList()), answers))
					answers = ArrayList.Synchronized(new ArrayList());
				
				if (that.numAnswers > 0)
				{
					SupportClass.IListSupport.AddAll(this.answers, this.numAnswers, (IList) ((ArrayList) that.answers).GetRange(0, that.numAnswers - 0));
					this.numAnswers += that.numAnswers;
				}
				if (that.numAuthorities > 0)
				{
					SupportClass.IListSupport.AddAll(this.answers, this.numAnswers + this.numAuthorities, (IList) ((ArrayList) that.answers).GetRange(that.numAnswers, that.numAnswers + that.numAuthorities - that.numAnswers));
					this.numAuthorities += that.numAuthorities;
				}
				if (that.numAdditionals > 0)
				{
					SupportClass.ICollectionSupport.AddAll(this.answers, (IList) ((ArrayList) that.answers).GetRange(that.numAnswers + that.numAuthorities, that.numAnswers + that.numAuthorities + that.numAdditionals - (that.numAnswers + that.numAuthorities)));
					this.numAdditionals += that.numAdditionals;
				}
			}
			else
			{
				throw new ArgumentException();
			}
		}
		
		internal int ElapsedSinceArrival()
		{
			return (int) ((DateTime.Now.Ticks - 621355968000000000) / 10000 - receivedTime);
		}
		static DNSIncoming()
		{
			logger = LogManager.GetLogger(typeof(DNSIncoming).ToString());
		}
	}
}
