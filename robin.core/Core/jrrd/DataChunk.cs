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
	/// Models a chunk of result data from an RRDatabase.
	/// 
	/// @author <a href="mailto:ciaran@codeloop.com">Ciaran Treanor</a>
	/// @version $Revision$
	/// </summary>
	internal class DataChunk
	{
		internal DataChunk(long startTime, int start, int end, long step, int dsCount, int rows)
		{
			StartTime = startTime;
			Step = step;
			Rows = rows;
			Start = start;
			End = end;
			DsCount = dsCount;
			Data = new double[rows][];
			for (int i = 0; i < rows; i++)
			{
				Data[i] = new double[dsCount];
			}
		}

		internal int Rows { get; private set; }
		internal long StartTime { get; private set; }
		internal long Step { get; private set; }
		internal double[][] Data { get; private set; }
		internal int DsCount { get; private set; }
		internal int End { get; private set; }
		internal int Start { get; private set; }

		/// <summary>
		/// Returns a summary of the contents of this data chunk. The first column is
		/// the time (RRD format) and the following columns are the data source
		/// values.
		/// </summary>
		/// <returns></returns>
		public override String ToString()
		{
			var sb = new StringBuilder();
			long time = StartTime;

			for (int row = 0; row < Rows; row++, time += Step)
			{
				sb.Append(time);
				sb.Append(": ");

				for (int ds = 0; ds < DsCount; ds++)
				{
					sb.Append(Data[row][ds]);
					sb.Append(" ");
				}

				sb.AppendLine();
			}

			return sb.ToString();
		}
	}
}