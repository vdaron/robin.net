/*******************************************************************************
 * Copyright (c) 2011 Craig Miskell
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
	internal class PercentileDef : Source
	{
		private readonly double m_percentile;
		private readonly Source m_source;

		private double m_value;

		public PercentileDef(String name, Source source, double percentile) : base(name)
		{
			m_percentile = percentile;
			m_source = source;

			//The best we can do at this point; until this object has it's value realized over a 
			// particular time period (with calculate()), there's not much else to do
			SetValue(Double.NaN);
		}

		public override long[] Timestamps
		{
			get { return base.Timestamps; }
			set
			{
				base.Timestamps = value;
				//And now also call setValue with the current value, to sort out "values"
				SetValue(m_value);
			}
		}

		/// <summary>
		/// Realize the calculation of this definition, over the given time period
		/// </summary>
		/// <param name="tStart"></param>
		/// <param name="tEnd"></param>
		public void Calculate(long tStart, long tEnd)
		{
			if (m_source != null)
			{
				SetValue(m_source.GetPercentile(tStart, tEnd, m_percentile));
			}
		}

		/// <summary>
		/// Takes the given value and puts it in each position in the 'values' array.
		/// </summary>
		/// <param name="value"></param>
		private void SetValue(double value)
		{
			m_value = value;
			long[] times = Timestamps;
			if (times != null)
			{
				int count = times.Length;
				var values = new double[count];
				for (int i = 0; i < count; i++)
				{
					values[i] = m_value;
				}
				Values = values;
			}
		}

		/// <summary>
		/// Same as SDef; the aggregates of a static value are all just the
		/// same static value.
		/// 
		/// Assumes this def has been realized by calling calculate(), otherwise
		/// the aggregated values will be NaN
		/// </summary>
		/// <param name="tStart"></param>
		/// <param name="tEnd"></param>
		/// <returns></returns>
		public override Aggregates GetAggregates(long tStart, long tEnd)
		{
			var agg = new Aggregates();
			agg.First = agg.Last = agg.Min = agg.Max = agg.Average = m_value;
			agg.Total = m_value*(tEnd - tStart);
			return agg;
		}

		/// <summary>
		/// Returns just the calculated percentile; the "Xth" percentile of a static value is
		/// the static value itself.
		///   
		/// Assumes this def has been realized by calling calculate(), otherwise
		/// the aggregated values will be NaN
		/// </summary>
		/// <param name="tStart"></param>
		/// <param name="tEnd"></param>
		/// <param name="percentile"></param>
		/// <returns></returns>
		public override double GetPercentile(long tStart, long tEnd, double percentile)
		{
			return m_value;
		}
	}
}