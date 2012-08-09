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
	/// Class to represent single archive definition within the RRD.
	/// Archive definition consists of the following four elements:
	/// <ul>
	/// <li>consolidation function</li>
	/// <li>X-files factor</li>
	/// <li>number of steps</li>
	/// <li>number of rows.</li>
	/// </ul>
	/// <p>For the complete explanation of all archive definition parameters, see RRDTool's
	/// <a href="../../../../man/rrdcreate.html" target="man">rrdcreate man page</a>
	/// </p>
	/// 
	/// @author <a href="mailto:saxon@jrobin.org">Sasa Markovic</a>
	/// </summary>
	public class ArcDef
	{
		/// <summary>
		/// Array of valid consolidation function names
		/// </summary>
		public static ConsolidationFunction[] CONSOL_FUNS = {ConsolidationFunction.AVERAGE, ConsolidationFunction.MAX, ConsolidationFunction.MIN, ConsolidationFunction.LAST};

		/// <summary>
		/// <p>Creates new archive definition object. This object should be passed as argument to
		/// {@link RrdDef#AddArchive(ArcDef) AddArchive()} method of
		/// {@link RrdDb RrdDb} object.</p>
		/// <p/>
		/// <p>For the complete explanation of all archive definition parameters, see RRDTool's
		/// <a href="../../../../man/rrdcreate.html" target="man">rrdcreate man page</a></p>
		/// </summary>
		/// <param name="consolFun">
		/// Consolidation function. Allowed values are "AVERAGE", "MIN",
		/// "MAX" and "LAST" (these string constants are conveniently defined in the
		/// {@link ConsolFuns} class).
		/// </param>
		/// <param name="xff">X-files factor, between 0 and 1.</param>
		/// <param name="steps">Number of archive steps.</param>
		/// <param name="rows">Number of archive rows</param>
		public ArcDef(ConsolidationFunction consolFun, double xff, int steps, int rows)
		{
			ConsolFun = consolFun;
			Xff = xff;
			Steps = steps;
			Rows = rows;
			Validate();
		}

		/// <summary>
		/// Adds single archive to RRD definition from a RRDTool-like
		/// archive definition string. The string must have five elements separated with colons
		/// (:) in the following order:<p>
		/// <code>
		/// RRA:consolidationFunction:XFilesFactor:steps:rows
		/// </code>
		/// For example:</p>
		/// <code>
		/// RRA:AVERAGE:0.5:10:1000
		/// </code>
		/// For more information on archive definition parameters see <code>rrdcreate</code>
		/// man page.
		/// </summary>
		/// <param name="rrdToolArcDef">Archive definition string with the syntax borrowed from RRDTool.</param>
		public static ArcDef FromRrdToolString(String rrdToolArcDef)
		{
			var rrdException = new RrdException("Wrong rrdtool-like archive definition: " + rrdToolArcDef);
			String[] tokens = rrdToolArcDef.Split(':');
			if (tokens.Length != 5)
				throw rrdException;
			if (!tokens[0].Equals("RRA", StringComparison.InvariantCultureIgnoreCase))
			{
				throw rrdException;
			}
			ConsolidationFunction consolFun;
			if (!Enum.TryParse(tokens[1], true, out consolFun))
				throw rrdException;

			double xff;
			if (!double.TryParse(tokens[2],NumberStyles.Float,CultureInfo.InvariantCulture.NumberFormat, out xff))
			{
				throw rrdException;
			}
			int steps;
			if (!int.TryParse(tokens[3], out steps))
			{
				throw rrdException;
			}
			int rows;
			if (!int.TryParse(tokens[4], out rows))
			{
				throw rrdException;
			}
			return new ArcDef(consolFun, xff, steps, rows);
		}

		/// <summary>
		/// Consolidation function.
		/// </summary>
		public ConsolidationFunction ConsolFun { get; private set; }

		/// <summary>
		///  X-files factor.
		/// </summary>
		/// <value></value>
		public double Xff { get; private set; }

		/// <summary>
		/// the number of primary RRD steps which complete a single archive step.
		/// </summary>
		/// <value></value>
		public int Steps { get; private set; }

		/// <summary>
		/// number of rows (aggregated values) stored in the archive.
		/// </summary>
		/// <value></value>
		public int Rows { get; internal set; }

		private void Validate()
		{
			if (!ValidConsolFun(ConsolFun))
			{
				throw new RrdException("Invalid consolidation function specified: " + ConsolFun);
			}
			if (Double.IsNaN(Xff) || Xff < 0.0 || Xff >= 1.0)
			{
				throw new RrdException("Invalid xff, must be >= 0 and < 1: " + Xff);
			}
			if (Steps < 1 || Rows < 2)
			{
				throw new RrdException("Invalid steps/rows settings: " + Steps + "/" + Rows +
				                       ". Minimal values allowed are steps=1, rows=2");
			}
		}

		/// <summary>
		/// String representing archive definition (RRDTool format).
		/// </summary>
		/// <returns></returns>
		public String Dump()
		{
			return "RRA:" + ConsolFun + ":" + Xff + ":" + Steps + ":" + Rows;
		}

		/// <summary>
		/// Checks if two archive definitions are equal.
		/// Archive definitions are considered equal if they have the same number of steps
		/// and the same consolidation function. It is not possible to create RRD with two
		/// equal archive definitions.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(Object obj)
		{
			if (obj is ArcDef)
			{
				var arcObj = (ArcDef) obj;
				return ConsolFun == arcObj.ConsolFun && Steps == arcObj.Steps;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (ConsolFun.GetHashCode() + Steps)*53;
		}

		/// <summary>
		/// Checks if function argument represents valid consolidation function name.
		/// </summary>
		/// <param name="consolFun">Consolidation function name</param>
		/// <returns></returns>
		public static bool ValidConsolFun(ConsolidationFunction consolFun)
		{
			return CONSOL_FUNS.Any(cFun => cFun == consolFun);
		}

		internal bool ExactlyEqual(ArcDef def)
		{
			return ConsolFun == def.ConsolFun && Xff == def.Xff &&
			       Steps == def.Steps && Rows == def.Rows;
		}

		public override string ToString()
		{
			return "ArcDef@" + GetHashCode().ToString("X") + "[consolFun=" + ConsolFun + ",xff=" + Xff + ",steps=" + Steps + ",rows=" + Rows + "]";
		}
	}
}