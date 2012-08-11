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
using System.Text;
using System.IO;
using System.Xml;

namespace robin.core
{
	/// <summary>
	/// <p>Class to represent definition of new Round Robin Database (RRD).
	/// Object of this class is used to create
	/// new RRD from scratch - pass its reference as a <code>RrdDb</code> constructor
	/// argument (see documentation for {@link RrdDb RrdDb} class). <code>RrdDef</code>
	/// object <b>does not</b> actually create new RRD. It just holds all necessary
	/// information which will be used during the actual creation process</p>
	/// <p/>
	/// <p>RRD definition (RrdDef object) consists of the following elements:</p>
	/// <p/>
	/// <ul>
	/// <li> path to RRD that will be created</li>
	/// <li> starting timestamp</li>
	/// <li> step</li>
	/// <li> one or more datasource definitions</li>
	/// <li> one or more archive definitions</li>
	/// </ul>
	/// <p>RrdDef provides API to set all these elements. For the complete explanation of all
	/// RRD definition parameters, see RRDTool's
	/// <a href="../../../../man/rrdcreate.html" target="man">rrdcreate man page</a>.</p>
	/// 
	/// @author <a href="mailto:saxon@jrobin.org">Sasa Markovic</a>
	/// </summary>
	public class RrdDef
	{
		/// <summary>
		/// Default RRD step to be used if not specified in constructor (300 seconds)
		/// </summary>
		public static long DEFAULT_STEP = 300L;

		/// <summary>
		/// if not specified in constructor, starting timestamp will be set to the current timestamp plus DEFAULT_INITIAL_SHIFT seconds (-10)
		/// </summary>
		public static long DEFAULT_INITIAL_SHIFT = -10L;
		private readonly List<ArcDef> arcDefs = new List<ArcDef>();
		private readonly List<DsDef> dsDefs = new List<DsDef>();

		/// <summary>
		/// <p>Creates new RRD definition object with the given path.
		/// When this object is passed to
		/// <code>RrdDb</code> constructor, new RRD will be created using the
		/// specified path. </p>
		/// </summary>
		/// <param name="path">Path to new RRD.</param>
		public RrdDef(String path)
		{
			Step = DEFAULT_STEP;
			StartTime = Util.GetCurrentTime() + DEFAULT_INITIAL_SHIFT;
			if (string.IsNullOrEmpty(path))
			{
				throw new RrdException("No path specified");
			}
			this.Path = path;
		}

		/// <summary>
		/// Creates new RRD definition object with the given path and step.
		/// </summary>
		/// <param name="path">Path to new RRD.</param>
		/// <param name="step">RRD step.</param>
		public RrdDef(String path, long step)
			: this(path)
		{
			if (step <= 0)
			{
				throw new RrdException("Invalid RRD step specified: " + step);
			}
			this.Step = step;
		}
		/// <summary>
		/// <p>Creates new RRD definition object with the given path, starting timestamp
		/// and step.</p>
		/// </summary>
		/// <param name="path">Path to new RRD.</param>
		/// <param name="startTime">RRD starting timestamp.</param>
		/// <param name="step">RRD step.</param>
		public RrdDef(String path, long startTime, long step)
			: this(path, step)
		{
			if (startTime < 0)
			{
				throw new RrdException("Invalid RRD start time specified: " + startTime);
			}
			this.StartTime = startTime;
		}

		/// <summary>
		/// path to the new RRD which should be created
		/// </summary>
		/// <value></value>
		public string Path { get; set; }

		/// <summary>
		/// RRD starting timestamp
		/// </summary>
		/// <value></value>
		public long StartTime { get; set; }

		/// <summary>
		/// RRD step
		/// </summary>
		/// <value></value>
		public long Step { get; set; }

		/// <summary>
		/// Adds single datasource definition represented with object of class <code>DsDef</code>.
		/// </summary>
		/// <param name="dsDef">Datasource definition.</param>
		public void AddDatasource(DsDef dsDef)
		{
			if (dsDefs.Contains(dsDef))
			{
				throw new RrdException("Datasource already defined: " + dsDef.Dump());
			}
			dsDefs.Add(dsDef);
		}

		/// <summary>
		/// <p>Adds single datasource to RRD definition by specifying its data source name, source type,
		/// heartbeat, minimal and maximal value. For the complete explanation of all data
		/// source definition parameters see RRDTool's
		/// <a href="../../../../man/rrdcreate.html" target="man">rrdcreate man page</a>.</p>
		/// <p/>
		/// <p><b>IMPORTANT NOTE:</b> If datasource name ends with '!', corresponding archives will never
		/// store NaNs as datasource values. In that case, NaN datasource values will be silently
		/// replaced with zeros by the framework.</p>
		/// </summary>
		/// <param name="dsName">Data source name.</param>
		/// <param name="dsType">Data source type.</param>
		/// <param name="heartbeat">Data source heartbeat.</param>
		/// <param name="minValue">Minimal acceptable value. Use <code>Double.NaN</code> if unknown.</param>
		/// <param name="maxValue">Maximal acceptable value. Use <code>Double.NaN</code> if unknown.</param>
		public void AddDatasource(String dsName, DataSourceType dsType, long heartbeat, double minValue, double maxValue)
		{
			AddDatasource(new DsDef(dsName, dsType, heartbeat, minValue, maxValue));
		}

		/// <summary>
		///  Adds data source definitions to RRD definition in bulk.
		/// </summary>
		/// <param name="dsDefs"></param>
		public void AddDatasource(IEnumerable<DsDef> dsDefs)
		{
			if (dsDefs == null) throw new ArgumentNullException("dsDefs");

			foreach (DsDef dsDef in dsDefs)
			{
				AddDatasource(dsDef);
			}
		}

		/// <summary>
		/// Adds single archive definition represented with object of class <code>ArcDef</code>.
		/// </summary>
		/// <param name="arcDef">Archive definition.</param>
		public void AddArchive(ArcDef arcDef)
		{
			if (arcDef == null) throw new ArgumentNullException("arcDef");

			if (arcDefs.Contains(arcDef))
			{
				throw new RrdException("Archive already defined: " + arcDef.Dump());
			}
			arcDefs.Add(arcDef);
		}

		/// <summary>
		/// Adds archive definitions to RRD definition in bulk.
		/// </summary>
		/// <param name="arcDefs"></param>
		public void AddArchive(IEnumerable<ArcDef> arcDefs)
		{
			if (arcDefs == null) throw new ArgumentNullException("arcDefs");

			foreach (ArcDef arcDef in arcDefs)
			{
				AddArchive(arcDef);
			}
		}

		/// <summary>
		/// Adds single archive definition by specifying its consolidation function, X-files factor,
		/// number of steps and rows. For the complete explanation of all archive
		/// definition parameters see RRDTool's
		/// <a href="../../../../man/rrdcreate.html" target="man">rrdcreate man page</a>.</p>
		/// </summary>
		/// <param name="consolFun">Consolidation function. </param>
		/// <param name="xff"> X-files factor. Valid values are between 0 and 1.</param>
		/// <param name="steps">Number of archive steps</param>
		/// <param name="rows">Number of archive rows</param>
		public void AddArchive(ConsolidationFunction consolFun, double xff, int steps, int rows)
		{
			AddArchive(new ArcDef(consolFun, xff, steps, rows));
		}

		internal void Validate()
		{
			if (dsDefs.Count == 0)
			{
				throw new RrdException("No RRD datasource specified. At least one is needed.");
			}
			if (arcDefs.Count == 0)
			{
				throw new RrdException("No RRD archive specified. At least one is needed.");
			}
		}

		/// <summary>
		/// All data source definition objects specified so far.
		/// </summary>
		/// <value></value>
		public DsDef[] DataSourceDefinitions
		{
			get { return dsDefs.ToArray(); }
		}

		/// <summary>
		/// Returns all archive definition objects specified so far.
		/// </summary>
		/// <value></value>
		public ArcDef[] ArchiveDefinitions
		{
			get { return arcDefs.ToArray(); }
		}

		/// <summary>
		/// Returns string that represents all specified RRD creation parameters. Returned string
		/// has the syntax of RRDTool's <code>create</code> command.
		/// </summary>
		/// <returns></returns>
		public String Dump()
		{
			var buffer = new StringBuilder("create \"");
			buffer.Append(Path).Append("\"");
			buffer.Append(" --start ").Append(StartTime);
			buffer.Append(" --step ").Append(Step).Append(" ");
			foreach (DsDef dsDef in dsDefs)
			{
				buffer.Append(dsDef.Dump()).Append(" ");
			}
			foreach (ArcDef arcDef in arcDefs)
			{
				buffer.Append(arcDef.Dump()).Append(" ");
			}
			return buffer.ToString().Trim();
		}

		/// <summary>
		/// Remove data source specified by data source name
		/// </summary>
		/// <param name="dsName">data source name to remove</param>
		public void RemoveDatasource(String dsName)
		{
			for (int i = 0; i < dsDefs.Count; i++)
			{
				DsDef dsDef = dsDefs[i];
				if (dsDef.Name == dsName)
				{
					dsDefs.RemoveAt(i);
					return;
				}
			}
			throw new RrdException("Could not find datasource named '" + dsName + "'");
		}

		/// <summary>
		/// TODO : Fix this method and add documentation
		/// </summary>
		/// <param name="dsName"></param>
		internal void SaveSingleDatasource(String dsName)
		{
			DsDef found = null;
			IEnumerator<DsDef> it = dsDefs.GetEnumerator();
			while (it.MoveNext())
			{
				DsDef dsDef = it.Current;
				if (dsDef.Name != dsName)
				{
					found = dsDef;
					break;
				}
			}
			if (found != null)
				dsDefs.Remove(found);
		}

		public void RemoveArchive(ConsolidationFunction consolFun, int steps)
		{
			ArcDef arcDef = FindArchive(consolFun, steps);
			if (!arcDefs.Remove(arcDef))
			{
				throw new RrdException("Could not remove archive " + consolFun + "/" + steps);
			}
		}

		internal ArcDef FindArchive(ConsolidationFunction consolFun, int steps)
		{
			ArcDef result = arcDefs.SingleOrDefault(x => x.ConsolFun == consolFun  && x.Steps == steps);
			if(result !=null)
				return result;
			throw new RrdException("Could not find archive " + consolFun + "/" + steps);
		}

		/// <summary>
		/// Exports RrdDef object to output stream in XML format.
		/// </summary>
		/// <param name="outxml">Output stream</param>
		public void ExportXmlTemplate(Stream outxml)
		{
			using(XmlWriter xml = XmlWriter.Create(outxml,new XmlWriterSettings{Encoding = Encoding.UTF8,CloseOutput = false}))
			{
				xml.WriteStartElement("rrd_def");
				xml.WriteElementString("path", Path);
				xml.WriteElementString("step", Step.ToString());
				xml.WriteElementString("start", StartTime.ToString());
				foreach (DsDef dsDef in DataSourceDefinitions)
				{
					xml.WriteStartElement("datasource");
					xml.WriteElementString("name", dsDef.Name);
					xml.WriteElementString("type", dsDef.Type.ToString());
					xml.WriteElementString("heartbeat", dsDef.Heartbeat.ToString());
					xml.WriteElementString("min", dsDef.MinValue.ToString());
					xml.WriteElementString("max", dsDef.MaxValue.ToString());
					xml.WriteEndElement(); // datasource
				}
				foreach (ArcDef arcDef in ArchiveDefinitions)
				{
					xml.WriteStartElement("archive");
					xml.WriteElementString("cf", arcDef.ConsolFun.ToString());
					xml.WriteElementString("xff", arcDef.Xff.ToString());
					xml.WriteElementString("steps", arcDef.Steps.ToString());
					xml.WriteElementString("rows", arcDef.Rows.ToString());
					xml.WriteEndElement(); // archive
				}
				xml.WriteEndElement(); // rrd_def
				xml.Flush();
			}
		}

		/// <summary>
		/// Exports RrdDef object to string in XML format.
		/// </summary>
		/// <returns>XML formatted string representing this RrdDef object</returns>
		public String ExportXmlTemplate()
		{
			using (MemoryStream stream = new MemoryStream())
			{
				ExportXmlTemplate(stream);
				stream.Seek(0, SeekOrigin.Begin);
				using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
				{
					return reader.ReadToEnd();
				}
			}
		}

		/// <summary>
		/// Exports RrdDef object to a file in XML format.
		/// </summary>
		/// <param name="filePath">Path to the file</param>
		public void ExportXmlTemplate( String filePath)
		{
			using (FileStream writer = File.OpenWrite(filePath))
			{
				ExportXmlTemplate(writer);
			}
		}

		/// <summary>
		/// Returns the number of storage bytes required to create RRD from this
		/// RrdDef object.
		/// </summary>
		/// <returns></returns>
		public long GetEstimatedSize()
		{
			int dsCount = dsDefs.Count;
			int arcCount = arcDefs.Count;
			int rowsCount = arcDefs.Sum(arcDef => arcDef.Rows);
			return CalculateSize(dsCount, arcCount, rowsCount);
		}

		internal static long CalculateSize(int dsCount, int arcCount, int rowsCount)
		{
			return (24L + 48L*dsCount + 16L*arcCount +
			        20L*dsCount*arcCount + 8L*dsCount*rowsCount) +
			       (1L + 2L*dsCount + arcCount)*2L*RrdPrimitive.STRING_LENGTH;
		}

		/**
	 * Compares the current RrdDef with another. RrdDefs are considered equal if:<p>
	 * <ul>
	 * <li>RRD steps match
	 * <li>all datasources have exactly the same definition in both RrdDef objects (datasource names,
	 * types, heartbeat, min and max values must match)
	 * <li>all archives have exactly the same definition in both RrdDef objects (archive consolidation
	 * functions, X-file factors, step and row counts must match)
	 * </ul>
	 *
	 * @param obj The second RrdDef object
	 * @return true if RrdDefs match exactly, false otherwise
	 */

		public override bool Equals(Object obj)
		{
			if (obj == null || !(obj is RrdDef))
			{
				return false;
			}
			var rrdDef2 = (RrdDef) obj;
			// check primary RRD step
			if (Step != rrdDef2.Step)
			{
				return false;
			}
			// check datasources
			DsDef[] dsDefs1 = DataSourceDefinitions;
			DsDef[] dsDefs2 = rrdDef2.DataSourceDefinitions;
			if (dsDefs1.Length != dsDefs2.Length)
			{
				return false;
			}
			foreach (DsDef dsDef in dsDefs1)
			{
				bool matched = false;
				foreach (DsDef dsDef2 in dsDefs2)
				{
					if (dsDef.ExactlyEqual(dsDef2))
					{
						matched = true;
						break;
					}
				}
				// this datasource could not be matched
				if (!matched)
				{
					return false;
				}
			}
			// check archives
			ArcDef[] arcDefs1 = ArchiveDefinitions;
			ArcDef[] arcDefs2 = rrdDef2.ArchiveDefinitions;
			if (arcDefs1.Length != arcDefs2.Length)
			{
				return false;
			}
			foreach (ArcDef arcDef1 in arcDefs1)
			{
				bool matched = false;
				foreach (ArcDef arcDef2 in arcDefs2)
				{
					if (arcDef1.ExactlyEqual(arcDef2))
					{
						matched = true;
						break;
					}
				}
				// this archive could not be matched
				if (!matched)
				{
					return false;
				}
			}
			// everything matches
			return true;
		}

		public override int GetHashCode()
		{
			var hashCode = (int) Step;
			hashCode = dsDefs.Aggregate(hashCode, (current, dsDef) => current*dsDef.GetHashCode());
			return arcDefs.Aggregate(hashCode, (current, arcDef) => current*arcDef.GetHashCode());
		}

		/// <summary>
		/// Removes all datasource definitions.
		/// </summary>
		public void RemoveAllDatasources()
		{
			dsDefs.Clear();
		}

		/// <summary>
		/// Removes all RRA archive definitions.
		/// </summary>
		public void RemoveAllArchives()
		{
			arcDefs.Clear();
		}

		public override string ToString()
		{
			return GetType().Name + "@" + GetHashCode().ToString("X") + "[arcDefs=[" + Join(ArchiveDefinitions) + "],dsDefs=[" + Join(DataSourceDefinitions) +
			       "]]";
		}

		private static String Join(Object[] objs)
		{
			var sb = new StringBuilder();
			for (int i = 0; i < objs.Length; i++)
			{
				sb.Append(objs[i]);
				if (i != (objs.Length - 1))
				{
					sb.Append(",");
				}
			}
			return sb.ToString();
		}
	}
}