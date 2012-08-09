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
	/// Class used to interpolate datasource values from the collection of (timestamp, values)
	/// points using natural cubic spline interpolation.<p>
	/// <p/>
	/// <b>WARNING</b>: So far, this class cannot handle NaN datasource values
	/// (an exception will be thrown by the constructor). Future releases might change this.
	/// </summary>
	public class CubicSplineInterpolator : Plottable
	{
		private readonly double[] x;
		private readonly double[] y;

		// second derivates come here
		private int khi;
		private int klo;
		private int n;
		private double[] y2;

		/// <summary>
		/// Creates cubic spline interpolator from arrays of timestamps and corresponding
		/// datasource values.
		/// </summary>
		/// <param name="timestamps">timestamps in seconds</param>
		/// <param name="values">corresponding datasource values</param>
		/// <exception cref="RrdException">
		/// Thrown if supplied arrays do not contain at least 3 values, or if
		/// timestamps are not ordered, or array Lengths are not equal, or some datasource value is NaN.
		/// </exception>
		public CubicSplineInterpolator(long[] timestamps, double[] values)
		{
			x = new double[timestamps.Length];
			for (int i = 0; i < timestamps.Length; i++)
			{
				x[i] = timestamps[i];
			}
			y = values;
			Validate();
			Spline();
		}

		/// <summary>
		/// Creates cubic spline interpolator from arrays of DateTime and corresponding
		/// datasource values.
		/// </summary>
		/// <param name="dates">timestamps in seconds</param>
		/// <param name="values">corresponding datasource values</param>
		/// <exception cref="RrdException">
		/// Thrown if supplied arrays do not contain at least 3 values, or if
		/// timestamps are not ordered, or array Lengths are not equal, or some datasource value is NaN.
		/// </exception>
		public CubicSplineInterpolator(DateTime[] dates, double[] values)
		{
			x = new double[dates.Length];
			for (int i = 0; i < dates.Length; i++)
			{
				x[i] = Util.GetTimestamp(dates[i]);
			}
			y = values;
			Validate();
			Spline();
		}


		/// <summary>
		/// Creates cubic spline interpolator for an array of 2D-points.
		/// </summary>
		/// <param name="x">x-axis point coordinates</param>
		/// <param name="y">y-axis point coordinates</param>
		/// <exception cref="RrdException">
		/// Thrown if supplied arrays do not contain at least 3 values, or if
		/// timestamps are not ordered, or array Lengths are not equal, or some datasource value is NaN.
		/// </exception>
		public CubicSplineInterpolator(double[] x, double[] y)
		{
			this.x = x;
			this.y = y;
			Validate();
			Spline();
		}

		private void Validate()
		{
			bool ok = true;
			if (x.Length != y.Length || x.Length < 3)
			{
				ok = false;
			}
			for (int i = 0; i < x.Length - 1 && ok; i++)
			{
				if (x[i] >= x[i + 1] || Double.IsNaN(y[i]))
				{
					ok = false;
				}
			}
			if (!ok)
			{
				throw new RrdException("Invalid plottable data supplied");
			}
		}

		private void Spline()
		{
			n = x.Length;
			y2 = new double[n];
			var u = new double[n - 1];
			y2[0] = y2[n - 1] = 0.0;
			u[0] = 0.0; // natural spline
			for (int i = 1; i <= n - 2; i++)
			{
				double sig = (x[i] - x[i - 1])/(x[i + 1] - x[i - 1]);
				double p = sig*y2[i - 1] + 2.0;
				y2[i] = (sig - 1.0)/p;
				u[i] = (y[i + 1] - y[i])/(x[i + 1] - x[i]) - (y[i] - y[i - 1])/(x[i] - x[i - 1]);
				u[i] = (6.0*u[i]/(x[i + 1] - x[i - 1]) - sig*u[i - 1])/p;
			}
			for (int k = n - 2; k >= 0; k--)
			{
				y2[k] = y2[k]*y2[k + 1] + u[k];
			}
			// prepare everything for getValue()
			klo = 0;
			khi = n - 1;
		}

		/// <summary>
		/// Calculates spline-interpolated y-value for the corresponding x-value. Call
		/// this if you need spline-interpolated values in your code.
		/// </summary>
		/// <param name="xval">x-value</param>
		/// <returns>inteprolated y-value</returns>
		public double GetValue(double xval)
		{
			if (xval < x[0] || xval > x[n - 1])
			{
				return Double.NaN;
			}
			if (xval < x[klo] || xval > x[khi])
			{
				// out of bounds
				klo = 0;
				khi = n - 1;
			}
			while (khi - klo > 1)
			{
				// find bounding interval using bisection method
				int k = (khi + klo) >> 1;
				if (x[k] > xval)
				{
					khi = k;
				}
				else
				{
					klo = k;
				}
			}
			double h = x[khi] - x[klo];
			double a = (x[khi] - xval)/h;
			double b = (xval - x[klo])/h;
			return a*y[klo] + b*y[khi] +
			       ((a*a*a - a)*y2[klo] + (b*b*b - b)*y2[khi])*(h*h)/6.0;
		}

		/// <summary>
		/// Method overriden from the base class. This method will be called by the framework. Call
		/// this method only if you need spline-interpolated values in your code.
		/// </summary>
		/// <param name="timestamp">timestamp in seconds</param>
		/// <returns></returns>
		public override double GetValue(long timestamp)
		{
			return GetValue(timestamp);
		}
	}
}