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
using System.Runtime.CompilerServices;

namespace robin.core
{
/// <summary>
	/// Factory class which creates actual {@link RrdMemoryBackend} objects. JRobin's support
	/// for in-memory RRDs is still experimental. You should know that all active RrdMemoryBackend
	/// objects are held in memory, each backend object stores RRD data in one big byte array. This
	/// implementation is therefore quite basic and memory hungry but runs very fast.
	/// 
	/// Calling {@link RrdDb#close() close()} on RrdDb objects does not release any memory at all
	/// (RRD data must be available for the next <code>new RrdDb(path)</code> call. To release allocated
	/// memory, you'll have to call {@link #delete(String) delete(path)} method of this class.
/// </summary>
	public class RrdMemoryBackendFactory : RrdBackendFactory
	{
		private const String NAME = "MEMORY";
		private readonly Dictionary<String, RrdMemoryBackend> backends = new Dictionary<String, RrdMemoryBackend>();

		public override string FactoryName
		{
			get { return NAME; }
		}

		/// <summary>
		/// Creates RrdMemoryBackend object.
		/// </summary>
		/// <param name="id">Since this backend holds all data in memory, this argument is interpreted as an ID for this memory-based storage.</param>
		/// <param name="readOnly">This parameter is ignored</param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public override RrdBackend Open(String id, bool readOnly)
		{
			RrdMemoryBackend backend;
			if (backends.ContainsKey(id))
			{
				backend = backends[id];
			}
			else
			{
				backend = new RrdMemoryBackend(id);
				backends.Add(id, backend);
			}
			return backend;
		}

	/// <summary>
		/// Method to determine if a memory storage with the given ID already exists.
	/// </summary>
	/// <param name="id">id of memory backend</param>
	/// <returns></returns>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public override bool Exists(String id)
		{
			return backends.ContainsKey(id);
		}

		/// <summary>
		/// Removes the storage with the given ID from the memory.
		/// </summary>
		/// <param name="id">id of memory backend</param>
		/// <returns></returns>
		public bool Delete(String id)
		{
			if (backends.ContainsKey(id))
			{
				backends.Remove(id);
				return true;
			}
			return false;
		}
	}
}