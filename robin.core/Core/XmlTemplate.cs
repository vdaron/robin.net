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
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace robin.core
{
	/// <summary>
	/// Class used as a base class for various XML template related classes. Class provides
	/// methods for XML source parsing and XML tree traversing. XML source may have unlimited
	/// number of placeholders (variables) in the format <code>${variable_name}</code>.
	/// Methods are provided to specify variable values at runtime.
	/// Note that this class has limited functionality: XML source gets parsed, and variable
	/// values are collected. You have to extend this class to do something more useful.<p>
	/// </summary>
	public abstract class XmlTemplate
	{
		private const String PATTERN_STRING = "\\$\\{(\\w+)\\}";
		private static readonly Regex PATTERN = new Regex(PATTERN_STRING, RegexOptions.Compiled);

		private readonly HashSet<XmlNode> validatedNodes = new HashSet<XmlNode>();
		private readonly Dictionary<String, Object> valueMap = new Dictionary<String, Object>();

		protected XmlDocument XmlDocument { get; private set; }

		protected XmlTemplate(String xmlString)
		{
			XmlDocument = new XmlDocument();
			XmlDocument.LoadXml(xmlString);
		}

		protected XmlTemplate(FileInfo xmlFile)
		{
			XmlDocument = new XmlDocument();
			XmlDocument.Load(xmlFile.FullName);
		}

		/// <summary>
		/// Removes all placeholder-value mappings.
		/// </summary>
		public void ClearValues()
		{
			valueMap.Clear();
		}

	/// <summary>
		/// Sets value for a single XML template variable. Variable name should be specified
		/// without leading '${' and ending '}' placeholder markers. For example, for a placeholder
		/// <code>${start}</code>, specify <code>start</code> for the <code>name</code> parameter.
	/// </summary>
		/// <param name="name">variable name</param>
		/// <param name="value">value to be set in the XML template</param>
		public void SetVariable<T>(String name, T value)
		{
			valueMap.Add(name, value);
		}

		/// <summary>
		/// Searches the XML template to see if there are variables in there that
		/// will need to be set.
		/// </summary>
		/// <returns>True if variables were detected, false if not.</returns>
		public bool HasVariables()
		{
			return PATTERN.IsMatch(XmlDocument.OuterXml);
		}

		/// <summary>
		/// Returns the list of variables that should be set in this template.
		/// </summary>
		/// <returns>List of variable names as an array of strings.</returns>
		public String[] GetVariables()
		{
			var list = new List<String>();
			MatchCollection matchCollection = PATTERN.Matches(XmlDocument.OuterXml);

			foreach (Match m in matchCollection)
			{
				string var = m.Groups[1].Value;
				if (!list.Contains(var))
					list.Add(var);
			}

			return list.ToArray();
		}

		protected static XmlNode[] GetChildNodes(XmlNode parentNode, String childName)
		{
			return Util.Xml.GetChildNodes(parentNode, childName);
		}

		protected static XmlNode[] GetChildNodes(XmlNode parentNode)
		{
			return Util.Xml.GetChildNodes(parentNode, null);
		}

		protected static XmlNode GetFirstChildNode(XmlNode parentNode, String childName)
		{
			return Util.Xml.GetFirstChildNode(parentNode, childName);
		}

		protected bool HasChildNode(XmlNode parentNode, String childName)
		{
			return Util.Xml.HasChildNode(parentNode, childName);
		}

		protected String GetChildValue(XmlNode parentNode, String childName)
		{
			return GetChildValue(parentNode, childName, true);
		}

		protected T GetChildValueAsEnum<T>(XmlNode parentNode, String childName)
		{
			if(!typeof(T).IsEnum)
				throw  new ArgumentException("Invalid type");
			return Util.Xml.GetChildValueAsEnum<T>(parentNode,childName);
		}

		protected String GetChildValue(XmlNode parentNode, String childName, bool trim)
		{
			String value = Util.Xml.GetChildValue(parentNode, childName, trim);
			return ResolveMappings(value);
		}

		protected String GetValue(XmlNode parentNode)
		{
			return GetValue(parentNode, true);
		}

		protected String GetValue(XmlNode parentNode, bool trim)
		{
			String value = Util.Xml.GetValue(parentNode, trim);
			return ResolveMappings(value);
		}

		private String ResolveMappings(String templateValue)
		{
			if (templateValue == null)
			{
				return null;
			}
			MatchCollection matcher = PATTERN.Matches(templateValue);
			var result = new StringBuilder();
			int lastMatchEnd = 0;
			foreach (Match match in matcher)
			{
				String var = match.Groups[1].Value;
				if (valueMap.ContainsKey(var))
				{
					// mapping found
					result.Append(templateValue.Substring(lastMatchEnd, match.Index));
					result.Append(valueMap[var].ToString());
					lastMatchEnd = match.Index + match.Length;
				}
				else
				{
					// no mapping found - this is illegal
					// throw runtime exception
					throw new ArgumentException("No mapping found for template variable ${" + var + "}");
				}
			}
			result.Append(templateValue.Substring(lastMatchEnd));
			return result.ToString();
		}

		protected int GetChildValueAsInt(XmlNode parentNode, String childName)
		{
			String valueStr = GetChildValue(parentNode, childName);
			return int.Parse(valueStr);
		}

		protected int GetValueAsInt(XmlNode parentNode)
		{
			String valueStr = GetValue(parentNode);
			return int.Parse(valueStr);
		}

		protected long GetChildValueAsLong(XmlNode parentNode, String childName)
		{
			String valueStr = GetChildValue(parentNode, childName);
			return long.Parse(valueStr);
		}

		protected long GetValueAsLong(XmlNode parentNode)
		{
			String valueStr = GetValue(parentNode);
			return long.Parse(valueStr);
		}

		protected double GetChildValueAsDouble(XmlNode parentNode, String childName)
		{
			String valueStr = GetChildValue(parentNode, childName);
			return Util.ParseDouble(valueStr);
		}

		protected double GetValueAsDouble(XmlNode parentNode)
		{
			String valueStr = GetValue(parentNode);
			return Util.ParseDouble(valueStr);
		}

		protected bool GetChildValueAsBoolean(XmlNode parentNode, String childName)
		{
			String valueStr = GetChildValue(parentNode, childName);
			return Util.Parsebool(valueStr);
		}

		protected bool GetValueAsBoolean(XmlNode parentNode)
		{
			String valueStr = GetValue(parentNode);
			return Util.Parsebool(valueStr);
		}

		protected Color GetValueAsColor(XmlNode parentNode)
		{
			String rgbStr = GetValue(parentNode);
			return Util.ParseColor(rgbStr);
		}

		protected bool IsEmptyNode(XmlNode xmlNode)
		{
			// comment XmlNode or empty text XmlNode
			return xmlNode.Name == "#comment" ||
			       (xmlNode.Name == "#text" && xmlNode.Value.Trim().Length == 0);
		}

		protected void ValidateTagsOnlyOnce(XmlNode parentNode, String[] allowedChildNames)
		{
			// validate XmlNode only once
			if (validatedNodes.Contains(parentNode))
			{
				return;
			}
			XmlNode[] childs = GetChildNodes(parentNode);

			foreach (XmlNode child in childs)
			{
				String childName = child.Name;
				for (int j = 0; j < allowedChildNames.Length; j++)
				{
					if (allowedChildNames[j].Equals(childName))
					{
						// only one such tag is allowed
						allowedChildNames[j] = "<--removed-->";
						continue;
					}
					if (allowedChildNames[j].Equals(childName + "*"))
					{
						// several tags allowed
						continue;
					}
				}
				if (!IsEmptyNode(child))
				{
					throw new RrdException("Unexpected tag encountered: <" + childName + ">");
				}
			}
			// everything is OK
			validatedNodes.Add(parentNode);
		}
	}
}