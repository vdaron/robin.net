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
class Normalizer {
	private long[] timestamps;
	int count;
	long step;

	internal Normalizer(long[] timestamps) {
		this.timestamps = timestamps;
		this.step = timestamps[1] - timestamps[0];
		this.count = timestamps.Length;
	}

	internal double[] normalize(long[] rawTimestamps, double[] rawValues) {
		int rawCount = rawTimestamps.Length;
		long rawStep = rawTimestamps[1] - rawTimestamps[0];
		// check if we have a simple match
		if (rawCount == count && rawStep == step && rawTimestamps[0] == timestamps[0]) {
			return getCopyOf(rawValues);
		}
		// reset all normalized values to NaN
		double[] values = new double[count];
		Array.ForEach(values, x => x = Double.NaN);
		for (int rawSeg = 0, seg = 0; rawSeg < rawCount && seg < count; rawSeg++) {
			double rawValue = rawValues[rawSeg];
			if (!Double.IsNaN(rawValue)) {
				long rawLeft = rawTimestamps[rawSeg] - rawStep;
				while (seg < count && rawLeft >= timestamps[seg]) {
					seg++;
				}
				bool overlap = true;
				for (int fillSeg = seg; overlap && fillSeg < count; fillSeg++) {
					long left = timestamps[fillSeg] - step;
					long t1 = Math.Max(rawLeft, left);
					long t2 = Math.Min(rawTimestamps[rawSeg], timestamps[fillSeg]);
					if (t1 < t2) {
						values[fillSeg] = Util.Sum(values[fillSeg], (t2 - t1) * rawValues[rawSeg]);
					}
					else {
						overlap = false;
					}
				}
			}
		}
		for (int seg = 0; seg < count; seg++) {
			values[seg] /= step;
		}
		return values;
	}

	private static double[] getCopyOf(double[] rawValues) {
		int n = rawValues.Length;
		double[] values = new double[n];
		Array.Copy(rawValues, 0, values, 0, n);
		return values;
	}
}


}