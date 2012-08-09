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
using System.Diagnostics;
using System.Text;

namespace robin.core
{
	internal class RrdDoubleArray : RrdPrimitive
	{
		private readonly int length;

		internal RrdDoubleArray(IRrdUpdater updater, int length) : base(updater, RRD_DOUBLE, length, false)
		{
			this.length = length;
		}

		internal void Set(int index, double value)
		{
			Set(index, value, 1);
		}

		internal void Set(int index, double value, int count)
		{
			// rollovers not allowed!
			Debug.Assert(index + count <= length,"Invalid robin index supplied: index=" + index + ", count=" + count + ", length=" + length);
			WriteDouble(index, value, count);
		}

		internal double Get(int index)
		{
			Debug.Assert(index < length, "Invalid index supplied: " + index + ", length=" + length);
			return ReadDouble(index);
		}

		internal double[] Get(int index, int count)
		{
			Debug.Assert(index + count <= length,
			             "Invalid index/count supplied: " + index + "/" + count + " (length=" + length + ")");
			return ReadDouble(index, count);
		}

		public override string ToString()
		{
			StringBuilder result = new StringBuilder();
			for (int i = 0; i < length; i++)
			{
				if(i > 0)
					result.Append(",");
				result.Append(Get(i));
			}
			return result.ToString();
		}
	}
}