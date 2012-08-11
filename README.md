robin.net
=========

RRDTool for C# (port of [jrobin.org] 1.5.14)

Creating rrd files and updates values is working fine, the graph part still need some works.

Example :

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
        
        //Use data to retrieve values...
    }
    

