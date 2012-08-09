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
using robin.core.jrrd;

namespace robin.core
{
	internal class RrdToolReader : DataImporter
	{
		private RRDatabase rrd;

		internal RrdToolReader(String rrdPath)
		{
			rrd = new RRDatabase(rrdPath);
		}

		public override string Version
		{
			get { return rrd.Header.Version; }
		}

		public override long LastUpdateTime
		{
			get { return Util.GetTimestamp(rrd.LastUpdate); }
		}

		public override long Step
		{
			get { return rrd.Header.PrimaryDataPointStep; }
		}

		public override int DataSourceCount
		{
			get { return rrd.Header.DataSourceCount; }
		}

		public override int ArchiveCount
		{
			get { return rrd.ArchiveCount; }
		}

		public override String GetDataSourceName(int dsIndex)
		{
			return rrd.GetDataSourceAt(dsIndex).Name;
		}

		public override String GetDataSourceType(int dsIndex)
		{
			return rrd.GetDataSourceAt(dsIndex).Type.ToString();
		}

		public override long GetDataSourceHeartbeat(int dsIndex)
		{
			return rrd.GetDataSourceAt(dsIndex).MinimumHeartbeat;
		}

		public override double GetDataSourceMinValue(int dsIndex)
		{
			return rrd.GetDataSourceAt(dsIndex).Minimum;
		}

		public override double GetDataSourceMaxValue(int dsIndex)
		{
			return rrd.GetDataSourceAt(dsIndex).Maximum;
		}

		public override double GetDataSourceLastValue(int dsIndex)
		{
			String valueStr = rrd.GetDataSourceAt(dsIndex).PDPStatusBlock.LastReading;
			return Util.ParseDouble(valueStr);
		}

		public override double GetDataSourceAccumulatedValue(int dsIndex)
		{
			return rrd.GetDataSourceAt(dsIndex).PDPStatusBlock.Value;
		}

		public override long GetDataSourceNanSeconds(int dsIndex)
		{
			return rrd.GetDataSourceAt(dsIndex).PDPStatusBlock.UnknownSeconds;
		}

		public override ConsolidationFunction GetArchiveConsolisationFunction(int arcIndex)
		{
			switch (rrd.GetArchiveAt(arcIndex).Type)
			{
				case ConsolidationFunctionType.Average:
					return ConsolidationFunction.AVERAGE;
				case ConsolidationFunctionType.Min:
					return ConsolidationFunction.MIN;
				case ConsolidationFunctionType.Max:
					return ConsolidationFunction.MAX;
				case ConsolidationFunctionType.Last:
					return ConsolidationFunction.LAST;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public override double GetArchiveXff(int arcIndex)
		{
			return rrd.GetArchiveAt(arcIndex).Xff;
		}

		public override int GetArchiveSteps(int arcIndex)
		{
			return rrd.GetArchiveAt(arcIndex).PdpCount;
		}

		public override int GetArchiveRows(int arcIndex)
		{
			return rrd.GetArchiveAt(arcIndex).RowCount;
		}

		public override double GetArchiveStateAccumulatedValue(int arcIndex, int dsIndex)
		{
			return rrd.GetArchiveAt(arcIndex).GetCDPStatusBlock(dsIndex).Value;
		}

		public override int GetArchiveStateNanSteps(int arcIndex, int dsIndex)
		{
			return rrd.GetArchiveAt(arcIndex).GetCDPStatusBlock(dsIndex).UnknownDatapoints;
		}

		public override double[] GetArchiveValues(int arcIndex, int dsIndex)
		{
			return rrd.GetArchiveAt(arcIndex).GetValues()[dsIndex];
		}

		public override void Dispose()
		{
			if (rrd != null)
			{
				rrd.Close();
				rrd = null;
			}
		}
	}
}