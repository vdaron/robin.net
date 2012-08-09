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
using System.Diagnostics;
using System.Drawing;
using robin.core;
using robin.data;

namespace robin.graph
{

class ValueScaler {
	static readonly String UNIT_UNKNOWN = "?";
	static readonly String[] UNIT_SYMBOLS = {
			"a", "f", "p", "n", "u", "m", " ", "k", "M", "G", "T", "P", "E"
	};
	const int SYMB_CENTER = 6;

	private double base_renamed;
	private double magfact = -1; // nothing scaled before, rescale
	private String unit;

	internal ValueScaler(double base_renamed) {
		this.base_renamed = base_renamed;
	}

	internal Scaled scale(double value, bool mustRescale) {
		Scaled scaled;
		if (mustRescale) {
			scaled = rescale(value);
		}
		else if (magfact >= 0) {
			// already scaled, need not rescale
			scaled = new Scaled(value / magfact, unit);
		}
		else {
			// scaling not requested, but never scaled before - must rescale anyway
			scaled = rescale(value);
			// if zero, scale again on the next try
			if (scaled.value == 0.0 || Double.IsNaN(scaled.value)) {
				magfact = -1.0;
			}
		}
		return scaled;
	}

	private Scaled rescale(double value) {
		int sindex;
		if (value == 0.0 || Double.IsNaN(value)) {
			sindex = 0;
			magfact = 1.0;
		}
		else {
			sindex = (int) (Math.Floor(Math.Log(Math.Abs(value)) / Math.Log(base_renamed)));
			magfact = Math.Pow(base_renamed, sindex);
		}
		if (sindex <= SYMB_CENTER && sindex >= -SYMB_CENTER) {
			unit = UNIT_SYMBOLS[sindex + SYMB_CENTER];
		}
		else {
			unit = UNIT_UNKNOWN;
		}
		return new Scaled(value / magfact, unit);
	}

	internal class Scaled {
		internal double value;
		internal String unit;

		public Scaled(double value, String unit) {
			this.value = value;
			this.unit = unit;
		}

		void dump() {
			Debug.WriteLine("[" + value + unit + "]");
		}
	}
}

}