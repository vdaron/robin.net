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
	internal class Def : Source
	{
		private readonly String backend;
		private readonly ConsolidationFunction consolFun;
		private readonly String dsName;
		private readonly String path;
		private FetchData fetchData;

		internal Def(String name, FetchData fetchData)
			: this(name, null, name, (ConsolidationFunction) 99, null)//Ugly hack to allow null ConsolFuns without having to play with nullable
		{
			SetFetchData(fetchData);
		}

		internal Def(String name, String path, String dsName, ConsolidationFunction consolFunc) :
			this(name, path, dsName, consolFunc, null)
		{
		}

		internal Def(String name, String path, String dsName, ConsolidationFunction consolFunc, String backend) : base(name)
		{
			this.path = path;
			this.dsName = dsName;
			consolFun = consolFunc;
			this.backend = backend;
		}

		internal string Path
		{
			get { return path; }
		}

		internal String GetCanonicalPath()
		{
			return Util.GetCanonicalPath(path);
		}

		internal string DataSourceName
		{
			get { return dsName; }
		}

		internal ConsolidationFunction ConsolidationFunction
		{
			get { return consolFun; }
		}

		public string Backend
		{
			get { return backend; }
		}

		internal bool IsCompatibleWith(Def def)
		{
			return GetCanonicalPath().Equals(def.GetCanonicalPath()) &&
			       ConsolidationFunction.Equals(def.consolFun) &&
			       ((backend == null && def.backend == null) ||
			        (backend != null && def.backend != null && backend.Equals(def.backend)));
		}

		internal void SetFetchData(FetchData fetchData)
		{
			this.fetchData = fetchData;
		}

		internal long[] RrdTimestamps
		{
			get { return fetchData.Timestamps; }
		}

		internal double[] RrdValues
		{
			get { return fetchData.GetValues(dsName); }
		}

		internal long ArchiveEndTime
		{
			get { return fetchData.ArchiveEndTime; }
		}

		internal long FetchStep
		{
			get { return fetchData.Step; }
		}

		public override Aggregates GetAggregates(long tStart, long tEnd)
		{
			long[] t = RrdTimestamps;
			double[] v = RrdValues;
			var agg = new Aggregator(t, v);
			return agg.GetAggregates(tStart, tEnd);
		}

		public override double GetPercentile(long tStart, long tEnd, double percentile)
		{
			long[] t = RrdTimestamps;
			double[] v = RrdValues;
			var agg = new Aggregator(t, v);
			return agg.GetPercentile(tStart, tEnd, percentile);
		}

		internal bool Loaded
		{
			get { return fetchData != null; }
		}
	}
}