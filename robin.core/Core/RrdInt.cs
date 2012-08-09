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

namespace robin.core
{
	internal class RrdInt : RrdPrimitive
	{
		private int cache;
		private bool cached;

		internal RrdInt(IRrdUpdater updater, bool isConstant)
			: base(updater, RRD_INT, isConstant)
		{
		}

		internal RrdInt(IRrdUpdater updater)
			: this(updater, false)
		{
		}

		internal void Set(int value)
		{
			if (!CachingAllowed)
			{
				WriteInt(value);
			}
				// caching allowed
			else if (!cached || cache != value)
			{
				// update cache
				WriteInt(cache = value);
				cached = true;
			}
		}

		internal int Get()
		{
			return cached ? cache : ReadInt();
		}

		public override string ToString()
		{
			return Get().ToString();
		}
	}
}