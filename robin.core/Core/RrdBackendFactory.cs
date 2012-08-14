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
	/// Base (abstract) backend factory class which holds references to all concrete
	/// backend factories and defines abstract methods which must be implemented in
	/// all concrete factory implementations.
	/// 
	/// Factory classes are used to create concrete {@link RrdBackend} implementations.
	/// Each factory creates unlimited number of specific backend objects.
	/// <p/>
	/// JRobin supports four different backend types (backend factories) out of the box:
	/// <ul>
	/// <li>{@link RrdFileBackend}: objects of this class are created from the
	/// {@link RrdFileBackendFactory} class. This was the default backend used in all
	/// JRobin releases before 1.4.0 release. It uses java.io.* package and RandomAccessFile class to store
	/// RRD data in files on the disk.</li>
	/// <li>{@link RrdSafeFileBackend}: objects of this class are created from the
	/// {@link RrdSafeFileBackendFactory} class. It uses java.io.* package and RandomAccessFile class to store
	/// RRD data in files on the disk. This backend is SAFE:
	/// it locks the underlying RRD file during update/fetch operations, and caches only static
	/// parts of a RRD file in memory. Therefore, this backend is safe to be used when RRD files should
	/// be shared <b>between several JVMs</b> at the same time. However, this backend is *slow* since it does
	/// not use fast java.nio.* package (it's still based on the RandomAccessFile class).</li>
	/// <p/>
	/// <li>{@link RrdNioBackend}: objects of this class are created from the
	/// {@link RrdNioBackendFactory} class. The backend uses java.io.* and java.nio.*
	/// classes (mapped ByteBuffer) to store RRD data in files on the disk. This is the default backend
	/// since 1.4.0 release.</li>
	/// <p/>
	/// <li>{@link RrdMemoryBackend}: objects of this class are created from the
	/// {@link RrdMemoryBackendFactory} class. This backend stores all data in memory. Once
	/// JVM exits, all data gets lost. The backend is extremely fast and memory hungry.</li>
	/// </ul>
	/// <p/>
	/// Each backend factory is identifed by its {@link #getFactoryName() name}. Constructors
	/// are provided in the {@link RrdDb} class to create RrdDb objects (RRD databases)
	/// backed with a specific backend.
	/// <p/>
	/// See javadoc for {@link RrdBackend} to find out how to create your custom backends.
	/// </summary>
	public abstract class RrdBackendFactory
	{
		private static readonly Dictionary<String, RrdBackendFactory> factories = new Dictionary<String, RrdBackendFactory>();
		private static RrdBackendFactory defaultFactory;

		static RrdBackendFactory()
		{
			try
			{
				var fileFactory = new RrdFileBackendFactory();
				RegisterFactory(fileFactory);
				var jrobin14Factory = new RrdJRobin14FileBackendFactory();
				RegisterFactory(jrobin14Factory);
				var memoryFactory = new RrdMemoryBackendFactory();
				RegisterFactory(memoryFactory);
				//RrdNioBackendFactory nioFactory = new RrdNioBackendFactory();
				//registerFactory(nioFactory);
				//RrdSafeFileBackendFactory safeFactory = new RrdSafeFileBackendFactory();
				//registerFactory(safeFactory);
				//RrdNioByteBufferBackendFactory nioByteBufferFactory = new RrdNioByteBufferBackendFactory();
				//registerFactory(nioByteBufferFactory);
				SelectDefaultFactory();
			}
			catch (RrdException e)
			{
				throw new InvalidOperationException("FATAL: Cannot register RRD backend factories: " + e);
			}
		}

		private static void SelectDefaultFactory()
		{
			SetDefaultFactory("FILE");
		}

		/// <summary>
		///  Returns backend factory for the given backend factory name.
		/// </summary>
		/// <param name="name">
		/// Backend factory name. Initially supported names are:
		/// <ul>
		/// <li><b>FILE</b>: Default factory which creates backends based on the
		/// java.io.* package. RRD data is stored in files on the disk</li>
		/// <li><b>SAFE</b>: Default factory which creates backends based on the
		/// java.io.* package. RRD data is stored in files on the disk. This backend
		/// is "safe". Being safe means that RRD files can be safely shared between
		/// several JVM's.</li>
		/// <li><b>NIO</b>: Factory which creates backends based on the
		/// java.nio.* package. RRD data is stored in files on the disk</li>
		/// <li><b>MEMORY</b>: Factory which creates memory-oriented backends.
		/// RRD data is stored in memory, it gets lost as soon as JVM exits.</li>
		/// </ul>
		/// </param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public static RrdBackendFactory GetFactory(String name)
		{
			if(factories.ContainsKey(name))
				return factories[name];

			throw new RrdException("No backend factory found with the name specified [" + name + "]");
		}

		/// <summary>
		///  Registers new (custom) backend factory within the JRobin framework.
		/// </summary>
		/// <param name="factory">New factory</param>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public static void RegisterFactory(RrdBackendFactory factory)
		{
			String name = factory.FactoryName;
			if (!factories.ContainsKey(name))
			{
				factories.Add(name, factory);
			}
			else
			{
				throw new RrdException("Backend factory of this name2 (" + name + ") already exists and cannot be registered");
			}
		}

		/// <summary>
		/// Registers new (custom) backend factory within the JRobin framework and sets this
		/// factory as the default.
		/// </summary>
		/// <param name="factory">Factory to be registered and set as default</param>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public static void RegisterAndSetAsDefaultFactory(RrdBackendFactory factory)
		{
			RegisterFactory(factory);
			SetDefaultFactory(factory.FactoryName);
		}
		
		/// <summary>
		/// Returns the defaul backend factory. This factory is used to construct
		/// <see cref="RrdDb"/>  objects if no factory is specified in the RrdDb constructor.
		/// </summary>
		/// <returns></returns>
		public static RrdBackendFactory GetDefaultFactory()
		{
			return defaultFactory;
		}

		/// <summary>
		/// Replaces the default backend factory with a new one. This method must be called before
		/// the first RRD gets created.
		/// </summary>
		/// <param name="factoryName">
		/// Name of the default factory. Out of the box, JRobin supports four
		/// different RRD backends: "FILE" (java.io.* based), "SAFE" (java.io.* based - use this
		/// backend if RRD files may be accessed from several JVMs at the same time),
		/// "NIO" (java.nio.* based) and "MEMORY" (byte[] based).
		/// </param>
		public static void SetDefaultFactory(String factoryName)
		{
			// We will allow this only if no RRDs are created
			if (!RrdBackend.InstanceCreated())
			{
				defaultFactory = GetFactory(factoryName);
			}
			else
			{
				throw new RrdException(
					"Could not change the default backend factory. This method must be called before the first RRD gets created");
			}
		}

		/// <summary>
		/// Whether or not the RRD backend has created an instance yet.
		/// </summary>
		/// <value></value>
		public static bool InstanceCreated
		{
			get { return RrdBackend.InstanceCreated(); }
		}

		/// <summary>
		/// Creates RrdBackend object for the given storage path.
		/// </summary>
		/// <param name="path">Storage path</param>
		/// <param name="readOnly">True, if the storage should be accessed in read/only mode. False otherwise.</param>
		/// <returns>Backend object which handles all I/O operations for the given storage path</returns>
		public abstract RrdBackend Open(String path, bool readOnly);

		/// <summary>
		/// Method to determine if a storage with the given path already exists.
		/// </summary>
		/// <param name="path">Storage path</param>
		/// <returns></returns>
		public abstract bool Exists(String path);

		/// <summary>
		/// Returns the name (primary ID) for the factory.
		/// </summary>
		/// <value></value>
		public abstract string FactoryName { get; }

		public override string ToString()
		{
			return GetType().Name + "@" + GetHashCode().ToString("X") + "[name=" + FactoryName + "]";
		}
	}
}