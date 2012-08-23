using System;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using robin.core;
using System.IO;
using robin.data;

namespace robin.tests.core
{
	[TestClass]
	public class BasicTests
	{
		private const string SAMPLE = "path.rrd";
		
		[TestMethod]
		[ExpectedException(typeof(RrdException))]
		public void CreateDbWithoutDatasourceThrowException()
		{
			RrdDef def = new RrdDef(@"c:\doesnotexists");
			RrdDb.Create(def); //No RRD datasource specified. At least one is needed.
		}

		[TestMethod]
		[ExpectedException(typeof(RrdException))]
		public void CreateDbWithoutArchiveThrowException()
		{
			RrdDef def = new RrdDef(@"c:\doesnotexists");
			def.AddDatasource("test",DataSourceType.GAUGE,100,0,100);
			RrdDb.Create(def); //No RRD Archive specified. At least one is needed.
		}

		[TestMethod]
		public void RrdDefCreation()
		{
			RrdDef def = new RrdDef("test");
			Assert.AreEqual("test", def.Path);
			Assert.AreEqual(0, def.DataSourceDefinitions.Length);
			Assert.AreEqual(0, def.ArchiveDefinitions.Length);

			def.AddDatasource("test", DataSourceType.GAUGE, 10, 0, 100);
			def.AddDatasource("test2", DataSourceType.DERIVE, 10, 0, 100);
			Assert.AreEqual(2, def.DataSourceDefinitions.Length);
			Assert.AreEqual("test",def.DataSourceDefinitions[0].Name);
			Assert.AreEqual(DataSourceType.GAUGE, def.DataSourceDefinitions[0].Type);
			Assert.AreEqual(10, def.DataSourceDefinitions[0].Heartbeat);
			Assert.AreEqual(0, def.DataSourceDefinitions[0].MinValue); 
			Assert.AreEqual(100, def.DataSourceDefinitions[0].MaxValue);

			def.AddArchive(ConsolidationFunction.MIN,0.5,2,3);
			Assert.AreEqual(1, def.ArchiveDefinitions.Length);
			Assert.AreEqual(0.5,def.ArchiveDefinitions[0].Xff);
			Assert.AreEqual(2, def.ArchiveDefinitions[0].Steps);
			Assert.AreEqual(3, def.ArchiveDefinitions[0].Rows);

			def.RemoveDatasource("test");
			Assert.AreEqual(1, def.DataSourceDefinitions.Length);
			Assert.AreEqual("test2", def.DataSourceDefinitions[0].Name);

			def.RemoveAllDatasources();
			Assert.AreEqual(0, def.DataSourceDefinitions.Length);
			Assert.AreEqual(1, def.ArchiveDefinitions.Length);

			def.RemoveArchive(ConsolidationFunction.MIN,2);
			Assert.AreEqual(0, def.ArchiveDefinitions.Length);
		}

		[TestMethod]
		public void CreateDataSourceDefinitionFromRrdToolString()
		{
			DsDef ds = DsDef.FromRrdToolString("DS:input:GAUGE:600:0:U");
			Assert.AreEqual("input",ds.Name);
			Assert.AreEqual(DataSourceType.GAUGE, ds.Type);
			Assert.AreEqual(600, ds.Heartbeat);
			Assert.AreEqual(0, ds.MinValue);
			Assert.AreEqual(double.NaN, ds.MaxValue);
		}

		[TestMethod]
		public void RrdbInitializationTest()
		{
			DateTime start = new DateTime(2000, 2, 1, 2, 3, 4);
			RrdDef def = new RrdDef(SAMPLE);
			def.Step = 10;
			def.StartTime = start.GetTimestamp();
			def.AddDatasource(DsDef.FromRrdToolString("DS:input:GAUGE:600:0:U"));
			def.AddArchive(ArcDef.FromRrdToolString("RRA:AVERAGE:0.5:10:1000"));
			using(RrdDb db = RrdDb.Create(def))
			{
				Assert.AreEqual(10,db.Header.Step);

				DateTime lastUpdate = db.LastUpdateTime.ToDateTime();

				Assert.AreEqual(2000, lastUpdate.Year);
				Assert.AreEqual(2, lastUpdate.Month);
				Assert.AreEqual(1, lastUpdate.Day);
				Assert.AreEqual(2, lastUpdate.Hour);
				Assert.AreEqual(3, lastUpdate.Minute);
				Assert.AreEqual(4, lastUpdate.Second);

				Assert.AreEqual(0, db.DataSources[0].AccumulatedValue);
				Assert.AreEqual(double.NaN, db.DataSources[0].LastValue);

				Archive a = db.Archives[0];

				//TODO: Add tests for StartTime/EndTime of Archive
				
			}

			if(File.Exists(SAMPLE))
				File.Delete(SAMPLE);
		}

		[TestMethod]
		public void TestConsolidationFunctionTest()
		{
			DateTime start = new DateTime(2000, 2, 1, 2, 3, 0); // Start at 0 seconds to align on the step parameter
			RrdDef def = new RrdDef(SAMPLE);
			def.Step = 10;
			def.StartTime = start.GetTimestamp();
			def.AddDatasource(DsDef.FromRrdToolString("DS:input:GAUGE:600:0:U"));
			def.AddArchive(ArcDef.FromRrdToolString("RRA:AVERAGE:0.5:2:1000"));
			def.AddArchive(ArcDef.FromRrdToolString("RRA:MAX:0.5:2:1000"));
			def.AddArchive(ArcDef.FromRrdToolString("RRA:MIN:0.5:2:1000"));
			def.AddArchive(ArcDef.FromRrdToolString("RRA:LAST:0.5:2:1000"));

			using (RrdDb db = RrdDb.Create(def))
			{
				DateTime sampleDate = start.AddSeconds(def.Step);
				Sample sample = db.CreateSample(sampleDate.GetTimestamp());
				sample.Values = new double[] { 10 };
				sample.Update();

				DateTime lastUpdate = db.LastUpdateTime.ToDateTime();
				Assert.AreEqual(sampleDate,lastUpdate);
				Assert.AreEqual(10,db.DataSources[0].LastValue);
				Assert.AreEqual(10, db.Archives[0].GetArcState(0).AccumulatedValue);

				sampleDate = sampleDate.AddSeconds(def.Step);
				sample.Time = sampleDate.GetTimestamp();
				sample.Values = new double[] { 20 };
				sample.Update();

				lastUpdate = db.LastUpdateTime.ToDateTime();
				Assert.AreEqual(sampleDate, lastUpdate);
				Assert.AreEqual(20, db.DataSources[0].LastValue);
				Assert.AreEqual(double.NaN, db.Archives[0].GetArcState(0).AccumulatedValue);
				Assert.AreEqual(15.0, db.Archives[0].GetRobin(0).GetValue(-1)); // Check average just compiled value
				Assert.AreEqual(20.0, db.Archives[1].GetRobin(0).GetValue(-1)); // Check max value
				Assert.AreEqual(10.0, db.Archives[2].GetRobin(0).GetValue(-1)); // Check min value
				Assert.AreEqual(20.0, db.Archives[3].GetRobin(0).GetValue(-1)); // Check last value
				
				Assert.AreEqual(20,db.Archives[0].TimeStep);
			}
			if (File.Exists(SAMPLE))
				File.Delete(SAMPLE);
		}

		[TestMethod]
		public void TestCounterFromTutorial()
		{
			RrdDef def = new RrdDef(SAMPLE);
			def.StartTime = 920804400;
			def.AddDatasource(DsDef.FromRrdToolString("DS:speed:COUNTER:600:U:U"));
			def.AddArchive(ArcDef.FromRrdToolString("RRA:AVERAGE:0.5:1:24"));
			def.AddArchive(ArcDef.FromRrdToolString("RRA:AVERAGE:0.5:6:10"));

			using (RrdDb db = RrdDb.Create(def))
			{
				Sample sample = db.CreateSample();
				sample.SetAndUpdate("920804700:12345", "920805000:12357", "920805300:12363");
				sample.SetAndUpdate("920805600:12363", "920805900:12363 ", "920806200:12373");
				sample.SetAndUpdate("920806500:12383", "920806800:12393", "920807100:12399");
				sample.SetAndUpdate("920807400:12405", "920807700:12411", "920808000:12415");
				sample.SetAndUpdate("920808300:12420", "920808600:12422", "920808900:12423");

				FetchRequest req = db.CreateFetchRequest(ConsolidationFunction.AVERAGE, 920804400, 920809200);
				FetchData data = db.FetchData(req);
				
				Assert.AreEqual(920804400, data.Timestamps[0]); // We have this additional that is not present 
				Assert.AreEqual(double.NaN, data.Values[0][0]); // in the tutorial, but it looks correct

				Assert.AreEqual(920804700, data.Timestamps[1]);
				Assert.AreEqual(double.NaN, data.Values[0][1]);

				Assert.AreEqual(920805000, data.Timestamps[2]);
				Assert.AreEqual(4.0000000000e-02, data.Values[0][2]);

				Assert.AreEqual(920805300, data.Timestamps[3]);
				Assert.AreEqual(2.0000000000e-02, data.Values[0][3]);

				Assert.AreEqual(920805600, data.Timestamps[4]);
				Assert.AreEqual(0.0000000000e+00, data.Values[0][4]);

				Assert.AreEqual(920805900, data.Timestamps[5]);
				Assert.AreEqual(0.0000000000e+00, data.Values[0][5]);

				Assert.AreEqual(920806200, data.Timestamps[6]);
				Assert.AreEqual(0.03333, Math.Round(data.Values[0][6], 5));

				Assert.AreEqual(920806500, data.Timestamps[7]);
				Assert.AreEqual(0.03333, Math.Round(data.Values[0][7], 5));

				Assert.AreEqual(920806800, data.Timestamps[8]);
				Assert.AreEqual(0.03333, Math.Round(data.Values[0][8], 5));

				Assert.AreEqual(920807100, data.Timestamps[9]);
				Assert.AreEqual(2.0000000000e-02, data.Values[0][9]);

				Assert.AreEqual(920807400, data.Timestamps[10]);
				Assert.AreEqual(2.0000000000e-02, data.Values[0][10]);

				Assert.AreEqual(920807700, data.Timestamps[11]);
				Assert.AreEqual(2.0000000000e-02, data.Values[0][11]);

				Assert.AreEqual(920808000, data.Timestamps[12]);
				Assert.AreEqual(0.01333, Math.Round(data.Values[0][12], 5));

				Assert.AreEqual(920808300, data.Timestamps[13]);
				Assert.AreEqual(0.01667, Math.Round(data.Values[0][13], 5));

				Assert.AreEqual(920808600, data.Timestamps[14]);
				Assert.AreEqual(0.00667, Math.Round(data.Values[0][14], 5));

				Assert.AreEqual(920808900, data.Timestamps[15]);
				Assert.AreEqual(0.00333, Math.Round(data.Values[0][15], 5));

				Assert.AreEqual(920809200, data.Timestamps[16]);
				Assert.AreEqual(double.NaN, data.Values[0][16]);

				Assert.AreEqual(17, data.Values[0].Length);
			}

			if (File.Exists(SAMPLE))
				File.Delete(SAMPLE);
		}

		[TestMethod]
		public void TestCounter2()
		{
			DateTime start = new DateTime(2012, 08, 22);

			RrdDef def = new RrdDef(SAMPLE,start.GetTimestamp(),60);
			def.AddDatasource(DsDef.FromRrdToolString("DS:speed:COUNTER:60:U:U")); // Step : every minute
			def.AddArchive(ConsolidationFunction.AVERAGE, 0, 5, 12 * 24 * 30);   // Archive average every 5 minutes during 30 days
			def.AddArchive(ConsolidationFunction.AVERAGE, 0, 5 * 12, 24 * 30);   // Archive average every hour during 30 days

			start = start.AddSeconds(40);
			using (RrdDb db = RrdDb.Create(def))
			{
				Sample sample = db.CreateSample();
				for (int i = 1; i < 60 * 24 * 3; i++) // add 3 days of samples
				{
					sample.Set(start.AddMinutes(i), 100*i);
					sample.Update();
				}

				FetchRequest request = db.CreateFetchRequest(ConsolidationFunction.AVERAGE,
				                                             start.AddHours(3).GetTimestamp(),
				                                             start.AddHours(13).GetTimestamp(),
				                                             3600);
				FetchData data = request.FetchData();
				Assert.AreEqual(3600, data.MatchingArchive.TimeStep);
				Assert.AreEqual(60, data.MatchingArchive.Steps);
				Assert.AreEqual(12, data.RowCount);

				start = new DateTime(2012, 08, 22);
				FetchRequest r2 = db.CreateFetchRequest(ConsolidationFunction.AVERAGE,
															start.AddHours(3).GetTimestamp(),
															start.AddHours(13).GetTimestamp(),
															300);
				FetchData data2 = r2.FetchData();
				Debug.WriteLine(data2.Dump());
				Assert.AreEqual(300, data2.MatchingArchive.TimeStep);
				Assert.AreEqual(5, data2.MatchingArchive.Steps);
				Assert.AreEqual(121, data2.RowCount);
				

			}
		}

		[TestMethod]
		public void TestDataProcessor()
		{
			DateTime start = new DateTime(2012, 08, 22);

			RrdDef def = new RrdDef(SAMPLE, start.GetTimestamp(), 60);
			def.AddDatasource("speed",DataSourceType.COUNTER,120,double.NaN,double.NaN); // Step : every minute
			def.AddArchive(ConsolidationFunction.AVERAGE, 0, 5, 12 * 24 * 30);   // Archive average every 5 minutes during 30 days
			def.AddArchive(ConsolidationFunction.AVERAGE, 0, 5 * 12, 24 * 30);   // Archive average every hour during 30 days

			start = start.AddSeconds(40);
			using (RrdDb db = RrdDb.Create(def))
			{
				Sample sample = db.CreateSample();
				for (int i = 1; i < 60 * 24 * 3; i++) // add 3 days of samples
				{
					sample.Set(start.AddMinutes(i), 100 * i);
					sample.Update();
				}
			}

			DataProcessor dataProcessor = new DataProcessor(start.AddHours(3), start.AddHours(13));
			dataProcessor.FetchRequestResolution = 3600;
			dataProcessor.AddDatasource("speed", SAMPLE, "speed", ConsolidationFunction.AVERAGE);
			dataProcessor.AddDatasource("speedByHour", "speed, STEP, *");

			dataProcessor.ProcessData();

			double[] vals = dataProcessor.GetValues("speedByHour");
			Assert.AreEqual(12, vals.Length);
			for (int i = 0; i < vals.Length; i++)
			{
				Assert.AreEqual(6000,((int)vals[i]));
			}
		}

	}
}

