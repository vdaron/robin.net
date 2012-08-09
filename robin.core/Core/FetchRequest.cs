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
using System.Linq;

namespace robin.core
{
	/// <summary>
	/// Class to represent fetch request. For the complete explanation of all
	/// fetch parameters consult RRDTool's
	/// <a href="../../../../man/rrdfetch.html" target="man">rrdfetch man page</a>.
	/// You cannot create <code>FetchRequest</code> directly (no public constructor
	/// is provided). Use {@link RrdDb#createFetchRequest(String, long, long, long)
	/// createFetchRequest()} method of your {@link RrdDb RrdDb} object.
	/// 
	/// @author <a href="mailto:saxon@jrobin.org">Sasa Markovic</a>
	/// </summary>
	public class FetchRequest
	{
		private String[] filter;

		internal FetchRequest(RrdDb parentDb, ConsolidationFunction consolFun, long fetchStart, long fetchEnd, long resolution)
		{
			ParentDb = parentDb;
			ConsolidationFunction = consolFun;
			FetchStart = fetchStart;
			FetchEnd = fetchEnd;
			Resolution = resolution;
			Validate();
		}

		/// <summary>
		/// Consolitation function to be used during the fetch process.
		/// </summary>
		/// <value></value>
		public ConsolidationFunction ConsolidationFunction { get; set; }

		/// <summary>
		/// starting timestamp to be used for the fetch request.
		/// </summary>
		/// <value></value>
		public long FetchStart { get; set; }

		/// <summary>
		/// Ending timestamp to be used for the fetch request.
		/// </summary>
		/// <value></value>
		public long FetchEnd { get; set; }

		/// <summary>
		/// fetch resolution to be used for the fetch request.
		/// </summary>
		/// <value></value>
		public long Resolution { get; set; }

		/// <summary>
		/// The underlying RrdDb object.
		/// </summary>
		/// <returns></returns>
		internal RrdDb ParentDb { get; set; }

		/// <summary>
		/// Sets request filter in order to fetch data only for
		/// the specified array of datasources (datasource names).
		/// If not set (or set to null), fetched data will
		/// containt values of all datasources defined in the corresponding RRD.
		/// To fetch data only from selected
		/// datasources, specify an array of datasource names as method argument.
		/// </summary>
		/// <param name="filter">Array of datsources (datsource names) to fetch data from.</param>
		public void SetFilter(IEnumerable<String> filter)
		{
			this.filter = filter.ToArray();
		}

		/// <summary>
		/// Sets request filter in order to fetch data only for
		/// a single datasource (datasource name).
		/// If not set (or set to null), fetched data will
		/// containt values of all datasources defined in the corresponding RRD.
		/// To fetch data for a single datasource only,
		/// specify an array of datasource names as method argument.
		/// </summary>
		/// <param name="filter">datsource names to fetch data from.</param>
		public void SetFilter(params String[] filter)
		{
			this.filter = filter;
		}

		/// <summary>
		/// Returns request filter. See {@link #setFilter(String[]) setFilter()} for
		/// complete explanation.
		/// </summary>
		/// <returns></returns>
		public String[] GetFilter()
		{
			return filter;
		}

		private void Validate()
		{
			if (!ArcDef.ValidConsolFun(ConsolidationFunction))
			{
				throw new RrdException("Invalid consolidation function in fetch request: " + ConsolidationFunction);
			}
			if (FetchStart < 0)
			{
				throw new RrdException("Invalid start time in fetch request: " + FetchStart);
			}
			if (FetchEnd < 0)
			{
				throw new RrdException("Invalid end time in fetch request: " + FetchEnd);
			}
			if (FetchStart > FetchEnd)
			{
				throw new RrdException("Invalid start/end time in fetch request: " + FetchStart +
				                       " > " + FetchEnd);
			}
			if (Resolution <= 0)
			{
				throw new RrdException("Invalid resolution in fetch request: " + Resolution);
			}
		}

		/// <summary>
		/// Dumps the content of fetch request using the syntax of RRDTool's fetch command.
		/// </summary>
		/// <returns></returns>
		public String Dump()
		{
			return "fetch \"" + ParentDb.GetRrdBackend().Path +
			       "\" " + ConsolidationFunction + " --start " + FetchStart + " --end " + FetchEnd +
			       (Resolution > 1 ? " --resolution " + Resolution : "");
		}

		/// <summary>
		/// Returns data from the underlying RRD and puts it in a single
		/// <see cref="core.FetchData"/> object.
		/// </summary>
		/// <returns>FetchData object filled with timestamps and datasource values.</returns>
		public FetchData FetchData()
		{
			return ParentDb.FetchData(this);
		}
	}
}