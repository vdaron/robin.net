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
using System.Globalization;
using System.Text;

namespace robin.core
{
	/// <summary>
	/// Class to represent data source values for the given timestamp. Objects of this
	/// class are never created directly (no public constructor is provided). To learn more how
	/// to update RRDs, see RRDTool's
	/// <a href="../../../../man/rrdupdate.html" target="man">rrdupdate man page</a>.
	/// <p/>
	/// <p>To update a RRD with JRobin use the following procedure:</p>
	/// <p/>
	/// <ol>
	/// <li>Obtain empty Sample object by calling method {@link RrdDb#createSample(long)</li>
	/// createSample()} on respective {@link RrdDb RrdDb} object.
	/// <li>Adjust Sample timestamp if necessary (see {@link #setTime(long) setTime()} method).</li>
	/// <li>Supply data source values (see {@link #setValue(String, double) setValue()}).</li>
	/// <li>Call Sample's {@link #update() update()} method.</li>
	/// </ol>
	/// <p/>
	/// <p>Newly created Sample object contains all data source values set to 'unknown'.
	/// You should specifify only 'known' data source values. However, if you want to specify
	/// 'unknown' values too, use <code>Double.NaN</code>.</p>
	/// 
	/// @author <a href="mailto:saxon@jrobin.org">Sasa Markovic</a>
	/// </summary>
	public class Sample
	{
		private readonly String[] dsNames;
		private readonly RrdDb parentDb;
		private readonly double[] values;
		private long time;

		internal Sample(RrdDb parentDb, long time)
		{
			this.parentDb = parentDb;
			this.time = time;
			dsNames = parentDb.DataSourceNames;
			values = new double[dsNames.Length];
			ClearCurrentValues();
		}

		private void ClearCurrentValues()
		{
			for (int i = 0; i < values.Length; i++)
			{
				values[i] = Double.NaN;
			}
		}

		/// <summary>
		/// Sets single data source value in the sample.
		/// </summary>
		/// <param name="dsName">Data source name.</param>
		/// <param name="value">Data source value.</param>
		/// <returns>This <code>Sample</code> object</returns>
		public Sample SetValue(String dsName, double value)
		{
			for (int i = 0; i < values.Length; i++)
			{
				if (dsNames[i] == dsName)
				{
					values[i] = value;
					return this;
				}
			}
			throw new RrdException("Datasource " + dsName + " not found");
		}

		/// <summary>
		/// Sets single datasource value using data source index. Data sources are indexed by
		/// the order specified during RRD creation (zero-based).
		/// </summary>
		/// <param name="i">Data source index</param>
		/// <param name="value">Data source values</param>
		/// <returns>This <code>Sample</code> object</returns>
		public Sample SetValue(int i, double value)
		{
			if (i < values.Length)
			{
				values[i] = value;
				return this;
			}
			throw new RrdException("Sample datasource index " + i + " out of bounds");
		}

		/// <summary>
		/// Data source values in the sample.
		/// </summary>
		/// <value></value>
		public double[] Values
		{
			get { return values; }
			set
			{
				if (value.Length > values.Length)
				{
					throw new RrdException("Invalid number of values specified (found " + value.Length + ", only " + dsNames.Length +
								  " allowed)");
				}
				Array.Copy(value, 0, values, 0, value.Length);
			}
		}

		/// <summary>
		/// Sample timestamp (in seconds, without milliseconds).
		/// </summary>
		/// <value></value>
		public long Time
		{
			get { return time; }
			set { time = value; }
		}

		/// <summary>
		/// Returns an array of all data source names. If you try to set value for the data source
		/// name not in this array, an exception is thrown.
		/// </summary>
		public string[] DataSourceNames
		{
			get { return dsNames; }
		}

		/// <summary>
		/// <p>Sets sample timestamp and data source values in a fashion similar to RRDTool.
		/// Argument string should be composed in the following way:
		/// <code>timestamp:value1:value2:...:valueN</code>.</p>
		/// <p/>
		/// You don't have to supply all datasource values. Unspecified values will be treated
		/// as unknowns. To specify unknown value in the argument string, use letter 'U'
		/// @return This <code>Sample</code> object
		/// 
		/// </summary>
		/// <param name="timeAndValues">
		/// String made by concatenating sample timestamp with corresponding
		/// data source values delmited with colons. For example:
		/// <pre>
		/// 1005234132:12.2:35.6:U:24.5
		/// NOW:12.2:35.6:U:24.5
		/// </pre>
		/// 'N' stands for the current timestamp (can be replaced with 'NOW')
		/// Method will throw an exception if timestamp is invalid (cannot be parsed as Long, and is not 'N'
		/// or 'NOW'). Datasource value which cannot be parsed as 'double' will be silently set to NaN.
		/// </param>
		/// <returns></returns>
		public Sample Set(String timeAndValues)
		{
			String[] tokens = timeAndValues.Split(':');
			int tokenCount = tokens.Length;
			if (tokenCount > values.Length + 1)
			{
				throw new RrdException("Invalid number of values specified (found " + values.Length + ", " + dsNames.Length +
				                       " allowed)");
			}
			for (int i = 0; i < tokenCount; i++)
			{
				if(i == 0)//Read time part
				{
					String timeToken = tokens[i];
					if (long.TryParse(timeToken, out time))
					{
						time = long.Parse(timeToken);
					}
					else
					{
						if (timeToken.Equals("N") || timeToken.Equals("NOW"))
						{
							time = Util.GetCurrentTime();
						}
						else
						{
							throw new RrdException("Invalid sample timestamp: " + timeToken);
						}
					}
				}
				else
				{
					values[i - 1] = Util.ParseDouble(tokens[i]);
				}
			}
			return this;
		}

		/// <summary>
		/// Stores sample in the corresponding RRD. If the update operation succeedes,
		/// all datasource values in the sample will be set to Double.NaN (unknown) values.
		/// </summary>
		public void Update()
		{
			parentDb.Store(this);
			ClearCurrentValues();
		}
		
		/// <summary>
		/// <p>Creates sample with the timestamp and data source values supplied
		/// in the argument string and stores sample in the corresponding RRD.
		/// This method is just a shortcut for:</p>
		/// <code>
		///     set(timeAndValues);
		///     update();
		/// </code>
		/// </summary>
		/// <param name="timeAndValues">
		/// String made by concatenating sample timestamp with corresponding
		/// data source values delmited with colons. For example:<br/>
		/// <code>1005234132:12.2:35.6:U:24.5</code><br/>
		/// <code>NOW:12.2:35.6:U:24.5</code>
		/// </param>
		public void SetAndUpdate(params string[] timeAndValues)
		{
			foreach (string timeAndValue in timeAndValues)
			{
				Set(timeAndValue);
				Update();

			}
		}

		/// <summary>
		/// Dumps sample content using the syntax of RRDTool's update command.
		/// </summary>
		/// <returns></returns>
		public String Dump()
		{
			var buffer = new StringBuilder("update \"");
			buffer.Append(parentDb.GetRrdBackend().Path).Append("\" ").Append(time);
			foreach (double value in values)
			{
				buffer.Append(":");
				buffer.Append(Util.FormatDouble(value, "U", false));
			}
			return buffer.ToString();
		}

		private String GetRrdToolCommand()
		{
			return Dump();
		}

		public override String ToString()
		{
			return GetType().Name + "@" + "[parentDb=" + parentDb + ",time=" + new DateTime(0, 0, 0, 0, 0, (int) time) +
			       ",dsNames=[" + PrintList(dsNames) + "],values=[" + PrintList(values) + "]]";
		}

		private static String PrintList(Object[] dataSourceNames)
		{
			if (dataSourceNames == null) return "null";
			var sb = new StringBuilder();
			for (int i = 0; i < dataSourceNames.Length; i++)
			{
				if (i == dataSourceNames.Length - 1)
				{
					sb.Append(dataSourceNames[i]);
				}
				else
				{
					sb.Append(dataSourceNames[i]).Append(", ");
				}
			}
			return sb.ToString();
		}

		private static String PrintList(double[] vals)
		{
			if (vals == null) return "null";
			var sb = new StringBuilder();
			for (int i = 0; i < vals.Length; i++)
			{
				if (i == vals.Length - 1)
				{
					sb.Append(vals[i]);
				}
				else
				{
					sb.Append(vals[i]).Append(", ");
				}
			}
			return sb.ToString();
		}
	}
}