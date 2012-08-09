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
using System.Globalization;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;

namespace robin.core
{
class RrdLong : RrdPrimitive {
	private long cache;
	private bool cached = false;

	internal RrdLong(IRrdUpdater updater, bool isConstant) :
		base(updater, RrdPrimitive.RRD_LONG, isConstant){
	}

	internal RrdLong(IRrdUpdater updater)
		: this(updater, false)
	{
	}
	internal void Set(long value)
	{
		if (!CachingAllowed) {
			WriteLong(value);
		}
		// caching allowed
		else if (!cached || cache != value) {
			// update cache
			WriteLong(cache = value);
			cached = true;
		}
	}

	internal long Get()
	{
		return cached ? cache : ReadLong();
	}

	public override string ToString()
	{
		return Get().ToString();
	}
}
}
