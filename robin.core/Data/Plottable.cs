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
	/// <summary>
	/// <p>Interface to be used for custom datasources.
	/// If you wish to use a custom datasource in a graph, you should create a class implementing this interface
	/// that represents that datasource, and then pass this class on to the RrdGraphDef.</p>
	/// </summary>
	public abstract class Plottable
	{
		/// <summary>
		/// Retrieves datapoint value based on a given timestamp.
		/// Use this method if you only have one series of data in this class.
		/// </summary>
		/// <param name="timestamp">Timestamp in seconds for the datapoint.</param>
		/// <returns></returns>
		public virtual double GetValue(long timestamp)
		{
			return Double.NaN;
		}
	}
}