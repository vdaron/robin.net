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
using System.Diagnostics;
using System.Text;

namespace robin.core
{
	/// <summary>
	/// Class to represent archive values for a single datasource. Robin class is the heart of
	/// the so-called "round robin database" concept. Basically, each Robin object is a
	/// fixed length array of double values. Each double value reperesents consolidated, archived
	/// value for the specific timestamp. When the underlying array of double values gets completely
	/// filled, new values will replace the oldest ones.
	/// 
	/// Robin object does not hold values in memory - such object could be quite large.
	/// Instead of it, Robin reads them from the backend I/O only when necessary.
	/// 
	/// @author <a href="mailto:saxon@jrobin.org">Sasa Markovic</a>
	/// </summary>
	public class Robin : IRrdUpdater
	{
		private readonly Archive parentArc;
		private readonly RrdInt pointer;
		private readonly int rows;
		private readonly RrdDoubleArray values;

		internal Robin(Archive parentArc, int rows, bool shouldInitialize)
		{
			this.parentArc = parentArc;
			pointer = new RrdInt(this);
			values = new RrdDoubleArray(this, rows);
			this.rows = rows;
			if (shouldInitialize)
			{
				pointer.Set(0);
				values.Set(0, Double.NaN, rows);
			}
		}

		#region IRrdUpdater Members

		/// <summary>
		/// Copies object's internal state to another ArcState object.
		/// </summary>
		/// <param name="other"> New ArcState object to copy state to</param>
		public void CopyStateTo(IRrdUpdater other)
		{
			if (!(other is Robin))
			{
				throw new RrdException(
					"Cannot copy Robin object to " + other.GetType().Name);
			}
			var robin = (Robin) other;
			int rowsDiff = rows - robin.rows;
			if (rowsDiff == 0)
			{
				// Identical dimensions. Do copy in BULK to speed things up
				robin.pointer.Set(pointer.Get());
				robin.values.WriteBytes(values.ReadBytes());
			}
			else
			{
				// different sizes
				for (int i = 0; i < robin.rows; i++)
				{
					int j = i + rowsDiff;
					robin.Store(j >= 0 ? GetValue(j) : Double.NaN);
				}
			}
		}

		/// <summary>
		/// Returns the underlying storage (backend) object which actually performs all
		/// I/O operations.
		/// </summary>
		/// <returns></returns>
		public RrdBackend GetRrdBackend()
		{
			return parentArc.GetRrdBackend();
		}

		/// <summary>
		/// Returns the underlying storage (backend) object which actually performs all
		/// I/O operations.
		/// </summary>
		/// <returns></returns>
		public RrdAllocator GetRrdAllocator()
		{
			return parentArc.GetRrdAllocator();
		}

		#endregion

		/// <summary>
		/// Array of double archive values, starting from the oldest one.
		/// </summary>
		/// <returns></returns>
		public double[] GetValues()
		{
			return GetValues(0, rows);
		}

		/// <summary>
		/// Stores single value
		/// </summary>
		/// <param name="newValue"></param>
		internal void Store(double newValue)
		{
			int position = pointer.Get();
			values.Set(position, newValue);
			pointer.Set((position + 1)%rows);
		}

		/// <summary>
		/// Stores the same value several times
		/// </summary>
		/// <param name="newValue"></param>
		/// <param name="bulkCount"></param>
		internal void BulkStore(double newValue, int bulkCount)
		{
			Debug.Assert(bulkCount <= rows, "Invalid number of bulk updates: " + bulkCount +
			                                " rows=" + rows);
			int position = pointer.Get();
			// update tail
			int tailUpdateCount = Math.Min(rows - position, bulkCount);
			values.Set(position, newValue, tailUpdateCount);
			pointer.Set((position + tailUpdateCount)%rows);
			// do we need to update from the start?
			int headUpdateCount = bulkCount - tailUpdateCount;
			if (headUpdateCount > 0)
			{
				values.Set(0, newValue, headUpdateCount);
				pointer.Set(headUpdateCount);
			}
		}

		internal void Update(double[] newValues)
		{
			Debug.Assert(rows == newValues.Length, "Invalid number of robin values supplied (" + newValues.Length +
			                                       "), exactly " + rows + " needed");
			pointer.Set(0);
			values.WriteDouble(0, newValues);
		}

		/// <summary>
		/// Updates archived values in bulk.
		/// </summary>
		/// <param name="newValues">Array of double values to be stored in the archive</param>
		public void SetValues(double[] newValues)
		{
			if (rows != newValues.Length)
			{
				throw new RrdException("Invalid number of robin values supplied (" + newValues.Length +
				                       "), exactly " + rows + " needed");
			}
			Update(newValues);
		}

		/// <summary>
		/// (Re)sets all values in this archive to the same value.
		/// </summary>
		/// <param name="newValue">New value</param>
		public void SetValues(double newValue)
		{
			var vals = new double[rows];
			for (int i = 0; i < vals.Length; i++)
			{
				vals[i] = newValue;
			}
			Update(vals);
		}

		internal String Dump()
		{
			var buffer = new StringBuilder("Robin " + pointer.Get() + "/" + rows + ": ");
			double[] vals = GetValues();
			foreach (double value in vals)
			{
				buffer.Append(Util.FormatDouble(value, true)).Append(" ");
			}
			buffer.Append("\n");
			return buffer.ToString();
		}

		/// <summary>
		/// Returns the i-th value from the Robin archive.
		/// </summary>
		/// <param name="index">index Value index</param>
		/// <returns>Value stored in the i-th position (the oldest value has zero index)</returns>
		public double GetValue(int index)
		{
			int arrayIndex = (pointer.Get() + index)%rows;
			return values.Get(arrayIndex);
		}
		
		/// <summary>
		/// Sets the i-th value in the Robin archive.
		/// </summary>
		/// <param name="index">index in the archive (the oldest value has zero index)</param>
		/// <param name="value">value to be stored</param>
		public void SetValue(int index, double value)
		{
			int arrayIndex = (pointer.Get() + index)%rows;
			values.Set(arrayIndex, value);
		}

		internal double[] GetValues(int index, int count)
		{
			Debug.Assert(count <= rows, "Too many values requested: " + count + " rows=" + rows);
			int startIndex = (pointer.Get() + index)%rows;
			int tailReadCount = Math.Min(rows - startIndex, count);
			double[] tailValues = values.Get(startIndex, tailReadCount);
			if (tailReadCount < count)
			{
				int headReadCount = count - tailReadCount;
				double[] headValues = values.Get(0, headReadCount);
				var vals = new double[count];
				int k = 0;
				foreach (double tailValue in tailValues)
				{
					vals[k++] = tailValue;
				}
				foreach (double headValue in headValues)
				{
					vals[k++] = headValue;
				}
				return vals;
			}
			return tailValues;
		}

		/// <summary>
		/// Archive object to which this Robin object belongs.
		/// </summary>
		/// <value></value>
		public Archive Parent
		{
			get { return parentArc; }
		}

		/// <summary>
		/// the size of the underlying array of archived values.
		/// </summary>
		/// <value></value>
		public int Size
		{
			get { return rows; }
		}

		/// <summary>
		/// Filters values stored in this archive based on the given boundary.
		/// Archived values found to be outside of <code>[minValue, maxValue]</code> interval (inclusive)
		/// will be silently replaced with <code>NaN</code>.
		/// </summary>
		/// <param name="minValue">lower boundary</param>
		/// <param name="maxValue">upper boundary</param>
		public void FilterValues(double minValue, double maxValue)
		{
			for (int i = 0; i < rows; i++)
			{
				double value = values.Get(i);
				if (!Double.IsNaN(minValue) && !Double.IsNaN(value) && minValue > value)
				{
					values.Set(i, Double.NaN);
				}
				if (!Double.IsNaN(maxValue) && !Double.IsNaN(value) && maxValue < value)
				{
					values.Set(i, Double.NaN);
				}
			}
		}
	}
}