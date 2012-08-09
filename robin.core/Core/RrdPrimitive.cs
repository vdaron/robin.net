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

namespace robin.core
{
	internal abstract class RrdPrimitive
	{
		internal const int STRING_LENGTH = 20;

		protected const int RRD_INT = 0;
		protected const int RRD_LONG = 1;
		protected const int RRD_DOUBLE = 2;
		protected const int RRD_STRING = 3;

		private static readonly int[] RRD_PRIM_SIZES = {4, 8, 8, 2*STRING_LENGTH};

		private readonly RrdBackend backend;
		private readonly int byteCount;
		private readonly bool cachingAllowed;
		private readonly long pointer;

		protected RrdPrimitive(IRrdUpdater updater, int type, bool isConstant)
			: this(updater, type, 1, isConstant)
		{
		}

		internal RrdPrimitive(IRrdUpdater updater, int type, int count, bool isConstant)
		{
			backend = updater.GetRrdBackend();
			byteCount = RRD_PRIM_SIZES[type]*count;
			pointer = updater.GetRrdAllocator().Allocate(byteCount);
			cachingAllowed = isConstant || backend.CachingAllowed;
		}

		internal byte[] ReadBytes()
		{
			var b = new byte[byteCount];
			backend.Read(pointer, b);
			return b;
		}

		internal void WriteBytes(byte[] b)
		{
			Debug.Assert(b.Length == byteCount); // "Invalid number of bytes supplied to RrdPrimitive.write method";
			backend.Write(pointer, b);
		}

		protected int ReadInt()
		{
			return backend.ReadInt(pointer);
		}

		protected void WriteInt(int value)
		{
			backend.WriteInt(pointer, value);
		}

		protected long ReadLong()
		{
			return backend.ReadLong(pointer);
		}

		protected void WriteLong(long value)
		{
			backend.WriteLong(pointer, value);
		}

		protected double ReadDouble()
		{
			return backend.ReadDouble(pointer);
		}

		protected double ReadDouble(int index)
		{
			long offset = pointer + (index*(long) RRD_PRIM_SIZES[RRD_DOUBLE]);
			return backend.ReadDouble(offset);
		}

		protected double[] ReadDouble(int index, int count)
		{
			long offset = pointer + (index*(long) RRD_PRIM_SIZES[RRD_DOUBLE]);
			return backend.ReadDouble(offset, count);
		}

		protected internal void WriteDouble(double value)
		{
			backend.WriteDouble(pointer, value);
		}

		protected void WriteDouble(int index, double value, int count)
		{
			long offset = pointer + (index*(long) RRD_PRIM_SIZES[RRD_DOUBLE]);
			backend.WriteDouble(offset, value, count);
		}

		protected internal void WriteDouble(int index, double[] values)
		{
			long offset = pointer + (index*(long) RRD_PRIM_SIZES[RRD_DOUBLE]);
			backend.WriteDouble(offset, values);
		}

		protected String ReadString()
		{
			return backend.ReadString(pointer);
		}

		protected void WriteString(String value)
		{
			backend.WriteString(pointer, value);
		}

		protected bool CachingAllowed
		{
			get { return cachingAllowed; }
		}
	}
}