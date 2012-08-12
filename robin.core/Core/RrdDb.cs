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
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;

namespace robin.core
{
	/// <summary>
	/// <p>Main class used to create and manipulate round robin databases (RRDs). Use this class to perform
	/// update and fetch operations on exisiting RRDs, to create new RRD from
	/// the definition (object of class {@link org.jrobin.core.RrdDef RrdDef}) or
	/// from XML file (dumped content of RRDTool's or JRobin's RRD file).</p>
	/// <p/>
	/// <p>Each RRD is backed with some kind of storage. For example, RRDTool supports only one kind of
	/// storage (disk file). On the contrary, JRobin gives you freedom to use other storage (backend) types
	/// even to create your own backend types for some special purposes. JRobin by default stores
	/// RRD data in files (as RRDTool), but you might choose to store RRD data in memory (this is
	/// supported in JRobin), to use java.nio.* instead of java.io.* package for file manipulation
	/// (also supported) or to store whole RRDs in the SQL database
	/// (you'll have to extend some classes to do this).</p>
	/// <p/>
	/// <p> Note that JRobin uses binary format different from RRDTool's format. You cannot
	/// use this class to manipulate RRD files created with RRDTool. <b>However, if you perform
	/// the same sequence of create, update and fetch operations, you will get exactly the same
	/// results from JRobin and RRDTool.</b></p>
	/// <p/>
	/// You will not be able to use JRobin API if you are not familiar with
	/// basic RRDTool concepts. Good place to start is the
	/// <a href="http://people.ee.ethz.ch/~oetiker/webtools/rrdtool/tutorial/rrdtutorial.html">official RRD tutorial</a>
	/// and relevant RRDTool man pages: <a href="../../../../man/rrdcreate.html" target="man">rrdcreate</a>,
	/// <a href="../../../../man/rrdupdate.html" target="man">rrdupdate</a>,
	/// <a href="../../../../man/rrdfetch.html" target="man">rrdfetch</a> and
	/// <a href="../../../../man/rrdgraph.html" target="man">rrdgraph</a>.
	/// For RRDTool's advanced graphing capabilities (RPN extensions), also supported in JRobin,
	/// there is an excellent
	/// <a href="http://people.ee.ethz.ch/~oetiker/webtools/rrdtool/tutorial/cdeftutorial.html" target="man">CDEF tutorial</a>.
	/// <seealso cref="RrdBackend"/>
	/// <seealso cref="RrdBackendFactory"/>
	/// </summary>
	public class RrdDb : IRrdUpdater, IDisposable
	{
		// static  String RRDTOOL = "rrdtool";
		private const int XML_INITIAL_BUFFER_CAPACITY = 100000; // bytes

		private readonly RrdAllocator allocator = new RrdAllocator();

		private readonly Archive[] archives;
		private readonly RrdBackend backend;
		private readonly DataSource[] datasources;
		private readonly Header header;

		/// <summary>
		/// <p>Constructor used to create new RRD object from the definition object but with a storage
		/// (backend) different from default.</p>
		/// <p/>
		/// <p>JRobin uses <i>factories</i> to create RRD backend objecs. There are three different
		/// backend factories supplied with JRobin, and each factory has its unique name:</p>
		/// <p/>
		/// <ul>
		/// <li><b>FILE</b>: backends created from this factory will store RRD data to files by using
		/// java.io.* classes and methods</li>
		/// <li><b>NIO</b>: backends created from this factory will store RRD data to files by using
		/// java.nio.* classes and methods</li>
		/// <li><b>MEMORY</b>: backends created from this factory will store RRD data in memory. This might
		/// be useful in runtime environments which prohibit disk utilization, or for storing temporary,
		/// non-critical data (it gets lost as soon as JVM exits).</li>
		/// </ul>
		/// <p/>
		/// <p>For example, to create RRD in memory, use the following code</p>
		/// <pre>
		/// RrdBackendFactory factory = RrdBackendFactory.getFactory("MEMORY");
		/// RrdDb rrdDb = new RrdDb(rrdDef, factory);
		/// rrdDb.close();
		/// </pre>
		/// <p/>
		/// <p>New RRD file structure is specified with an object of class
		/// {@link org.jrobin.core.RrdDef <b>RrdDef</b>}. The underlying RRD storage is created as soon
		/// as the constructor returns.</p>
		/// <seealso cref="RrdBackendFactory"/> 
		/// 
		/// </summary>
		/// <param name="rrdDef">RRD definition object</param>
		/// <param name="factory">The factory which will be used to create storage for this RRD</param>
		private RrdDb(RrdDef rrdDef, RrdBackendFactory factory)
		{
			rrdDef.Validate();
			String path = rrdDef.Path;
			backend = factory.Open(path, false);
			try
			{
				backend.SetLength(rrdDef.GetEstimatedSize());
				// create header
				header = new Header(this, rrdDef);
				// create datasources
				DsDef[] dsDefs = rrdDef.DataSourceDefinitions;
				datasources = new DataSource[dsDefs.Length];
				for (int i = 0; i < dsDefs.Length; i++)
				{
					datasources[i] = new DataSource(this, dsDefs[i]);
				}
				// create archives
				ArcDef[] arcDefs = rrdDef.ArchiveDefinitions;
				archives = new Archive[arcDefs.Length];
				for (int i = 0; i < arcDefs.Length; i++)
				{
					archives[i] = new Archive(this, arcDefs[i]);
				}
			}
			catch (IOException e)
			{
				backend.Close();
				throw new RrdException(e);
			}
		}

		/// <summary>
		/// <p>Constructor used to open already existing RRD backed
		/// with a storage (backend) different from default. Constructor
		/// obtains read or read/write access to this RRD.</p>
		/// </summary>
		/// <param name="path"> Path to existing RRD.</param>
		/// <param name="readOnly">
		/// Should be set to <code>false</code> if you want to update
		/// the underlying RRD. If you want just to fetch data from the RRD file
		/// (read-only access), specify <code>true</code>. If you try to update RRD file
		/// open in read-only mode (<code>m_readOnly</code> set to <code>true</code>),
		/// <code>IOException</code> will be thrown.
		/// </param>
		/// <param name="factory">Backend factory which will be used for this RRD.</param>
		private RrdDb(String path, bool readOnly, RrdBackendFactory factory)
		{
			// opens existing RRD file - throw exception if the file does not exist...
			if (!factory.Exists(path))
			{
				throw new FileNotFoundException("Could not open " + path + " [non existent]");
			}
			backend = factory.Open(path, readOnly);
			try
			{
				// restore header
				header = new Header(this, (RrdDef) null);
				header.ValidateHeader();
				// restore datasources
				int dsCount = header.DataSourceCount;
				datasources = new DataSource[dsCount];
				for (int i = 0; i < dsCount; i++)
				{
					datasources[i] = new DataSource(this, null);
				}
				// restore archives
				int arcCount = header.ArchiveCount;
				archives = new Archive[arcCount];
				for (int i = 0; i < arcCount; i++)
				{
					archives[i] = new Archive(this, null);
				}
			}
			catch (RrdException)
			{
				backend.Close();
				throw;
			}
			catch (IOException e)
			{
				backend.Close();
				throw new RrdException(e);
			}
		}

		/// <summary>
		/// <p>Constructor used to create RRD files from external file sources with a backend type
		/// different from default. Supported external file sources are:</p>
		/// <p/>
		/// <ul>
		/// <li>RRDTool/JRobin XML file dumps (i.e files created with <code>rrdtool dump</code> command).</li>
		/// <li>RRDTool binary files.</li>
		/// </ul>
		/// <p/>
		/// <p>JRobin and RRDTool use the same format for XML dump and this constructor should be used to
		/// (re)create JRobin RRD files from XML dumps. First, dump the content of a RRDTool
		/// RRD file (use command line):</p>
		/// <p/>
		/// <pre>
		/// rrdtool dump original.rrd > original.xml
		/// </pre>
		/// <p/>
		/// <p>Than, use the file <code>original.xml</code> to create JRobin RRD file named
		/// <code>copy.rrd</code>:</p>
		/// <p/>
		/// <pre>
		/// RrdDb rrd = new RrdDb("copy.rrd", "original.xml");
		/// </pre>
		/// <p/>
		/// <p>or:</p>
		/// <p/>
		/// <pre>
		/// RrdDb rrd = new RrdDb("copy.rrd", "xml:/original.xml");
		/// </pre>
		/// <p/>
		/// <p>See documentation for {@link #dumpXml(java.lang.String) dumpXml()} method
		/// to see how to convert JRobin files to RRDTool's format.</p>
		/// <p/>
		/// <p>To read RRDTool files directly, specify <code>rrdtool:/</code> prefix in the
		/// <code>externalPath</code> argument. For example, to create JRobin compatible file named
		/// <code>copy.rrd</code> from the file <code>original.rrd</code> created with RRDTool, use
		/// the following code:</p>
		/// <p/>
		/// <pre>
		/// RrdDb rrd = new RrdDb("copy.rrd", "rrdtool:/original.rrd");
		/// </pre>
		/// <p/>
		/// <p>Note that the prefix <code>xml:/</code> or <code>rrdtool:/</code> is necessary to distinguish
		/// between XML and RRDTool's binary sources. If no prefix is supplied, XML format is assumed</p>
		/// </summary>
		/// <param name="rrdPath">Path to RRD which will be created</param>
		/// <param name="reader">DataImporter to use for importation</param>
		/// <param name="factory">Backend factory which will be used to create storage (backend) for this RRD.</param>
		private RrdDb(String rrdPath, DataImporter reader, RrdBackendFactory factory)
		{
			backend = factory.Open(rrdPath, false);
			try
			{
				backend.SetLength(reader.GetEstimatedSize());
				// create header
				header = new Header(this, reader);
				// create datasources
				datasources = new DataSource[reader.DataSourceCount];
				for (int i = 0; i < datasources.Length; i++)
				{
					datasources[i] = new DataSource(this, reader, i);
				}
				// create archives
				archives = new Archive[reader.ArchiveCount];
				for (int i = 0; i < archives.Length; i++)
				{
					archives[i] = new Archive(this, reader, i);
				}
			}
			catch (RrdException)
			{
				backend.Close();
				throw;
			}
			catch (IOException e)
			{
				backend.Close();
				throw new RrdException(e);
			}
		}

		/// <summary>
		/// Returns true if the RRD is closed.
		/// </summary>
		/// <value></value>
		public bool Closed { get; private set; }

		/// <summary>
		/// Returns RRD header.
		/// </summary>
		/// <value></value>
		public Header Header
		{
			get { return header; }
		}

		/// <summary>
		/// Datasource names defined in RRD
		/// </summary>
		/// <value></value>
		public string[] DataSourceNames
		{
			get
			{
				int n = datasources.Length;
				var dsNames = new String[n];
				for (int i = 0; i < n; i++)
				{
					dsNames[i] = datasources[i].Name;
				}
				return dsNames;
			}
		}

		public DataSource[] DataSources
		{
			get { return datasources; }
		}

		public Archive[] Archives
		{
			get { return archives; }
		}

		/// <summary>
		/// time of last update operation as timestamp (in seconds).
		/// </summary>
		/// <value></value>
		public long LastUpdateTime
		{
			[MethodImpl(MethodImplOptions.Synchronized)]
			get { return header.LastUpdateTime; }
		}

		#region IDisposable Members

		public void Dispose()
		{
			Close();
		}

		#endregion

		#region IRrdUpdater Members

		/// <summary>
		/// Copies object's internal state to another ArcState object.
		/// </summary>
		/// <param name="other"> New ArcState object to copy state to</param>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public void CopyStateTo(IRrdUpdater other)
		{
			if (!(other is RrdDb))
			{
				throw new RrdException("Cannot copy RrdDb object to " + other.GetType().Name);
			}
			var otherRrd = (RrdDb) other;
			header.CopyStateTo(otherRrd.header);
			for (int i = 0; i < datasources.Length; i++)
			{
				int j = Util.GetMatchingDatasourceIndex(this, i, otherRrd);
				if (j >= 0)
				{
					datasources[i].CopyStateTo(otherRrd.datasources[j]);
				}
			}
			for (int i = 0; i < archives.Length; i++)
			{
				int j = Util.GetMatchingArchiveIndex(this, i, otherRrd);
				if (j >= 0)
				{
					archives[i].CopyStateTo(otherRrd.archives[j]);
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
			return backend;
		}


		/// <summary>
		/// Returns the underlying storage (backend) object which actually performs all
		/// I/O operations.
		/// </summary>
		/// <returns></returns>
		public RrdAllocator GetRrdAllocator()
		{
			return allocator;
		}

		#endregion

		public static RrdDb Create(RrdDef rrdDef)
		{
			return Create(rrdDef, RrdBackendFactory.GetDefaultFactory());
		}

		public static RrdDb Create(RrdDef rrdDef, RrdBackendFactory factory)
		{
			return new RrdDb(rrdDef, factory);
		}

		public static RrdDb Open(string path, bool readOnly = false)
		{
			return Open(path, readOnly, RrdBackendFactory.GetDefaultFactory());
		}

		public static RrdDb Open(string path, RrdBackendFactory factory)
		{
			return new RrdDb(path, false, factory);
		}

		public static RrdDb Open(string path, bool readOnly, RrdBackendFactory factory)
		{
			return new RrdDb(path, readOnly, factory);
		}

		public static RrdDb Import(string rrdPath, DataImporter externalPath)
		{
			return new RrdDb(rrdPath, externalPath, RrdBackendFactory.GetDefaultFactory());
		}

		public static RrdDb Import(string rrdPath, DataImporter externalPath, RrdBackendFactory factory)
		{
			return new RrdDb(rrdPath, externalPath, factory);
		}

		/// <summary>
		/// Closes RRD. No further operations are allowed on this RrdDb object.
		/// </summary>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public void Close()
		{
			if (!Closed)
			{
				Closed = true;
				backend.Close();
			}
		}

		/// <summary>
		/// Datasource object for the given datasource index.
		/// </summary>
		/// <param name="dsIndex">index of the DataSource</param>
		/// <returns></returns>
		public DataSource GetDatasource(int dsIndex)
		{
			return datasources[dsIndex];
		}

		/// <summary>
		/// Archive object for the given archive index.
		/// </summary>
		/// <param name="arcIndex">index of the archive</param>
		/// <returns></returns>
		public Archive GetArchive(int arcIndex)
		{
			return archives[arcIndex];
		}

		/// <summary>
		/// <p>Creates new sample with the given timestamp and all datasource values set to
		/// 'unknown'. Use returned <code>Sample</code> object to specify
		/// datasource values for the given timestamp. See documentation for
		/// {@link org.jrobin.core.Sample Sample} for an explanation how to do this.</p>
		/// <p/>
		/// <p>Once populated with data source values, call Sample's
		/// {@link org.jrobin.core.Sample#update() update()} method to actually
		/// store sample in the RRD associated with it.</p>
		/// </summary>
		/// <param name="time">Sample timestamp rounded to the nearest second (without milliseconds).</param>
		/// <returns>Fresh sample with the given timestamp and all data source values set to 'unknown'.</returns>
		public Sample CreateSample(long time)
		{
			return new Sample(this, time);
		}

		/// <summary>
		/// <p>Creates new sample with the current timestamp and all data source values set to
		/// 'unknown'. Use returned <code>Sample</code> object to specify
		/// datasource values for the current timestamp. See documentation for
		/// {@link org.jrobin.core.Sample Sample} for an explanation how to do this.</p>
		/// <p/>
		/// <p>Once populated with data source values, call Sample's
		/// {@link org.jrobin.core.Sample#update() update()} method to actually
		/// store sample in the RRD associated with it.</p>
		/// </summary>
		/// <returns>Fresh sample with the current timestamp and all data source values set to 'unknown'.</returns>
		public Sample CreateSample()
		{
			return CreateSample(Util.GetCurrentTime());
		}

		/// <summary>
		/// <p>Prepares fetch request to be executed on this RRD. Use returned
		/// <code>FetchRequest</code> object and its {@link org.jrobin.core.FetchRequest#fetchData() fetchData()}
		/// method to actually fetch data from the RRD file.</p>
		/// </summary>
		/// <param name="consolFun">Consolidation function to be used in fetch request.</param>
		/// <param name="fetchStart">Starting timestamp for fetch request.</param>
		/// <param name="fetchEnd"> Ending timestamp for fetch request.</param>
		/// <param name="resolution">
		/// Fetch resolution (see RRDTool's
		/// <a href="../../../../man/rrdfetch.html" target="man">rrdfetch man page</a> for an
		/// explanation of this parameter.
		/// By default, data will be fetched with the smallest possible resolution
		/// </param>
		/// <returns>
		///  Request object that should be used to actually fetch data from RRD.
		/// </returns>
		public FetchRequest CreateFetchRequest(ConsolidationFunction consolFun, long fetchStart, long fetchEnd,
		                                       long resolution = 1)
		{
			return new FetchRequest(this, consolFun, fetchStart, fetchEnd, resolution);
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		internal void Store(Sample sample)
		{
			if (Closed)
			{
				throw new RrdException("RRD already closed, cannot store this  sample");
			}
			long newTime = sample.Time;
			long lastTime = header.LastUpdateTime;
			if (lastTime >= newTime)
			{
				throw new RrdException("Bad sample timestamp " + newTime +
				                       ". Last update time was " + lastTime + ", at least one second step is required");
			}
			double[] newValues = sample.Values;
			for (int i = 0; i < datasources.Length; i++)
			{
				double newValue = newValues[i];
				datasources[i].Process(newTime, newValue);
			}
			header.LastUpdateTime = newTime;
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		internal FetchData FetchData(FetchRequest request)
		{
			if (Closed)
			{
				throw new RrdException("RRD already closed, cannot fetch data");
			}
			Archive archive = FindMatchingArchive(request);
			return archive.FetchData(request);
		}

		private Archive FindMatchingArchive(FetchRequest request)
		{
			ConsolidationFunction consolFun = request.ConsolidationFunction;
			long fetchStart = request.FetchStart;
			long fetchEnd = request.FetchEnd;
			long resolution = request.Resolution;
			Archive bestFullMatch = null, bestPartialMatch = null;
			long bestStepDiff = 0, bestMatch = 0;
			foreach (Archive archive in archives)
			{
				if (archive.ConsolidationFunction == consolFun)
				{
					long arcStep = archive.TimeStep;
					long arcStart = archive.GetStartTime() - arcStep;
					long arcEnd = archive.GetEndTime();
					long fullMatch = fetchEnd - fetchStart;
					if (arcEnd >= fetchEnd && arcStart <= fetchStart)
					{
						long tmpStepDiff = Math.Abs(archive.TimeStep - resolution);

						if (tmpStepDiff < bestStepDiff || bestFullMatch == null)
						{
							bestStepDiff = tmpStepDiff;
							bestFullMatch = archive;
						}
					}
					else
					{
						long tmpMatch = fullMatch;

						if (arcStart > fetchStart)
						{
							tmpMatch -= (arcStart - fetchStart);
						}
						if (arcEnd < fetchEnd)
						{
							tmpMatch -= (fetchEnd - arcEnd);
						}
						if (bestPartialMatch == null || bestMatch < tmpMatch)
						{
							bestPartialMatch = archive;
							bestMatch = tmpMatch;
						}
					}
				}
			}
			if (bestFullMatch != null)
			{
				return bestFullMatch;
			}
			if (bestPartialMatch != null)
			{
				return bestPartialMatch;
			}
			throw new RrdException("RRD file does not contain RRA:" + consolFun + " archive");
		}

		/// <summary>
		/// Finds the archive that best matches to the start time (time period being start-time until now)
		/// and requested resolution.
		/// </summary>
		/// <param name="consolFun">Consolidation function of the datasource.</param>
		/// <param name="startTime">Start time of the time period in seconds.</param>
		/// <param name="resolution">Requested fetch resolution.</param>
		/// <returns>Reference to the best matching archive.</returns>
		public Archive FindStartMatchArchive(String consolFun, long startTime, long resolution)
		{
			int fallBackIndex = 0;
			int arcIndex = -1;
			long minDiff = long.MaxValue;
			long fallBackDiff = long.MaxValue;

			for (int i = 0; i < archives.Length; i++)
			{
				if (archives[i].ConsolidationFunction.Equals(consolFun))
				{
					long arcStep = archives[i].TimeStep;
					long diff = Math.Abs(resolution - arcStep);

					// Now compare start time, see if this archive encompasses the requested interval
					if (startTime >= archives[i].GetStartTime())
					{
						if (diff == 0) // Best possible match either way
						{
							return archives[i];
						}

						if (diff < minDiff)
						{
							minDiff = diff;
							arcIndex = i;
						}
					}
					else if (diff < fallBackDiff)
					{
						fallBackDiff = diff;
						fallBackIndex = i;
					}
				}
			}

			return (arcIndex >= 0 ? archives[arcIndex] : archives[fallBackIndex]);
		}

		/// <summary>
		/// <p>Returns string representing complete internal RRD state. The returned
		/// string can be printed to <code>stdout</code> and/or used for debugging purposes.</p>
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public String Dump()
		{
			var buffer = new StringBuilder();
			buffer.Append(header.Dump());
			foreach (DataSource datasource in datasources)
			{
				buffer.Append(datasource.Dump());
			}
			foreach (Archive archive in archives)
			{
				buffer.Append(archive.Dump());
			}
			return buffer.ToString();
		}

		internal void Archive(DataSource datasource, double value, long numUpdates)
		{
			int dsIndex = GetDataSourceIndex(datasource.Name);
			foreach (Archive archive in archives)
			{
				archive.archive(dsIndex, value, numUpdates);
			}
		}

		/// <summary>
		/// <p>Returns internal index number for the given datasource name. This index is heavily
		/// used by jrobin.graph package and has no value outside of it.</p>
		/// </summary>
		/// <param name="dsName">Data source name.</param>
		/// <returns>Internal index of the given data source name in this RRD.</returns>
		public int GetDataSourceIndex(String dsName)
		{
			for (int i = 0; i < datasources.Length; i++)
			{
				if (datasources[i].Name == dsName)
				{
					return i;
				}
			}
			throw new RrdException("Unknown datasource name: " + dsName);
		}

		/// <summary>
		/// Checks presence of a specific datasource.
		/// </summary>
		/// <param name="dsName">DataSource name to check</param>
		/// <returns></returns>
		public bool ContainsDataSource(String dsName)
		{
			return datasources.Any(datasource => datasource.Name == dsName);
		}

		/// <summary>
		/// <p>Writes the RRD content to OutputStream using XML format. This format
		/// is fully compatible with RRDTool's XML dump format and can be used for conversion
		/// purposes or debugging.</p>
		/// </summary>
		/// <param name="destination">Output stream to receive XML data</param>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public void ToXml(Stream destination)
		{
			XmlWriter writer = XmlWriter.Create(destination);
			writer.WriteStartElement("rrd");
			// dump header
			header.AppendXml(writer);
			// dump datasources
			foreach (DataSource datasource in datasources)
			{
				datasource.AppendXml(writer);
			}
			// dump archives
			foreach (Archive archive in archives)
			{
				archive.AppendXml(writer);
			}
			writer.WriteEndElement();
			writer.Flush();
		}

		/// <summary>
		/// <p>Returns string representing internal RRD state in XML format. This format
		/// is fully compatible with RRDTool's XML dump format and can be used for conversion
		/// purposes or debugging.</p>
		/// </summary>
		/// <returns>Internal RRD state in XML format.</returns>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public String ToXml()
		{
			using (var ms = new MemoryStream(XML_INITIAL_BUFFER_CAPACITY))
			{
				ToXml(ms);
				ms.Seek(0, SeekOrigin.Begin);
				using (TextReader reader = new StreamReader(ms))
				{
					return reader.ReadToEnd();
				}
			}
		}

		/// <summary>
		/// <p>Dumps internal RRD state to XML file.
		/// Use this XML file to convert your JRobin RRD to RRDTool format.</p>
		/// <p/>
		/// <p>Suppose that you have a JRobin RRD file <code>original.rrd</code> and you want
		/// to convert it to RRDTool format. First, execute the following java code:</p>
		/// <p/>
		/// <code>RrdDb rrd = new RrdDb("original.rrd");
		/// rrd.DumpXml("original.xml");</code>
		/// <p/>
		/// Use <code>original.xml</code> file to create the corresponding RRDTool file
		/// (from your command line):
		/// <p/>
		/// <code>rrdtool restore copy.rrd original.xml</code>
		/// </summary>
		/// <param name="filename">Path to XML file which will be created.</param>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public void ToXml(String filename)
		{
			using (FileStream outputStream = File.Create(filename))
			{
				ToXml(outputStream);
			}
		}

		/// <summary>
		/// <p>Returns RRD definition object which can be used to create new RRD
		/// with the same creation parameters but with no data in it.</p>
		/// <p/>
		/// <p>Example:</p>
		/// <p/>
		/// <code>
		/// RrdDb rrd1 = new RrdDb("original.rrd");
		/// RrdDef def = rrd1.getRrdDef();
		/// // fix path
		/// def.setPath("empty_copy.rrd");
		/// // create new RRD file
		/// RrdDb rrd2 = new RrdDb(def);
		/// </code>
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public RrdDef GetRrdDef()
		{
			// set header
			long startTime = header.LastUpdateTime;
			long step = header.Step;
			String path = backend.Path;

			var rrdDef = new RrdDef(path, startTime, step);
			// add datasources
			foreach (DataSource datasource in datasources)
			{
				var dsDef = new DsDef(datasource.Name,
				                      datasource.Type, datasource.Heartbeat,
				                      datasource.MinValue, datasource.MaxValue);
				rrdDef.AddDatasource(dsDef);
			}
			// add archives
			foreach (Archive archive in archives)
			{
				var arcDef = new ArcDef(archive.ConsolidationFunction,
				                        archive.Xff, archive.Steps, archive.Rows);
				rrdDef.AddArchive(arcDef);
			}
			return rrdDef;
		}

		/// <summary>
		/// Returns Datasource object corresponding to the given datasource name.
		/// </summary>
		/// <param name="dsName">Datasource name</param>
		/// <returns>Datasource object corresponding to the give datasource name or null if not found.</returns>
		public DataSource GetDatasource(String dsName)
		{
			return DataSources.FirstOrDefault(dataSource => dataSource.Name == dsName);
		}

		/// <summary>
		/// Returns index of Archive object with the given consolidation function and the number
		/// of steps. Exception is thrown if such archive could not be found.
		/// </summary>
		/// <param name="consolFun">Consolidation function</param>
		/// <param name="steps">Number of archive steps</param>
		/// <returns>Requested Archive object index</returns>
		public int GetArchiveIndex(ConsolidationFunction consolFun, int steps)
		{
			for (int i = 0; i < archives.Length; i++)
			{
				if (archives[i].ConsolidationFunction == consolFun &&
				    archives[i].Steps == steps)
				{
					return i;
				}
			}
			throw new RrdException("Could not find archive " + consolFun + "/" + steps);
		}

		/// <summary>
		/// Returns Archive object with the given consolidation function and the number
		/// of steps.
		/// </summary>
		/// <param name="consolFun">Consolidation function</param>
		/// <param name="steps">Number of archive steps</param>
		/// <returns>Requested Archive object or null if no such archive could be found</returns>
		public Archive GetArchive(ConsolidationFunction consolFun, int steps)
		{
			return Archives.FirstOrDefault(archive => archive.ConsolidationFunction == consolFun && archive.Steps == steps);
		}

		/// <summary>
		/// Returns canonical path to the underlying RRD file. Note that this method makes sense just for
		/// ordinary RRD files created on the disk - an exception will be thrown for RRD objects created in
		/// memory or with custom backends.
		/// </summary>
		/// <returns></returns>
		public String GetCanonicalPath()
		{
			if (backend is RrdFileBackend)
			{
				return ((RrdFileBackend) backend).GetCanonicalPath();
			}
			throw new IOException("The underlying backend has no canonical path");
		}

		/// <summary>
		/// Returns path to this RRD.
		/// </summary>
		/// <returns></returns>
		public String GetPath()
		{
			return backend.Path;
		}

		/// <summary>
		/// Returns an array of bytes representing the whole RRD.
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public byte[] GetBytes()
		{
			return backend.ReadAll();
		}

		/// <summary>
		/// Sets default backend factory to be used. This method is just an alias for
		/// {@link RrdBackendFactory#setDefaultFactory(java.lang.String)}.
		/// </summary>
		/// <param name="factoryName">Name of the backend factory to be set as default.</param>
		public static void SetDefaultFactory(String factoryName)
		{
			RrdBackendFactory.SetDefaultFactory(factoryName);
		}

		/// <summary>
		/// Returns an array of last datasource values. The first value in the array corresponds
		/// to the first datasource defined in the RrdDb and so on.
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public double[] GetLastDatasourceValues()
		{
			var values = new double[datasources.Length];
			for (int i = 0; i < values.Length; i++)
			{
				values[i] = datasources[i].LastValue;
			}
			return values;
		}

		/// <summary>
		/// Returns the last stored value for the given datasource.
		/// </summary>
		/// <param name="dsName">Datasource name</param>
		/// <returns>Last stored value for the given datasource</returns>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public double GetLastDatasourceValue(String dsName)
		{
			int dsIndex = GetDataSourceIndex(dsName);
			return datasources[dsIndex].LastValue;
		}

		/// <summary>
		///  Returns the number of datasources defined in the file
		/// </summary>
		/// <returns></returns>
		public int GetDataSourceCount()
		{
			return datasources.Length;
		}

		/// <summary>
		/// Returns the number of RRA arcihves defined in the file
		/// </summary>
		/// <returns></returns>
		public int GetArchiveCount()
		{
			return archives.Length;
		}

		/// <summary>
		/// Returns the last time when some of the archives in this RRD was updated. This time is not the
		/// same as the {@link #getLastUpdateTime()} since RRD file can be updated without updating any of
		/// the archives.
		/// </summary>
		/// <returns>last time when some of the archives in this RRD was updated</returns>
		public long GetLastArchiveUpdateTime()
		{
			long last = 0;
			foreach (Archive archive in archives)
			{
				last = Math.Max(last, archive.GetEndTime());
			}
			return last;
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public String GetInfo()
		{
			return header.Info;
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public void SetInfo(String info)
		{
			header.Info = info;
		}

		//public static void main(String[] args)
		//{
		//   Console.WriteLine("JRobin Java Library :: RRDTool choice for the Java world");
		//   Console.WriteLine("==================================================================");
		//   //Console.WriteLine("JRobin base directory: " + Util.getJRobinHomeDirectory());
		//   long time = Util.getTime();
		//   Console.WriteLine("Current timestamp: " + time + ": " + new DateTime(0, 0, 0, 0, 0, (int) time));
		//   Console.WriteLine("------------------------------------------------------------------");
		//   Console.WriteLine("For the latest information visit: http://www.jrobin.org");
		//   Console.WriteLine("(C) 2003-2005 Sasa Markovic. All rights reserved.");
		//}

		public override String ToString()
		{
			return GetType().Name + "@" + GetHashCode().ToString("X") + "[" + Path.GetFileName(GetPath()) + "]";
		}
	}
}