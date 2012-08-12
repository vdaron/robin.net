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
using System.Xml;
using System.IO;

namespace robin.core
{
	public class XmlImporter : DataImporter
	{
		private readonly XmlNodeList arcNodes;
		private readonly XmlNodeList dsNodes;
		private readonly XmlDocument document;
		private readonly XmlNode root;

		public XmlImporter(String xmlFilePath)
		{
			document = new XmlDocument();
			document.LoadXml(xmlFilePath);
			root = document.DocumentElement;
			if (root == null)
			{
				throw new ArgumentException("Invalid XML");
			}
			dsNodes = root.SelectNodes("ds");
			arcNodes = root.SelectNodes("rra");
		}

		public XmlImporter(FileInfo xmlFilePath)
			:this(File.ReadAllText(xmlFilePath.FullName))
		{
		}

		public override string Version
		{
			get { return Util.Xml.GetChildValue(root, "version"); }
		}

		public override long LastUpdateTime
		{
			get { return Util.Xml.GetChildValueAsLong(root, "lastupdate"); }
		}

		public override long Step
		{
			get { return Util.Xml.GetChildValueAsLong(root, "step"); }
		}

		public override int DataSourceCount
		{
			get { return dsNodes.Count; }
		}

		public override int ArchiveCount
		{
			get { return arcNodes.Count; }
		}

		public override string GetDataSourceName(int dsIndex)
		{
			return Util.Xml.GetChildValue(dsNodes[dsIndex], "name");
		}

		public override string GetDataSourceType(int dsIndex)
		{
			return Util.Xml.GetChildValue(dsNodes[dsIndex], "type");
		}

		public override long GetDataSourceHeartbeat(int dsIndex)
		{
			return Util.Xml.GetChildValueAsLong(dsNodes[dsIndex], "minimal_heartbeat");
		}

		public override double GetDataSourceMinValue(int dsIndex)
		{
			return Util.Xml.GetChildValueAsDouble(dsNodes[dsIndex], "min");
		}

		public override double GetDataSourceMaxValue(int dsIndex)
		{
			return Util.Xml.GetChildValueAsDouble(dsNodes[dsIndex], "max");
		}

		public override double GetDataSourceLastValue(int dsIndex)
		{
			return Util.Xml.GetChildValueAsDouble(dsNodes[dsIndex], "last_ds");
		}

		public override double GetDataSourceAccumulatedValue(int dsIndex)
		{
			return Util.Xml.GetChildValueAsDouble(dsNodes[dsIndex], "value");
		}

		public override long GetDataSourceNanSeconds(int dsIndex)
		{
			return Util.Xml.GetChildValueAsLong(dsNodes[dsIndex], "unknown_sec");
		}

		public override ConsolidationFunction GetArchiveConsolisationFunction(int arcIndex)
		{
			return Util.Xml.GetChildValueAsEnum<ConsolidationFunction>(arcNodes[arcIndex], "cf");
		}

		public override double GetArchiveXff(int arcIndex)
		{
			return Util.Xml.GetChildValueAsDouble(arcNodes[arcIndex], "xff");
		}

		public override int GetArchiveSteps(int arcIndex)
		{
			return Util.Xml.GetChildValueAsInt(arcNodes[arcIndex], "pdp_per_row");
		}

		public override int GetArchiveRows(int arcIndex)
		{
			XmlNode dbNode = Util.Xml.GetFirstChildNode(arcNodes[arcIndex], "database");
			XmlNodeList rows = dbNode.SelectNodes("row");
			return rows != null ? rows.Count : 0;
		}

		public override double GetArchiveStateAccumulatedValue(int arcIndex, int dsIndex)
		{
			XmlNode cdpNode = Util.Xml.GetFirstChildNode(arcNodes[arcIndex], "cdp_prep");
			XmlNodeList nodes = cdpNode.SelectNodes("ds");
			return nodes != null ? Util.Xml.GetChildValueAsDouble(nodes[dsIndex], "value") : 0;
		}

		public override int GetArchiveStateNanSteps(int arcIndex, int dsIndex)
		{
			XmlNode cdpNode = Util.Xml.GetFirstChildNode(arcNodes[arcIndex], "cdp_prep");
			XmlNodeList nodes = cdpNode.SelectNodes("ds");
			return nodes != null ? Util.Xml.GetChildValueAsInt(nodes[dsIndex], "unknown_datapoints") : 0;
		}

		public override double[] GetArchiveValues(int arcIndex, int dsIndex)
		{
			XmlNode dbNode = Util.Xml.GetFirstChildNode(arcNodes[arcIndex], "database");
			XmlNodeList rows = dbNode.SelectNodes("row");
			if (rows != null)
			{
				var values = new double[rows.Count];
				for (int i = 0; i < rows.Count; i++)
				{
					XmlNodeList vNodes = rows[i].SelectNodes("v");
					if (vNodes != null)
					{
						XmlNode vNode = vNodes[dsIndex];
						values[i] = Util.ParseDouble(vNode.FirstChild.Value.Trim());
					}
				}
				return values;
			}
			return new double[0];
		}
	}
}