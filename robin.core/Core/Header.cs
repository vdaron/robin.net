/*******************************************************************************
 * Copyright (c) 2001-2005 Sasa Markovic and Ciaran Treanor.
 * Copyright (c) 2011 The OpenNMS Group, Inc.
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 *******************************************************************************/

using System;
using System.Xml;

namespace robin.core
{
	/// <summary>
	/// Class to represent RRD header. Header information is mainly static (once set, it
	/// cannot be changed), with the exception of last update time (this value is changed whenever
	/// RRD gets updated).
	/// 
	/// Normally, you don't need to manipulate the Header object directly - JRobin framework
	/// does it for you.
	/// </summary>
	public class Header : IRrdUpdater
	{
		private static int SIGNATURE_LENGTH = 8;
		private static String SIGNATURE = "JRobin, ";

		private static String DEFAULT_SIGNATURE = "JRobin, version 0.1";
		private static String RRDTOOL_VERSION = "0001";
		private readonly RrdInt arcCount;
		private readonly RrdLong lastUpdateTime;

		private readonly RrdDb parentDb;

		private readonly RrdString signature;
		private readonly RrdLong step;
		internal RrdInt dsCount;
		private int? primitiveArcCount;
		private int? primitiveDsCount;
		private long? primitiveStep;

		internal Header(RrdDb parentDb, RrdDef rrdDef)
		{
			bool shouldInitialize = rrdDef != null;
			this.parentDb = parentDb;
			signature = new RrdString(this); // NOT constant, may NOT be cached
			step = new RrdLong(this, true); // constant, may be cached
			dsCount = new RrdInt(this, true); // constant, may be cached
			arcCount = new RrdInt(this, true); // constant, may be cached
			lastUpdateTime = new RrdLong(this);
			if (shouldInitialize)
			{
				signature.Set(DEFAULT_SIGNATURE);
				step.Set(rrdDef.Step);
				dsCount.Set(rrdDef.DataSourceDefinitions.Length);
				arcCount.Set(rrdDef.ArchiveDefinitions.Length);
				lastUpdateTime.Set(rrdDef.StartTime);
			}
		}

		internal Header(RrdDb parentDb, DataImporter reader) : this(parentDb, (RrdDef) null)
		{
			String version = reader.Version;
			int intVersion = int.Parse(version);
			if (intVersion > 3)
			{
				throw new RrdException("Could not unserialize xml version " + version);
			}
			signature.Set(DEFAULT_SIGNATURE);
			step.Set(reader.Step);
			dsCount.Set(reader.DataSourceCount);
			arcCount.Set(reader.ArchiveCount);
			lastUpdateTime.Set(reader.LastUpdateTime);
		}

		#region IRrdUpdater Members

		/// <summary>
		/// Copies object's internal state to another ArcState object.
		/// </summary>
		/// <param name="updater"> New ArcState object to copy state to</param>
		public void CopyStateTo(IRrdUpdater other)
		{
			if (!(other is Header))
			{
				throw new RrdException("Cannot copy Header object to " + other.GetType().Name);
			}
			var header = (Header) other;
			header.signature.Set(signature.Get());
			header.lastUpdateTime.Set(lastUpdateTime.Get());
		}

		/// <summary>
		/// Returns the underlying storage (backend) object which actually performs all
		/// I/O operations.
		/// </summary>
		/// <returns></returns>
		public RrdBackend GetRrdBackend()
		{
			return parentDb.GetRrdBackend();
		}

		/// <summary>
		/// Returns the underlying storage (backend) object which actually performs all
		/// I/O operations.
		/// </summary>
		/// <returns></returns>
		public RrdAllocator GetRrdAllocator()
		{
			return parentDb.GetRrdAllocator();
		}

		#endregion

		/// <summary>
		/// Returns RRD signature. Initially, the returned string will be
		/// of the form <b><i>JRobin, version x.x</i></b>. Note: RRD format did not
		/// change since Jrobin 1.0.0 release (and probably never will).
		/// </summary>
		/// <value></value>
		public string Signature
		{
			get { return signature.Get(); }
		}

		public string Info
		{
			get { return Signature.Substring(SIGNATURE_LENGTH); }
			set
			{
				if (!string.IsNullOrEmpty(value))
				{
					signature.Set(SIGNATURE + value);
				}
				else
				{
					signature.Set(SIGNATURE);
				}
			}
		}

		/// <summary>
		/// The last update time of the RRD.
		/// </summary>
		/// <value></value>
		public long LastUpdateTime
		{
			get { return lastUpdateTime.Get(); }
			internal set { this.lastUpdateTime.Set(value); }
		}

		/// <summary>
		/// Primary time step in seconds
		/// </summary>
		/// <value></value>
		public long Step
		{
			get
			{
				if (primitiveStep == null)
				{
					primitiveStep = step.Get();
				}
				return primitiveStep.Value;
			}
		}

		/// <summary>
		/// number of datasources defined in the RRD.
		/// </summary>
		/// <value></value>
		public int DataSourceCount
		{
			get
			{
				if (primitiveDsCount == null)
				{
					primitiveDsCount = dsCount.Get();
				}
				return primitiveDsCount.Value;
			}
		}

		/// <summary>
		/// Number of archives defined
		/// </summary>
		/// <value></value>
		public int ArchiveCount
		{
			get
			{
				if (primitiveArcCount == null)
				{
					primitiveArcCount = arcCount.Get();
				}
				return primitiveArcCount.Value;
			}
		}

		public String Dump()
		{
			return "== HEADER ==\n" +
			       "signature:" + Signature +
			       " lastUpdateTime:" + LastUpdateTime +
			       " step:" + Step +
			       " dsCount:" + DataSourceCount +
			       " arcCount:" + ArchiveCount + "\n";
		}

		internal void AppendXml(XmlWriter writer)
		{
			writer.WriteComment(signature.Get());
			writer.WriteElementString("version", RRDTOOL_VERSION);
			writer.WriteComment("Seconds");
			writer.WriteElementString("step", step.Get().ToString());
			writer.WriteComment(LastUpdateTime.ToDateTime().ToString());
			writer.WriteElementString("lastupdate", lastUpdateTime.Get().ToString());
		}

		private bool IsJRobinHeader
		{
			get { return signature.Get().StartsWith(SIGNATURE); }
		}

		internal void ValidateHeader()
		{
			if (!IsJRobinHeader)
			{
				throw new RrdException("Invalid file header. File [" + parentDb.GetCanonicalPath() + "] is not a JRobin RRD file");
			}
		}
	}
}