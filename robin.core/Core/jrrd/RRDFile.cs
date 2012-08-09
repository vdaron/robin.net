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
using System.IO;
using System.Text;

namespace robin.core.jrrd
{
	/// <summary>
	/// This class is a quick hack to read information from an RRD file. Writing
	/// to RRD files is not currently supported. As I said, this is a quick hack.
	/// Some thought should be put into the overall design of the file IO.
	/// <p/>
	/// Currently this can read RRD files that were generated on Solaris (Sparc)
	/// and Linux (x86).
	/// 
	/// @author <a href="mailto:ciaran@codeloop.com">Ciaran Treanor</a>
	/// </summary>
	internal class RRDFile : IDisposable
	{
		private readonly byte[] buffer = new byte[128];
		private readonly bool debug;
		private readonly FileStream reader;

		public RRDFile(String name) :
			this(new FileInfo(name))
		{
		}

		public RRDFile(FileInfo file)
		{
			try
			{
				reader = File.OpenRead(file.FullName);

				debug = false;
				InitDataLayout(file);
			}
			catch (Exception)
			{
				Close();
				throw;
			}
			
		}

		private void InitDataLayout(FileInfo file)
		{
			if (file.Exists)
			{
				// Load the data formats from the file
				int bytes = reader.Read(buffer, 0, 24);
				if (bytes < 24)
				{
					throw new RrdException("Invalid RRD file");
				}

				int index;

				if ((index = IndexOf(Constants.FLOAT_COOKIE_BIG_ENDIAN, buffer)) != -1)
				{
					IsBigEndian = true;
				}
				else if ((index = IndexOf(Constants.FLOAT_COOKIE_LITTLE_ENDIAN, buffer))
				         != -1)
				{
					IsBigEndian = false;
				}
				else
				{
					throw new RrdException("Invalid RRD file");
				}

				switch (index)
				{
					case 12:
						Alignment = 4;
						break;

					case 16:
						Alignment = 8;
						break;

					default:
						throw new Exception("Unsupported architecture - neither 32-bit nor 64-bit, or maybe the file is corrupt");
				}
			}
			reader.Seek(0, SeekOrigin.Begin);
		}

		private static int IndexOf(byte[] pattern, byte[] array)
		{
			return (Encoding.ASCII.GetString(array)).IndexOf(Encoding.ASCII.GetString(pattern));
		}

		public bool IsBigEndian { get; private set; }

		public int Alignment { get; private set; }

		public double ReadDouble()
		{
			if (debug)
			{
				Debug.WriteLine("Read 8 bytes (Double) from offset " + FilePointer + ":");
			}

			//double value;
			var tx = new byte[8];

			if (reader.Read(buffer, 0, 8) != 8)
			{
				throw new RrdException("Invalid RRD file");
			}

			if (IsBigEndian)
			{
				tx = buffer;
			}
			else
			{
				for (int i = 0; i < 8; i++)
				{
					tx[7 - i] = buffer[i];
				}
			}

			Double result = BitConverter.ToDouble(tx, 0);
			if (debug)
			{
				Debug.WriteLine(result);
			}
			return result;
		}

		public int ReadInt()
		{
			return ReadInt(false);
		}

		/// <summary>
		/// Reads the next integer (4 or 8 bytes depending on alignment), advancing the file pointer
		///  and returns it
		///  If the alignment is 8-bytes (64-bit), then 8 bytes are read, but only the lower 4-bytes (32-bits) are
		///  returned.  The upper 4 bytes are ignored.
		/// </summary>
		/// <param name="dump"></param>
		/// <returns>the 32-bit integer read from the file</returns>
		public int ReadInt(bool dump)
		{
			//An integer is "alignment" bytes long - 4 bytes on 32-bit, 8 on 64-bit.
			if (debug)
			{
				Debug.WriteLine("Read " + Alignment + " bytes (int) from offset " + FilePointer + ":");
			}

			if (reader.Read(buffer, 0, Alignment) != Alignment)
			{
				throw new RrdException("Invalid RRD file");
			}

			int value;

			if (IsBigEndian)
			{
				if (Alignment == 8)
				{
					//For big-endian, the low 4-bytes of the 64-bit integer are the last 4 bytes
					value = (0xFF & buffer[7]) | ((0xFF & buffer[6]) << 8)
					        | ((0xFF & buffer[5]) << 16) | ((0xFF & buffer[4]) << 24);
				}
				else
				{
					value = (0xFF & buffer[3]) | ((0xFF & buffer[2]) << 8)
					        | ((0xFF & buffer[1]) << 16) | ((0xFF & buffer[0]) << 24);
				}
			}
			else
			{
				//For little-endian, there's no difference between 4 and 8 byte alignment.
				// The first 4 bytes are the low end of a 64-bit number
				value = (0xFF & buffer[0]) | ((0xFF & buffer[1]) << 8)
				        | ((0xFF & buffer[2]) << 16) | ((0xFF & buffer[3]) << 24);
			}

			if (debug)
			{
				Debug.WriteLine(value);
			}
			return value;
		}

		public String ReadString(int maxLength)
		{
			if (debug)
			{
				Debug.WriteLine("Read " + maxLength + " bytes (string) from offset " + FilePointer + ":");
			}
			maxLength = reader.Read(buffer, 0, maxLength);
			if (maxLength == -1)
			{
				throw new RrdException("Invalid RRD file");
			}

			//Info: vdaron - We are using maxLength-1 to avoid reading the \0 with the string
			String result = Encoding.ASCII.GetString(buffer, 0, maxLength - 1).Trim();
			if (debug)
			{
				Debug.WriteLine(result + ":");
			}
			return result;
		}

		public void SkipBytes(int n)
		{
			long bytesSkipped = reader.Seek(n, SeekOrigin.Current);
			if (debug)
			{
				Debug.WriteLine("Skipping " + bytesSkipped + " bytes");
			}
		}

		public void Seek(long offset, SeekOrigin origin)
		{
			reader.Seek(offset, origin);
		}

		public int Align(int boundary)
		{
			int skip = (int) (boundary - (reader.Position%boundary))%boundary;

			if (skip != 0)
			{
				skip = (int) reader.Seek(skip, SeekOrigin.Current);
			}
			if (debug)
			{
				Debug.WriteLine("Aligning to boundary " + boundary + ".  Offset is now " + FilePointer);
			}
			return skip;
		}

		public int Align()
		{
			return Align(Alignment);
		}

		public long info()
		{
			return reader.Position;
		}

		public long FilePointer
		{
			get { return reader.Position; }
		}

		public void Close()
		{
			if(reader != null)
				reader.Close();
		}

		public void Dispose()
		{
			Close();
		}
	}
}