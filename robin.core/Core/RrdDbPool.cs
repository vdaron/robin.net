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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using robin.core;

namespace robin.data
{
	/// <summary>
	/// This class should be used to synchronize access to RRD files
	/// in a multithreaded environment. This class should be also used to prevent openning of
	/// too many RRD files at the same time (thus avoiding operating system limits)
	/// </summary>
	public class RrdDbPool
	{
		/// <summary>
		/// Initial capacity of the pool i.e. maximum number of simultaneously open RRD files. The pool will
		/// never open too many RRD files at the same time.
		/// </summary>
		public const int INITIAL_CAPACITY = 200;

		private static RrdDbPool instance;

		private readonly Dictionary<String, RrdEntry> rrdMap = new Dictionary<String, RrdEntry>(INITIAL_CAPACITY);
		private int capacity = INITIAL_CAPACITY;

		/// <summary>
		/// Creates a single instance of the class on the first call, or returns already existing one.
		/// </summary>
		private RrdDbPool()
		{
			RrdBackendFactory factory = RrdBackendFactory.GetDefaultFactory();
			if (!(factory is RrdFileBackendFactory))
			{
				throw new RrdException("Cannot create instance of " + GetType().Name + " with " +
				                       "a default backend factory not derived from RrdFileBackendFactory");
			}
		}

		/// <summary>
		/// Maximum number of simultaneously open RRD files.
		/// </summary>
		public int Capacity
		{
			[MethodImpl(MethodImplOptions.Synchronized)]
			get { return capacity; }
			[MethodImpl(MethodImplOptions.Synchronized)]
			set { capacity = value; }
		}

		/// <summary>
		/// Number of currently open RRD files held in the pool.
		/// </summary>
		/// <value></value>
		public int OpenFileCount
		{
			[MethodImpl(MethodImplOptions.Synchronized)]
			get { return rrdMap.Count; }
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public static RrdDbPool GetInstance()
		{
			return instance ?? (instance = new RrdDbPool());
		}

		/// <summary>
		/// Requests a RrdDb reference for the given RRD file path.<p>
		/// <ul>
		/// <li>If the file is already open, previously returned RrdDb reference will be returned. Its usage count
		/// will be incremented by one.
		/// <li>If the file is not already open and the number of already open RRD files is less than
		/// {@link #INITIAL_CAPACITY}, the file will be open and a new RrdDb reference will be returned.
		/// If the file is not already open and the number of already open RRD files is equal to
		/// {@link #INITIAL_CAPACITY}, the method blocks until some RRD file is closed.
		/// </ul>
		/// </summary>
		/// <param name="path">Path to existing RRD file</param>
		/// <returns>reference for the give RRD file</returns>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public RrdDb RequestRrdDb(String path)
		{
			String canonicalPath = Util.GetCanonicalPath(path);
			while (!rrdMap.ContainsKey(canonicalPath) && rrdMap.Count >= capacity)
			{
				try
				{
					Monitor.Wait(instance);
				}
				catch (Exception e)
				{
					throw new RrdException(e);
				}
			}

			if (rrdMap.ContainsKey(canonicalPath))
			{
				// already open, just increase usage count
				RrdEntry entry = rrdMap[canonicalPath];
				entry.Count++;
				return entry.RrdDb;
			}

			// not open, open it now and add to the map
			RrdDb rrdDb = RrdDb.Open(canonicalPath);
			rrdMap.Add(canonicalPath, new RrdEntry(rrdDb));
			return rrdDb;
		}

		/// <summary>
		/// Requests a RrdDb reference for the given RRD file definition object.<p>
		/// <ul>
		/// <li>If the file with the path specified in the RrdDef object is already open,
		/// the method blocks until the file is closed.
		/// <li>If the file is not already open and the number of already open RRD files is less than
		/// {@link #INITIAL_CAPACITY}, a new RRD file will be created and a its RrdDb reference will be returned.
		/// If the file is not already open and the number of already open RRD files is equal to
		/// {@link #INITIAL_CAPACITY}, the method blocks until some RRD file is closed.
		/// </ul>
		/// </summary>
		/// <param name="rrdDef">Definition of the RRD file to be created</param>
		/// <returns>Reference to the newly created RRD file</returns>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public RrdDb RequestRrdDb(RrdDef rrdDef)
		{
			String canonicalPath = Util.GetCanonicalPath(rrdDef.Path);
			while (rrdMap.ContainsKey(canonicalPath) || rrdMap.Count >= capacity)
			{
				try
				{
					Monitor.Wait(instance);
				}
				catch (Exception e)
				{
					throw new RrdException(e);
				}
			}
			RrdDb rrdDb = RrdDb.Create(rrdDef);
			rrdMap.Add(canonicalPath, new RrdEntry(rrdDb));
			return rrdDb;
		}

		/// <summary>
		/// Requests a RrdDb reference for the given path. The file will be created from
		/// external data (from XML dump, RRD file or RRDTool's binary RRD file).<p>
		/// <ul>
		/// <li>If the file with the path specified is already open,
		/// the method blocks until the file is closed.
		/// <li>If the file is not already open and the number of already open RRD files is less than
		/// {@link #INITIAL_CAPACITY}, a new RRD file will be created and a its RrdDb reference will be returned.
		/// If the file is not already open and the number of already open RRD files is equal to
		/// {@link #INITIAL_CAPACITY}, the method blocks until some RRD file is closed.
		/// </ul>
		/// </summary>
		/// <param name="path">Path to RRD file which should be created</param>
		/// <param name="sourcePath">Path to external data which is to be converted to JRobin's native RRD file format</param>
		/// <returns>Reference to the newly created RRD file</returns>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public RrdDb RequestRrdDb(String path, String sourcePath)
		{
			String canonicalPath = Util.GetCanonicalPath(path);
			while (rrdMap.ContainsKey(canonicalPath) || rrdMap.Count >= capacity)
			{
				try
				{
					Monitor.Wait(instance);
				}
				catch (Exception e)
				{
					throw new RrdException(e);
				}
			}
			RrdDb rrdDb = RrdDb.Import(canonicalPath, sourcePath);
			rrdMap.Add(canonicalPath, new RrdEntry(rrdDb));
			return rrdDb;
		}

		/// <summary>
		/// Releases RrdDb reference previously obtained from the pool. When a reference is released, its usage
		/// count is decremented by one. If usage count drops to zero, the underlying RRD file will be closed.
		/// </summary>
		/// <param name="rrdDb">RrdDb reference to be returned to the pool</param>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public void Release(RrdDb rrdDb)
		{
			// null pointer should not kill the thread, just ignore it
			if (rrdDb == null)
			{
				return;
			}
			String canonicalPath = Util.GetCanonicalPath(rrdDb.GetPath());
			if (!rrdMap.ContainsKey(canonicalPath))
			{
				throw new RrdException("Could not release [" + canonicalPath + "], the file was never requested");
			}
			RrdEntry entry = rrdMap[canonicalPath];
			if (--entry.Count <= 0)
			{
				// no longer used
				rrdMap.Remove(canonicalPath);
				Monitor.PulseAll(instance);
				entry.RrdDb.Close();
			}
		}

		/// <summary>
		/// Array with canonical paths to open RRD files held in the pool.
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public String[] GetOpenFiles()
		{
			return rrdMap.Keys.ToArray();
		}

		#region Nested type: RrdEntry

		private class RrdEntry
		{
			internal RrdEntry(RrdDb rrdDb)
			{
				RrdDb = rrdDb;
				Count = 1;
			}

			public RrdDb RrdDb { get; private set; }
			public int Count { get; internal set; }
		}

		#endregion
	}
}