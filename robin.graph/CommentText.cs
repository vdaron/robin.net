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
using robin.core;
using robin.data;

namespace robin.graph
{
class CommentText {
    private String text; // original text

   public String resolvedText; // resolved text
   public String marker; // end-of-text marker
	public bool enabled; // hrule and vrule comments can be disabled at runtime
	public int x; // coordinates, evaluated later
	public int y; // coordinates, evaluated later

	public CommentText(String text) {
        this.text = text;
    }

	public virtual void resolveText(DataProcessor dproc, ValueScaler valueScaler) {
        resolvedText = text;
        marker = "";
        if (resolvedText != null) {
            foreach (String mark in RrdGraphConstants.MARKERS) {
                if (resolvedText.EndsWith(mark)) {
                    marker = mark;
                    resolvedText = resolvedText.Substring(0, resolvedText.Length - marker.Length);
                    trimIfGlue();
                    break;
                }
            }
        }
        enabled = resolvedText != null;
    }

	protected void trimIfGlue() {
		 if (marker.Equals(RrdGraphConstants.GLUE_MARKER))
		 {
            resolvedText = resolvedText.Trim();
        }
    }

	public virtual bool isPrint() {
        return false;
    }

	public bool isValidGraphElement() {
        return !isPrint() && enabled;
    }
}
}