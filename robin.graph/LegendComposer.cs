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
using System.Diagnostics;
using System.Drawing;
using robin.core;
using robin.data;

namespace robin.graph
{

class LegendComposer {
	private RrdGraphDef gdef;
	private ImageWorker worker;
	private int legX, legY, legWidth;
	private double interlegendSpace;
	private double leading;
	private double smallLeading;
	private double boxSpace;

	internal LegendComposer(RrdGraph rrdGraph, int legX, int legY, int legWidth) {
		this.gdef = rrdGraph.gdef;
		this.worker = rrdGraph.worker;
		this.legX = legX;
		this.legY = legY;
		this.legWidth = legWidth;
		interlegendSpace = rrdGraph.getInterlegendSpace();
		leading = rrdGraph.getLeading();
		smallLeading = rrdGraph.getSmallLeading();
		boxSpace = rrdGraph.getBoxSpace();
	}

	internal int placeComments() {
		Line line = new Line(this);
		foreach (CommentText comment in gdef.comments) {
			if (comment.isValidGraphElement()) {
				if (!line.canAccomodate(comment)) {
					line.layoutAndAdvance(false);
					line.clear();
				}
				line.add(comment);
			}
		}
		line.layoutAndAdvance(true);
		worker.dispose();
		return legY;
	}

	class Line {
		private String lastMarker;
		private double width;
		private int spaceCount;
		private bool noJustification;
		private List<CommentText> comments = new List<CommentText>();
		private LegendComposer legendComposer;
		internal Line(LegendComposer legendComposer)
		{
			this.legendComposer = legendComposer;
			clear();
		}

		public void clear() {
			lastMarker = "";
			width = 0;
			spaceCount = 0;
			noJustification = false;
			comments.Clear();
		}

		public bool canAccomodate(CommentText comment) {
			// always accommodate if empty
			if (comments.Count == 0) {
				return true;
			}
			// cannot accommodate if the last marker was \j, \l, \r, \c, \s
			if (lastMarker.Equals(RrdGraphConstants.ALIGN_LEFT_MARKER) || lastMarker.Equals(RrdGraphConstants.ALIGN_CENTER_MARKER) ||
					lastMarker.Equals(RrdGraphConstants.ALIGN_RIGHT_MARKER) || lastMarker.Equals(RrdGraphConstants.ALIGN_JUSTIFIED_MARKER) ||
					lastMarker.Equals(RrdGraphConstants.VERTICAL_SPACING_MARKER))
			{
				return false;
			}
			// cannot accommodate if line would be too long
			double commentWidth = getCommentWidth(comment);
			if (!lastMarker.Equals(RrdGraphConstants.GLUE_MARKER))
			{
				commentWidth += legendComposer.interlegendSpace;
			}
			return width + commentWidth <= legendComposer.legWidth;
		}

		public void add(CommentText comment) {
			double commentWidth = getCommentWidth(comment);
			if (comments.Count > 0 && !lastMarker.Equals(RrdGraphConstants.GLUE_MARKER))
			{
				commentWidth += legendComposer.interlegendSpace;
				spaceCount++;
			}
			width += commentWidth;
			lastMarker = comment.marker;
			noJustification |= lastMarker.Equals(RrdGraphConstants.NO_JUSTIFICATION_MARKER);
			comments.Add(comment);
		}

		public void layoutAndAdvance(bool isLastLine) {
			if (comments.Count > 0) {
				if (lastMarker.Equals(RrdGraphConstants.ALIGN_LEFT_MARKER))
				{
					placeComments(legendComposer.legX, legendComposer.interlegendSpace);
				}
				else if (lastMarker.Equals(RrdGraphConstants.ALIGN_RIGHT_MARKER))
				{
					placeComments(legendComposer.legX + legendComposer.legWidth - width, legendComposer.interlegendSpace);
				}
				else if (lastMarker.Equals(RrdGraphConstants.ALIGN_CENTER_MARKER))
				{
					placeComments(legendComposer.legX + (legendComposer.legWidth - width) / 2.0, legendComposer.interlegendSpace);
				}
				else if (lastMarker.Equals(RrdGraphConstants.ALIGN_JUSTIFIED_MARKER))
				{
					// anything to justify?
					if (spaceCount > 0) {
						placeComments(legendComposer.legX, (legendComposer.legWidth - width) / spaceCount + legendComposer.interlegendSpace);
					}
					else {
						placeComments(legendComposer.legX, legendComposer.interlegendSpace);
					}
				}
				else if (lastMarker.Equals(RrdGraphConstants.VERTICAL_SPACING_MARKER))
				{
					placeComments(legendComposer.legX, legendComposer.interlegendSpace);
				}
				else {
					// nothing specified, align with respect to '\J'
					if (noJustification || isLastLine) {
						placeComments(legendComposer.legX, legendComposer.interlegendSpace);
					}
					else {
						placeComments(legendComposer.legX, (legendComposer.legWidth - width) / spaceCount + legendComposer.interlegendSpace);
					}
				}
				if (lastMarker.Equals(RrdGraphConstants.VERTICAL_SPACING_MARKER))
				{
					legendComposer.legY += (int)legendComposer.smallLeading;
				}
				else {
					legendComposer.legY += (int)legendComposer.leading;
				}
			}
		}

		private double getCommentWidth(CommentText comment) {
			double commentWidth = legendComposer.worker.getStringWidth(comment.resolvedText, legendComposer.gdef.smallFont);
			if (comment is LegendText) {
				commentWidth += legendComposer.boxSpace;
			}
			return commentWidth;
		}

		private void placeComments(double xStart, double space) {
			double x = xStart;
			foreach (CommentText comment in comments) {
				comment.x = (int)x;
				comment.y = legendComposer.legY;
				x += getCommentWidth(comment);
				if (!comment.marker.Equals(RrdGraphConstants.GLUE_MARKER))
				{
					x += space;
				}
				
			}
		}
	}
}

}