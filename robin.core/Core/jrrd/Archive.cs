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
using System.Text;

namespace robin.core.jrrd
{
	/// <summary>
	/// Instances of this class model an archive section of an RRD file.
	/// <author> <a href="mailto:ciaran@codeloop.com">Ciaran Treanor</a></author>s
	/// </summary>
	internal class Archive
	{
		private readonly RRDatabase db;
		private readonly long offset;
		private readonly long size;
		private long dataOffset;
		private List<CDPStatusBlock> cdpStatusBlocks;
		private int currentRow;

		private double[][] values;

		internal Archive(RRDatabase db)
		{
			this.db = db;

			RRDFile file = db.rrdFile;

			offset = file.FilePointer;
			Type = (ConsolidationFunctionType)Enum.Parse(typeof(ConsolidationFunctionType), file.ReadString(Constants.CF_NAM_SIZE), true);
			RowCount = file.ReadInt();
			PdpCount = file.ReadInt();

			file.Align();

			Xff = file.ReadDouble();

			// Skip rest of rra_def_t.par[]
			file.Align();
			file.SkipBytes(72);

			size = file.FilePointer - offset;
		}

		/// <summary>
		/// Returns the type of function used to calculate the consolidated data point.
		/// </summary>
		/// <value>The type of function used to calculate the consolidated data point.</value>
		public ConsolidationFunctionType Type { get; private set; }

		internal void LoadCdpStatusBlocks(RRDFile file, int numBlocks)
		{
			cdpStatusBlocks = new List<CDPStatusBlock>();

			for (int i = 0; i < numBlocks; i++)
			{
				cdpStatusBlocks.Add(new CDPStatusBlock(file));
			}
		}

		/// <summary>
		/// Returns the <code>CDPStatusBlock</code> at the specified position in this archive.
		/// </summary>
		/// <param name="index">index index of <code>CDPStatusBlock</code> to return.</param>
		/// <returns>the <code>CDPStatusBlock</code> at the specified position in this archive.</returns>
		public CDPStatusBlock GetCDPStatusBlock(int index)
		{
			return cdpStatusBlocks[index];
		}

		/// <summary>
		/// Returns an iterator over the CDP status blocks in this archive in proper sequence.
		/// <see cref="CDPStatusBlock"/>
		/// </summary>
		/// <returns>an iterator over the CDP status blocks in this archive in proper sequence.</returns>
		public IEnumerator<CDPStatusBlock> GetCDPStatusBlocks()
		{
			return cdpStatusBlocks.GetEnumerator();
		}

		internal void LoadCurrentRow(RRDFile file)
		{
			currentRow = file.ReadInt();
		}

		internal void LoadData(RRDFile file, int dsCount)
		{
			dataOffset = file.FilePointer;

			// Skip over the data to position ourselves at the start of the next archive
			file.SkipBytes(8*RowCount*dsCount);
		}

		internal DataChunk LoadData(DataChunk chunk)
		{
			DateTime end = DateTime.Now;
			DateTime start = end.AddDays(-1);

			LoadData(chunk, start.GetTimestamp(), end.GetTimestamp());
			return chunk;
		}


		private void LoadData(DataChunk chunk, long startTime, long endTime) // startTime and endTime are unused...
		{
			long pointer;

			if (chunk.Start < 0)
			{
				pointer = currentRow + 1;
			}
			else
			{
				pointer = currentRow + chunk.Start + 1;
			}

			db.rrdFile.Seek(dataOffset + (pointer*8), SeekOrigin.Begin);
			//cat.debug("Archive Base: " + dataOffset + " Archive Pointer: " + pointer);
			//cat.debug("Start Offset: " + chunk.start + " End Offset: "
			//          + (rowCount - chunk.end));

			double[][] data = chunk.Data;

			/*
			 * This is also terrible - cleanup - CT
			 */
			int row = 0;
			for (int i = chunk.Start; i < RowCount - chunk.End; i++, row++)
			{
				if (i < 0)
				{
					// no valid data yet
					for (int ii = 0; ii < chunk.DsCount; ii++)
					{
						data[row][ii] = Double.NaN;
					}
				}
				else if (i >= RowCount)
				{
					// past valid data area
					for (int ii = 0; ii < chunk.DsCount; ii++)
					{
						data[row][ii] = Double.NaN;
					}
				}
				else
				{
					// inside the valid are but the pointer has to be wrapped
					if (pointer >= RowCount)
					{
						pointer -= RowCount;

						db.rrdFile.Seek(dataOffset + (pointer*8), SeekOrigin.Begin);
					}

					for (int ii = 0; ii < chunk.DsCount; ii++)
					{
						data[row][ii] = db.rrdFile.ReadDouble();
					}

					pointer++;
				}
			}
		}

		internal void PrintInfo(TextWriter s, string numberFormat, int index)
		{
			var sb = new StringBuilder("rra[");

			sb.Append(index);
			s.Write(sb);
			s.Write("].cf = \"");
			s.Write(Type);
			s.WriteLine("\"");
			s.Write(sb);
			s.Write("].rows = ");
			s.WriteLine(RowCount);
			s.Write(sb);
			s.Write("].pdp_per_row = ");
			s.WriteLine(PdpCount);
			s.Write(sb);
			s.Write("].xff = ");
			s.WriteLine(Xff);
			sb.Append("].cdp_prep[");

			int cdpIndex = 0;

			foreach (CDPStatusBlock cdp in cdpStatusBlocks)
			{
				s.Write(sb);
				s.Write(cdpIndex);
				s.Write("].value = ");

				double value = cdp.Value;

				s.WriteLine(Double.IsNaN(value)
				            	? "NaN"
				            	: value.ToString(numberFormat));
				s.Write(sb);
				s.Write(cdpIndex++);
				s.Write("].unknown_datapoints = ");
				s.WriteLine(cdp.UnknownDatapoints);
			}
		}

		internal void ToXml(TextWriter s)
		{
			try
			{
				s.WriteLine("\t<rra>");
				s.Write("\t\t<cf> ");
				s.Write(Type);
				s.WriteLine(" </cf>");
				s.Write("\t\t<pdp_per_row> ");
				s.Write(PdpCount);
				s.Write(" </pdp_per_row> <!-- ");
				s.Write(db.Header.PrimaryDataPointStep*PdpCount);
				s.WriteLine(" seconds -->");
				s.Write("\t\t<xff> ");
				s.Write(Xff);
				s.WriteLine(" </xff>");
				s.WriteLine();
				s.WriteLine("\t\t<cdp_prep>");

				foreach (CDPStatusBlock t in cdpStatusBlocks)
				{
					t.ToXml(s);
				}

				s.WriteLine("\t\t</cdp_prep>");
				s.WriteLine("\t\t<database>");

				long timer = -(RowCount - 1);
				int counter = 0;
				int row = currentRow;

				db.rrdFile.Seek(dataOffset + (row + 1)*16, SeekOrigin.Begin);

				long lastUpdate = db.LastUpdate.GetTimestamp();
				int pdpStep = db.Header.PrimaryDataPointStep;
				const string numberFormat = "E";
				const string dateFormat = "yyyy-MM-dd HH:mm:ss z";

				while (counter++ < RowCount)
				{
					row++;

					if (row == RowCount)
					{
						row = 0;

						db.rrdFile.Seek(dataOffset, SeekOrigin.Begin);
					}

					long now = (lastUpdate - lastUpdate%(PdpCount*pdpStep))
					           + (timer*PdpCount*pdpStep);

					timer++;

					s.Write("\t\t\t<!-- ");
					s.Write(DateTime.Now.ToString(dateFormat));
					s.Write(" / ");
					s.Write(now);
					s.Write(" --> ");

					for (int col = 0; col < db.Header.DataSourceCount; col++)
					{
						s.Write("<v> ");
						double value = db.rrdFile.ReadDouble();
						s.Write(value.ToString(numberFormat));
						s.Write(" </v>");
					}

					s.WriteLine("</row>");
				}

				s.WriteLine("\t\t</database>");
				s.WriteLine("\t</rra>");
			}
			catch (IOException e)
			{
				// Is the best thing to do here?
				throw new Exception(e.Message);
			}
		}

		public double[][] GetValues()
		{
			// OK PART
			if (values != null)
			{
				return values;
			}
			values = new double[db.Header.DataSourceCount][];
			for (int i = 0; i < values.Length; i++)
			{
				values[i] = new double[RowCount];
			}

			int row = currentRow;
			// HERE ARE THE DRAGONS!
			db.rrdFile.Seek(dataOffset + (row + 1)*db.Header.DataSourceCount*8, SeekOrigin.Begin);
			// OK, TOO!
			for (int counter = 0; counter < RowCount; counter++)
			{
				row++;
				if (row == RowCount)
				{
					row = 0;
					db.rrdFile.Seek(dataOffset, SeekOrigin.Begin);
				}
				for (int col = 0; col < db.Header.DataSourceCount; col++)
				{
					double value = db.rrdFile.ReadDouble();
					values[col][counter] = value;
				}
			}
			return values;
		}

		/// <summary>
		/// Returns the number of primary data points required for a consolidated
		/// data point in this archive.
		/// </summary>
		public int PdpCount { get; private set; }

		/// <summary>
		/// Returns the number of entries in this archive.
		/// </summary>
		public int RowCount { get; private set; }

		/// <summary>
		/// Returns the X-Files Factor for this archive.
		/// </summary>
		public double Xff { get; private set; }

		/// <summary>
		/// Returns a summary the contents of this archive.
		/// </summary>
		/// <returns></returns>
		public override String ToString()
		{
			var sb = new StringBuilder("[Archive: OFFSET=0x");

			sb.Append(offset.ToString("x"));
			sb.Append(", SIZE=0x");
			sb.Append(size.ToString("x"));
			sb.Append(", type=");
			sb.Append(Type);
			sb.Append(", rowCount=");
			sb.Append(RowCount);
			sb.Append(", pdpCount=");
			sb.Append(PdpCount);
			sb.Append(", xff=");
			sb.Append(Xff);
			sb.Append(", currentRow=");
			sb.Append(currentRow);
			sb.Append("]");

			foreach (CDPStatusBlock cdp in cdpStatusBlocks)
			{
				sb.Append("\n\t\t");
				sb.Append(cdp.ToString());
			}

			return sb.ToString();
		}
	}
}
