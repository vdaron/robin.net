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
using System.IO;
using System.Linq;
using System.Text;

namespace robin.core.jrrd
{
	/// <summary>
	/// Instances of this class model
	/// <a href="http://people.ee.ethz.ch/~oetiker/webtools/rrdtool/">Round Robin Database</a>
	/// (RRD) files.
	/// 
	/// @author <a href="mailto:ciaran@codeloop.com">Ciaran Treanor</a>
	/// </summary>
	internal class RRDatabase : IDisposable
	{
		// RRD file name
		private readonly List<Archive> archives;
		private readonly List<DataSource> dataSources;
		private readonly String name;

		internal RRDFile rrdFile;

		/// <summary>
		/// Creates a database to read from.
		/// </summary>
		/// <param name="name">name the filename of the file to read from.</param>
		public RRDatabase(String name) : this(new FileInfo(name))
		{
		}

		/// <summary>
		/// Creates a database to read from.
		/// </summary>
		/// <param name="file">FileInfo of the file to read from.</param>
		public RRDatabase(FileInfo file)
		{
			name = file.FullName;
			rrdFile = new RRDFile(file);
			Header = new Header(rrdFile);

			// Load the data sources
			dataSources = new List<DataSource>();
			for (int i = 0; i < Header.DataSourceCount; i++)
			{
				dataSources.Add(new DataSource(rrdFile));
			}

			// Load the archives
			archives = new List<Archive>();
			for (int i = 0; i < Header.ArchiveCount; i++)
			{
				archives.Add(new Archive(this));
			}

			rrdFile.Align();

			long timestamp = (long) (rrdFile.ReadInt())*1000;
			if (Header.IntVersion >= 3)
			{
				//Version 3 has an additional microsecond field
				int microSeconds = rrdFile.ReadInt();
				timestamp += (microSeconds/1000); //Date only does up to milliseconds
			}
			LastUpdate = Util.GetDateTime(timestamp);

			// Load PDPStatus(s)
			foreach (DataSource dataSource in dataSources)
			{
				dataSource.LoadPdpStatusBlock(rrdFile);
			}

			// Load CDPStatus(s)
			foreach (Archive archive in archives)
			{
				archive.LoadCdpStatusBlocks(rrdFile, Header.DataSourceCount);
			}

			// Load current row information for each archive
			foreach (Archive archive in archives)
			{
				archive.LoadCurrentRow(rrdFile);
			}

			// Now load the data
			foreach (Archive archive in archives)
			{
				archive.LoadData(rrdFile, Header.DataSourceCount);
			}
		}

		/// <summary>
		/// Returns the <code>Header</code> for this database.
		/// </summary>
		public Header Header { get; private set; }

		/// <summary>
		/// Returns the date this database was last updated.
		/// </summary>
		public DateTime LastUpdate { get; private set; }

		/// <summary>
		/// Returns the <code>DataSource</code> at the specified position in this database.
		/// </summary>
		/// <param name="index">index of datasource to return</param>
		/// <returns></returns>
		public DataSource GetDataSourceAt(int index)
		{
			return dataSources[index];
		}

		/// <summary>
		/// Returns an IEnumerable over the data sources in this database in proper sequence.
		/// </summary>
		/// <value></value>
		public IEnumerable<DataSource> DataSources
		{
			get { return dataSources; }
		}

		/// <summary>
		/// Returns the <code>Archive</code> at the specified position in this database.
		/// </summary>
		/// <param name="index">index of the archive to return</param>
		/// <returns></returns>
		public Archive GetArchiveAt(int index)
		{
			return archives[index];
		}

		/// <summary>
		/// Returns an IEnumerable over the archives in this database in proper sequence.
		/// </summary>
		public IEnumerable<Archive> Archives
		{
			get { return archives; }
		}

		/// <summary>
		/// Returns the number of archives in this database.
		/// </summary>
		/// <value></value>
		public int ArchiveCount
		{
			get { return Header.ArchiveCount; }
		}

		/// <summary>
		/// Returns an iterator over the archives in this database of the given type
		/// in proper sequence.
		/// </summary>
		/// <param name="type">the consolidation function that should have been applied to
		///             the data</param>
		/// <returns>an iterator over the archives in this database of the given type
		///         in proper sequence</returns>
		public IEnumerable<Archive> GetArchives(ConsolidationFunctionType type)
		{
			return archives.Where(archive => archive.Type == type);
		}

		/// <summary>
		/// Closes this database stream and releases any associated system resources.
		/// </summary>
		public void Close()
		{
			rrdFile.Close();
		}

		/// <summary>
		/// Outputs the header information of the database to the given print stream
		/// using the default number format. The default format for <code>double</code>
		/// is 0.0000000000E0.
		/// </summary>
		/// <param name="s">the textWriter to print the header information to.</param>
		public void PrintInfo(TextWriter s)
		{
			PrintInfo(s, "E");
		}

		/// <summary>
		/// Returns data from the database corresponding to the given consolidation
		/// function and a step size of 1.
		/// </summary>
		/// <param name="type">the consolidation function that should have been applied to the data. </param>
		/// <returns>the raw data.</returns>
		public DataChunk GetData(ConsolidationFunctionType type)
		{
			return GetData(type, 1);
		}

		/// <summary>
		/// Returns data from the database corresponding to the given consolidation
		/// function and a step size of 1.
		/// </summary>
		/// <param name="type">the consolidation function that should have been applied to the data. </param>
		/// <param name="step">the step size to use</param>
		/// <returns>the raw data.</returns>
		public DataChunk GetData(ConsolidationFunctionType type, long step)
		{
			IEnumerable<Archive> possibleArchives = GetArchives(type);

			if (possibleArchives.Count() == 0)
			{
				throw new RrdException("Database does not contain an Archive of consolidation function type "
				                       + type);
			}

			DateTime endCal = DateTime.Now;
			DateTime startCal = endCal;

			startCal.AddDays(-1);

			long end = endCal.GetTimestamp();
			long start = startCal.GetTimestamp();
			Archive archive = FindBestArchive(start, end, step, possibleArchives);

			// Tune the parameters
			step = Header.PrimaryDataPointStep*archive.PdpCount;
			start -= start%step;

			if (end%step != 0)
			{
				end += step - end%step;
			}

			var rows = (int) ((end - start)/step + 1);

			//cat.debug("start " + start + " end " + end + " step " + step + " rows "
			//          + rows);

			// Find start and end offsets
			// This is terrible - some of this should be encapsulated in Archive - CT.
			long lastUpdateLong = LastUpdate.GetTimestamp();
			long archiveEndTime = lastUpdateLong - (lastUpdateLong%step);
			long archiveStartTime = archiveEndTime - (step*(archive.RowCount - 1));
			var startOffset = (int) ((start - archiveStartTime)/step);
			var endOffset = (int) ((archiveEndTime - end)/step);

			//cat.debug("start " + archiveStartTime + " end " + archiveEndTime
			//          + " startOffset " + startOffset + " endOffset "
			//          + (archive.rowCount - endOffset));

			var chunk = new DataChunk(start, startOffset, endOffset, step,
			                          Header.DataSourceCount, rows);

			archive.LoadData(chunk);

			return chunk;
		}

		/// <summary>
		/// This is almost a verbatim copy of the original C code by Tobias Oetiker.
		/// I need to put more of a Java style on it - CT
		/// </summary>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <param name="step"></param>
		/// <param name="archives"></param>
		/// <returns></returns>
		private Archive FindBestArchive(long start, long end, long step, IEnumerable<Archive> archives)
		{
			Archive archive = null;
			Archive bestFullArchive = null;
			Archive bestPartialArchive = null;
			long lastUpdateLong = LastUpdate.GetTimestamp();
			int firstPart = 1;
			int firstFull = 1;
			long bestMatch = 0;
			//long bestPartRRA = 0;
			long bestStepDiff = 0;
			long tmpStepDiff = 0;

			foreach (Archive t in archives)
			{
				archive = t;

				long calEnd = lastUpdateLong
				              - (lastUpdateLong
				                 %(archive.PdpCount*Header.PrimaryDataPointStep));
				long calStart = calEnd
				                - (archive.PdpCount*archive.RowCount
				                   *Header.PrimaryDataPointStep);
				long fullMatch = end - start;

				if ((calEnd >= end) && (calStart < start))
				{
					// Best full match
					tmpStepDiff = Math.Abs(step - (Header.PrimaryDataPointStep*archive.PdpCount));

					if ((firstFull != 0) || (tmpStepDiff < bestStepDiff))
					{
						firstFull = 0;
						bestStepDiff = tmpStepDiff;
						bestFullArchive = archive;
					}
				}
				else
				{
					// Best partial match
					long tmpMatch = fullMatch;

					if (calStart > start)
					{
						tmpMatch -= calStart - start;
					}

					if (calEnd < end)
					{
						tmpMatch -= end - calEnd;
					}

					if ((firstPart != 0) || (bestMatch < tmpMatch))
					{
						firstPart = 0;
						bestMatch = tmpMatch;
						bestPartialArchive = archive;
					}
				}
			}

			// See how the matching went
			// optimise this
			if (firstFull == 0)
			{
				archive = bestFullArchive;
			}
			else if (firstPart == 0)
			{
				archive = bestPartialArchive;
			}

			return archive;
		}

		/// <summary>
		/// Outputs the header information of the database to the given print stream
		/// using the given number format. The format is almost identical to that
		/// produced by
		/// <a href="http://people.ee.ethz.ch/~oetiker/webtools/rrdtool/manual/rrdinfo.html">rrdtool info</a>
		/// </summary>
		/// <param name="s">the TextWriter to print the header information to.</param>
		/// <param name="numberFormat">the format to print <code>double</code>s as</param>
		public void PrintInfo(TextWriter s, string numberFormat)
		{
			s.Write("filename = \"");
			s.Write(name);
			s.WriteLine("\"");
			s.Write("rrd_version = \"");
			s.Write(Header.Version);
			s.WriteLine("\"");
			s.Write("step = ");
			s.WriteLine(Header.PrimaryDataPointStep);
			s.Write("last_update = ");
			s.WriteLine(LastUpdate.GetTimestamp());

			foreach (DataSource ds in dataSources)
			{
				ds.PrintInfo(s, numberFormat);
			}

			int index = 0;

			foreach (Archive ds in archives)
			{
				ds.PrintInfo(s, numberFormat, index++);
			}
		}

		/// <summary>
		/// Outputs the content of the database to the given print stream
		/// as a stream of XML. The XML format is almost identical to that produced by
		/// <a href="http://people.ee.ethz.ch/~oetiker/webtools/rrdtool/manual/rrddump.html">rrdtool dump</a>
		/// </summary>
		/// <param name="s">the textWriter to send the XML to</param>
		public void ToXml(TextWriter s)
		{
			s.WriteLine("<!--");
			s.WriteLine("  -- Round Robin RRDatabase Dump ");
			s.WriteLine("  -- Generated by jRRD <ciaran@codeloop.com>");
			s.WriteLine("  -->");
			s.WriteLine("<rrd>");
			s.Write("\t<version> ");
			s.Write(Header.Version);
			s.WriteLine(" </version>");
			s.Write("\t<step> ");
			s.Write(Header.PrimaryDataPointStep);
			s.WriteLine(" </step> <!-- Seconds -->");
			s.Write("\t<lastupdate> ");
			s.Write(LastUpdate.GetTimestamp());
			s.Write(" </lastupdate> <!-- ");
			s.Write(LastUpdate.ToString());
			s.WriteLine(" -->");
			s.WriteLine();


			foreach (DataSource ds in dataSources)
			{
				ds.ToXml(s);
			}

			s.WriteLine("<!-- Round Robin Archives -->");

			foreach (Archive archive in archives)
			{
				archive.ToXml(s);
			}

			s.WriteLine("</rrd>");
		}

		/// <summary>
		/// Returns a summary the contents of this database.
		/// </summary>
		/// <returns></returns>
		public override String ToString()
		{
			var sb = new StringBuilder("\n");

			sb.Append(Header.ToString());

			foreach (DataSource ds in dataSources)
			{
				sb.Append("\n\t");
				sb.Append(ds.ToString());
			}

			foreach (Archive archive in archives)
			{
				sb.Append("\n\t");
				sb.Append(archive.ToString());
			}

			return sb.ToString();
		}

		public void Dispose()
		{
			Close();
		}
	}
}