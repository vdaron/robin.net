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
using System.Text.RegularExpressions;
using System.Threading;
using robin.core;
using robin.data;

namespace robin.graph
{
class ImageParameters {
	public long start, end;
	public double minval, maxval;
	public int unitsexponent;
	public double base_renamed;
	public double magfact;
	public char symbol;
	public double ygridstep;
	public int ylabfact;
	public double decimals;
	public int quadrant;
	public double scaledstep;
	public int xsize;
	public int ysize;
	public int xorigin;
	public int yorigin;
	public int unitslength;
	public int xgif, ygif;
	public String unit;
}
}
