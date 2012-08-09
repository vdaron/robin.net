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
using System.Text;

namespace robin.core.jrrd
{
	/// <summary>
	/// Instances of this class model the primary data point status from an RRD file.
	/// 
	/// @author <a href="mailto:ciaran@codeloop.com">Ciaran Treanor</a>
	/// @version $Revision$
	/// </summary>
	internal class PrimaryDataPointStatusBlock
	{
		private readonly long offset;
		private readonly long size;

		internal PrimaryDataPointStatusBlock(RRDFile file)
		{
			offset = file.FilePointer;
			LastReading = file.ReadString(Constants.LAST_DS_LEN);

			file.Align(4);

			UnknownSeconds = file.ReadInt();

			file.Align(8); //8 bytes per scratch value in pdp_prep; align on that

			Value = file.ReadDouble();

			// Skip rest of pdp_prep_t.par[]
			file.SkipBytes(64);

			size = file.FilePointer - offset;
		}

		/// <summary>
		/// Returns the last reading from the data source.
		/// </summary>
		/// <value></value>
		public string LastReading { get;private set; }

		/// <summary>
		/// Returns the current value of the primary data point.
		/// </summary>
		public double Value { get; private set; }

		/// <summary>
		/// Returns the number of seconds of the current primary data point is
		/// unknown data.
		/// </summary>
		public int UnknownSeconds { get; private set; }

		/// <summary>
		/// Returns a summary the contents of thisPrimaryDataPoint Status block.
		/// </summary>
		/// <returns></returns>
		public override String ToString()
		{
			var sb = new StringBuilder("[PrimaryDataPointStatus: OFFSET=0x");

			sb.Append(offset.ToString("x"));
			sb.Append(", SIZE=0x");
			sb.Append(size.ToString("x"));
			sb.Append(", lastReading=");
			sb.Append(LastReading);
			sb.Append(", unknownSeconds=");
			sb.Append(UnknownSeconds);
			sb.Append(", value=");
			sb.Append(Value);
			sb.Append("]");

			return sb.ToString();
		}
	}
}