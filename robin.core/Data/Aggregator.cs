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
using System.Collections.Generic;
using System.Diagnostics;
using robin.core;

namespace robin.data
{
	internal class Aggregator
	{
		private readonly long step;
		private readonly long[] timestamps;
		private readonly double[] values;

		internal Aggregator(long[] timestamps, double[] values)
		{
			Debug.Assert(timestamps.Length == values.Length, "Incompatible timestamps/values arrays (unequal lengths)");
			Debug.Assert(timestamps.Length >= 2, "At least two timestamps must be supplied");
			this.timestamps = timestamps;
			this.values = values;
			step = timestamps[1] - timestamps[0];
		}

		internal Aggregates GetAggregates(long tStart, long tEnd)
		{
			var agg = new Aggregates();
			long totalSeconds = 0;
			bool firstFound = false;
			for (int i = 0; i < timestamps.Length; i++)
			{
				long left = Math.Max(timestamps[i] - step, tStart);
				long right = Math.Min(timestamps[i], tEnd);
				long delta = right - left;

				// delta is only > 0 when the timestamp for a given buck is within the range of tStart and tEnd
				if (delta > 0)
				{
					double value = values[i];
					agg.Min = Util.Min(agg.Min, value);
					agg.Max = Util.Max(agg.Max, value);
					if (!firstFound)
					{
						agg.First = value;
						firstFound = true;
						agg.Last = value;
					}
					else if (delta >= step)
					{
						// an entire bucket is included in this range
						agg.Last = value;

						/*
					 * Algorithmically, we're only updating last if it's either the first
					 * bucket encountered, or it's a "full" bucket.

					if ( !isInRange(tEnd, left, right) ||
							 (isInRange(tEnd, left, right) && !Double.isNaN(value))
							 ) {
							agg.last = value;
						}
					*/
					}
					if (!Double.IsNaN(value))
					{
						agg.Total = Util.Sum(agg.Total, delta*value);
						totalSeconds += delta;
					}
				}
			}
			agg.Average = totalSeconds > 0 ? (agg.Total/totalSeconds) : Double.NaN;
			return agg;
		}

		internal double GetPercentile(long tStart, long tEnd, double percentile)
		{
			var valueList = new List<Double>();
			// create a list of included datasource values (different from NaN)
			for (int i = 0; i < timestamps.Length; i++)
			{
				long left = Math.Max(timestamps[i] - step, tStart);
				long right = Math.Min(timestamps[i], tEnd);
				if (right > left && !Double.IsNaN(values[i]))
				{
					valueList.Add(values[i]);
				}
			}
			// create an array to work with
			int count = valueList.Count;
			if (count > 1)
			{
				var valuesCopy = new double[count];
				for (int i = 0; i < count; i++)
				{
					valuesCopy[i] = valueList[i];
				}
				// sort array
				Array.Sort(valuesCopy);
				// skip top (100% - percentile) values
				double topPercentile = (100.0 - percentile)/100.0;
				count -= (int) Math.Ceiling(count*topPercentile);
				// if we have anything left...
				if (count > 0)
				{
					return valuesCopy[count - 1];
				}
			}
			// not enough data available
			return Double.NaN;
		}
	}
}