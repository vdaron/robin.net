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

namespace robin.core
{
	public abstract class DataImporter : IDisposable
	{
		// header

		#region IDisposable Members

		public virtual void Dispose()
		{
			// NOP
		}

		#endregion

		public abstract string Version { get; }

		public abstract long LastUpdateTime { get; }

		public abstract long Step { get; }

		public abstract int DataSourceCount { get; }

		public abstract int ArchiveCount { get; }

		// datasource
		public abstract String GetDataSourceName(int dsIndex);

		public abstract String GetDataSourceType(int dsIndex);

		public abstract long GetDataSourceHeartbeat(int dsIndex);

		public abstract double GetDataSourceMinValue(int dsIndex);

		public abstract double GetDataSourceMaxValue(int dsIndex);

		// datasource state
		public abstract double GetDataSourceLastValue(int dsIndex);

		public abstract double GetDataSourceAccumulatedValue(int dsIndex);

		public abstract long GetDataSourceNanSeconds(int dsIndex);

		// archive
		public abstract ConsolidationFunction GetArchiveConsolisationFunction(int arcIndex);

		public abstract double GetArchiveXff(int arcIndex);

		public abstract int GetArchiveSteps(int arcIndex);

		public abstract int GetArchiveRows(int arcIndex);

		// archive state
		public abstract double GetArchiveStateAccumulatedValue(int arcIndex, int dsIndex);

		public abstract int GetArchiveStateNanSteps(int arcIndex, int dsIndex);

		public abstract double[] GetArchiveValues(int arcIndex, int dsIndex);

		internal long GetEstimatedSize()
		{
			int dsCount = DataSourceCount;
			int arcCount = ArchiveCount;
			int rowCount = 0;
			for (int i = 0; i < arcCount; i++)
			{
				rowCount += GetArchiveRows(i);
			}
			return RrdDef.calculateSize(dsCount, arcCount, rowCount);
		}
	}
}