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
using System.Threading;

namespace robin.core
{
	/// <summary>
	/// Backend to be used to store all RRD bytes in memory.<p>
	/// </summary>
	public class RrdMemoryBackend : RrdBackend
	{
		private const int WRITER_LOCK_TIMEOUT = 1000;
		private const int READER_LOCK_TIMEOUT = 1000;
		private static readonly ReaderWriterLock readWritelock = new ReaderWriterLock();
		private byte[] buffer = new byte[0];

		public RrdMemoryBackend(String path) : base(path)
		{
		}

		/// <summary>
		/// This method suggests the caching policy to the JRobin frontend (high-level) classes. If <code>true</code>
		/// is returned, frontent classes will cache frequently used parts of a RRD file in memory to improve
		/// performance. If <code>false</code> is returned, high level classes will never cache RRD file sections
		/// in memory.
		/// </summary>
		/// <value>
		/// 	&lt;code&gt;true&lt;/code&gt; if file caching is enabled, &lt;code&gt;false&lt;/code&gt; otherwise. By default, the
		/// 	method returns &lt;code&gt;true&lt;/code&gt; but it can be overriden in subclasses.
		/// </value>
		public override bool CachingAllowed
		{
			get { return false; }
		}

		/// <summary>
		/// Writes an array of bytes to the underlying storage starting from the given
		/// storage offset.
		/// </summary>
		/// <param name="offset">Storage offset.</param>
		/// <param name="b">Array of bytes that should be copied to the underlying storage</param>
		protected internal override void Write(long offset, byte[] b)
		{
			readWritelock.AcquireWriterLock(WRITER_LOCK_TIMEOUT);
			if (!readWritelock.IsWriterLockHeld)
				throw new Exception("Unable to aquire write lock");
			try
			{
				var pos = (int) offset;
				foreach (byte singleByte in b)
				{
					buffer[pos++] = singleByte;
				}
			}
			finally
			{
				readWritelock.ReleaseWriterLock();
			}
		}

		/// <summary>
		/// Reads an array of bytes from the underlying storage starting from the given
		/// storage offset.
		/// </summary>
		/// <param name="offset">Storage offset.</param>
		/// <param name="b">Array which receives bytes from the underlying storage</param>
		protected internal override void Read(long offset, byte[] b)
		{
			readWritelock.AcquireReaderLock(READER_LOCK_TIMEOUT);
			if (!readWritelock.IsReaderLockHeld)
				throw new Exception("Unable to aquire read lock");

			try
			{
				var pos = (int) offset;
				if (pos + b.Length <= buffer.Length)
				{
					for (int i = 0; i < b.Length; i++)
					{
						b[i] = buffer[pos++];
					}
				}
				else
				{
					throw new IOException("Not enough bytes available in memory " + Path);
				}
			}
			finally
			{
				readWritelock.ReleaseReaderLock();
			}
		}

		/// <summary>
		/// Number of RRD bytes in the storage.
		/// </summary>
		/// <returns></returns>
		public override long GetLength()
		{
			readWritelock.AcquireReaderLock(READER_LOCK_TIMEOUT);
			if (!readWritelock.IsReaderLockHeld)
				throw new Exception("Unable to aquire read lock");

			try
			{
				return buffer.Length;
			}
			finally
			{
				readWritelock.ReleaseReaderLock();
			}
		}


		/// <summary>
		/// Sets the number of bytes in the underlying RRD storage.
		/// This method is called only once, immediately after a new RRD storage gets created.
		/// </summary>
		/// <param name="newLength">
		/// Length of the underlying RRD storage in bytes.
		/// </param>
		public override void SetLength(long newLength)
		{
			if (newLength > int.MaxValue)
			{
				throw new IOException("Cannot create this big memory backed RRD");
			}

			readWritelock.AcquireWriterLock(WRITER_LOCK_TIMEOUT);
			if (!readWritelock.IsWriterLockHeld)
				throw new Exception("Unable to aquire write lock");
			try
			{
				buffer = new byte[(int) newLength];
			}
			finally
			{
				readWritelock.ReleaseWriterLock();
			}
		}


		/// <summary>
		/// Closes the underlying backend.
		/// </summary>
		public override void Close()
		{
			// NOP
		}
	}
}