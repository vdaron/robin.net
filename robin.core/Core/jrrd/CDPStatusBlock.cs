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
	/// Instances of this class model the consolidation data point status from an RRD file.
	/// 
	/// @author <a href="mailto:ciaran@codeloop.com">Ciaran Treanor</a>
	/// </summary>
	internal class CDPStatusBlock
	{
		private readonly long offset;
		private readonly long size;

		internal CDPStatusBlock(RRDFile file)
		{
			offset = file.FilePointer;
			Value = file.ReadDouble();
			UnknownDatapoints = file.ReadInt();
			file.Align(8);
			// Skip rest of cdp_prep_t.scratch
			file.SkipBytes(64);

			size = file.FilePointer - offset;
		}

		/// <summary>
		/// Returns the number of unknown primary data points that were integrated.
		/// </summary>
		/// <value></value>
		public int UnknownDatapoints { get; private set; }

		/// <summary>
		/// Returns the value of this consolidated data point.
		/// </summary>
		/// <value>Value of this consolidated data point.</value>
		public double Value { get; private set; }

		internal void ToXml(TextWriter s)
		{
			s.Write("\t\t\t<ds><value> ");
			s.Write(Value);
			s.Write(" </value>  <unknown_datapoints> ");
			s.Write(UnknownDatapoints);
			s.WriteLine(" </unknown_datapoints></ds>");
		}

		/// <summary>
		/// Returns a summary the contents of this CDP status block.
		/// </summary>
		/// <returns></returns>
		public override String ToString()
		{
			var sb = new StringBuilder("[CDPStatusBlock: OFFSET=0x");

			sb.Append(offset.ToString("X"));
			sb.Append(", SIZE=0x");
			sb.Append(size.ToString("X"));
			sb.Append(", unknownDatapoints=");
			sb.Append(UnknownDatapoints);
			sb.Append(", value=");
			sb.Append(Value);
			sb.Append("]");

			return sb.ToString();
		}
	}
}