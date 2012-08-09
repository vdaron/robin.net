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

namespace robin.core
{
	/// <summary>
	/// Factory class which creates actual {@link RrdFileBackend} objects. This was the default
	/// backend factory in JRobin before 1.4.0 release.
	/// </summary>
	public class RrdJRobin14FileBackendFactory : RrdBackendFactory
	{
	private const String NAME = "14FILE";
		private readonly RrdJRobin14FileBackend.LockMode lockMode = RrdJRobin14FileBackend.LockMode.NO_LOCKS;

		public RrdJRobin14FileBackendFactory()
		{
		}

		public RrdJRobin14FileBackendFactory(RrdJRobin14FileBackend.LockMode lockMode)
		{
			this.lockMode = lockMode;
		}

		public override string FactoryName
		{
			get { return NAME; }
		}

		/// <summary>
		/// Creates RrdBackend object for the given storage path.
		/// </summary>
		/// <param name="path">Storage path</param>
		/// <param name="readOnly">True, if the storage should be accessed in read/only mode. False otherwise.</param>
		/// <returns>Backend object which handles all I/O operations for the given storage path</returns>
		public override RrdBackend Open(String path, bool readOnly)
		{
			return new RrdJRobin14FileBackend(path, readOnly, lockMode);
		}

		/// <summary>
		/// Method to determine if a storage with the given path already exists.
		/// </summary>
		/// <param name="path">Storage path</param>
		/// <returns></returns>
		public override bool Exists(String path)
		{
			return Util.FileExists(path);
		}

		public override string ToString()
		{
			return GetType().Name + "@" + GetHashCode().ToString("X") + "[name=" + FactoryName + ",lockMode=" + lockMode + "]";
		}
	}
}