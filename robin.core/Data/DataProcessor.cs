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
using robin.core;
using robin.Data.Utils;

namespace robin.data
{
	/// <summary>
	/// Class which should be used for all calculations based on the data fetched from RRD files. This class
	/// supports ordinary DEF datasources (defined in RRD files), CDEF datasources (RPN expressions evaluation),
	/// SDEF (static datasources - extension of JRobin) and PDEF (plottables, see
	/// {@link Plottable Plottable} for more information.
	/// Typical class usage:
	/// <code>
	///  long t1 = ...
	///  long t2 = ...
	/// DataProcessor dp = new DataProcessor(t1, t2);
	/// // DEF datasource
	/// dp.addDatasource("x", "demo.rrd", "some_source", "AVERAGE");
	/// // DEF datasource
	/// dp.addDatasource("y", "demo.rrd", "some_other_source", "AVERAGE");
	/// // CDEF datasource, z = (x + y) / 2
	/// dp.addDatasource("z", "x,y,+,2,/");
	/// // ACTION!
	/// dp.processData();
	/// // Dump calculated values
	/// System.out.println(dp.dump());
	/// </code>
	/// </summary>
	public class DataProcessor
	{
		private const double DEFAULT_PERCENTILE = 95.0; // %

		/// <summary>
		/// Constant representing the default number of pixels on a JRobin graph (will be used if
		/// no other value is specified with {@link #setStep(long) setStep()} method.
		/// </summary>
		public static int DEFAULT_PIXEL_COUNT = 600;

		/// <summary>
		/// Constant that defines the default {@link RrdDbPool} usage policy. Defaults to <code>false</code>
		/// (i.e. the pool will not be used to fetch data from RRD files)
		/// </summary>
		public static bool DEFAULT_POOL_USAGE_POLICY;

		private readonly LinkedHashMap<String, Source> sources = new LinkedHashMap<String, Source>();

		private readonly long tStart;
		private Def[] defSources;
		private long lastRrdArchiveUpdateTime;
		// this will be adjusted later
		private long tEnd;
		private long[] timestamps;
		// resolution to be used for RRD fetch operation

		/// <summary>
		/// Creates new DataProcessor object for the given time span. Ending timestamp may be set to zero.
		/// In that case, the class will try to find the optimal ending timestamp based on the last update time of
		/// RRD files processed with the {@link #processData()} method.
		/// </summary>
		/// <param name="t1">Starting timestamp in seconds without milliseconds</param>
		/// <param name="t2">Ending timestamp in seconds without milliseconds</param>
		public DataProcessor(long t1, long t2)
		{
			FetchRequestResolution = 1;
			PixelCount = DEFAULT_PIXEL_COUNT;
			IsPoolUsed = DEFAULT_POOL_USAGE_POLICY;
			if ((t1 < t2 && t1 > 0 && t2 > 0) || (t1 > 0 && t2 == 0))
			{
				tStart = t1;
				tEnd = t2;
			}
			else
			{
				throw new RrdException("Invalid timestamps specified: " + t1 + ", " + t2);
			}
		}

		/// <summary>
		/// Creates new DataProcessor object for the given time span. Ending date may be set to null.
		/// In that case, the class will try to find optimal ending date based on the last update time of
		/// RRD files processed with the {@link #processData()} method.
		/// </summary>
		/// <param name="d1">Starting date</param>
		/// <param name="d2">Ending date</param>
		public DataProcessor(DateTime d1, DateTime? d2)
			: this(Util.GetTimestamp(d1), d2.HasValue ? Util.GetTimestamp(d2.Value) : 0)
		{
		}

		/// <summary>
		/// return true, if the pool will be used internally to fetch data from RRD files, false otherwise.
		/// </summary>
		/// <value></value>
		public bool IsPoolUsed { get; set; }


		/// <summary>
		/// Sets the number of pixels (target graph width). This number is used only to calculate pixel coordinates
		/// for JRobin graphs (methods {@link #getValuesPerPixel(String)} and {@link #getTimestampsPerPixel()}),
		/// but has influence neither on datasource values calculated with the
		/// {@link #processData()} method nor on aggregated values returned from {@link #getAggregates(String)}
		/// and similar methods. In other words, aggregated values will not change once you decide to change
		/// the dimension of your graph.
		/// <p/>
		/// The default number of pixels is defined by constant {@link #DEFAULT_PIXEL_COUNT}
		/// and can be changed with a {@link #setPixelCount(int)} method.
		/// </summary>
		/// <value>
		/// 	The number of pixels. If you process RRD data in order to display it on the graph,
		/// 	this should be the width of your graph.
		/// </value>
		public int PixelCount { get; set; }

		/// <summary>
		/// Roughly corresponds to the --step option in RRDTool's graph/xport commands. Here is an explanation borrowed
		/// from RRDTool:
		/// <p/>
		/// <i>"By default rrdgraph calculates the width of one pixel in the time
		/// domain and tries to get data at that resolution from the RRD. With
		/// this switch you can override this behavior. If you want rrdgraph to
		/// get data at 1 hour resolution from the RRD, then you can set the
		/// step to 3600 seconds. Note, that a step smaller than 1 pixel will
		/// be silently ignored."</i>
		/// <p/>
		/// I think this option is not that useful, but it's here just for compatibility.<p>
		/// 
		/// Time step at which data should be fetched from RRD files. If this method is not used,
		///             the step will be equal to the smallest RRD step of all processed RRD files. If no RRD file is processed,
		///             the step will be roughly equal to the with of one graph pixel (in seconds).
		/// </summary>
		public long Step { get; set; }

		/// <summary>
		/// Returns desired RRD archive step (reslution) in seconds to be used while fetching data
		/// from RRD files. In other words, this value will used as the last parameter of
		/// {@link RrdDb#createFetchRequest(String, long, long, long) RrdDb.createFetchRequest()} method
		/// when this method is called internally by this DataProcessor.
		/// 
		/// Sets desired RRD archive step in seconds to be used internally while fetching data
		/// from RRD files. In other words, this value will used as the last parameter of
		/// {@link RrdDb#createFetchRequest(String, long, long, long) RrdDb.createFetchRequest()} method
		/// when this method is called internally by this DataProcessor. If this method is never called, fetch
		/// request resolution defaults to 1 (smallest possible archive step will be chosen automatically).
		/// </summary>
		public long FetchRequestResolution { get; set; }

		/**
	 * Returns ending timestamp. Basically, this value is equal to the ending timestamp
	 * specified in the constructor. However, if the ending timestamps was zero, it
	 * will be replaced with the real timestamp when the {@link #processData()} method returns. The real
	 * value will be calculated from the last update times of processed RRD files.
	 *
	 * @return Ending timestamp in seconds
	 */

		public long EndingTimestamp
		{
			get { return tEnd; }
		}

		/// <summary>
		/// Returns consolidated timestamps created with the {@link #processData()} method.
		/// </summary>
		public long[] GetTimestamps()
		{
			return timestamps;
		}

		/// <summary>
		/// Returns calculated values for a single datasource. Corresponding timestamps can be obtained from
		/// the {@link #getTimestamps()} method.
		/// </summary>
		/// <param name="sourceName">Datasource name</param>
		/// <returns>an array of datasource values</returns>
		public double[] GetValues(String sourceName)
		{
			Source source = GetSource(sourceName);
			double[] values = source.Values;
			if (values == null)
			{
				throw new RrdException("Values not available for source [" + sourceName + "]");
			}
			return values;
		}

		/// <summary>
		/// Returns single aggregated value for a single datasource.
		/// </summary>
		/// <param name="sourceName">Datasource name</param>
		/// <param name="consolFun">Consolidation function to be applied to fetched datasource values.</param>
		/// <returns>
		/// MIN, MAX, LAST, FIRST, AVERAGE or TOTAL value calculated from the data
		/// for the given datasource name
		/// </returns>
		public double GetAggregate(String sourceName, ConsolidationFunction consolFun)
		{
			Source source = GetSource(sourceName);
			return source.GetAggregates(tStart, tEnd).GetAggregate(consolFun);
		}

		/// <summary>
		/// Returns all (MIN, MAX, LAST, FIRST, AVERAGE and TOTAL) aggregated values for a single datasource.
		/// </summary>
		/// <param name="sourceName">Datasource name</param>
		/// <returns>Object containing all aggregated values</returns>
		public Aggregates GetAggregates(String sourceName)
		{
			Source source = GetSource(sourceName);
			return source.GetAggregates(tStart, tEnd);
		}

		/// <summary>
		///  This method is just an alias for {@link #getPercentile(String)} method.
		/// 
		///  Used by ISPs which charge for bandwidth utilization on a "95th percentile" basis.
		///  
		///  The 95th percentile is the highest source value left when the top 5% of a numerically sorted set
		///  of source data is discarded. It is used as a measure of the peak value used when one discounts
		///  a fair amount for transitory spikes. This makes it markedly different from the average.
		///  
		///  Read more about this topic at
		///  <a href="http://www.red.net/support/resourcecentre/leasedline/percentile.php">Rednet</a> or
		///  <a href="http://www.bytemark.co.uk/support/tech/95thpercentile.html">Bytemark</a>.
		/// </summary>
		/// <param name="sourceName"> Datasource name</param>
		/// <returns> 95th percentile of fetched source values</returns>
		public double Get95Percentile(String sourceName)
		{
			return GetPercentile(sourceName);
		}

		/// <summary>
		///  Used by ISPs which charge for bandwidth utilization on a "95th percentile" basis.
		///  
		///  The 95th percentile is the highest source value left when the top 5% of a numerically sorted set
		///  of source data is discarded. It is used as a measure of the peak value used when one discounts
		///  a fair amount for transitory spikes. This makes it markedly different from the average.
		///  
		///  Read more about this topic at
		///  <a href="http://www.red.net/support/resourcecentre/leasedline/percentile.php">Rednet</a> or
		///  <a href="http://www.bytemark.co.uk/support/tech/95thpercentile.html">Bytemark</a>.
		/// </summary>
		/// <param name="sourceName"> Datasource name</param>
		/// <returns> 95th percentile of fetched source values</returns>
		public double GetPercentile(String sourceName)
		{
			return GetPercentile(sourceName, DEFAULT_PERCENTILE);
		}

		/// <summary>
		/// The same as {@link #getPercentile(String)} but with a possibility to define custom percentile boundary (different from 95).
		/// </summary>
		/// <param name="sourceName">Datasource name.</param>
		/// <param name="percentile">
		/// Boundary percentile. Value of 95 (%) is suitable in most cases, but you are free
		/// to provide your own percentile boundary between zero and 100.
		/// </param>
		/// <returns>Requested percentile of fetched source values</returns>
		public double GetPercentile(String sourceName, double percentile)
		{
			if (percentile <= 0.0 || percentile > 100.0)
			{
				throw new RrdException("Invalid percentile [" + percentile + "], should be between 0 and 100");
			}
			Source source = GetSource(sourceName);
			return source.GetPercentile(tStart, tEnd, percentile);
		}

		/// <summary>
		/// Returns array of datasource names defined in this DataProcessor.
		/// </summary>
		/// <returns></returns>
		public String[] GetSourceNames()
		{
			return sources.Keys.ToArray();
		}

		/// <summary>
		/// Returns an array of all datasource values for all datasources. Each row in this two-dimensional
		/// array represents an array of calculated values for a single datasource. The order of rows is the same
		/// as the order in which datasources were added to this DataProcessor object.
		/// </summary>
		/// <returns>
		///  All datasource values for all datasources. The first index is the index of the datasource,
		/// the second index is the index of the datasource value. The number of datasource values is equal
		/// to the number of timestamps returned with {@link #getTimestamps()}  method.
		/// </returns>
		public double[][] GetValues()
		{
			String[] names = GetSourceNames();
			var values = new double[names.Length][];
			for (int i = 0; i < names.Length; i++)
			{
				values[i] = GetValues(names[i]);
			}
			return values;
		}

		private Source GetSource(String sourceName)
		{
			Source source = sources[sourceName];
			if (source != null)
			{
				return source;
			}
			throw new RrdException("Unknown source: " + sourceName);
		}

		/////////////////////////////////////////////////////////////////
		// DATASOURCE DEFINITIONS
		/////////////////////////////////////////////////////////////////

		/// <summary>
		/// <p>Adds a custom, {@link org.jrobin.data.Plottable plottable} datasource (<b>PDEF</b>).
		/// The datapoints should be made available by a class extending
		/// {@link org.jrobin.data.Plottable Plottable} class.</p>
		/// </summary>
		/// <param name="name">source name.</param>
		/// <param name="plottable">class that extends Plottable class and is suited for graphing.</param>
		public void AddDatasource(String name, Plottable plottable)
		{
			var pDef = new PDef(name, plottable);
			sources.Add(name, pDef);
		}

		/// <summary>
		/// <p>Adds complex source (<b>CDEF</b>).
		/// Complex sources are evaluated using the supplied <code>RPN</code> expression.</p>
		/// <p/>
		/// <p>Complex source <code>name</code> can be used:</p>
		/// <ul>
		/// <li>To specify sources for line, area and stack plots.</li>
		/// <li>To define other complex sources.</li>
		/// </ul>
		/// <p/>
		/// <p>JRobin supports the following RPN functions, operators and constants: +, -, *, /,
		/// %, SIN, COS, LOG, EXP, FLOOR, CEIL, ROUND, POW, ABS, SQRT, RANDOM, LT, LE, GT, GE, EQ,
		/// IF, MIN, MAX, LIMIT, DUP, EXC, POP, UN, UNKN, NOW, TIME, PI, E,
		/// AND, OR, XOR, PREV, PREV(sourceName), INF, NEGINF, STEP, YEAR, MONTH, DATE,
		/// HOUR, MINUTE, SECOND, WEEK, SIGN and RND.</p>
		/// <p/>
		/// <p>JRobin does not force you to specify at least one simple source name as RRDTool.</p>
		/// <p/>
		/// <p>For more details on RPN see RRDTool's
		/// <a href="http://people.ee.ethz.ch/~oetiker/webtools/rrdtool/manual/rrdgraph.html" target="man">
		/// rrdgraph man page</a>.</p>
		/// </summary>
		/// <param name="name">source name.</param>
		/// <param name="rpnExpression">
		/// RPN expression containig comma (or space) delimited simple and complex
		/// source names, RPN constants, functions and operators.
		/// </param>
		public void AddDatasource(String name, String rpnExpression)
		{
			var cDef = new CDef(name, rpnExpression);
			sources.Add(name, cDef);
		}

		/// <summary>
		/// <p>Adds static source (<b>SDEF</b>). Static sources are the result of a consolidation function applied
		/// to *any* other source that has been defined previously.</p>
		/// </summary>
		/// <param name="name">source name.</param>
		/// <param name="defName">Name of the datasource to calculate the value from.</param>
		/// <param name="consolFun">Consolidation function to use for value calculation</param>
		public void AddDatasource(String name, String defName, ConsolidationFunction consolFun)
		{
			var sDef = new SDef(name, defName, consolFun);
			sources.Add(name, sDef);
		}

		/// <summary>
		/// <p>Adds simple datasource (<b>DEF</b>). Simple source <code>name</code>
		/// can be used:</p>
		/// <ul>
		/// <li>To specify sources for line, area and stack plots.</li>
		/// <li>To define complex sources</li>
		/// </ul>
		/// </summary>
		/// <param name="name">source name.</param>
		/// <param name="file">Path to RRD file.</param>
		/// <param name="dsName">Datasource name defined in the RRD file.</param>
		/// <param name="consolFunc">
		/// Consolidation function that will be used to extract data from the RRD
		/// file ("AVERAGE", "MIN", "MAX" or "LAST" - these string constants are conveniently defined
		/// in the {@link org.jrobin.core.ConsolFuns ConsolFuns} class).
		/// </param>
		public void AddDatasource(String name, String file, String dsName, ConsolidationFunction consolFunc)
		{
			var def = new Def(name, file, dsName, consolFunc);
			sources.Add(name, def);
		}

		/// <summary>
		/// <p>Adds simple source (<b>DEF</b>). Source <code>name</code> can be used:</p>
		/// <ul>
		/// <li>To specify sources for line, area and stack plots.</li>
		/// <li>To define complex sources</li>
		/// </ul>
		/// </summary>
		/// <param name="name">Source name</param>
		/// <param name="file">Path to RRD file.</param>
		/// <param name="dsName">Data source name defined in the RRD file.</param>
		/// <param name="consolFunc">
		/// Consolidation function that will be used to extract data from the RRD
		/// file ("AVERAGE", "MIN", "MAX" or "LAST"
		/// </param>
		/// <param name="backend">Name of the RrdBackendFactory that should be used for this RrdDb.</param>
		public void AddDatasource(String name, String file, String dsName, ConsolidationFunction consolFunc, String backend)
		{
			var def = new Def(name, file, dsName, consolFunc, backend);
			sources.Add(name, def);
		}

		/// <summary>
		/// Adds DEF datasource with datasource values already available in the FetchData object. This method is
		/// used internally by JRobin and probably has no purpose outside of it.
		/// </summary>
		/// <param name="name">Source name.</param>
		/// <param name="fetchData">Fetched data containing values for the given source name.</param>
		internal void AddDatasource(String name, FetchData fetchData)
		{
			var def = new Def(name, fetchData);
			sources.Add(name, def);
		}

		/// <summary>
		/// Creates a new VDEF datasource that performs a percentile calculation on an
		/// another named datasource to yield a single value.  
		/// 
		/// Requires that the other datasource has already been defined; otherwise, it'll
		/// end up with no data
		/// 
		/// @param name - 
		/// @param sourceName - 
		///                     
		/// @param percentile - 
		/// 
		/// </summary>
		/// <param name="name">the new virtual datasource name</param>
		/// <param name="sourceName">
		/// the datasource from which to extract the percentile.  Must be a previously
		/// defined virtual datasource
		/// </param>
		/// <param name="percentile">the percentile to extract from the source datasource</param>
		public void AddDatasource(String name, String sourceName, double percentile)
		{
			Source source = sources[sourceName];
			sources.Add(name, new PercentileDef(name, source, percentile));
		}

		/////////////////////////////////////////////////////////////////
		// CALCULATIONS
		/////////////////////////////////////////////////////////////////

		/// <summary>
		/// Method that should be called once all datasources are defined. Data will be fetched from
		/// RRD files, RPN expressions will be calculated, etc.
		/// </summary>
		public void ProcessData()
		{
			ExtractDefs();
			FetchRrdData();
			FixZeroEndingTimestamp();
			ChooseOptimalStep();
			CreateTimestamps();
			AssignTimestampsToSources();
			NormalizeRrdValues();
			CalculateNonRrdSources();
		}

		/// <summary>
		/// Method used to calculate datasource values which should be presented on the graph
		/// based on the desired graph width. Each value returned represents a single pixel on the graph.
		/// Corresponding timestamp can be found in the array returned from {@link #getTimestampsPerPixel()}
		/// method.
		/// </summary>
		/// <param name="sourceName">Datasource name</param>
		/// <param name="pixelCount"> Graph width</param>
		/// <returns>Per-pixel datasource values</returns>
		public double[] GetValuesPerPixel(String sourceName, int pixelCount)
		{
			PixelCount = pixelCount;
			return GetValuesPerPixel(sourceName);
		}

		/// <summary>
		/// Method used to calculate datasource values which should be presented on the graph
		/// based on the graph width set with a {@link #setPixelCount(int)} method call.
		/// Each value returned represents a single pixel on the graph. Corresponding timestamp can be
		/// found in the array returned from {@link #getTimestampsPerPixel()} method.
		/// </summary>
		/// <param name="sourceName">Datasource name</param>
		/// <returns>Per-pixel datasource values</returns>
		public double[] GetValuesPerPixel(String sourceName)
		{
			double[] values = GetValues(sourceName);
			var pixelValues = new double[PixelCount];
			for (int i = 0; i < pixelValues.Length; i++)
			{
				pixelValues[i] = Double.NaN;
			}

			long span = tEnd - tStart;
			// this is the ugliest nested loop I have ever made
			for (int pix = 0, reff = 0; pix < PixelCount; pix++)
			{
				double t = tStart + (span*pix)/(double) (PixelCount - 1);
				while (reff < timestamps.Length)
				{
					if (t <= timestamps[reff] - Step)
					{
						// too left, nothing to do, already NaN
						break;
					}
					if (t <= timestamps[reff])
					{
						// in brackets, get this value
						pixelValues[pix] = values[reff];
						break;
					}
					// too right
					reff++;
				}
			}
			return pixelValues;
		}

		/// <summary>
		/// Calculates timestamps which correspond to individual pixels on the graph.
		/// </summary>
		/// <param name="pixelCount"></param>
		/// <returns></returns>
		public long[] GetTimestampsPerPixel(int pixelCount)
		{
			PixelCount = pixelCount;
			return GetTimestampsPerPixel();
		}

		/// <summary>
		/// Calculates timestamps which correspond to individual pixels on the graph
		/// based on the graph width set with a {@link #setPixelCount(int)} method call.
		/// </summary>
		/// <returns></returns>
		public long[] GetTimestampsPerPixel()
		{
			var times = new long[PixelCount];
			long span = tEnd - tStart;
			for (int i = 0; i < PixelCount; i++)
			{
				times[i] = (long) Math.Round(tStart + (span*i)/(double) (PixelCount - 1));
			}
			return times;
		}

		/// <summary>
		/// Dumps timestamps and values of all datasources in a tabelar form. Very useful for debugging.
		/// </summary>
		/// <returns>Dumped object content.</returns>
		public String Dump()
		{
			String[] names = GetSourceNames();
			double[][] values = GetValues();
			var buffer = new StringBuilder();
			buffer.Append(Format("timestamp", 12));
			foreach (String name in names)
			{
				buffer.Append(Format(name, 20));
			}
			buffer.Append("\n");
			for (int i = 0; i < timestamps.Length; i++)
			{
				buffer.Append(Format("" + timestamps[i], 12));
				for (int j = 0; j < names.Length; j++)
				{
					buffer.Append(Format(Util.FormatDouble(values[j][i]), 20));
				}
				buffer.Append("\n");
			}
			return buffer.ToString();
		}

		/// <summary>
		/// Returns time when last RRD archive was updated (all RRD files are considered).
		/// </summary>
		/// <returns></returns>
		public long GetLastRrdArchiveUpdateTime()
		{
			return lastRrdArchiveUpdateTime;
		}

		// PRIVATE METHODS
		private void ExtractDefs()
		{
			defSources = sources.Values.OfType<Def>().ToArray();
		}

		private void FetchRrdData()
		{
			long tEndFixed = (tEnd == 0) ? Util.GetCurrentTime() : tEnd;
			for (int i = 0; i < defSources.Length; i++)
			{
				if (!defSources[i].Loaded)
				{
					// not fetched yet
					var dsNames = new HashSet<String> {defSources[i].DataSourceName};
					// look for all other datasources with the same path and the same consolidation function
					for (int j = i + 1; j < defSources.Length; j++)
					{
						if (defSources[i].IsCompatibleWith(defSources[j]))
						{
							dsNames.Add(defSources[j].DataSourceName);
						}
					}
					// now we have everything
					RrdDb rrd = null;
					try
					{
						rrd = GetRrd(defSources[i]);
						lastRrdArchiveUpdateTime = Math.Max(lastRrdArchiveUpdateTime, rrd.GetLastArchiveUpdateTime());
						FetchRequest req = rrd.CreateFetchRequest(defSources[i].ConsolidationFunction,
						                                          tStart, tEndFixed, FetchRequestResolution);
						req.SetFilter(dsNames);
						FetchData data = req.FetchData();
						defSources[i].SetFetchData(data);
						for (int j = i + 1; j < defSources.Length; j++)
						{
							if (defSources[i].IsCompatibleWith(defSources[j]))
							{
								defSources[j].SetFetchData(data);
							}
						}
					}
					finally
					{
						if (rrd != null)
						{
							ReleaseRrd(rrd, defSources[i]);
						}
					}
				}
			}
		}

		private void FixZeroEndingTimestamp()
		{
			if (tEnd == 0)
			{
				if (defSources.Length == 0)
				{
					throw new RrdException("Could not adjust zero ending timestamp, no DEF source provided");
				}
				tEnd = defSources[0].ArchiveEndTime;
				for (int i = 1; i < defSources.Length; i++)
				{
					tEnd = Math.Min(tEnd, defSources[i].ArchiveEndTime);
				}
				if (tEnd <= tStart)
				{
					throw new RrdException("Could not resolve zero ending timestamp.");
				}
			}
		}

		// Tricky and ugly. Should be redesigned some time in the future
		private void ChooseOptimalStep()
		{
			long newStep = long.MaxValue;
			foreach (Def defSource in defSources)
			{
				long fetchStep = defSource.FetchStep, tryStep = fetchStep;
				if (Step > 0)
				{
					tryStep = Math.Min(newStep, (((Step - 1)/fetchStep) + 1)*fetchStep);
				}
				newStep = Math.Min(newStep, tryStep);
			}
			Step = newStep != long.MaxValue ? newStep : Math.Max((tEnd - tStart)/PixelCount, 1);
		}

		private void CreateTimestamps()
		{
			long t1 = Util.Normalize(tStart, Step);
			long t2 = Util.Normalize(tEnd, Step);
			if (t2 < tEnd)
			{
				t2 += Step;
			}
			var count = (int) (((t2 - t1)/Step) + 1);
			timestamps = new long[count];
			for (int i = 0; i < count; i++)
			{
				timestamps[i] = t1;
				t1 += Step;
			}
		}

		private void AssignTimestampsToSources()
		{
			foreach (Source src in sources.Values)
			{
				src.Timestamps = timestamps;
			}
		}

		private void NormalizeRrdValues()
		{
			var normalizer = new Normalizer(timestamps);
			foreach (Def def in defSources)
			{
				long[] rrdTimestamps = def.RrdTimestamps;
				double[] rrdValues = def.RrdValues;
				double[] values = normalizer.normalize(rrdTimestamps, rrdValues);
				def.Values = values;
			}
		}

		private void CalculateNonRrdSources()
		{
			foreach (Source source in sources.Values)
			{
				if (source is SDef)
				{
					CalculateSDef((SDef) source);
				}
				else if (source is CDef)
				{
					CalculateCDef((CDef) source);
				}
				else if (source is PDef)
				{
					CalculatePDef((PDef) source);
				}
				else if (source is PercentileDef)
				{
					CalculatePercentileDef((PercentileDef) source);
				}
			}
		}

		private static void CalculatePDef(PDef pdef)
		{
			pdef.CalculateValues();
		}

		private void CalculateCDef(CDef cDef)
		{
			var calc = new RpnCalculator(cDef.GetRpnExpression(), cDef.Name, this);
			cDef.Values = calc.CalculateValues();
		}

		private void CalculateSDef(SDef sDef)
		{
			String defName = sDef.DefName;
			ConsolidationFunction consolFun = sDef.ConsolidationFunction;
			Source source = GetSource(defName);
			double value = source.GetAggregates(tStart, tEnd).GetAggregate(consolFun);
			sDef.SetValue(value);
		}

		//Yeah, this is different from the other calculation methods
		// Frankly, this is how it *should* be done, and the other methods will
		// be refactored to this design (and the instanceof's removed) at some point
		private void CalculatePercentileDef(PercentileDef def)
		{
			def.Calculate(tStart, tEnd);
		}


		private RrdDb GetRrd(Def def)
		{
			String path = def.Path, backend = def.Backend;
			if (IsPoolUsed && backend == null)
			{
				return RrdDbPool.GetInstance().RequestRrdDb(path);
			}
			if (backend != null)
			{
				return RrdDb.Open(path, true, RrdBackendFactory.GetFactory(backend));
			}
			return RrdDb.Open(path, true);
		}

		private void ReleaseRrd(RrdDb rrd, Def def)
		{
			String backend = def.Backend;
			if (IsPoolUsed && backend == null)
			{
				RrdDbPool.GetInstance().Release(rrd);
			}
			else
			{
				rrd.Close();
			}
		}

		private static String Format(String s, int length)
		{
			var b = new StringBuilder(s);
			for (int i = 0; i < length - s.Length; i++)
			{
				b.Append(' ');
			}
			return b.ToString();
		}

		/**
	 * Cute little demo. Uses demo.rrd file previously created by basic JRobin demo.
	 *
	 * @param args Not used
	 * @throws IOException
	 * @throws RrdException
	 */

		//public static void main(String[] args)
		//{
		//   // time span
		//   long t1 = Util.GetTimestamp(2003, 4, 1);
		//   long t2 = Util.GetTimestamp(2003, 5, 1);
		//   Console.WriteLine("t1 = " + t1);
		//   Console.WriteLine("t2 = " + t2);

		//   // RRD file to use
		//   String rrdPath = Util.GetJRobinDemoPath("demo.rrd");

		//   // constructor
		//   var dp = new DataProcessor(t1, t2);

		//   // uncomment and run again
		//   //dp.setFetchRequestResolution(86400);

		//   // uncomment and run again
		//   //dp.setStep(86500);

		//   // datasource definitions
		//   dp.AddDatasource("X", rrdPath, "sun", ConsolidationFunction.AVERAGE);
		//   dp.AddDatasource("Y", rrdPath, "shade", ConsolidationFunction.AVERAGE);
		//   dp.AddDatasource("Z", "X,Y,+,2,/");
		//   dp.AddDatasource("DERIVE[Z]", "Z,PREV(Z),-,STEP,/");
		//   dp.AddDatasource("TREND[Z]", "DERIVE[Z],SIGN");
		//   dp.AddDatasource("AVG[Z]", "Z", ConsolidationFunction.AVERAGE);
		//   dp.AddDatasource("DELTA", "Z,AVG[Z],-");

		//   // action
		//   DateTime laptime = DateTime.Now;
		//   //dp.setStep(86400);
		//   dp.ProcessData();
		//   Console.WriteLine("Data processed in " + (DateTime.Now - laptime) + " milliseconds\n---");
		//   Console.WriteLine(dp.Dump());

		//   // aggregates
		//   Console.WriteLine("\nAggregates for X");
		//   Aggregates agg = dp.GetAggregates("X");
		//   Console.WriteLine(agg.ToString());
		//   Console.WriteLine("\nAggregates for Y");
		//   agg = dp.GetAggregates("Y");
		//   Console.WriteLine(agg.ToString());

		//   // 95-percentile
		//   Console.WriteLine("\n95-percentile for X: " + Util.FormatDouble(dp.Get95Percentile("X")));
		//   Console.WriteLine("95-percentile for Y: " + Util.FormatDouble(dp.Get95Percentile("Y")));

		//   // lastArchiveUpdateTime
		//   Console.WriteLine("\nLast archive update time was: " + dp.GetLastRrdArchiveUpdateTime());
		//}
	}
}