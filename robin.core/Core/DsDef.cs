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
using System.Linq;

namespace robin.core
{
	/// <summary>
	///  Class to represent single data source definition within the RRD.
	///  Datasource definition consists of the following five elements:
	///  <ul>
	///  <li>data source name</li>
	///  <li>data soruce type</li>
	///  <li>heartbeat</li>
	///  <li>minimal value</li>
	///  <li>maximal value</li>
	///  </ul>
	///  For the complete explanation of all source definition parameters, see RRDTool's
	///  <a href="../../../../man/rrdcreate.html" target="man">rrdcreate man page</a>.
	/// 
	///  @author <a href="mailto:saxon@jrobin.org">Sasa Markovic</a>
	/// </summary>
	public class DsDef
	{
		internal const string FORCE_ZEROS_FOR_NANS_SUFFIX = "!";

		/// <summary>
		/// array of valid source types
		/// </summary>
		public static DataSourceType[] VALID_DATASOURCE_TYPES = {
		                                          	DataSourceType.GAUGE, DataSourceType.COUNTER, DataSourceType.DERIVE,
		                                          	DataSourceType.ABSOLUTE
		                                          };

		/// <summary>
		/// <p>Creates new data source definition object. This object should be passed as argument
		/// to {@link RrdDef#addDatasource(DsDef) addDatasource()}
		/// method of {@link RrdDb RrdDb} object.</p>
		/// <p/>
		/// <p>For the complete explanation of all source definition parameters, see RRDTool's
		/// <a href="../../../../man/rrdcreate.html" target="man">rrdcreate man page</a></p>
		/// <p/>
		/// <p><b>IMPORTANT NOTE:</b> If datasource name ends with '!', corresponding archives will never
		/// store NaNs as datasource values. In that case, NaN datasource values will be silently
		/// replaced with zeros by the framework.</p>
		/// </summary>
		/// <param name="dsName">Data source name.</param>
		/// <param name="dsType">Data source type. Valid values are "COUNTER", "GAUGE", "DERIVE"
		/// 							    and "ABSOLUTE" (these string constants are conveniently defined in the
		/// 							    {@link DsTypes} class).
		/// </param>
		/// <param name="heartbeat">Hearbeat</param>
		/// <param name="minValue">Minimal value. Use <code>Double.NaN</code> if unknown.</param>
		/// <param name="maxValue">Maximal value. Use <code>Double.NaN</code> if unknown.</param>
		public DsDef(String dsName, DataSourceType dsType, long heartbeat, double minValue, double maxValue)
		{
			Name = dsName;
			Type = dsType;
			Heartbeat = heartbeat;
			MinValue = minValue;
			MaxValue = maxValue;
			Validate();
		}

		/// <summary>
		/// Create a DsDef from a RRDTool-like datasource definition string. The string must have six elements separated with colons
		/// (:) in the following order:<p>
		/// <code>
		/// DS:name:type:heartbeat:minValue:maxValue
		/// </code>
		/// For example:</p>
		/// <code>
		/// DS:input:COUNTER:600:0:U
		/// </code>
		/// For more information on datasource definition parameters see <code>rrdcreate</code>
		/// man page.
		/// </summary>
		/// <param name="rrdToolDataSourceDefinition"></param>
		public static DsDef FromRrdToolString(string rrdToolDataSourceDefinition)
		{
			var rrdException = new RrdException("Wrong rrdtool-like datasource definition: " + rrdToolDataSourceDefinition);

			string[] tokens = rrdToolDataSourceDefinition.Split(':');
			if (tokens.Length != 6)
			{
				throw rrdException;
			}

			if (!tokens[0].Equals("DS", StringComparison.InvariantCultureIgnoreCase))
			{
				throw rrdException;
			}
			String dsName = tokens[1];
			DataSourceType dsType;
			if (!Enum.TryParse(tokens[2], true, out dsType))
				throw new RrdException("Invalid DataSource Type");

			long dsHeartbeat;
			if (!long.TryParse(tokens[3], out dsHeartbeat))
			{
				throw rrdException;
			}
			double minValue = Double.NaN;
			if (!tokens[4].Equals("U", StringComparison.InvariantCultureIgnoreCase))
			{
				if (!double.TryParse(tokens[4], NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out minValue))
					throw rrdException;
			}
			double maxValue = Double.NaN;
			if (!tokens[5].Equals("U", StringComparison.InvariantCultureIgnoreCase))
			{
				if (!double.TryParse(tokens[5], NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out maxValue))
					throw rrdException;
			}
			return  new DsDef(dsName,dsType,dsHeartbeat,minValue,maxValue);
		}

		/// <summary>
		/// Data source name.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Source type
		/// </summary>
		/// <value></value>
		public DataSourceType Type { get; set; }

		/// <summary>
		/// Source heartbeat.
		/// </summary>
		/// <value></value>
		public long Heartbeat { get; set; }

		/// <summary>
		/// Minimal value.
		/// </summary>
		/// <value></value>
		public double MinValue { get; set; }

		/// <summary>
		/// Maximal value.
		/// </summary>
		/// <value></value>
		public double MaxValue { get; set; }

		private void Validate()
		{
			if (Name == null)
			{
				throw new RrdException("Null datasource name specified");
			}
			if (Name.Length == 0)
			{
				throw new RrdException("Datasource name length equal to zero");
			}
			if (Name.Length > RrdPrimitive.STRING_LENGTH)
			{
				throw new RrdException("Datasource name [" + Name + "] to long (" +
				                       Name.Length + " chars found, only " + RrdPrimitive.STRING_LENGTH + " allowed");
			}
			if (!ValidDataSourceType(Type))
			{
				throw new RrdException("Invalid datasource type specified: " + Type);
			}
			if (Heartbeat <= 0)
			{
				throw new RrdException("Invalid heartbeat, must be positive: " + Heartbeat);
			}
			if (!Double.IsNaN(MinValue) && !Double.IsNaN(MaxValue) && MinValue >= MaxValue)
			{
				throw new RrdException("Invalid min/max values specified: " +
				                       MinValue + "/" + MaxValue);
			}
		}

		/// <summary>
		/// Checks if function argument represents valid source type.
		/// </summary>
		/// <param name="dsType"></param>
		/// <returns></returns>
		public static bool ValidDataSourceType(DataSourceType dsType)
		{
			return VALID_DATASOURCE_TYPES.Any(type => type.Equals(dsType));
		}

		/// <summary>
		/// Returns string representing source definition (RRDTool format).
		/// </summary>
		/// <returns></returns>
		public String Dump()
		{
			return "DS:" + Name + ":" + Type + ":" + Heartbeat +
			       ":" + Util.FormatDouble(MinValue, "U", false) +
			       ":" + Util.FormatDouble(MaxValue, "U", false);
		}

		/// <summary>
		/// Checks if two datasource definitions are equal.
		/// Source definitions are treated as equal if they have the same source name.
		/// It is not possible to create RRD with two equal archive definitions.
		/// </summary>
		/// <param name="obj">Archive definition to compare with.</param>
		/// <returns>
		///  <code>true</code> if archive definitions are equal,
		///  <code>false</code> otherwise.
		/// </returns>
		public override bool Equals(Object obj)
		{
			if (obj is DsDef)
			{
				var dsObj = (DsDef) obj;
				return Name == dsObj.Name;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return Name.GetHashCode()*47;
		}

		internal bool ExactlyEqual(DsDef def)
		{
			return Name == def.Name && Type == def.Type &&
			       Heartbeat == def.Heartbeat && MinValue == def.MinValue &&
			       MaxValue == def.MaxValue;
		}

		public override String ToString()
		{
			return GetType().Name + "@" + GetHashCode().ToString("X") + "[" + Dump() + "]";
		}
	}
}