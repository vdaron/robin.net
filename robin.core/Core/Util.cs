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
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using robin.Core.TimeSpec;

namespace robin.core
{
	/// <summary>
	/// Class defines various utility functions used in JRobin.
	/// 
	/// @author <a href="mailto:saxon@jrobin.org">Sasa Markovic</a>
	/// </summary>
	public class Util
	{
		public const long MAX_LONG = long.MaxValue;
		public const long MIN_LONG = -long.MaxValue;

		public const double MAX_DOUBLE = Double.MaxValue;
		public const double MIN_DOUBLE = -Double.MaxValue;

		// pattern RRDTool uses to format doubles in XML files
		private const String PATTERN = "0.0000000000E00";
		// directory under $USER_HOME used for demo graphs storing
		private const String JROBIN_DIR = "jrobin-demo";
		private static DateTime Epoch = new DateTime(1970, 1, 1);

		/// <summary>
		/// Converts an array of long primitives to an array of doubles.
		/// </summary>
		/// <param name="array"></param>
		/// <returns></returns>
		public static double[] ToDoubleArray(long[] array)
		{
			var values = new double[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				values[i] = array[i];
			}
			return values;
		}

		/// <summary>
		/// Returns current timestamp in seconds (without milliseconds). Returned timestamp
		/// is obtained with the following expression: 
		/// <code>(DateTime.Now - Epoch).TotalSeconds</code>
		/// </summary>
		/// <returns></returns>
		public static long GetCurrentTime()
		{
			return (long) (DateTime.Now - Epoch).TotalSeconds;
		}

		/// <summary>
		/// Rounds the given timestamp to the nearest whole &quote;step&quote;. Rounded value is obtained
		/// from the following expression:
		/// <code>timestamp - timestamp % step;</code>
		/// </summary>
		/// <param name="timestamp">Timestamp in seconds</param>
		/// <param name="step">Step in seconds</param>
		/// <returns></returns>
		public static long Normalize(long timestamp, long step)
		{
			return timestamp - timestamp%step;
		}

		/// <summary>
		/// Returns the greater of two double values, but treats NaN as the smallest possible
		/// value. Note that <code>Math.max()</code> behaves differently for NaN arguments.
		/// </summary>
		/// <param name="x">x an argument</param>
		/// <param name="y">y another argument</param>
		/// <returns>the lager of arguments</returns>
		public static double Max(double x, double y)
		{
			return Double.IsNaN(x) ? y : Double.IsNaN(y) ? x : Math.Max(x, y);
		}

		/// <summary>
		/// Returns the smaller of two double values, but treats NaN as the greatest possible
		/// value. Note that <code>Math.min()</code> behaves differently for NaN arguments.
		/// </summary>
		/// <param name="x">x an argument</param>
		/// <param name="y">y another argument</param>
		/// <returns>the smaller of arguments</returns>
		public static double Min(double x, double y)
		{
			return Double.IsNaN(x) ? y : Double.IsNaN(y) ? x : Math.Min(x, y);
		}

		/// <summary>
		/// Calculates sum of two doubles, but treats NaNs as zeros.
		/// </summary>
		/// <param name="x">x First double</param>
		/// <param name="y">y Second double</param>
		/// <returns>Sum(x,y) calculated as <code>Double.IsNaN(x)? y: Double.IsNaN(y)? x: x + y;</code></returns>
		public static double Sum(double x, double y)
		{
			return Double.IsNaN(x) ? y : Double.IsNaN(y) ? x : x + y;
		}

		/// <summary>
		/// Return the week number corresponding to the secified date
		/// </summary>
		/// <param name="date"></param>
		/// <returns>Week number</returns>
		public static int GetWeekNumber(DateTime date)
		{
			CultureInfo ciCurr = CultureInfo.CurrentCulture;
			int weekNum = ciCurr.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
			return weekNum;
		}

		internal static String FormatDouble(double x, String nanString, bool forceExponents)
		{
			if (Double.IsNaN(x))
			{
				return nanString;
			}
			if (forceExponents)
			{
				return x.ToString("0.0000000000e+00",CultureInfo.InvariantCulture.NumberFormat);
			}
			return x.ToString();
		}

		internal static String FormatDouble(double x, bool forceExponents)
		{
			return FormatDouble(x, "NaN", forceExponents);
		}

		/// <summary>
		/// Formats double as a string using exponential notation (RRDTool like). Used for debugging
		/// throught the project.
		/// </summary>
		/// <param name="x"> x value to be formatted</param>
		/// <returns>string like "+1.234567E+02"</returns>
		public static String FormatDouble(double x)
		{
			return FormatDouble(x, true);
		}

		/// <summary>
		/// Returns <code>DateTime</code> object for the given timestamp (in seconds, without
		/// milliseconds)
		/// </summary>
		/// <param name="timestamp"></param>
		/// <returns></returns>
		public static DateTime GetDateTime(long timestamp)
		{
			return Epoch.AddSeconds(timestamp);
		}

		/// <summary>
		/// Returns timestamp (unix epoch) for the given Date object
		/// </summary>
		/// <param name="date"></param>
		/// <returns></returns>
		public static long GetTimestamp(DateTime date)
		{
			return date.GetTimestamp();
		}

		/// <summary>
		/// Returns timestamp (unix epoch) for the given year, month and day.
		/// </summary>
		/// <param name="year">Year</param>
		/// <param name="month">Month</param>
		/// <param name="day">Day in month</param>
		/// <param name="hour">Hour</param>
		/// <param name="min">Minute</param>
		/// <returns></returns>
		public static long GetTimestamp(int year, int month, int day, int hour, int min)
		{
			return GetTimestamp(new DateTime(year, month, day, hour, min, 0));
		}

		/// <summary>
		/// Returns timestamp (unix epoch) for the given year, month and day.
		/// </summary>
		/// <param name="year">Year</param>
		/// <param name="month">Month</param>
		/// <param name="day">Day in month</param>
		public static long GetTimestamp(int year, int month, int day)
		{
			return GetTimestamp(year, month, day, 0, 0);
		}

		/// <summary>
		/// Parses at-style time specification and returns the corresponding timestamp. For example:
		/// <code>
		/// long t = Util.getTimestamp("now-1d");
		/// </code>
		/// </summary>
		/// <param name="atStyleTimeSpec">at-style time specification. For the complete explanation of the syntax allowed see RRDTool's <code>rrdfetch</code> man page.</param>
		/// <returns></returns>
		public static long GetTimestamp(String atStyleTimeSpec)
		{
			TimeSpec timeSpec = new TimeParser(atStyleTimeSpec).Parse();
			return timeSpec.GetTimestamp();
		}

		/// <summary>
		/// Parses two related at-style time specifications and returns corresponding timestamps. For example:
		/// <code>
		/// long[] t = Util.getTimestamps("end-1d","now");
		/// </code>
		/// </summary>
		/// <param name="atStyleTimeSpec1">Starting at-style time specification. For the complete explanation of the syntax allowed see RRDTool's <code>rrdfetch</code> man page.</param>
		/// <param name="atStyleTimeSpec2">Ending at-style time specification. For the complete explanation of the syntax allowed see RRDTool's <code>rrdfetch</code> man page.</param>
		/// <returns>An array of two longs representing starting and ending timestamp in seconds since epoch.</returns>
		public static long[] GetTimestamps(String atStyleTimeSpec1, String atStyleTimeSpec2)
		{
			TimeSpec timeSpec1 = new TimeParser(atStyleTimeSpec1).Parse();
			TimeSpec timeSpec2 = new TimeParser(atStyleTimeSpec2).Parse();
			return TimeSpec.GetTimestamps(timeSpec1, timeSpec2);
		}

		/// <summary>
		/// Parses input string as a double value. If the value cannot be parsed, Double.NaN
		/// is returned (NumberFormatException is never thrown).
		/// </summary>
		/// <param name="valueStr">String representing double value</param>
		/// <returns></returns>
		public static double ParseDouble(String valueStr)
		{
			double value;
			if (!double.TryParse(valueStr, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out value))
			{
				value = Double.NaN;
			}
			return value;
		}

		/// <summary>
		/// Checks if a string can be parsed as double.
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static bool IsDouble(String s)
		{
			double v;
			return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out v);
		}

		/// <summary>
		/// Parses input string as a bool value. The parser is case insensitive.
		/// </summary>
		/// <param name="valueStr">String representing bool value</param>
		/// <returns><code>true</code>, if valueStr equals to 'true', 'on', 'yes', 'y' or '1'; <code>false</code> in all other cases.</returns>
		public static bool Parsebool(String valueStr)
		{
			return valueStr.Equals("true", StringComparison.InvariantCultureIgnoreCase) ||
			       valueStr.Equals("on", StringComparison.InvariantCultureIgnoreCase) ||
			       valueStr.Equals("yes", StringComparison.InvariantCultureIgnoreCase) ||
			       valueStr.Equals("y", StringComparison.InvariantCultureIgnoreCase) ||
			       valueStr.Equals("1", StringComparison.InvariantCultureIgnoreCase);
		}

		/// <summary>
		/// Parses input string as color. The color string should be of the form #RRGGBB (no alpha specified,
		/// opaque color) or #RRGGBBAA (alpa specified, transparent colors). Leading character '#' is
		/// optional.
		/// </summary>
		/// <param name="valueStr">Input string, for example #FFAA24, #AABBCC33, 010203 or ABC13E4F</param>
		/// <returns>Color objecy</returns>
		public static Color ParseColor(String valueStr)
		{
			String c = valueStr.StartsWith("#") ? valueStr.Substring(1) : valueStr;
			if (c.Length != 6 && c.Length != 8)
			{
				throw new RrdException("Invalid color specification: " + valueStr);
			}
			String r = c.Substring(0, 2), g = c.Substring(2, 2), b = c.Substring(4, 2);
			if (c.Length == 6)
			{
				return Color.FromArgb(Int32.Parse(r, NumberStyles.HexNumber), Int32.Parse(g, NumberStyles.HexNumber),
				                      Int32.Parse(b, NumberStyles.HexNumber));
			}

			String a = c.Substring(6);
			return Color.FromArgb(Int32.Parse(a, NumberStyles.HexNumber), Int32.Parse(r, NumberStyles.HexNumber),
			                      Int32.Parse(g, NumberStyles.HexNumber), Int32.Parse(b, NumberStyles.HexNumber));
		}

		/// <summary>
		/// Returns file system separator string.
		/// </summary>
		/// <returns></returns>
		public static char GetFileSeparator()
		{
			return Path.PathSeparator;
		}

		/// <summary>
		/// Returns path to user's home directory.
		/// </summary>
		/// <returns></returns>
		public static String GetUserHomeDirectory()
		{
			return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
		}

		/// <summary>
		/// Returns path to directory used for placement of JRobin demo graphs and creates it
		/// if necessary.
		/// </summary>
		/// <returns></returns>
		public static String GetJRobinDemoDirectory()
		{
			String homeDirPath = GetUserHomeDirectory() + JROBIN_DIR + GetFileSeparator();
			if (!Directory.Exists(homeDirPath))
				Directory.CreateDirectory(homeDirPath);
			return homeDirPath;
		}


		/// <summary>
		/// Returns full path to the file stored in the demo directory of JRobin
		/// </summary>
		/// <param name="filename">Partial path to the file stored in the demo directory of JRobin (just name and extension, without parent directories)</param>
		/// <returns></returns>
		public static String GetJRobinDemoPath(String filename)
		{
			String demoDir = GetJRobinDemoDirectory();
			return demoDir != null ? Path.Combine(demoDir, filename) : null;
		}

		internal static bool SameFilePath(String path1, String path2)
		{
			return Path.GetFullPath(path1) == Path.GetFullPath(path2);
		}

		internal static int GetMatchingDatasourceIndex(RrdDb rrd1, int dsIndex, RrdDb rrd2)
		{
			String dsName = rrd1.GetDatasource(dsIndex).Name;
			try
			{
				return rrd2.GetDataSourceIndex(dsName);
			}
			catch (RrdException)
			{
				return -1;
			}
		}

		internal static int GetMatchingArchiveIndex(RrdDb rrd1, int arcIndex, RrdDb rrd2)
		{
			Archive archive = rrd1.GetArchive(arcIndex);
			ConsolidationFunction consolFun = archive.ConsolidationFunction;
			int steps = archive.Steps;
			try
			{
				return rrd2.GetArchiveIndex(consolFun, steps);
			}
			catch (RrdException)
			{
				return -1;
			}
		}

		public static String GetTmpFilename()
		{
			return Path.GetTempFileName();
		}

		internal static class Xml
		{
			internal static string GetValue(XmlNode parentNode, bool trim)
			{
				if (parentNode == null) return String.Empty;

				return trim ? parentNode.FirstChild.Value.Trim() : parentNode.FirstChild.Value;
			}

			internal static string GetChildValue(XmlNode parent, string name, bool trim = false)
			{
				if (parent == null) return String.Empty;

				XmlNode node = parent.SelectSingleNode(name);
				if (node != null)
					return trim ? node.FirstChild.Value.Trim() : node.FirstChild.Value;
				return null;
			}

			internal static long GetChildValueAsLong(XmlNode parent, string name)
			{
				if (parent == null) return 0;

				XmlNode node = parent.SelectSingleNode(name);
				if (node != null)
					return long.Parse(node.FirstChild.Value);
				return 0;
			}

			internal static int GetChildValueAsInt(XmlNode parent, string name)
			{
				if (parent == null) return 0;

				XmlNode node = parent.SelectSingleNode(name);
				if (node != null)
					return int.Parse(node.FirstChild.Value);
				return 0;
			}

			internal static double GetChildValueAsDouble(XmlNode parent, string name)
			{
				if (parent == null) return 0;

				XmlNode node = parent.SelectSingleNode(name);
				if (node != null)
				{
					return ParseDouble(node.FirstChild.Value);
				}
				return double.NaN;
			}

			internal static T GetChildValueAsEnum<T>(XmlNode parent, string name)
			{
				if (parent == null) return default(T);

				XmlNode node = parent.SelectSingleNode(name);
				if (node != null)
					return (T)Enum.Parse(typeof(T), node.FirstChild.Value);
				return default(T);
			}

			internal static XmlNode GetFirstChildNode(XmlNode parent, string name)
			{
				XmlNode[] list = GetChildNodes(parent, name);
				return list.Length > 0 ? list[0] : null;
			}

			internal static bool HasChildNode(XmlNode parent, string name)
			{
				XmlNode[] list = GetChildNodes(parent, name);
				return list.Length > 0;
			}

			internal static XmlNode[] GetChildNodes(XmlNode parentNode, string name)
			{
				XmlNodeList nodeList = parentNode.SelectNodes(name ?? ".");
				return nodeList != null ? nodeList.Cast<XmlNode>().ToArray() : new XmlNode[0];
			}
		}

		/// <summary>
		/// Returns canonical file path for the given file path
		/// </summary>
		/// <param name="path">Absolute or relative file path</param>
		/// <returns></returns>
		public static String GetCanonicalPath(String path)
		{
			return Path.GetFullPath(path);
		}

		/// <summary>
		/// Checks if the file with the given file name exists
		/// </summary>
		/// <param name="filename">File name</param>
		/// <returns></returns>
		public static bool FileExists(String filename)
		{
			return File.Exists(filename);
		}

		/// <summary>
		/// Finds max value for an array of doubles (NaNs are ignored). If all values in the array
		/// are NaNs, NaN is returned.
		/// 
		/// @param values Array of double values
		/// @return max value in the array (NaNs are ignored)
		/// </summary>
		/// <param name="values"></param>
		/// <returns></returns>
		public static double Max(double[] values)
		{
			return values.Where(t => !double.IsNaN(t)).Aggregate(double.NaN,
			                                                     (current, t) => double.IsNaN(current) ? t : Max(current, t));
		}

		/// <summary>
		/// Finds min value for an array of doubles (NaNs are ignored). If all values in the array
		/// are NaNs, NaN is returned.
		/// </summary>
		/// <param name="values">Array of double values</param>
		/// <returns></returns>
		public static double Min(double[] values)
		{
			return values.Where(t => !double.IsNaN(t)).Aggregate(double.NaN,
			                                                     (current, t) => double.IsNaN(current) ? t : Min(current, t));
		}
	}
}