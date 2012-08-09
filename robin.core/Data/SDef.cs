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
	internal class SDef : Source
	{
		private readonly ConsolidationFunction consolFun;
		private readonly String defName;
		private double value;

		public SDef(String name, String defName, ConsolidationFunction consolFun)
			: base(name)
		{
			this.defName = defName;
			this.consolFun = consolFun;
		}

		internal string DefName
		{
			get { return defName; }
		}

		internal ConsolidationFunction ConsolidationFunction
		{
			get { return consolFun; }
		}

		public void SetValue(double value)
		{
			this.value = value;
			int count = Timestamps.Length;
			var values = new double[count];
			for (int i = 0; i < count; i++)
			{
				values[i] = value;
			}
			Values = values;
		}

		public override Aggregates GetAggregates(long tStart, long tEnd)
		{
			var agg = new Aggregates();
			agg.First = agg.Last = agg.Min = agg.Max = agg.Average = value;
			agg.Total = value*(tEnd - tStart);
			return agg;
		}

		public override double GetPercentile(long tStart, long tEnd, double percentile)
		{
			return value;
		}
	}
}