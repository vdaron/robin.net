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
using robin.core;

namespace robin.data
{
	/// <summary>
	/// Simple class which holds aggregated values (MIN, MAX, FIRST, LAST, AVERAGE and TOTAL). You
	/// don't need to create objects of this class directly. Objects of this class are returned from
	/// <code>getAggregates()</code> method in
	/// {@link org.jrobin.core.FetchData#getAggregates(String) FetchData} and
	/// {@link DataProcessor#getAggregates(String)} DataProcessor} classes.
	/// </summary>
	public class Aggregates
	{
		internal Aggregates()
		{
			Average = Double.NaN;
			First = Double.NaN;
			Last = Double.NaN;
			Max = Double.NaN;
			Min = Double.NaN;
			Total = Double.NaN;
		}

		/// <summary>
		/// The minimal value
		/// </summary>
		public double Min { get; set; }

		/// <summary>
		/// The maximum value
		/// </summary>
		public double Max { get; set; }

		/// <summary>
		/// The first falue
		/// </summary>
		public double First { get; set; }

		/// <summary>
		/// the last value
		/// </summary>
		/// <value></value>
		public double Last { get; set; }

		/// <summary>
		/// average
		/// </summary>
		/// <value></value>
		public double Average { get; set; }

		/// <summary>
		/// total value
		/// </summary>
		public double Total { get; set; }

		/// <summary>
		/// Returns single aggregated value for the give consolidation function
		/// </summary>
		/// <param name="consolFun">Consolidation Function</param>
		/// <returns>aggregated value</returns>
		public double GetAggregate(ConsolidationFunction consolFun)
		{
			switch (consolFun)
			{
				case ConsolidationFunction.AVERAGE:
					return Average;
				case ConsolidationFunction.MIN:
					return Min;
				case ConsolidationFunction.MAX:
					return Max;
				case ConsolidationFunction.LAST:
					return Last;
				case ConsolidationFunction.FIRST:
					return First;
				case ConsolidationFunction.TOTAL:
					return Total;
				default:
					throw new ArgumentOutOfRangeException("consolFun");
			}
		}

		/// <summary>
		/// Returns String representing all aggregated values. Just for debugging purposes.
		/// </summary>
		/// <returns></returns>
		public override String ToString()
		{
			return "MIN=" + Util.FormatDouble(Min) + ", MAX=" + Util.FormatDouble(Max) + "\n" +
			       "FIRST=" + Util.FormatDouble(First) + ", LAST=" + Util.FormatDouble(Last) + "\n" +
			       "AVERAGE=" + Util.FormatDouble(Average) + ", TOTAL=" + Util.FormatDouble(Total);
		}
	}
}