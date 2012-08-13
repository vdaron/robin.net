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
using System.Xml;

namespace robin.core
{
	/// <summary>
	/// Class to represent single RRD archive in a RRD with its internal state.
	/// Normally, you don't need methods to manipulate archive objects directly
	/// because JRobin framework does it automatically for you.
	/// 
	/// Each archive object consists of three parts: archive definition, archive state objects
	/// (one state object for each datasource) and round robin archives (one round robin for
	/// each datasource). API (read-only) is provided to access each of theese parts.
	/// </summary>
	public class Archive : IRrdUpdater
	{
		// definition
		private readonly RrdString consolFun;
		private readonly RrdDb parentDb;
		// state
		private readonly Robin[] robins;
		private readonly RrdInt rows;
		private readonly ArcState[] states;
		private readonly RrdInt steps;
		private readonly RrdDouble xff;

		internal Archive(RrdDb parentDb, ArcDef arcDef)
		{
			bool shouldInitialize = arcDef != null;
			this.parentDb = parentDb;
			consolFun = new RrdString(this, true); // constant, may be cached
			xff = new RrdDouble(this);
			steps = new RrdInt(this, true); // constant, may be cached
			rows = new RrdInt(this, true); // constant, may be cached
			if (shouldInitialize)
			{
				consolFun.Set(arcDef.ConsolFun.ToString().ToUpper());
				xff.Set(arcDef.Xff);
				steps.Set(arcDef.Steps);
				rows.Set(arcDef.Rows);
			}
			int dsCount = parentDb.Header.DataSourceCount;
			states = new ArcState[dsCount];
			robins = new Robin[dsCount];
			int numRows = rows.Get();
			for (int i = 0; i < dsCount; i++)
			{
				states[i] = new ArcState(this, shouldInitialize);
				robins[i] = new Robin(this, numRows, shouldInitialize);
			}
		}

		// read from XML
		internal Archive(RrdDb parentDb, DataImporter reader, int arcIndex)
			: this(parentDb, new ArcDef(
			                 	reader.GetArchiveConsolisationFunction(arcIndex), reader.GetArchiveXff(arcIndex),
			                 	reader.GetArchiveSteps(arcIndex), reader.GetArchiveRows(arcIndex)))
		{
			int dsCount = parentDb.Header.DataSourceCount;
			for (int i = 0; i < dsCount; i++)
			{
				// restore state
				states[i].AccumulatedValue = reader.GetArchiveStateAccumulatedValue(arcIndex, i);
				states[i].NanSteps = reader.GetArchiveStateNanSteps(arcIndex, i);
				// restore robins
				double[] values = reader.GetArchiveValues(arcIndex, i);
				robins[i].Update(values);
			}
		}

		/// <summary>
		/// Returns archive time step in seconds. Archive step is equal to RRD step
		/// multiplied with the number of archive steps.
		/// </summary>
		/// <value></value>
		public long TimeStep
		{
			get
			{
				long step = parentDb.Header.Step;
				return step*steps.Get();
			}
		}

		internal RrdDb ParentDb
		{
			get { return parentDb; }
		}

		/// <summary>
		/// Archive consolidation function ("AVERAGE", "MIN", "MAX" or "LAST").
		/// </summary>
		/// <value></value>
		public ConsolidationFunction ConsolidationFunction
		{
			get
			{
				ConsolidationFunction funs;
				if (!Enum.TryParse(consolFun.Get(), true, out funs))
				{
					throw new RrdException("Invalid ConsolFun name");
				}
				return funs;
			}
		}

		/// <summary>
		/// Archive X-files factor (between 0 and 1).
		/// </summary>
		/// <value></value>
		public double Xff
		{
			get { return xff.Get(); }
			set
			{
				if (value < 0 || value >= 1)
				{
					throw new RrdException("Invalid xff supplied (" + value + "), must be >= 0 and < 1");
				}
				xff.Set(value);
			}
		}

		/// <summary>
		/// Number of archive steps.
		/// </summary>
		/// <value></value>
		public int Steps
		{
			get { return steps.Get(); }
		}

		/// <summary>
		/// Number of archive rows.
		/// </summary>
		/// <value></value>
		public int Rows
		{
			get { return rows.Get(); }
		}

		#region IRrdUpdater Members

		/// <summary>
		/// Copies object's internal state to another ArcState object.
		/// </summary>
		/// <param name="other"> New ArcState object to copy state to</param>
		public void CopyStateTo(IRrdUpdater other)
		{
			if (!(other is Archive))
			{
				throw new RrdException("Cannot copy Archive object to " + other.GetType().Name);
			}
			var arc = (Archive) other;
			if (!arc.consolFun.Get().Equals(consolFun.Get()))
			{
				throw new RrdException("Incompatible consolidation functions");
			}
			if (arc.steps.Get() != steps.Get())
			{
				throw new RrdException("Incompatible number of steps");
			}
			int count = parentDb.Header.DataSourceCount;
			for (int i = 0; i < count; i++)
			{
				int j = Util.GetMatchingDatasourceIndex(parentDb, i, arc.parentDb);
				if (j >= 0)
				{
					states[i].CopyStateTo(arc.states[j]);
					robins[i].CopyStateTo(arc.robins[j]);
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

		public String Dump()
		{
			var buffer = new StringBuilder("== ARCHIVE ==\n");
			buffer.Append("RRA:").Append(consolFun.Get()).Append(":").Append(xff.Get()).Append(":").Append(steps.Get()).
				Append(":").Append(rows.Get()).Append("\n");
			buffer.Append("interval [").Append(GetStartTime()).Append(", ").Append(GetEndTime()).Append("]" + "\n");
			for (int i = 0; i < robins.Length; i++)
			{
				buffer.Append(states[i].Dump());
				buffer.Append(robins[i].Dump());
			}
			return buffer.ToString();
		}

		internal void ArchiveValue(int dsIndex, double value, long numStepUpdates)
		{
			Robin robin = robins[dsIndex];
			ArcState state = states[dsIndex];
			long step = parentDb.Header.Step;
			long lastUpdateTime = parentDb.Header.LastUpdateTime;
			long updateTime = Util.Normalize(lastUpdateTime, step) + step;
			long arcStep = TimeStep;
			int numSteps = steps.Get();
			int numRows = rows.Get();
			double xffValue = xff.Get();

			// finish current step
			long numUpdates = numStepUpdates;
			while (numUpdates > 0)
			{
				Accumulate(state, value, ConsolidationFunction);
				numUpdates--;
				if (updateTime%arcStep == 0)
				{
					FinalizeStep(state, robin, ConsolidationFunction, numSteps, xffValue);
					break;
				}
				else
				{
					updateTime += step;
				}
			}
			// update robin in bulk
			var bulkUpdateCount = (int) Math.Min(numUpdates/numSteps, numRows);
			robin.BulkStore(value, bulkUpdateCount);
			// update remaining steps
			long remainingUpdates = numUpdates%numSteps;
			for (long i = 0; i < remainingUpdates; i++)
			{
				Accumulate(state, value, ConsolidationFunction);
			}
		}

		private static void Accumulate(ArcState state, double value, ConsolidationFunction consolFun)
		{
			if (Double.IsNaN(value))
			{
				state.NanSteps = state.NanSteps + 1;
			}
			else
			{
				double accumValue = state.AccumulatedValue;

				switch (consolFun)
				{
					case ConsolidationFunction.AVERAGE:
						state.AccumulatedValue = Util.Sum(accumValue, value);
						break;
					case ConsolidationFunction.MIN:
						double minValue = Util.Min(accumValue, value);
						if (minValue != accumValue)
						{
							state.AccumulatedValue = minValue;
						}
						break;
					case ConsolidationFunction.MAX:
						double maxValue = Util.Max(accumValue, value);
						if (maxValue != accumValue)
						{
							state.AccumulatedValue = maxValue;
						}
						break;
					case ConsolidationFunction.LAST:
						state.AccumulatedValue = value;
						break;
					case ConsolidationFunction.FIRST:
					case ConsolidationFunction.TOTAL:
					default:
						throw new ArgumentOutOfRangeException("consolFun");
				}
			}
		}

		private static void FinalizeStep(ArcState state, Robin robin, ConsolidationFunction consolFunString, long numSteps,
		                                 double xffValue)
		{
			long nanSteps = state.NanSteps;
			//double nanPct = (double) nanSteps / (double) arcSteps;
			double accumValue = state.AccumulatedValue;
			if (nanSteps <= xffValue*numSteps && !Double.IsNaN(accumValue))
			{
				if (consolFunString == ConsolidationFunction.AVERAGE)
				{
					accumValue /= (numSteps - nanSteps);
				}
				robin.Store(accumValue);
			}
			else
			{
				robin.Store(Double.NaN);
			}
			state.AccumulatedValue = Double.NaN;
			state.NanSteps = 0;
		}

		/// <summary>
		/// Current starting timestamp. This value is not constant.
		/// </summary>
		/// <returns></returns>
		public long GetStartTime()
		{
			long endTime = GetEndTime();
			long arcStep = TimeStep;
			long numRows = rows.Get();
			return endTime - (numRows - 1)*arcStep;
		}

		/// <summary>
		/// Current ending timestamp. This value is not constant.
		/// </summary>
		/// <returns></returns>
		public long GetEndTime()
		{
			long arcStep = TimeStep;
			long lastUpdateTime = parentDb.Header.LastUpdateTime;
			return Util.Normalize(lastUpdateTime, arcStep);
		}

		/// <summary>
		/// Returns the underlying archive state object. Each datasource has its
		/// corresponding ArcState object (archive states are managed independently
		/// for each RRD datasource).
		/// </summary>
		/// <param name="dsIndex">dsIndex Datasource index</param>
		/// <returns>Underlying archive state object</returns>
		internal ArcState GetArcState(int dsIndex)
		{
			return states[dsIndex];
		}

		/// <summary>
		///  Returns the underlying round robin archive. Robins are used to store actual
		///  archive values on a per-datasource basis.
		/// </summary>
		/// <param name="dsIndex">dsIndex Index of the datasource in the RRD.</param>
		/// <returns>Underlying round robin archive for the given datasource.</returns>
		public Robin GetRobin(int dsIndex)
		{
			return robins[dsIndex];
		}

		internal FetchData FetchData(FetchRequest request)
		{
			long arcStep = TimeStep;
			long fetchStart = Util.Normalize(request.FetchStart, arcStep);
			long fetchEnd = Util.Normalize(request.FetchEnd, arcStep);
			if (fetchEnd < request.FetchEnd)
			{
				fetchEnd += arcStep;
			}
			long startTime = GetStartTime();
			long endTime = GetEndTime();
			String[] dsToFetch = request.GetFilter() ?? parentDb.DataSourceNames;
			int dsCount = dsToFetch.Length;
			var ptsCount = (int) ((fetchEnd - fetchStart)/arcStep + 1);
			var timestamps = new long[ptsCount];
			var values = new double[dsCount][];
			for (int i = 0; i < dsCount; i++)
			{
				values[i] = new double[ptsCount];
			}
			long matchStartTime = Math.Max(fetchStart, startTime);
			long matchEndTime = Math.Min(fetchEnd, endTime);
			double[][] robinValues = null;
			if (matchStartTime <= matchEndTime)
			{
				// preload robin values
				var matchCount = (int) ((matchEndTime - matchStartTime)/arcStep + 1);
				var matchStartIndex = (int) ((matchStartTime - startTime)/arcStep);
				robinValues = new double[dsCount][];
				for (int i = 0; i < dsCount; i++)
				{
					int dsIndex = parentDb.GetDataSourceIndex(dsToFetch[i]);
					robinValues[i] = robins[dsIndex].GetValues(matchStartIndex, matchCount);
				}
			}
			for (int ptIndex = 0; ptIndex < ptsCount; ptIndex++)
			{
				long time = fetchStart + ptIndex*arcStep;
				timestamps[ptIndex] = time;
				for (int i = 0; i < dsCount; i++)
				{
					double value = Double.NaN;
					if (time >= matchStartTime && time <= matchEndTime)
					{
						// inbound time
						var robinValueIndex = (int) ((time - matchStartTime)/arcStep);
						Debug.Assert(robinValues != null);
						value = robinValues[i][robinValueIndex];
					}
					values[i][ptIndex] = value;
				}
			}
			return new FetchData(this, request) {Timestamps = timestamps, Values = values};
		}

		internal void AppendXml(XmlWriter writer)
		{
			writer.WriteStartElement("rra");
			writer.WriteElementString("cf", consolFun.ToString());
			writer.WriteComment(TimeStep + " seconds");
			writer.WriteElementString("pdp_per_row", steps.Get().ToString());
			writer.WriteElementString("xff", Util.FormatDouble(xff.Get()));
			writer.WriteStartElement("cdp_prep");
			foreach (ArcState state in states)
			{
				state.AppendXml(writer);
			}
			writer.WriteEndElement(); // cdp_prep
			writer.WriteStartElement("database");
			long startTime = GetStartTime();
			for (int i = 0; i < rows.Get(); i++)
			{
				long time = startTime + i*TimeStep;
				writer.WriteComment(time.ToDateTime() + " / " + time);
				writer.WriteStartElement("row");
				foreach (Robin robin in robins)
				{
					writer.WriteElementString("v", Util.FormatDouble(robin.GetValue(i)));
				}
				writer.WriteEndElement(); // row
			}
			writer.WriteEndElement(); // database
			writer.WriteEndElement(); // rra
		}

		public override String ToString()
		{
			return "Archive@" + GetHashCode().ToString("X") + "[parentDb=" + parentDb + ",consolFun=" + consolFun + ",xff=" + xff +
			       ",steps=" +
			       steps + ",rows=" + rows + ",robins=" + robins + ",states=" + states + "]";
		}
	}
}