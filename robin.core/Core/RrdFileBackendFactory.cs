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
	public class RrdFileBackendFactory : RrdBackendFactory
	{
		private const String NAME = "FILE";

		public override string FactoryName
		{
			get { return NAME; }
		}

		/// <summary>
		/// Creates RrdFileBackend object for the given file path.
		/// </summary>
		/// <param name="path">File path</param>
		/// <param name="readOnly">True, if the file should be accessed in read/only mode. False otherwise.</param>
		/// <returns>RrdFileBackend object which handles all I/O operations for the given file path</returns>
		public override RrdBackend Open(String path, bool readOnly)
		{
			return new RrdFileBackend(path, readOnly);
		}

		/// <summary>
		/// Method to determine if a file with the given path already exists.
		/// </summary>
		/// <param name="path">File path</param>
		/// <returns>True, if such file exists, false otherwise.</returns>
		public override bool Exists(String path)
		{
			return Util.FileExists(path);
		}
	}
}