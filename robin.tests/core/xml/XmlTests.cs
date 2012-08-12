using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using robin.core;

namespace robin.tests.core.xml
{
	[TestClass]
	public class XmlTests
	{
		[TestMethod]
		public void TestMethod1()
		{
		RrdDef def = new RrdDef("test");
			def.Step = 10;
			def.StartTime = DateTime.Now.GetTimestamp();
			def.AddDatasource(DsDef.FromRrdToolString("DS:input:GAUGE:600:0:U"));
			def.AddArchive(ArcDef.FromRrdToolString("RRA:AVERAGE:0.5:10:1000"));
			using(RrdDb db = RrdDb.Create(def))
			{
				string tmp = db.ToXml();
				using(RrdDb db2 = RrdDb.Import("test2", new XmlImporter(tmp)))
				{
					Assert.AreEqual(db.Header.Info, db2.Header.Info);
					Assert.AreEqual(db.Header.LastUpdateTime, db2.Header.LastUpdateTime);
					Assert.AreEqual(db.Header.ArchiveCount, db2.Header.ArchiveCount);
					Assert.AreEqual(db.Header.DataSourceCount, db2.Header.DataSourceCount);
					Assert.AreEqual(db.Header.Step, db2.Header.Step);
					Assert.AreEqual(db.LastUpdateTime,db2.LastUpdateTime);
					foreach (DataSource dataSource in db.DataSources)
					{
						DataSource ds2 = db2.GetDatasource(dataSource.Name);
						Assert.AreEqual(dataSource.AccumulatedValue,ds2.AccumulatedValue);
						Assert.AreEqual(dataSource.Heartbeat, ds2.Heartbeat);
						Assert.AreEqual(dataSource.Index, ds2.Index);
						Assert.AreEqual(dataSource.LastValue, ds2.LastValue);
						Assert.AreEqual(dataSource.MaxValue, ds2.MaxValue);
						Assert.AreEqual(dataSource.MinValue, ds2.MinValue);
						Assert.AreEqual(dataSource.Name, ds2.Name);
						Assert.AreEqual(dataSource.NanSeconds, ds2.NanSeconds);
						Assert.AreEqual(dataSource.Type, ds2.Type);
					}

					foreach (Archive archive in db.Archives)
					{
						Archive a2 = db2.GetArchive(archive.ConsolidationFunction,archive.Steps);
						Assert.AreEqual(archive.ConsolidationFunction, a2.ConsolidationFunction);
						Assert.AreEqual(archive.Rows, a2.Rows);
						Assert.AreEqual(archive.Steps, a2.Steps);
						Assert.AreEqual(archive.TimeStep, a2.TimeStep);
						Assert.AreEqual(archive.Xff, a2.Xff);

						for (int i = 0; i < db.Header.DataSourceCount; i++)
						{
							Robin r1 = archive.GetRobin(i);
							Robin r2 = a2.GetRobin(i);

							double[] v1 = r1.GetValues();
							double[] v2 = r2.GetValues();
							for (int j = 0; j < v1.Length; j++)
							{
								Assert.AreEqual(v1[j],v2[j]);
							}
						}
					}
				}
			}
		}
	}
}
