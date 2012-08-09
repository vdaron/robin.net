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
using System.Text;
using robin.data;

namespace robin.core
{
	/// <summary>
	/// Class used to represent data fetched from the RRD.
	/// Object of this class is created when the method
	/// {@link FetchRequest#fetchData() fetchData()} is
	/// called on a {@link FetchRequest FetchRequest} object.<p>
	/// <p/>
	/// Data returned from the RRD is, simply, just one big table filled with
	/// timestamps and corresponding datasource values.
	/// Use {@link #getRowCount() getRowCount()} method to count the number
	/// of returned timestamps (table rows).<p>
	/// <p/>
	/// The first table column is filled with timestamps. Time intervals
	/// between consecutive timestamps are guaranteed to be equal. Use
	/// {@link #getTimestamps() getTimestamps()} method to get an array of
	/// timestamps returned.<p>
	/// <p/>
	/// Remaining columns are filled with datasource values for the whole timestamp range,
	/// on a column-per-datasource basis. Use {@link #getColumnCount() getColumnCount()} to find
	/// the number of datasources and {@link #getValues(int) getValues(i)} method to obtain
	/// all values for the i-th datasource. Returned datasource values correspond to
	/// the values returned with {@link #getTimestamps() getTimestamps()} method.<p>
	/// </summary>
	public class FetchData
	{
		// anything fuuny will do
		private const String RPN_SOURCE_NAME = "WHERE THE SPEECHLES UNITE IN A SILENT ACCORD";
		private readonly long arcEndTime;
		private readonly long arcStep;

		private readonly String[] dsNames;
		private readonly Archive matchingArchive;
		private readonly FetchRequest request;

		public FetchData(Archive matchingArchive, FetchRequest request)
		{
			this.matchingArchive = matchingArchive;
			arcStep = matchingArchive.TimeStep;
			arcEndTime = matchingArchive.GetEndTime();
			dsNames = request.GetFilter();
			if (dsNames == null)
			{
				dsNames = matchingArchive.ParentDb.DataSourceNames;
			}
			this.request = request;
		}

		/// <summary>
		/// Array of timestamps covering the whole range specified in the
		/// </summary>
		public long[] Timestamps { get; internal set; }

		/// <summary>
		/// Archived values for all datasources.
		/// values correspond to timestamps returned with <see cref="Timestamps"/>
		/// </summary>
		/// <value>Two-dimensional aray of all datasource values.</value>
		public double[][] Values { get; internal set; }

		/// <summary>
		/// Returns the number of rows fetched from the corresponding RRD.
		/// Each row represents datasource values for the specific timestamp.
		/// </summary>
		public int RowCount
		{
			get { return Timestamps.Length; }
		}

		/// <summary>
		/// Returns the number of columns fetched from the corresponding RRD.
		/// This number is always equal to the number of datasources defined
		/// in the RRD. Each column represents values of a single datasource.
		/// </summary>
		/// <value></value>
		public int ColumnCount
		{
			get { return dsNames.Length; }
		}

		/// <summary>
		/// The step with which this data was fetched.
		/// </summary>
		public long Step
		{
			get { return Timestamps[1] - Timestamps[0]; }
		}

		/// <summary>
		/// Returns all archived values for a single datasource.
		/// Returned values correspond to timestamps
		/// returned with {@link #getTimestamps() getTimestamps()} method.
		/// </summary>
		/// <param name="dsIndex">dsIndex Datasource index.</param>
		/// <returns>Array of single datasource values.</returns>
		public double[] GetValues(int dsIndex)
		{
			return Values[dsIndex];
		}

		/// <summary>
		/// Returns all archived values for a single datasource.
		/// Returned values correspond to timestamps
		/// returned with {@link #getTimestamps() getTimestamps()} method.
		/// </summary>
		/// <param name="dsName">dsName Datasource name.</param>
		/// <returns>Array of single datasource values.</returns>
		public double[] GetValues(String dsName)
		{
			for (int dsIndex = 0; dsIndex < ColumnCount; dsIndex++)
			{
				if (dsName == dsNames[dsIndex])
				{
					return GetValues(dsIndex);
				}
			}
			throw new RrdException("Datasource [" + dsName + "] not found");
		}

		/// <summary>
		/// Returns a set of values created by applying RPN expression to the fetched data.
		/// For example, if you have two datasources named <code>x</code> and <code>y</code>
		/// in this FetchData and you want to calculate values for <code>(x+y)/2</code>code> use something like: <p>
		/// <code>getRpnValues("x,y,+,2,/");</code>
		/// </summary>
		/// <param name="rpnExpression">RRDTool-like RPN expression</param>
		/// <returns>Calculated values</returns>
		public double[] GetRpnValues(String rpnExpression)
		{
			DataProcessor dataProcessor = CreateDataProcessor(rpnExpression);
			return dataProcessor.GetValues(RPN_SOURCE_NAME);
		}

		/// <summary>
		/// object used to create this FetchData object.
		/// </summary>
		/// <value></value>
		public FetchRequest Request
		{
			get { return request; }
		}

		/// <summary>
		/// the first timestamp in this FetchData object.
		/// </summary>
		public long FirstTimestamp
		{
			get { return Timestamps[0]; }
		}

		/// <summary>
		/// the last timestamp in this FecthData object.
		/// </summary>
		/// <value></value>
		public long LastTimestamp
		{
			get { return Timestamps[Timestamps.Length - 1]; }
		}

		/// <summary>
		/// Returns Archive object which is determined to be the best match for the
		/// timestamps specified in the fetch request. All datasource values are obtained
		/// from round robin archives belonging to this archive.
		/// </summary>
		/// <value></value>
		public Archive MatchingArchive
		{
			get { return matchingArchive; }
		}

		/// <summary>
		/// Returns array of datasource names found in the corresponding RRD. If the request
		/// was filtered (data was fetched only for selected datasources), only datasources selected
		/// for fetching are returned.
		/// </summary>
		/// <value></value>
		public string[] DataSourceNames
		{
			get { return dsNames; }
		}

		/// <summary>
		/// Retrieve the table index number of a datasource by name.  Names are case sensitive.
		/// </summary>
		/// <param name="dsName"></param>
		/// <returns></returns>
		public int GetDsIndex(String dsName)
		{
			// Let's assume the table of dsNames is always small, so it is not necessary to use a hashmap for lookups
			for (int i = 0; i < dsNames.Length; i++)
			{
				if (dsNames[i] == dsName)
				{
					return i;
				}
			}
			return -1; // Datasource not found !
		}

		/// <summary>
		/// Dumps the content of the whole FetchData object. Useful for debugging.
		/// </summary>
		/// <returns></returns>
		public String Dump()
		{
			var buffer = new StringBuilder("");
			for (int row = 0; row < RowCount; row++)
			{
				buffer.Append(Timestamps[row]);
				buffer.Append(":  ");
				for (int dsIndex = 0; dsIndex < ColumnCount; dsIndex++)
				{
					buffer.Append(Util.FormatDouble(Values[dsIndex][row], true));
					buffer.Append("  ");
				}
				buffer.Append("\n");
			}
			return buffer.ToString();
		}

		/// <summary>
		/// string representing fetched data in a RRDTool-like form.
		/// </summary>
		/// <returns></returns>
		public override String ToString()
		{
			// print header row
			var buff = new StringBuilder();
			buff.Append(PadWithBlanks("", 10));
			buff.Append(" ");
			foreach (String dsName in dsNames)
			{
				buff.Append(PadWithBlanks(dsName, 18));
			}
			buff.Append("\n \n");
			for (int i = 0; i < Timestamps.Length; i++)
			{
				buff.Append(PadWithBlanks("" + Timestamps[i], 10));
				buff.Append(":");
				for (int j = 0; j < dsNames.Length; j++)
				{
					double value = Values[j][i];
					String valueStr = Double.IsNaN(value) ? "nan" : Util.FormatDouble(value);
					buff.Append(PadWithBlanks(valueStr, 18));
				}
				buff.Append("\n");
			}
			return buff.ToString();
		}

		private static String PadWithBlanks(String input, int width)
		{
			var buff = new StringBuilder("");
			int diff = width - input.Length;
			while (diff-- > 0)
			{
				buff.Append(' ');
			}
			buff.Append(input);
			return buff.ToString();
		}

		/// <summary>
		/// Returns single aggregated value from the fetched data for a single datasource.
		/// </summary>
		/// <param name="dsName">Datasource name</param>
		/// <param name="consolFun">Consolidation function to be applied to fetched datasource values.</param>
		/// <returns>
		/// MIN, MAX, LAST, FIRST, AVERAGE or TOTAL value calculated from the fetched data
		/// for the given datasource name
		/// </returns>
		public double GetAggregate(String dsName, ConsolidationFunction consolFun)
		{
			DataProcessor dp = CreateDataProcessor(null);
			return dp.GetAggregate(dsName, consolFun);
		}

		/// <summary>
		/// Returns aggregated value from the fetched data for a single datasource.
		/// Before applying aggregation functions, specified RPN expression is applied to fetched data.
		/// For example, if you have a gauge datasource named 'foots' but you want to find the maximum
		/// fetched value in meters use something like: 
		/// <code>getAggregate("foots", "MAX", "foots,0.3048,*");</code>
		/// </summary>
		/// <param name="dsName">Datasource name</param>
		/// <param name="consolFun">Consolidation function (MIN, MAX, LAST, FIRST, AVERAGE or TOTAL)</param>
		/// <param name="rpnExpression">RRDTool-like RPN expression</param>
		/// <returns>Aggregated value</returns>
		[Obsolete]
		public double GetAggregate(String dsName, ConsolidationFunction consolFun, String rpnExpression)
		{
			// for backward compatibility
			rpnExpression = rpnExpression.Replace("value", dsName);
			return GetRpnAggregate(rpnExpression, consolFun);
		}

		/// <summary>
		/// Returns aggregated value for a set of values calculated by applying an RPN expression to the
		/// fetched data. For example, if you have two datasources named <code>x</code> and <code>y</code>
		/// in this FetchData and you want to calculate MAX value of <code>(x+y)/2</code>code> use something like:
		/// <code>getRpnAggregate("x,y,+,2,/", "MAX");</code>
		/// </summary>
		/// <param name="rpnExpression">RRDTool-like RPN expression</param>
		/// <param name="consolFun">Consolidation function (MIN, MAX, LAST, FIRST, AVERAGE or TOTAL)</param>
		/// <returns></returns>
		public double GetRpnAggregate(String rpnExpression, ConsolidationFunction consolFun)
		{
			DataProcessor dataProcessor = CreateDataProcessor(rpnExpression);
			return dataProcessor.GetAggregate(RPN_SOURCE_NAME, consolFun);
		}

		/// <summary>
		/// Returns all aggregated values (MIN, MAX, LAST, FIRST, AVERAGE or TOTAL) calculated from the fetched data
		/// for a single datasource.
		/// </summary>
		/// <param name="dsName">Datasource name.</param>
		/// <returns>Simple object containing all aggregated values.</returns>
		public Aggregates GetAggregates(String dsName)
		{
			DataProcessor dataProcessor = CreateDataProcessor(null);
			return dataProcessor.GetAggregates(dsName);
		}

		/// <summary>
		/// Returns all aggregated values for a set of values calculated by applying an RPN expression to the
		/// fetched data. For example, if you have two datasources named <code>x</code> and <code>y</code>
		/// in this FetchData and you want to calculate MIN, MAX, LAST, FIRST, AVERAGE and TOTAL value
		/// of <code>(x+y)/2</code>code> use something like:
		/// <code>getRpnAggregates("x,y,+,2,/");</code>
		/// </summary>
		/// <param name="rpnExpression">RRDTool-like RPN expression</param>
		/// <returns>Object containing all aggregated values</returns>
		public Aggregates GetRpnAggregates(String rpnExpression)
		{
			DataProcessor dataProcessor = CreateDataProcessor(rpnExpression);
			return dataProcessor.GetAggregates(RPN_SOURCE_NAME);
		}

		/// <summary>
		/// Used by ISPs which charge for bandwidth utilization on a "95th percentile" basis.
		/// 
		/// The 95th percentile is the highest source value left when the top 5% of a numerically sorted set
		/// of source data is discarded. It is used as a measure of the peak value used when one discounts
		/// a fair amount for transitory spikes. This makes it markedly different from the average.
		/// 
		/// Read more about this topic at:
		/// <a href="http://www.red.net/support/resourcecentre/leasedline/percentile.php">Rednet</a> or
		/// <a href="http://www.bytemark.co.uk/support/tech/95thpercentile.html">Bytemark</a>.
		/// </summary>
		/// <param name="dsName">Datasource name</param>
		/// <returns>95th percentile of fetched source values</returns>
		public double Get95Percentile(String dsName)
		{
			DataProcessor dataProcessor = CreateDataProcessor(null);
			return dataProcessor.Get95Percentile(dsName);
		}

		/// <summary>
		/// Same as {@link #Get95Percentile(String)}, but for a set of values calculated with the given
		/// RPN expression.
		/// </summary>
		/// <param name="rpnExpression">RRDTool-like RPN expression</param>
		/// <returns>95-percentile</returns>
		public double getRpn95Percentile(String rpnExpression)
		{
			DataProcessor dataProcessor = CreateDataProcessor(rpnExpression);
			return dataProcessor.Get95Percentile(RPN_SOURCE_NAME);
		}

		/**
	 * Dumps fetch data to output stream in XML format.
	 *
	 * @param outputStream Output stream to dump fetch data to
	 * @Thrown in case of I/O error
	 */

		//public void exportXml(Stream outputStream)
		//{
		//   XmlWriter writer = new XmlWriter(outputStream);
		//   writer.startTag("fetch_data");
		//   writer.startTag("request");
		//   writer.writeTag("file", request.getParentDb().getPath());
		//   writer.writeComment(Util.getDate(request.getFetchStart()));
		//   writer.writeTag("start", request.getFetchStart());
		//   writer.writeComment(Util.getDate(request.getFetchEnd()));
		//   writer.writeTag("end", request.getFetchEnd());
		//   writer.writeTag("resolution", request.getResolution());
		//   writer.writeTag("cf", request.getConsolFun());
		//   writer.closeTag(); // request
		//   writer.startTag("datasources");
		//   for (String dsName : dsNames) {
		//      writer.writeTag("name", dsName);
		//   }
		//   writer.closeTag(); // datasources
		//   writer.startTag("data");
		//   for (int i = 0; i < timestamps.Length; i++) {
		//      writer.startTag("row");
		//      writer.writeComment(Util.getDate(timestamps[i]));
		//      writer.writeTag("timestamp", timestamps[i]);
		//      writer.startTag("values");
		//      for (int j = 0; j < dsNames.Length; j++) {
		//         writer.writeTag("v", values[j][i]);
		//      }
		//      writer.closeTag(); // values
		//      writer.closeTag(); // row
		//   }
		//   writer.closeTag(); // data
		//   writer.closeTag(); // fetch_data
		//   writer.flush();
		//}

		/**
	 * Dumps fetch data to file in XML format.
	 *
	 * @param filepath Path to destination file
	 * @Thrown in case of I/O error
	 */

		//public void exportXml(String filepath)
		//{
		//   OutputStream outputStream = null;
		//   try {
		//      outputStream = new FileOutputStream(filepath);
		//      exportXml(outputStream);
		//   }
		//   ly {
		//      if (outputStream != null) {
		//         outputStream.close();
		//      }
		//   }
		//}

		/**
	 * Dumps fetch data in XML format.
	 *
	 * @return String containing XML formatted fetch data
	 * @Thrown in case of I/O error
	 */

		//public String exportXml()
		//{
		//   ByteArrayOutputStream outputStream = new ByteArrayOutputStream();
		//   exportXml(outputStream);
		//   return outputStream.toString();
		//}

		/// <summary>
		/// step of the corresponding RRA archive
		/// </summary>
		/// <value></value>
		public long ArchiveStep
		{
			get { return arcStep; }
		}

		/// <summary>
		/// timestamp of the last populated slot in the corresponding RRA archive
		/// </summary>
		/// <value></value>
		public long ArchiveEndTime
		{
			get { return arcEndTime; }
		}

		private DataProcessor CreateDataProcessor(String rpnExpression)
		{
			var dataProcessor = new DataProcessor(request.FetchStart, request.FetchEnd);
			foreach (String dsName in dsNames)
			{
				dataProcessor.AddDatasource(dsName, this);
			}
			if (rpnExpression != null)
			{
				dataProcessor.AddDatasource(RPN_SOURCE_NAME, rpnExpression);
				try
				{
					dataProcessor.ProcessData();
				}
				catch (IOException ioe)
				{
					// highly unlikely, since all datasources have already calculated values
					throw new InvalidOperationException("Impossible error: " + ioe);
				}
			}
			return dataProcessor;
		}
	}
}