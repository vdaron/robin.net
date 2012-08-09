/* ============================================================
 * JRobin : Pure java implementation of RRDTool's functionality
 * ============================================================
 *
 * Project Info:  http://www.jrobin.org
 * Project Lead:  Sasa Markovic (saxon@jrobin.org);
 *
 * (C) Copyright 2003, by Sasa Markovic.
 *
 * Developers:    Sasa Markovic (saxon@jrobin.org)
 *                Arne Vandamme (cobralord@jrobin.org)
 *
 * This library is free software; you can redistribute it and/or modify it under the terms
 * of the GNU Lesser General Public License as published by the Free Software Foundation;
 * either version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 * See the GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License along with this
 * library; if not, write to the Free Software Foundation, Inc., 59 Temple Place, Suite 330,
 * Boston, MA 02111-1307, USA.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace robin.core
{
	/// <summary>
	/// JRobin backend which is used to store RRD data to ordinary files on the disk. This was the
	/// default factory before 1.4.0 version<p>
	/// <p/>
	/// This backend is based on the RandomAccessFile class (java.io.* package).
	/// </summary>
	public sealed class RrdJRobin14FileBackend : RrdBackend
	{
		#region LockMode enum

		public enum LockMode
		{
			EXCEPTION_IF_LOCKED,
			WAIT_IF_LOCKED,
			NO_LOCKS
		} ;

		#endregion

		private const int LOCK_DELAY = 100; // 0.1sec

		private static readonly ISet<String> openFiles = new HashSet<String>();

		/** random access file handle */
		private readonly FileStream File;
		/** file lock */
		private readonly LockMode lockMode;
		private FileStream fileLock;

		/// <summary>
		/// Creates RrdFileBackend object for the given file path, backed by RandomAccessFile object.
		/// </summary>
		/// <param name="path">Path to a file</param>
		/// <param name="readOnly">True, if file should be open in a read-only mode. False otherwise</param>
		/// <param name="lockMode">Locking mode, as described in {@link RrdDb#getLockMode()}</param>
		public RrdJRobin14FileBackend(String path, bool readOnly, LockMode lockMode) : base(path, readOnly)
		{
			this.lockMode = lockMode;
			File = System.IO.File.Open(path, readOnly ? FileMode.Open : FileMode.Create);
			try
			{
				LockFile();
				RegisterWriter();
			}
			catch (IOException)
			{
				Close();
				throw;
			}
			Debug.WriteLine(String.Format("backend initialized with path={0}, readOnly={1}, lockMode={2}", path, readOnly,
			                              lockMode));
		}

		private void LockFile()
		{
			switch (lockMode)
			{
				case LockMode.EXCEPTION_IF_LOCKED:
					try
					{
						fileLock = System.IO.File.Open(Path + ".lck", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
					}
					catch (Exception)
					{
						throw new IOException("Access denied. " + "File [" + Path + "] already locked");
					}
					break;
				case LockMode.WAIT_IF_LOCKED:
					while (fileLock == null)
					{
						try
						{
							fileLock = System.IO.File.Open(Path + ".lck", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
						}
						catch (Exception)
						{
							Thread.Sleep(LOCK_DELAY);
							fileLock = null;
						}
					}
					break;
				case LockMode.NO_LOCKS:
					break;
			}
		}

		private void RegisterWriter()
		{
			if (!ReadOnly)
			{
				String canonicalPath = Util.GetCanonicalPath(Path);
				lock (openFiles)
				{
					if (openFiles.Contains(canonicalPath))
					{
						throw new IOException("File \"" + Path + "\" already open for R/W access. " +
						                      "You cannot open the same file for R/W access twice");
					}
					else
					{
						openFiles.Add(canonicalPath);
					}
				}
			}
		}

		/// <summary>
		/// Closes the underlying backend.
		/// </summary>
		public override void Close()
		{
			UnregisterWriter();
			try
			{
				UnlockFile();
			}
			finally
			{
				File.Close();
			}
		}

		private void UnlockFile()
		{
			if (fileLock != null)
			{
				fileLock.Close();
				try
				{
					System.IO.File.Delete(Path + ".lck");
				}
				catch (Exception)
				{
				}
			}
		}

		private void UnregisterWriter()
		{
			if (!ReadOnly)
			{
				lock (openFiles)
				{
					openFiles.Remove(Util.GetCanonicalPath(Path));
				}
			}
		}

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