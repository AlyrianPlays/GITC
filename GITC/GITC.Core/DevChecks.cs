using System;
using System.Collections.Generic;

namespace GITC.Core
{
    //TESTING, REMOVE AFTER: startup self-checks for the data layer. Set env var GITC_DEVCHECKS=1 to also echo Debug output to the console.
    internal static class DevChecks
    {
        public static void Run()
        {
            if (Environment.GetEnvironmentVariable("GITC_DEVCHECKS") == "1")
                System.Diagnostics.Trace.Listeners.Add(new System.Diagnostics.ConsoleTraceListener());

            // T-2.1: sample JSON loads and a field prints
            Dictionary<string, string> test = DataLoader.Load<Dictionary<string, string>>("test.json");
            System.Diagnostics.Debug.WriteLine("DevChecks test.json message = " + (test != null && test.ContainsKey("message") ? test["message"] : "<null>"));
            System.Diagnostics.Debug.WriteLine("DevChecks missing file returns null = " + (DataLoader.Load<Dictionary<string, string>>("nope.json") == null));

            // T-2.2: a skeleton company mission file and a fully populated MissionDef round-trip
            CompanyMissions swiftail = DataLoader.Load<CompanyMissions>("companies/swiftail.json");
            System.Diagnostics.Debug.WriteLine("DevChecks swiftail skeleton = " + (swiftail != null ? swiftail.companyId + ", missions=" + swiftail.missions.Count : "<null>"));
            MissionDef sample = new MissionDef() { id = "sample", modules = { new ModuleConfig() { module = "vm", vitals = new VitalsConfig() } } };
            MissionDef back = System.Text.Json.JsonSerializer.Deserialize<MissionDef>(System.Text.Json.JsonSerializer.Serialize(sample, DataLoader.options), DataLoader.options);
            System.Diagnostics.Debug.WriteLine("DevChecks MissionDef round-trip = " + (back != null && back.modules.Count == 1 && back.modules[0].vitals.heartRate.safeMax == 110f));
        }
    }
}
