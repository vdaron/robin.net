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
using System.IO;
using System.Linq;
using System.Text;
using robin.core;

namespace robin.Core
{
	/// <summary>
	/// <p>Class used to perform various complex operations on RRD files. Use an instance of the
	/// RrdToolkit class to:</p>
	/// <ul>
	/// <li>add datasource to a RRD file.</li>
	/// <li>add archive to a RRD file.</li>
	/// <li>remove datasource from a RRD file.</li>
	/// <li>remove archive from a RRD file.</li>
	/// </ul>
	/// <p>All these operations can be performed on the copy of the original RRD file, or on the
	/// original file itself (with possible backup file creation)</p>
	/// <p/>
	/// <p><b><u>IMPORTANT</u></b>: NEVER use methods found in this class on 'live' RRD files
	/// (files which are currently in use).</p>
	/// </summary>
	public class RrdToolkit
	{
		/// <summary>
		/// Creates a new RRD file with one more datasource in it. RRD file is created based on the
		/// existing one (the original RRD file is not modified at all). All data from
		/// the original RRD file is copied to the new one.
		/// </summary>
		/// <param name="sourcePath">path to a RRD file to import data from (will not be modified)</param>
		/// <param name="destPath">path to a new RRD file (will be created)</param>
		/// <param name="newDatasource">Datasource definition to be added to the new RRD file</param>
		public static void AddDatasource(String sourcePath, String destPath, DsDef newDatasource)
		{
			if (Util.SameFilePath(sourcePath, destPath))
			{
				throw new RrdException("Source and destination paths are the same");
			}
			using (RrdDb rrdSource = RrdDb.Open(sourcePath, true))
			{
				RrdDef rrdDef = rrdSource.GetRrdDef();
				rrdDef.Path = destPath;
				rrdDef.AddDatasource(newDatasource);
				using (RrdDb rrdDest = RrdDb.Create(rrdDef))
				{
					rrdSource.CopyStateTo(rrdDest);
				}
			}
		}

		/// <summary>
		///  <p>Adds one more datasource to a RRD file.</p>
		///  <p>WARNING: This method is potentialy dangerous! It will modify your RRD file.
		///  It is highly recommended to preserve the original RRD file (<i>saveBackup</i>
		///  should be set to <code>true</code>). The backup file will be created in the same
		///  directory as the original one with <code>.bak</code> extension added to the
		///  original name.</p>
		///  <p>Before applying this method, be sure that the specified RRD file is not in use
		///  (not open)</p>
		/// </summary>
		/// <param name="sourcePath">path to a RRD file to add datasource to.</param>
		/// <param name="newDatasource">Datasource definition to be added to the RRD file</param>
		/// <param name="saveBackup">true, if backup of the original file should be created; false, otherwise</param>
		public static void AddDatasource(String sourcePath, DsDef newDatasource, bool saveBackup)
		{
			String destPath = Util.GetTmpFilename();
			AddDatasource(sourcePath, destPath, newDatasource);
			CopyFile(destPath, sourcePath, saveBackup);
		}

		/// <summary>
		/// Creates a new RRD file with one datasource removed. RRD file is created based on the
		/// existing one (the original RRD file is not modified at all). All remaining data from
		/// the original RRD file is copied to the new one.
		/// </summary>
		/// <param name="sourcePath">path to a RRD file to import data from (will not be modified)</param>
		/// <param name="destPath">path to a new RRD file (will be created)</param>
		/// <param name="dsName">Name of the Datasource to be removed from the new RRD file</param>
		public static void RemoveDatasource(String sourcePath, String destPath, String dsName)
		{
			if (Util.SameFilePath(sourcePath, destPath))
			{
				throw new RrdException("Source and destination paths are the same");
			}
			using (RrdDb rrdSource = RrdDb.Open(sourcePath, true))
			{
				RrdDef rrdDef = rrdSource.GetRrdDef();
				rrdDef.Path = destPath;
				rrdDef.RemoveDatasource(dsName);
				using (RrdDb rrdDest = RrdDb.Create(rrdDef))
				{
					rrdSource.CopyStateTo(rrdDest);
				}
			}
		}

		/// <summary>
		/// <p>Removes single datasource from a RRD file.</p>
		/// <p>WARNING: This method is potentialy dangerous! It will modify your RRD file.
		/// It is highly recommended to preserve the original RRD file (<i>saveBackup</i>
		/// should be set to <code>true</code>). The backup file will be created in the same
		/// directory as the original one with <code>.bak</code> extension added to the
		/// original name.</p>
		/// <p>Before applying this method, be sure that the specified RRD file is not in use
		/// (not open)</p>
		/// </summary>
		/// <param name="sourcePath">path to a RRD file to remove datasource from.</param>
		/// <param name="dsName">Name of the Datasource to be removed from the RRD file</param>
		/// <param name="saveBackup">true, if backup of the original file should be created;false, otherwise</param>
		public static void RemoveDatasource(String sourcePath, String dsName, bool saveBackup)
		{
			String destPath = Util.GetTmpFilename();
			RemoveDatasource(sourcePath, destPath, dsName);
			CopyFile(destPath, sourcePath, saveBackup);
		}

		/// <summary>
		/// Renames single datasource in the given RRD file.
		/// </summary>
		/// <param name="sourcePath">Path to a RRD file</param>
		/// <param name="oldDsName">Old datasource name</param>
		/// <param name="newDsName">New datasource name</param>
		public static void RenameDatasource(String sourcePath, String oldDsName, String newDsName)
		{
			using (RrdDb rrd = RrdDb.Open(sourcePath))
			{
				if (rrd.ContainsDataSource(oldDsName))
				{
					DataSource datasource = rrd.GetDatasource(oldDsName);
					datasource.Name = newDsName;
				}
				else
				{
					throw new RrdException("Could not find datasource [" + oldDsName + "] in file " + sourcePath);
				}
			}
		}

		/// <summary>
		/// Updates single or all datasource names in the specified RRD file
		/// by appending '!' (if not already present). Datasources with names ending with '!'
		/// will never store NaNs in RRA archives (zero value will be used instead). Might be useful
		/// from time to time
		/// </summary>
		/// <param name="sourcePath">Path to a RRD file</param>
		/// <param name="dsName">Datasource name or null if you want to rename all datasources</param>
		/// <returns>Number of datasources successfully renamed</returns>
		public static int ForceZerosForNans(String sourcePath, String dsName)
		{
			using (RrdDb rrd = RrdDb.Open(sourcePath))
			{
				DataSource[] datasources;
				if (dsName == null)
				{
					datasources = rrd.DataSources;
				}
				else
				{
					if (rrd.ContainsDataSource(dsName))
					{
						datasources = new[] {rrd.GetDatasource(dsName)};
					}
					else
					{
						throw new RrdException("Could not find datasource [" + dsName + "] in file " + sourcePath);
					}
				}
				int count = 0;
				foreach (DataSource datasource in datasources)
				{
					String currentDsName = datasource.Name;
					if (!currentDsName.EndsWith(DsDef.FORCE_ZEROS_FOR_NANS_SUFFIX))
					{
						datasource.Name = currentDsName + DsDef.FORCE_ZEROS_FOR_NANS_SUFFIX;
						count++;
					}
				}
				return count;
			}
		}

		/// <summary>
		/// Creates a new RRD file with one more archive in it. RRD file is created based on the
		/// existing one (the original RRD file is not modified at all). All data from
		/// the original RRD file is copied to the new one.
		/// </summary>
		/// <param name="sourcePath">path to a RRD file to import data from (will not be modified)</param>
		/// <param name="destPath">path to a new RRD file (will be created)</param>
		/// <param name="newArchive">Archive definition to be added to the new RRD file</param>
		public static void AddArchive(String sourcePath, String destPath, ArcDef newArchive)
		{
			if (Util.SameFilePath(sourcePath, destPath))
			{
				throw new RrdException("Source and destination paths are the same");
			}
			using (RrdDb rrdSource = RrdDb.Open(sourcePath))
			{
				RrdDef rrdDef = rrdSource.GetRrdDef();
				rrdDef.Path = destPath;
				rrdDef.AddArchive(newArchive);
				using (RrdDb rrdDest = RrdDb.Create(rrdDef))
				{
					rrdSource.CopyStateTo(rrdDest);
				}
			}
		}

		/// <summary>
		/// <p>Adds one more archive to a RRD file.</p>
		/// <p>WARNING: This method is potentialy dangerous! It will modify your RRD file.
		/// It is highly recommended to preserve the original RRD file (<i>saveBackup</i>
		/// should be set to <code>true</code>). The backup file will be created in the same
		/// directory as the original one with <code>.bak</code> extension added to the
		/// original name.</p>
		/// <p>Before applying this method, be sure that the specified RRD file is not in use
		/// (not open)</p>
		/// </summary>
		/// <param name="sourcePath">path to a RRD file to add datasource to.</param>
		/// <param name="newArchive">Archive definition to be added to the RRD file</param>
		/// <param name="saveBackup">true, if backup of the original file should be created;false, otherwise</param>
		public static void AddArchive(String sourcePath, ArcDef newArchive, bool saveBackup)
		{
			String destPath = Util.GetTmpFilename();
			AddArchive(sourcePath, destPath, newArchive);
			CopyFile(destPath, sourcePath, saveBackup);
		}

		/// <summary>
		/// Creates a new RRD file with one archive removed. RRD file is created based on the
		/// existing one (the original RRD file is not modified at all). All relevant data from
		/// the original RRD file is copied to the new one.
		/// </summary>
		/// <param name="sourcePath">path to a RRD file to import data from (will not be modified)</param>
		/// <param name="destPath">path to a new RRD file (will be created)</param>
		/// <param name="consolFun">Consolidation function of Archive which should be removed</param>
		/// <param name="steps">Number of steps for Archive which should be removed</param>
		public static void RemoveArchive(String sourcePath, String destPath, ConsolidationFunction consolFun, int steps)
		{
			if (Util.SameFilePath(sourcePath, destPath))
			{
				throw new RrdException("Source and destination paths are the same");
			}
			using (RrdDb rrdSource = RrdDb.Open(sourcePath))
			{
				RrdDef rrdDef = rrdSource.GetRrdDef();
				rrdDef.Path = destPath;
				rrdDef.RemoveArchive(consolFun, steps);
				using (RrdDb rrdDest = RrdDb.Create(rrdDef))
				{
					rrdSource.CopyStateTo(rrdDest);
				}
			}
		}

		/// <summary>
		/// <p>Removes one archive from a RRD file.</p>
		/// <p>WARNING: This method is potentialy dangerous! It will modify your RRD file.
		/// It is highly recommended to preserve the original RRD file (<i>saveBackup</i>
		/// should be set to <code>true</code>). The backup file will be created in the same
		/// directory as the original one with <code>.bak</code> extension added to the
		/// original name.</p>
		/// <p>Before applying this method, be sure that the specified RRD file is not in use
		/// (not open)</p>
		/// </summary>
		/// <param name="sourcePath">path to a RRD file to add datasource to.</param>
		/// <param name="consolFun">Consolidation function of Archive which should be removed</param>
		/// <param name="steps">Number of steps for Archive which should be removed</param>
		/// <param name="saveBackup">true, if backup of the original file should be created;false, otherwise</param>
		public static void RemoveArchive(String sourcePath, ConsolidationFunction consolFun, int steps, bool saveBackup)
		{
			String destPath = Util.GetTmpFilename();
			RemoveArchive(sourcePath, destPath, consolFun, steps);
			CopyFile(destPath, sourcePath, saveBackup);
		}

		private static void CopyFile(String sourcePath, String destPath, bool saveBackup)
		{
			if (saveBackup)
			{
				String backupPath = GetBackupPath(destPath);
				if (Util.FileExists(backupPath))
					File.Delete(backupPath);
				File.Copy(sourcePath, backupPath);
			}
			if (Util.FileExists(destPath))
				File.Delete(destPath);
			File.Move(sourcePath, destPath);
		}

		private static String GetBackupPath(String destPath)
		{
			var sb = new StringBuilder(destPath);
			do
			{
				sb.Append(".bak");
			} while (Util.FileExists(sb.ToString()));
			return sb.ToString();
		}

		/// <summary>
		/// Sets datasource heartbeat to a new value.
		/// </summary>
		/// <param name="sourcePath">Path to exisiting RRD file (will be updated)</param>
		/// <param name="datasourceName">Name of the datasource in the specified RRD file</param>
		/// <param name="newHeartbeat">New datasource heartbeat</param>
		public static void SetDsHeartbeat(String sourcePath, String datasourceName, long newHeartbeat)
		{
			using (RrdDb rrd = RrdDb.Open(sourcePath))
			{
				DataSource ds = rrd.GetDatasource(datasourceName);
				ds.Heartbeat = newHeartbeat;
			}
		}

		/// <summary>
		/// Sets datasource heartbeat to a new value.
		/// </summary>
		/// <param name="sourcePath">Path to exisiting RRD file (will be updated)</param>
		/// <param name="dsIndex">Index of the datasource in the specified RRD file</param>
		/// <param name="newHeartbeat">New datasource heartbeat</param>
		public static void SetDsHeartbeat(String sourcePath, int dsIndex, long newHeartbeat)
		{
			using (RrdDb rrd = RrdDb.Open(sourcePath))
			{
				DataSource ds = rrd.GetDatasource(dsIndex);
				ds.Heartbeat = newHeartbeat;
			}
		}

		/// <summary>
		/// Sets datasource min value to a new value
		/// </summary>
		/// <param name="sourcePath">Path to exisiting RRD file (will be updated)</param>
		/// <param name="datasourceName">Name of the datasource in the specified RRD file</param>
		/// <param name="newMinValue"> New min value for the datasource</param>
		/// <param name="filterArchivedValues">set to <code>true</code> if archived values less than <code>newMinValue</code> should be set to NaN; set to false, otherwise.</param>
		public static void SetDsMinValue(String sourcePath, String datasourceName,
		                                 double newMinValue, bool filterArchivedValues)
		{
			using (RrdDb rrd = RrdDb.Open(sourcePath))
			{
				DataSource ds = rrd.GetDatasource(datasourceName);
				ds.SetMinValue(newMinValue, filterArchivedValues);
			}
		}

		/// <summary>
		/// Sets datasource max value to a new value
		/// </summary>
		/// <param name="sourcePath">Path to exisiting RRD file (will be updated)</param>
		/// <param name="datasourceName">Name of the datasource in the specified RRD file</param>
		/// <param name="newMaxValue"> New max value for the datasource</param>
		/// <param name="filterArchivedValues">set to <code>true</code> if archived values greater than <code>newMaxValue</code> should be set to NaN; set to false, otherwise.</param>
		public static void SetDsMaxValue(String sourcePath, String datasourceName,
													double newMaxValue, bool filterArchivedValues)
		{
			using (RrdDb rrd = RrdDb.Open(sourcePath))
			{
				DataSource ds = rrd.GetDatasource(datasourceName);
				ds.SetMaxValue(newMaxValue, filterArchivedValues);
			}
		}

		/// <summary>
		/// Updates valid value range for the given datasource.
		/// </summary>
		/// <param name="sourcePath">Path to exisiting RRD file (will be updated)</param>
		/// <param name="datasourceName">Name of the datasource in the specified RRD file</param>
		/// <param name="newMinValue">New min value for the datasource</param>
		/// <param name="newMaxValue">New max value for the datasource</param>
		/// <param name="filterArchivedValues">set to <code>true</code> if archived values outside of the specified min/max range should be replaced with NaNs.</param>
		public static void SetDsMinMaxValue(String sourcePath, String datasourceName,
		                                    double newMinValue, double newMaxValue, bool filterArchivedValues)
		{
			using (RrdDb rrd = RrdDb.Open(sourcePath))
			{
				DataSource ds = rrd.GetDatasource(datasourceName);
				ds.SetMinMaxValue(newMinValue, newMaxValue, filterArchivedValues);
			}
		}

		/// <summary>
		/// Sets single archive's X-files factor to a new value.
		/// </summary>
		/// <param name="sourcePath">Path to existing RRD file (will be updated)</param>
		/// <param name="consolFun">Consolidation function of the target archive</param>
		/// <param name="steps">Number of steps of the target archive</param>
		/// <param name="newXff">New X-files factor for the target archive</param>
		public static void SetArcXff(String sourcePath, ConsolidationFunction consolFun, int steps,
		                             double newXff)
		{
			using (RrdDb rrd = RrdDb.Open(sourcePath))
			{
				Archive arc = rrd.GetArchive(consolFun, steps);
				arc.Xff = newXff;
			}
		}

		/**
	 * Creates new RRD file based on the existing one, but with a different
	 * size (number of rows) for a single archive. The archive to be resized
	 * is identified by its consolidation function and the number of steps.
	 *
	 * @param sourcePath Path to the source RRD file (will not be modified)
	 * @param destPath   Path to the new RRD file (will be created)
	 * @param consolFun  Consolidation function of the archive to be resized
	 * @param numSteps   Number of steps of the archive to be resized
	 * @param newRows	New archive size (number of archive rows)
	 * @throws IOException  Thrown in case of I/O error
	 * @throws RrdException Thrown in case of JRobin specific error
	 */

		public static void ResizeArchive(String sourcePath, String destPath, ConsolidationFunction consolFun,
		                                 int numSteps, int newRows)
		{
			if (Util.SameFilePath(sourcePath, destPath))
			{
				throw new RrdException("Source and destination paths are the same");
			}
			if (newRows < 2)
			{
				throw new RrdException("New arcihve size must be at least 2");
			}
			using (RrdDb rrdSource = RrdDb.Open(sourcePath))
			{
				RrdDef rrdDef = rrdSource.GetRrdDef();
				ArcDef arcDef = rrdDef.FindArchive(consolFun, numSteps);
				if (arcDef.Rows != newRows)
				{
					arcDef.Rows = newRows;
					rrdDef.Path = destPath;
					using (RrdDb rrdDest = RrdDb.Create(rrdDef))
					{
						rrdSource.CopyStateTo(rrdDest);
					}
				}
			}
		}

		/// <summary>
		/// Modifies existing RRD file, by resizing its chosen archive. The archive to be resized
		/// is identified by its consolidation function and the number of steps.
		/// </summary>
		/// <param name="sourcePath">Path to the RRD file (will be modified)</param>
		/// <param name="consolFun">Consolidation function of the archive to be resized</param>
		/// <param name="numSteps">Number of steps of the archive to be resized</param>
		/// <param name="newRows">New archive size (number of archive rows)</param>
		/// <param name="saveBackup">true, if backup of the original file should be created;false, otherwise</param>
		public static void ResizeArchive(String sourcePath, ConsolidationFunction consolFun,
		                                 int numSteps, int newRows, bool saveBackup)
		{
			String destPath = Util.GetTmpFilename();
			ResizeArchive(sourcePath, destPath, consolFun, numSteps, newRows);
			CopyFile(destPath, sourcePath, saveBackup);
		}

		/// <summary>
		/// Splits single RRD file with several datasources into a number of smaller RRD files
		/// with a single datasource in it. All archived values are preserved. If
		/// you have a RRD file named 'traffic.rrd' with two datasources, 'in' and 'out', this
		/// method will create two files (with a single datasource, in the same directory)
		/// named 'in-traffic.rrd' and 'out-traffic.rrd'.
		/// </summary>
		/// <param name="sourcePath">Path to a RRD file with multiple datasources defined</param>
		public static void Split(String sourcePath)
		{
			using (RrdDb rrdSource = RrdDb.Open(sourcePath))
			{
				String[] dsNames = rrdSource.DataSourceNames;
				foreach (String dsName in dsNames)
				{
					RrdDef rrdDef = rrdSource.GetRrdDef();
					rrdDef.Path = CreateSplitPath(dsName, sourcePath);
					rrdDef.SaveSingleDatasource(dsName);
					using (RrdDb rrdDest = RrdDb.Create(rrdDef))
					{
						rrdSource.CopyStateTo(rrdDest);
					}
				}
			}
		}

		/// <summary>
		/// Returns list of canonical file names with the specified extension in the given directory. This
		/// method is not RRD related, but might come handy to create a quick list of all RRD files
		/// in the given directory.
		/// </summary>
		/// <param name="directory">Source directory</param>
		/// <param name="extension">File extension (like ".rrd", ".jrb", ".rrd.jrb")</param>
		/// <param name="resursive">true if all subdirectories should be traversed for the same extension, false otherwise</param>
		/// <returns></returns>
		public static String[] GetCanonicalPaths(String directory, String extension, bool resursive)
		{
			if (!Directory.Exists(directory))
			{
				throw new IOException("Not a directory: " + directory);
			}
			var fileList = new List<String>();
			TraverseDirectory(directory, extension, resursive, fileList);
			fileList.Sort();
			String[] result = fileList.ToArray();
			return result;
		}

		private static void TraverseDirectory(string directory, String extension, bool recursive, List<String> list)
		{
			list.AddRange(Directory.GetFiles("*." + extension).Select(Util.GetCanonicalPath));

			if (recursive)
			{
				foreach (string subDir in Directory.GetDirectories(directory))
				{
					TraverseDirectory(subDir, extension, true, list);
				}
			}
		}

		private static String CreateSplitPath(String dsName, String sourcePath)
		{
			String newName = dsName + "-" + Path.GetFileName(sourcePath);
			String parentDir = Path.GetDirectoryName(sourcePath);

			return parentDir != null ? Path.Combine(parentDir, newName) : null;
		}
	}
}