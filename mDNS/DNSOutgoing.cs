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
using System.Collections;
using System.IO;
using System.Text;
using mDNS.Logging;

namespace mDNS
{
	
	/// <summary> An outgoing DNS message.
	/// 
	/// </summary>
	/// <author> 	Arthur van Hoff, Rick Blair, Werner Randelshofer
	/// </author>
	/// <version>  	%I%, %G%
	/// </version>
	sealed class DNSOutgoing
	{
		internal bool Query
		{
			get
			{
				return (flags & DNSConstants.FLAGS_QR_MASK) == DNSConstants.FLAGS_QR_QUERY;
			}
			
		}
		public bool Empty
		{
			get
			{
				return numQuestions == 0 && numAuthorities == 0 && numAdditionals == 0 && numAnswers == 0;
			}
			
		}
		private static ILog logger;
		internal int id;
		internal int flags;
		private bool multicast;
		private int numQuestions;
		private int numAnswers;
		private int numAuthorities;
		private int numAdditionals;
		private Hashtable names;
		
		internal sbyte[] data;
		internal int off;
		internal int len;
		
		/// <summary> Create an outgoing multicast query or response.</summary>
		internal DNSOutgoing(int flags):this(flags, true)
		{
		}
		
		/// <summary> Create an outgoing query or response.</summary>
		internal DNSOutgoing(int flags, bool multicast)
		{
			this.flags = flags;
			this.multicast = multicast;
			names = Hashtable.Synchronized(new Hashtable());
			data = new sbyte[DNSConstants.MAX_MSG_TYPICAL];
			off = 12;
		}
		
		/// <summary> Add a question to the message.</summary>
		internal void AddQuestion(DNSQuestion rec)
		{
			if (numAnswers > 0 || numAuthorities > 0 || numAdditionals > 0)
			{
				// TODO is this the right exception?
				throw new Exception("Questions must be added before answers");
			}
			numQuestions++;
			WriteQuestion(rec);
		}
		
		/// <summary> Add an answer if it is not suppressed.</summary>
		internal void AddAnswer(DNSIncoming in_Renamed, DNSRecord rec)
		{
			if (numAuthorities > 0 || numAdditionals > 0)
			{
				// TODO is this the right exception
				throw new Exception("Answers must be added before authorities and additionals");
			}
			if (!rec.SuppressedBy(in_Renamed))
			{
				AddAnswer(rec, 0);
			}
		}
		
		/// <summary> Add an additional answer to the record. Omit if there is no room.</summary>
		internal void AddAdditionalAnswer(DNSIncoming in_Renamed, DNSRecord rec)
		{
			if ((off < DNSConstants.MAX_MSG_TYPICAL - 200) && !rec.SuppressedBy(in_Renamed))
			{
				WriteRecord(rec, 0);
				numAdditionals++;
			}
		}
		
		/// <summary> Add an answer to the message.</summary>
		internal void AddAnswer(DNSRecord rec, long now)
		{
			if (numAuthorities > 0 || numAdditionals > 0)
			{
				// TODO is this the right exception?
				throw new Exception("Questions must be added before answers");
			}
			if (rec != null)
			{
				if ((now == 0) || !rec.IsExpired(now))
				{
					WriteRecord(rec, now);
					numAnswers++;
				}
			}
		}
		private IList authorativeAnswers = new ArrayList();
		/// <summary> Add an authorative answer to the message.</summary>
		internal void  AddAuthorativeAnswer(DNSRecord rec)
		{
			if (numAdditionals > 0)
			{
				// TODO is this the right exception?
				throw new Exception("Authorative answers must be added before additional answers");
			}
			authorativeAnswers.Add(rec);
			WriteRecord(rec, 0);
			numAuthorities++;
			
			// VERIFY:
		}
		
		internal void WriteByte(int value_Renamed)
		{
			if (off >= data.Length)
			{
				throw new IOException("buffer full");
			}
			data[off++] = (sbyte) value_Renamed;
		}
		
		internal void WriteBytes(string str, int off, int len)
		{
			for (int i = 0; i < len; i++)
			{
				WriteByte(str[off + i]);
			}
		}
		
		internal void WriteBytes(sbyte[] data)
		{
			if (data != null)
				WriteBytes(data, 0, data.Length);
		}
		
		internal void WriteBytes(sbyte[] data, int off, int len)
		{
			for (int i = 0; i < len; i++)
			{
				WriteByte(data[off + i]);
			}
		}
		
		internal void  WriteShort(int value_Renamed)
		{
			WriteByte(value_Renamed >> 8);
			WriteByte(value_Renamed);
		}
		
		internal void  WriteInt(int value_Renamed)
		{
			WriteShort(value_Renamed >> 16);
			WriteShort(value_Renamed);
		}
		
		internal void  WriteUTF(string str, int off, int len)
		{
			// compute utf length
			int utflen = 0;
			for (int i = 0; i < len; i++)
			{
				int ch = str[off + i];
				if ((ch >= 0x0001) && (ch <= 0x007F))
				{
					utflen += 1;
				}
				else if (ch > 0x07FF)
				{
					utflen += 3;
				}
				else
				{
					utflen += 2;
				}
			}
			// write utf length
			WriteByte(utflen);
			// write utf data
			for (int i = 0; i < len; i++)
			{
				int ch = str[off + i];
				if ((ch >= 0x0001) && (ch <= 0x007F))
				{
					WriteByte(ch);
				}
				else if (ch > 0x07FF)
				{
					WriteByte(0xE0 | ((ch >> 12) & 0x0F));
					WriteByte(0x80 | ((ch >> 6) & 0x3F));
					WriteByte(0x80 | ((ch >> 0) & 0x3F));
				}
				else
				{
					WriteByte(0xC0 | ((ch >> 6) & 0x1F));
					WriteByte(0x80 | ((ch >> 0) & 0x3F));
				}
			}
		}
		
		internal void WriteName(string name)
		{
			while (true)
			{
				int n = name.IndexOf('.');
				if (n < 0)
				{
					n = name.Length;
				}
				if (n <= 0)
				{
					WriteByte(0);
					return ;
				}

				if (names.Contains(name))
				{
					int offset = (int)names[name];
					int val = offset;
					
					if (val > off)
					{
						logger.Warn("DNSOutgoing writeName failed val=" + val + " name=" + name);
					}
					
					WriteByte((val >> 8) | 0xC0);
					WriteByte(val);
					return ;
				}
				names[name] = off;
				WriteUTF(name, 0, n);
				name = name.Substring(n);
				if (name.StartsWith("."))
				{
					name = name.Substring(1);
				}
			}
		}
		
		internal void WriteQuestion(DNSQuestion question)
		{
			WriteName(question.name);
			WriteShort(question.type);
			WriteShort(question.clazz);
		}
		
		internal void WriteRecord(DNSRecord rec, long now)
		{
			int save = off;
			try
			{
				WriteName(rec.name);
				WriteShort(rec.type);
				WriteShort(rec.clazz | ((rec.unique && multicast)?DNSConstants.CLASS_UNIQUE:0));
				WriteInt((now == 0)?rec.ttl:rec.GetRemainingTTL(now));
				WriteShort(0);
				int start = off;
				rec.Write(this);
				int len = off - start;
				data[start - 2] = (sbyte) (len >> 8);
				data[start - 1] = (sbyte) (len & 0xFF);
			}
			catch (IOException e)
			{
				off = save;
				throw e;
			}
		}
		
		/// <summary> Finish the message before sending it off.</summary>
		internal void Finish()
		{
			int save = off;
			off = 0;
			
			WriteShort(multicast?0:id);
			WriteShort(flags);
			WriteShort(numQuestions);
			WriteShort(numAnswers);
			WriteShort(numAuthorities);
			WriteShort(numAdditionals);
			off = save;
		}
		
		
		public override string ToString()
		{
			StringBuilder buf = new StringBuilder();
			buf.Append(Query?"dns[query,":"dns[response,");
			//buf.append(packet.getAddress().getHostAddress());
			buf.Append(':');
			//buf.append(packet.getPort());
			//buf.append(",len=");
			//buf.append(packet.getLength());
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
			buf.Append(",\nnames=" + SupportClass.CollectionToString(names));
			buf.Append(",\nauthorativeAnswers=" + SupportClass.CollectionToString(authorativeAnswers));
			
			buf.Append("]");
			return buf.ToString();
		}
		static DNSOutgoing()
		{
			logger = LogManager.GetLogger(typeof(DNSOutgoing).ToString());
		}
	}
}
