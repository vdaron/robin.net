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
using System.Drawing;
using System.Text.RegularExpressions;
using robin.core;
using robin.data;

namespace robin.graph
{
class PrintText : CommentText {
	static string UNIT_MARKER = "([^%]?)%(s|S)"; //TODO : Fix this to use {0} instead of %
	static Regex UNIT_PATTERN = new Regex(UNIT_MARKER,RegexOptions.Compiled);

	private String srcName;
	private ConsolidationFunction consolFun;
	private bool includedInGraph;

	public PrintText(String srcName, ConsolidationFunction consolFun, String text, bool includedInGraph):base(text) {
		this.srcName = srcName;
		this.consolFun = consolFun;
		this.includedInGraph = includedInGraph;
	}

	public override bool isPrint() {
		return !includedInGraph;
	}

	public override void resolveText(DataProcessor dproc, ValueScaler valueScaler){
		base.resolveText(dproc, valueScaler);
		if (resolvedText != null) {
			double value = dproc.GetAggregate(srcName, consolFun);
			Match matcher = UNIT_PATTERN.Match(resolvedText);
			if (matcher.Success) {
				// unit specified
				ValueScaler.Scaled scaled = valueScaler.scale(value, matcher.Groups[2].Equals("s"));
				resolvedText = resolvedText.Substring(0, matcher.Index) +
						matcher.Groups[1] + scaled.unit + resolvedText.Substring(matcher.Index + matcher.Length);
				value = scaled.value;
			}
			resolvedText = String.Format(resolvedText, value);
			trimIfGlue();
		}
	}
}

}