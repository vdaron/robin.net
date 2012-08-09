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

namespace robin.data
{
	internal abstract class Source
	{
		protected Source(String name)
		{
			Name = name;
		}

		protected internal string Name { get; protected set; }

		public virtual double[] Values { get; set; }

		public virtual long[] Timestamps { get; set; }

		public virtual Aggregates GetAggregates(long tStart, long tEnd)
		{
			var agg = new Aggregator(Timestamps, Values);
			return agg.GetAggregates(tStart, tEnd);
		}

		public virtual double GetPercentile(long tStart, long tEnd, double percentile)
		{
			var agg = new Aggregator(Timestamps, Values);
			return agg.GetPercentile(tStart, tEnd, percentile);
		}
	}
}