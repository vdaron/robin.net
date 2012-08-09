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
using System.Threading;

namespace robin.core.jrrd
{
	/// <summary>
	/// Instances of this class model a data source in an RRD file.
	/// 
	/// @author <a href="mailto:ciaran@codeloop.com">Ciaran Treanor</a>
	/// </summary>
	internal class DataSource
	{
		private readonly long offset;
		private readonly long size;
		private readonly DataSourceType type;

		internal DataSource(RRDFile file)
		{
			offset = file.FilePointer;
			Name = file.ReadString(Constants.DS_NAM_SIZE);
			if(!Enum.TryParse(file.ReadString(Constants.DST_SIZE), true, out type))
				throw new InvalidCastException(file.ReadString(Constants.DST_SIZE) + " is not a valid DataSourceType");

			file.Align(8);

			MinimumHeartbeat = file.ReadInt(true);

			file.Align(8);

			Minimum = file.ReadDouble();
			Maximum = file.ReadDouble();

			// Skip rest of ds_def_t.par[]
			file.Align();
			file.SkipBytes(56);

			size = file.FilePointer - offset;
		}

		internal void LoadPdpStatusBlock(RRDFile file)
		{
			PDPStatusBlock = new PrimaryDataPointStatusBlock(file);
		}

		/// <summary>
		/// Returns the primary data point status block for this data source.
		/// </summary>
		public PrimaryDataPointStatusBlock PDPStatusBlock{get;private set;}

		/// <summary>
		/// Returns the minimum required heartbeat for this data source.
		/// </summary>
		/// <value></value>
		public int MinimumHeartbeat { get; private set; }

		/// <summary>
		/// Returns the minimum value input to this data source can have.
		/// </summary>
		/// <value></value>
		public double Minimum { get; private set; }

		/// <summary>
		/// Returns the type this data source is.
		/// </summary>
		/// <value></value>
		public DataSourceType Type { get { return type; } }

		/// <summary>
		/// Returns the maximum value input to this data source can have.
		/// </summary>
		/// <value></value>
		public double Maximum{get; private set; }

		/// <summary>
		/// Returns the name of this data source.
		/// </summary>
		/// <value></value>
		public string Name { get; private set; }

		internal void PrintInfo(TextWriter s, string numberFormat)
		{
			var sb = new StringBuilder("ds[");

			sb.Append(Name);
			s.Write(sb);
			s.Write("].type = \"");
			s.Write(type);
			s.WriteLine("\"");
			s.Write(sb);
			s.Write("].minimal_heartbeat = ");
			s.WriteLine(MinimumHeartbeat);
			s.Write(sb);
			s.Write("].min = ");
			s.WriteLine(Double.IsNaN(Minimum)
			            	? "NaN"
			            	: Minimum.ToString(numberFormat));
			s.Write(sb);
			s.Write("].max = ");
			s.WriteLine(Double.IsNaN(Maximum)
			            	? "NaN"
			            	: Maximum.ToString(numberFormat));
			s.Write(sb);
			s.Write("].last_ds = ");
			s.WriteLine(PDPStatusBlock.LastReading);
			s.Write(sb);
			s.Write("].value = ");

			double value = PDPStatusBlock.Value;

			s.WriteLine(Double.IsNaN(value)
			            	? "NaN"
			            	: value.ToString(numberFormat));
			s.Write(sb);
			s.Write("].unknown_sec = ");
			s.WriteLine(PDPStatusBlock.UnknownSeconds);
		}

		internal void ToXml(TextWriter s)
		{
			s.WriteLine("\t<ds>");
			s.Write("\t\t<name> ");
			s.Write(Name);
			s.WriteLine(" </name>");
			s.Write("\t\t<type> ");
			s.Write(type);
			s.WriteLine(" </type>");
			s.Write("\t\t<minimal_heartbeat> ");
			s.Write(MinimumHeartbeat);
			s.WriteLine(" </minimal_heartbeat>");
			s.Write("\t\t<min> ");
			s.Write(Minimum);
			s.WriteLine(" </min>");
			s.Write("\t\t<max> ");
			s.Write(Maximum);
			s.WriteLine(" </max>");
			s.WriteLine();
			s.WriteLine("\t\t<!-- PDP Status -->");
			s.Write("\t\t<last_ds> ");
			s.Write(PDPStatusBlock.LastReading);
			s.WriteLine(" </last_ds>");
			s.Write("\t\t<value> ");
			s.Write(PDPStatusBlock.Value);
			s.WriteLine(" </value>");
			s.Write("\t\t<unknown_sec> ");
			s.Write(PDPStatusBlock.UnknownSeconds);
			s.WriteLine(" </unknown_sec>");
			s.WriteLine("\t</ds>");
			s.WriteLine();
		}

		/// <summary>
		/// Returns a summary the contents of this data source.
		/// </summary>
		/// <returns></returns>
		public override String ToString()
		{
			var sb = new StringBuilder("[DataSource: OFFSET=0x");

			sb.Append(offset.ToString("x"));
			sb.Append(", SIZE=0x");
			sb.Append(size.ToString("x"));
			sb.Append(", name=");
			sb.Append(Name);
			sb.Append(", type=");
			sb.Append(type.ToString());
			sb.Append(", minHeartbeat=");
			sb.Append(MinimumHeartbeat);
			sb.Append(", min=");
			sb.Append(Minimum);
			sb.Append(", max=");
			sb.Append(Maximum);
			sb.Append("]");
			sb.Append("\n\t\t");
			sb.Append(PDPStatusBlock.ToString());

			return sb.ToString();
		}
	}
}