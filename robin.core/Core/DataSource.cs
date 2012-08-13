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
using System.Xml;

namespace robin.core
{
	/// <summary>
	/// Class to represent single datasource within RRD. Each datasource object holds the
	/// following information: datasource definition (once set, never changed) and
	/// datasource state variables (changed whenever RRD gets updated).
	/// 
	/// Normally, you don't need to manipluate Datasource objects directly, it's up to
	/// JRobin framework to do it for you.
	/// 
	/// @author <a href="mailto:saxon@jrobin.org">Sasa Markovic</a>
	/// </summary>
	public class DataSource : IRrdUpdater
	{
		private static readonly double MAX_32_BIT = Math.Pow(2, 32);
		private static readonly double MAX_64_BIT = Math.Pow(2, 64);
		private readonly RrdDouble accumValue;

		// definition
		private readonly RrdString dsName;
		private readonly RrdString dsType;
		private readonly RrdLong heartbeat;
		private readonly RrdDouble lastValue;
		private readonly RrdDouble maxValue;
		private readonly RrdDouble minValue;
		private readonly RrdLong nanSeconds;
		private readonly RrdDb parentDb;

		// cache
		private String primitiveDsName;
		private DataSourceType? primitiveDsType;

		// state variables

		internal DataSource(RrdDb parentDb, DsDef dsDef)
		{
			bool shouldInitialize = dsDef != null;
			this.parentDb = parentDb;
			dsName = new RrdString(this);
			dsType = new RrdString(this);
			heartbeat = new RrdLong(this);
			minValue = new RrdDouble(this);
			maxValue = new RrdDouble(this);
			lastValue = new RrdDouble(this);
			accumValue = new RrdDouble(this);
			nanSeconds = new RrdLong(this);
			if (shouldInitialize)
			{
				dsName.Set(dsDef.Name);
				primitiveDsName = null;
				dsType.Set(dsDef.Type.ToString());
				primitiveDsType = null;
				heartbeat.Set(dsDef.Heartbeat);
				minValue.Set(dsDef.MinValue);
				maxValue.Set(dsDef.MaxValue);
				lastValue.Set(Double.NaN);
				accumValue.Set(0.0);
				Header header = parentDb.Header;
				nanSeconds.Set(header.LastUpdateTime%header.Step);
			}
		}

		internal DataSource(RrdDb parentDb, DataImporter reader, int dsIndex) : this(parentDb, null)
		{
			dsName.Set(reader.GetDataSourceName(dsIndex));
			primitiveDsName = null;
			dsType.Set(reader.GetDataSourceType(dsIndex));
			primitiveDsType = null;
			heartbeat.Set(reader.GetDataSourceHeartbeat(dsIndex));
			minValue.Set(reader.GetDataSourceMinValue(dsIndex));
			maxValue.Set(reader.GetDataSourceMaxValue(dsIndex));
			lastValue.Set(reader.GetDataSourceLastValue(dsIndex));
			accumValue.Set(reader.GetDataSourceAccumulatedValue(dsIndex));
			nanSeconds.Set(reader.GetDataSourceNanSeconds(dsIndex));
		}

		/// <summary>
		/// Datasource name
		/// </summary>
		public string Name
		{
			get { return primitiveDsName ?? (primitiveDsName = dsName.Get()); }
			internal set
			{
				if (value.Length > RrdPrimitive.STRING_LENGTH)
				{
					throw new RrdException("Invalid datasource name specified: " + value);
				}
				if (parentDb.ContainsDataSource(value))
				{
					throw new RrdException("Datasource already defined in this RRD: " + value);
				}
				dsName.Set(value);
				primitiveDsName = null;
			}
		}

		/// <summary>
		/// Datasource type (GAUGE, COUNTER, DERIVE, ABSOLUTE).
		/// </summary>
		/// <value></value>
		public DataSourceType Type
		{
			get
			{
				if (!primitiveDsType.HasValue)
				{
					DataSourceType t;
					if (!Enum.TryParse(dsType.Get(), true, out t))
						throw new RrdException("Invalid DataSource type");
					primitiveDsType = t;
				}
				return primitiveDsType.Value;
			}
			internal set
			{
				if (!DsDef.ValidDataSourceType(value))
				{
					throw new RrdException("Invalid datasource type: " + value);
				}
				// set datasource type
				dsType.Set(value.ToString());
				primitiveDsType = null;
				// reset datasource status
				lastValue.Set(Double.NaN);
				accumValue.Set(0.0);
				// reset archive status
				int dsIndex = parentDb.GetDataSourceIndex(dsName.Get());
				foreach (Archive archive in parentDb.Archives)
				{
					archive.GetArcState(dsIndex).AccumulatedValue = Double.NaN;
				}
			}
		}

		/// <summary>
		/// Datasource heartbeat
		/// </summary>
		/// <value></value>
		public long Heartbeat
		{
			get { return heartbeat.Get(); }
			internal set
			{
				if (value < 1)
				{
					throw new RrdException("Invalid heartbeat specified: " + value);
				}
				heartbeat.Set(value);
			}
		}

		/// <summary>
		/// Minimal value allowed.
		/// </summary>
		/// <value></value>
		public double MinValue
		{
			get { return minValue.Get(); }
		}

		/// <summary>
		/// Maximal value allowed.
		/// </summary>
		/// <value></value>
		public double MaxValue
		{
			get { return maxValue.Get(); }
		}

		/// <summary>
		/// Last known value of the datasource.
		/// </summary>
		/// <value></value>
		public double LastValue
		{
			get { return lastValue.Get(); }
		}

		/// <summary>
		/// Value this datasource accumulated so far.
		/// </summary>
		/// <value></value>
		public double AccumulatedValue
		{
			get { return accumValue.Get(); }
		}

		/// <summary>
		/// The number of accumulated NaN seconds.
		/// </summary>
		/// <value></value>
		public long NanSeconds
		{
			get { return nanSeconds.Get(); }
		}

		/// <summary>
		/// Index of this Datasource object in the RRD.
		/// </summary>
		/// <value></value>
		public int Index
		{
			get
			{
				try
				{
					return parentDb.GetDataSourceIndex(dsName.Get());
				}
				catch (RrdException)
				{
					return -1;
				}
			}
		}

		#region IRrdUpdater Members

		/// <summary>
		/// Copies object's internal state to another ArcState object.
		/// </summary>
		/// <param name="other"> New ArcState object to copy state to</param>
		public void CopyStateTo(IRrdUpdater other)
		{
			if (!(other is DataSource))
			{
				throw new RrdException("Cannot copy Datasource object to " + other.GetType().Name);
			}
			var datasource = (DataSource) other;
			if (!datasource.dsName.Get().Equals(dsName.Get()))
			{
				throw new RrdException("Incomaptible datasource names");
			}
			if (!datasource.dsType.Get().Equals(dsType.Get()))
			{
				throw new RrdException("Incomaptible datasource types");
			}
			datasource.lastValue.Set(lastValue.Get());
			datasource.nanSeconds.Set(nanSeconds.Get());
			datasource.accumValue.Set(accumValue.Get());
		}

		/// <summary>
		/// Returns the underlying storage (backend) object which actually performs all
		/// I/O operations.
		/// </summary>
		/// <returns></returns>
		public RrdBackend GetRrdBackend()
		{
			return parentDb.GetRrdBackend();
		}

		/// <summary>
		/// Returns the underlying storage (backend) object which actually performs all
		/// I/O operations.
		/// </summary>
		/// <returns></returns>
		public RrdAllocator GetRrdAllocator()
		{
			return parentDb.GetRrdAllocator();
		}

		#endregion

		internal void Process(long newTime, double newValue)
		{
			Header header = parentDb.Header;
			long step = header.Step;
			long oldTime = header.LastUpdateTime;
			long startTime = Util.Normalize(oldTime, step);
			long endTime = startTime + step;
			double oldValue = lastValue.Get();
			double updateValue = CalculateUpdateValue(oldTime, oldValue, newTime, newValue);
			if (newTime < endTime)
			{
				Accumulate(oldTime, newTime, updateValue);
			}
			else
			{
				// should store something
				long boundaryTime = Util.Normalize(newTime, step);
				Accumulate(oldTime, boundaryTime, updateValue);
				double value = CalculateTotal(startTime, boundaryTime);
				// how many updates?
				long numSteps = (boundaryTime - endTime)/step + 1L;
				// ACTION!
				parentDb.Archive(this, value, numSteps);
				// cleanup
				nanSeconds.Set(0);
				accumValue.Set(0.0);
				Accumulate(boundaryTime, newTime, updateValue);
			}
		}

		private double CalculateUpdateValue(long oldTime, double oldValue, long newTime, double newValue)
		{
			double updateValue = Double.NaN;
			if (newTime - oldTime <= heartbeat.Get())
			{
				DataSourceType type;
				if (!Enum.TryParse(dsType.Get(), true, out type))
					throw new RrdException("Invalid DataSourceType");

				if (type == DataSourceType.GAUGE)
				{
					updateValue = newValue;
				}
				else if (type == DataSourceType.ABSOLUTE)
				{
					if (!Double.IsNaN(newValue))
					{
						updateValue = newValue/(newTime - oldTime);
					}
				}
				else if (type == DataSourceType.DERIVE)
				{
					if (!Double.IsNaN(newValue) && !Double.IsNaN(oldValue))
					{
						updateValue = (newValue - oldValue)/(newTime - oldTime);
					}
				}
				else if (type == DataSourceType.COUNTER)
				{
					if (!Double.IsNaN(newValue) && !Double.IsNaN(oldValue))
					{
						double diff = newValue - oldValue;
						if (diff < 0)
						{
							diff += MAX_32_BIT;
						}
						if (diff < 0)
						{
							diff += MAX_64_BIT - MAX_32_BIT;
						}
						if (diff >= 0)
						{
							updateValue = diff/(newTime - oldTime);
						}
					}
				}
				if (!Double.IsNaN(updateValue))
				{
					double minVal = minValue.Get();
					double maxVal = maxValue.Get();
					if (!Double.IsNaN(minVal) && updateValue < minVal)
					{
						updateValue = Double.NaN;
					}
					if (!Double.IsNaN(maxVal) && updateValue > maxVal)
					{
						updateValue = Double.NaN;
					}
				}
			}
			lastValue.Set(newValue);
			return updateValue;
		}

		private void Accumulate(long oldTime, long newTime, double updateValue)
		{
			if (Double.IsNaN(updateValue))
			{
				nanSeconds.Set(nanSeconds.Get() + (newTime - oldTime));
			}
			else
			{
				accumValue.Set(accumValue.Get() + updateValue*(newTime - oldTime));
			}
		}

		private double CalculateTotal(long startTime, long boundaryTime)
		{
			double totalValue = Double.NaN;
			long validSeconds = boundaryTime - startTime - nanSeconds.Get();
			if (nanSeconds.Get() <= heartbeat.Get() && validSeconds > 0)
			{
				totalValue = accumValue.Get()/validSeconds;
			}
			// IMPORTANT:
			// if datasource name ends with "!", we'll send zeros instead of NaNs
			// this might be handy from time to time
			if (Double.IsNaN(totalValue) && Name.EndsWith(DsDef.FORCE_ZEROS_FOR_NANS_SUFFIX))
			{
				totalValue = 0;
			}
			return totalValue;
		}

		internal void AppendXml(XmlWriter writer)
		{
			writer.WriteStartElement("ds");
			writer.WriteElementString("name", dsName.Get());
			writer.WriteElementString("type", dsType.Get());
			writer.WriteElementString("minimal_heartbeat", heartbeat.Get().ToString());
			writer.WriteElementString("min", Util.FormatDouble(minValue.Get()));
			writer.WriteElementString("max", Util.FormatDouble(maxValue.Get()));
			writer.WriteComment("PDP Status");
			writer.WriteElementString("last_ds", (double.IsNaN(lastValue.Get()) ? "U" : Util.FormatDouble(lastValue.Get())));
			writer.WriteElementString("value", Util.FormatDouble(accumValue.Get()));
			writer.WriteElementString("unknown_sec", nanSeconds.Get().ToString());
			writer.WriteEndElement(); // ds
		}

		/// <summary>
		/// Sets minimum allowed value for this datasource. If <code>filterArchivedValues</code>
		/// argment is set to true, all archived values less then <code>minValue</code> will
		/// be fixed to NaN.
		/// </summary>
		/// <param name="min">New minimal value. Specify <code>Double.NaN</code> if no minimal value should be set</param>
		/// <param name="filterArchivedValues">true, if archived datasource values should be fixed; false, otherwise.</param>
		public void SetMinValue(double min, bool filterArchivedValues)
		{
			double max = maxValue.Get();
			if (!Double.IsNaN(min) && !Double.IsNaN(max) && min >= max)
			{
				throw new RrdException("Invalid min/max values: " + min + "/" + max);
			}
			minValue.Set(min);
			if (!Double.IsNaN(min) && filterArchivedValues)
			{
				int dsIndex = Index;
				Archive[] archives = parentDb.Archives;
				foreach (Archive archive in archives)
				{
					archive.GetRobin(dsIndex).FilterValues(min, Double.NaN);
				}
			}
		}

		/// <summary>
		/// Sets maximum allowed value for this datasource. If <code>filterArchivedValues</code>
		/// argment is set to true, all archived values greater then <code>maxValue</code> will
		/// be fixed to NaN.
		/// </summary>
		/// <param name="max">New maximal value. Specify <code>Double.NaN</code> if no max value should be set.</param>
		/// <param name="filterArchivedValues">true, if archived datasource values should be fixed; false, otherwise.</param>
		public void SetMaxValue(double max, bool filterArchivedValues)
		{
			double min = minValue.Get();
			if (!Double.IsNaN(min) && !Double.IsNaN(max) && min >= max)
			{
				throw new RrdException("Invalid min/max values: " + min + "/" + max);
			}
			maxValue.Set(max);
			if (!Double.IsNaN(max) && filterArchivedValues)
			{
				int dsIndex = Index;
				Archive[] archives = parentDb.Archives;
				foreach (Archive archive in archives)
				{
					archive.GetRobin(dsIndex).FilterValues(Double.NaN, max);
				}
			}
		}

		/// <summary>
		/// Sets min/max values allowed for this datasource. If <code>filterArchivedValues</code>
		/// argment is set to true, all archived values less then <code>minValue</code> or
		/// greater then <code>maxValue</code> will be fixed to NaN.
		/// </summary>
		/// <param name="min">New minimal value. Specify <code>Double.NaN</code> if no min value should be set.</param>
		/// <param name="max">New maximal value. Specify <code>Double.NaN</code> if no max value should be set.</param>
		/// <param name="filterArchivedValues">true, if archived datasource values should be fixed; false, otherwise.</param>
		public void SetMinMaxValue(double min, double max, bool filterArchivedValues)
		{
			if (!Double.IsNaN(min) && !Double.IsNaN(max) && min >= max)
			{
				throw new RrdException("Invalid min/max values: " + min + "/" + max);
			}
			minValue.Set(min);
			maxValue.Set(max);
			if (!(Double.IsNaN(min) && Double.IsNaN(max)) && filterArchivedValues)
			{
				int dsIndex = Index;
				Archive[] archives = parentDb.Archives;
				foreach (Archive archive in archives)
				{
					archive.GetRobin(dsIndex).FilterValues(min, max);
				}
			}
		}

		public override String ToString()
		{
			return GetType().Name + "@" + GetHashCode().ToString("X") + "[parentDb=" + parentDb
			       + ",dsName=" + Name + ",dsType=" + Type + ",heartbeat=" + Heartbeat
			       + ",minValue=" + Util.FormatDouble(MinValue) + ",maxValue=" + Util.FormatDouble(MaxValue) + "]";
		}
	}
}