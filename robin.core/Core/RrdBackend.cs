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
	/// <summary>
	/// Base implementation class for all backend classes. Each Round Robin Database object
	/// ({@link RrdDb} object) is backed with a single RrdBackend object which performs
	/// actual I/O operations on the underlying storage. JRobin supports
	/// three different bakcends out of the box:
	/// <ul>
	/// <li>{@link RrdFileBackend}: objects of this class are created from the
	/// {@link RrdFileBackendFactory} class. This was the default backend used in all
	/// JRobin releases prior to 1.4.0. It uses java.io.* package and
	/// RandomAccessFile class to store RRD data in files on the disk.</li>
	/// <p/>
	/// <li>{@link RrdNioBackend}: objects of this class are created from the
	/// {@link RrdNioBackendFactory} class. The backend uses java.io.* and java.nio.*
	/// classes (mapped ByteBuffer) to store RRD data in files on the disk. This backend is fast, very fast,
	/// but consumes a lot of memory (borrowed not from the JVM but from the underlying operating system
	/// directly). <b>This is the default backend used in JRobin since 1.4.0 release.</b></li>
	/// <p/>
	/// <li>{@link RrdMemoryBackend}: objects of this class are created from the
	/// {@link RrdMemoryBackendFactory} class. This backend stores all data in memory. Once
	/// JVM exits, all data gets lost. The backend is extremely fast and memory hungry.</li>
	/// </ul>
	/// 
	/// To create your own backend in order to provide some custom type of RRD storage,
	/// you should do the following:
	/// 
	/// <ul>
	/// <li>Create your custom RrdBackend class (RrdCustomBackend, for example)
	/// by extending RrdBackend class. You have to implement all abstract methods defined
	/// in the base class.</li>
	/// <p/>
	/// <li>Create your custom RrdBackendFactory class (RrdCustomBackendFactory,
	/// for example) by extending RrdBackendFactory class. You have to implement all
	/// abstract methods defined in the base class. Your custom factory class will actually
	/// create custom backend objects when necessary.</li>
	/// <p/>
	/// <li>Create instance of your custom RrdBackendFactory and register it as a regular
	/// factory available to JRobin framework. See javadoc for {@link RrdBackendFactory} to
	/// find out how to do this</li>
	/// </ul>
	/// </summary>
	public abstract class RrdBackend : IDisposable
	{
		private static bool instanceCreated;
		private readonly String path;
		private readonly bool readOnly;

		/// <summary>
		///  Creates backend for a RRD storage with the given path.
		/// </summary>
		/// <param name="path">
		/// String identifying RRD storage. For files on the disk, this
		/// argument should represent file path. Other storage types might interpret
		/// this argument differently.
		/// </param>
		protected RrdBackend(String path) : this(path, false)
		{
		}

		protected RrdBackend(String path, bool readOnly)
		{
			this.path = path;
			this.readOnly = readOnly;
			SetInstanceCreated();
		}

		/// <summary>
		/// Storage path
		/// </summary>
		/// <value></value>
		public string Path
		{
			get { return path; }
		}

		/// <summary>
		/// Is the RRD ReadOnly?
		/// </summary>
		/// <value></value>
		public bool ReadOnly
		{
			get { return readOnly; }
		}

		/// <summary>
		/// Writes an array of bytes to the underlying storage starting from the given
		/// storage offset.
		/// </summary>
		/// <param name="offset">Storage offset.</param>
		/// <param name="b">Array of bytes that should be copied to the underlying storage</param>
		protected internal abstract void Write(long offset, byte[] b);

		/// <summary>
		/// Reads an array of bytes from the underlying storage starting from the given
		/// storage offset.
		/// </summary>
		/// <param name="offset">Storage offset.</param>
		/// <param name="b">Array which receives bytes from the underlying storage</param>
		protected internal abstract void Read(long offset, byte[] b);

		/// <summary>
		/// Number of RRD bytes in the storage.
		/// </summary>
		/// <returns></returns>
		public abstract long GetLength();

		/// <summary>
		/// Sets the number of bytes in the underlying RRD storage.
		/// This method is called only once, immediately after a new RRD storage gets created.
		/// </summary>
		/// <param name="length">
		/// Length of the underlying RRD storage in bytes.
		/// </param>
		public abstract void SetLength(long length);

		/// <summary>
		/// Closes the underlying backend.
		/// </summary>
		public virtual void Close()
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
		public virtual bool CachingAllowed
		{
			get { return true; }
		}

		/// <summary>
		/// Reads all RRD bytes from the underlying storage
		/// </summary>
		/// <returns></returns>
		public byte[] ReadAll()
		{
			var b = new byte[(int) GetLength()];
			Read(0, b);
			return b;
		}

		internal void WriteInt(long offset, int value)
		{
			Write(offset, GetIntBytes(value));
		}

		internal void WriteLong(long offset, long value)
		{
			Write(offset, GetLongBytes(value));
		}

		internal void WriteDouble(long offset, double value)
		{
			Write(offset, GetDoubleBytes(value));
		}

		internal void WriteDouble(long offset, double value, int count)
		{
			byte[] b = GetDoubleBytes(value);
			var image = new byte[8*count];
			for (int i = 0, k = 0; i < count; i++)
			{
				image[k++] = b[0];
				image[k++] = b[1];
				image[k++] = b[2];
				image[k++] = b[3];
				image[k++] = b[4];
				image[k++] = b[5];
				image[k++] = b[6];
				image[k++] = b[7];
			}
			Write(offset, image);
		}

		internal void WriteDouble(long offset, double[] values)
		{
			int count = values.Length;
			var image = new byte[8*count];
			for (int i = 0, k = 0; i < count; i++)
			{
				byte[] b = GetDoubleBytes(values[i]);
				image[k++] = b[0];
				image[k++] = b[1];
				image[k++] = b[2];
				image[k++] = b[3];
				image[k++] = b[4];
				image[k++] = b[5];
				image[k++] = b[6];
				image[k++] = b[7];
			}
			Write(offset, image);
		}

		internal void WriteString(long offset, String rawValue)
		{
			String value = rawValue.Trim();
			var b = new byte[RrdPrimitive.STRING_LENGTH*2];
			for (int i = 0, k = 0; i < RrdPrimitive.STRING_LENGTH; i++)
			{
				char c = (i < value.Length) ? value[i] : ' ';
				byte[] cb = GetCharBytes(c);
				b[k++] = cb[0];
				b[k++] = cb[1];
			}
			Write(offset, b);
		}

		internal int ReadInt(long offset)
		{
			var b = new byte[4];
			Read(offset, b);
			return GetInt(b);
		}

		internal long ReadLong(long offset)
		{
			var b = new byte[8];
			Read(offset, b);
			return GetLong(b);
		}

		internal double ReadDouble(long offset)
		{
			var b = new byte[8];
			Read(offset, b);
			return GetDouble(b);
		}

		internal double[] ReadDouble(long offset, int count)
		{
			int byteCount = 8*count;
			var image = new byte[byteCount];
			Read(offset, image);
			var values = new double[count];
			for (int i = 0, k = -1; i < count; i++)
			{
				var b = new[]
				        	{
				        		image[++k], image[++k], image[++k], image[++k],
				        		image[++k], image[++k], image[++k], image[++k]
				        	};
				values[i] = GetDouble(b);
			}
			return values;
		}

		internal String ReadString(long offset)
		{
			var b = new byte[RrdPrimitive.STRING_LENGTH*2];
			var c = new char[RrdPrimitive.STRING_LENGTH];
			Read(offset, b);
			for (int i = 0, k = -1; i < RrdPrimitive.STRING_LENGTH; i++)
			{
				var cb = new[] {b[++k], b[++k]};
				c[i] = GetChar(cb);
			}
			return new String(c).Trim();
		}

		// static helper methods

		private static byte[] GetIntBytes(int value)
		{
			var b = new byte[4];
			b[0] = (byte) ((value >> 24) & 0xFF);
			b[1] = (byte) ((value >> 16) & 0xFF);
			b[2] = (byte) ((value >> 8) & 0xFF);
			b[3] = (byte) ((value) & 0xFF);
			return b;
		}

		private static byte[] GetLongBytes(long value)
		{
			var b = new byte[8];
			b[0] = (byte) ((int) (value >> 56) & 0xFF);
			b[1] = (byte) ((int) (value >> 48) & 0xFF);
			b[2] = (byte) ((int) (value >> 40) & 0xFF);
			b[3] = (byte) ((int) (value >> 32) & 0xFF);
			b[4] = (byte) ((int) (value >> 24) & 0xFF);
			b[5] = (byte) ((int) (value >> 16) & 0xFF);
			b[6] = (byte) ((int) (value >> 8) & 0xFF);
			b[7] = (byte) ((int) (value) & 0xFF);
			return b;
		}

		private static byte[] GetCharBytes(char value)
		{
			var b = new byte[2];
			b[0] = (byte) ((value >> 8) & 0xFF);
			b[1] = (byte) ((value) & 0xFF);
			return b;
		}

		private static byte[] GetDoubleBytes(double value)
		{
			return GetLongBytes(BitConverter.DoubleToInt64Bits(value));
		}

		private static int GetInt(byte[] b)
		{
			Debug.Assert(b.Length == 4, "Invalid number of bytes for integer conversion");
			return (int) ((b[0] << 24) & 0xFF000000) + ((b[1] << 16) & 0x00FF0000) +
			       ((b[2] << 8) & 0x0000FF00) + (b[3] & 0x000000FF);
		}

		private static long GetLong(byte[] b)
		{
			Debug.Assert(b.Length == 8, "Invalid number of bytes for long conversion");
			int high = GetInt(new[] {b[0], b[1], b[2], b[3]});
			int low = GetInt(new[] {b[4], b[5], b[6], b[7]});
			return ((long) (high) << 32) + (low & 0xFFFFFFFFL);
		}

		private static char GetChar(byte[] b)
		{
			Debug.Assert(b.Length == 2, "Invalid number of bytes for char conversion");
			return (char) (((b[0] << 8) & 0x0000FF00)
			               + (b[1] & 0x000000FF));
		}

		private static double GetDouble(byte[] b)
		{
			Debug.Assert(b.Length == 8, "Invalid number of bytes for double conversion");
			return BitConverter.Int64BitsToDouble(GetLong(b));
		}

		private static void SetInstanceCreated()
		{
			instanceCreated = true;
		}

		internal static bool InstanceCreated()
		{
			return instanceCreated;
		}

		public void Dispose()
		{
			Close();
		}
	}
}