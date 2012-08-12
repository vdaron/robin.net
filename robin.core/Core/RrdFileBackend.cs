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
using System.IO;

namespace robin.core
{
	/// <summary>
	/// JRobin backend which is used to store RRD data to ordinary files on the disk. This was the
	/// default factory before 1.4.0 version
	/// <p/>
	/// This backend is based on the FileStream class.
	/// </summary>
	public class RrdFileBackend : RrdBackend
	{
		protected FileStream File;

		/// <summary>
		/// Creates RrdFileBackend object for the given file path, backed by RandomAccessFile object.
		/// </summary>
		/// <param name="path">Path to a file</param>
		/// <param name="readOnly">True, if file should be open in a read-only mode. False otherwise</param>
		public RrdFileBackend(String path, bool readOnly) : base(path, readOnly)
		{
			File = System.IO.File.Open(path, FileMode.OpenOrCreate, readOnly ? FileAccess.Read : FileAccess.ReadWrite);
		}

	/// <summary>
		/// Closes the underlying RRD file.
	/// </summary>
		public override void Close()
		{
			File.Close();
		}

		/// <summary>
		/// Canonical path to the file on the disk.
		/// </summary>
		/// <returns></returns>
		public String GetCanonicalPath()
		{
			return Util.GetCanonicalPath(Path);
		}

		/// <summary>
		/// Writes an array of bytes to the underlying storage starting from the given
		/// storage offset.
		/// </summary>
		/// <param name="offset">Storage offset.</param>
		/// <param name="b">Array of bytes that should be copied to the underlying storage</param>
		protected internal override void Write(long offset, byte[] b)
		{
			File.Seek(offset, SeekOrigin.Begin);
			File.Write(b, 0, b.Length);
		}

		/// <summary>
		/// Reads an array of bytes from the underlying storage starting from the given
		/// storage offset.
		/// </summary>
		/// <param name="offset">Storage offset.</param>
		/// <param name="b">Array which receives bytes from the underlying storage</param>
		protected internal override void Read(long offset, byte[] b)
		{
			File.Seek(offset, SeekOrigin.Begin);
			if (File.Read(b, 0, b.Length) != b.Length)
			{
				throw new IOException("Not enough bytes available in file " + Path);
			}
		}

		/// <summary>
		/// Number of RRD bytes in the storage.
		/// </summary>
		/// <returns></returns>
		public override long GetLength()
		{
			return File.Length;
		}

		/// <summary>
		/// Sets the number of bytes in the underlying RRD storage.
		/// This method is called only once, immediately after a new RRD storage gets created.
		/// </summary>
		/// <param name="length">
		/// Length of the underlying RRD storage in bytes.
		/// </param>
		public override void SetLength(long length)
		{
			File.SetLength(length);
		}
	}
}