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
using System.IO;
using System.Text;

namespace robin.core.jrrd
{
	/// <summary>
	/// Instances of this class model the header section of an RRD file.
	/// </summary>
	internal class Header
	{
		private static long offset;
		private readonly long size;

		internal Header(RRDFile file)
		{
			if (file.ReadString(4) != Constants.COOKIE)
			{
				throw new IOException("Invalid COOKIE");
			}

			Version = file.ReadString(5);
			IntVersion = int.Parse(Version);
			if (IntVersion > 3)
			{
				throw new IOException("Unsupported RRD version (" + Version + ")");
			}

			file.Align();

			// Consume the FLOAT_COOKIE
			file.ReadDouble();

			DataSourceCount = file.ReadInt();
			ArchiveCount = file.ReadInt();
			PrimaryDataPointStep = file.ReadInt();

			// Skip rest of stat_head_t.par
			file.Align();
			file.SkipBytes(80);

			size = file.FilePointer - offset;
		}

		/// <summary>
		/// Returns the version of the database.
		/// </summary>
		/// <value></value>
		public string Version { get;private set;}

		/// <summary>
		/// Returns the version of the database as int.
		/// </summary>
		/// <value></value>
		public int IntVersion { get; private set; }

		/// <summary>
		/// Returns the number of <code>DataSource</code>s in the database.
		/// </summary>
		/// <value></value>
		public int DataSourceCount { get; private set; }

		/// <summary>
		/// Returns the number of <code>Archive</code>s in the database.
		/// </summary>
		/// <value></value>
		public int ArchiveCount { get; private set; }

		/// <summary>
		/// Returns the primary data point interval in seconds.
		/// </summary>
		/// <value></value>
		public int PrimaryDataPointStep { get; private set; }

		/// <summary>
		/// Returns a summary the contents of this header.
		/// </summary>
		/// <returns></returns>
		public override String ToString()
		{
			var sb = new StringBuilder("[Header: OFFSET=0x00, SIZE=0x");

			sb.Append(size.ToString("X"));
			sb.Append(", version=");
			sb.Append(Version);
			sb.Append(", dsCount=");
			sb.Append(DataSourceCount);
			sb.Append(", rraCount=");
			sb.Append(ArchiveCount);
			sb.Append(", pdpStep=");
			sb.Append(PrimaryDataPointStep);
			sb.Append("]");

			return sb.ToString();
		}
	}
}