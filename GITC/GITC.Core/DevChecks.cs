using System;
using System.Collections.Generic;

namespace GITC.Core
{
    //TESTING, REMOVE AFTER: startup self-checks for the data layer. Set env var GITC_DEVCHECKS=1 to also echo Debug output to the console.
    internal static class DevChecks
    {
        private static void SimulateSeconds(GITCGame game, Mission mission, int seconds)
        {
            for (int s = 1; s <= seconds; s++)
                mission.Update(new Microsoft.Xna.Framework.GameTime(TimeSpan.FromSeconds(s), TimeSpan.FromSeconds(1)), game.currGameState.input);
        }

        public static void Run(GITCGame currGame)
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

            // T-2.3: all 8 companies load and match the Companies enum
            List<Company> companies = DataLoader.Load<List<Company>>("companies.json");
            System.Diagnostics.Debug.WriteLine("DevChecks companies = " + (companies != null ? companies.Count + ": " + string.Join(", ", companies.ConvertAll(c => c.name + (c.isPrior ? " (Prior)" : "") + "/" + c.ceo)) : "<null>"));
            if (companies != null)
                foreach (Company c in companies)
                    if (!Enum.TryParse<Companies>(c.id, out _))
                        System.Diagnostics.Debug.WriteLine("DevChecks company id not in Companies enum: " + c.id);

            // T-4.1: a mission is won at the end of its timer and lost at zero OPs (simulated, 1 second per frame)
            GITCGame game = currGame;
            Mission win = new Mission(game, new MissionDef() { id = "debug_win", durationSeconds = 5, startingOps = 30 });
            win.moduleManager.Raise(new ModuleEvent(ModuleEventTypes.op_penalty, ModuleTypes.pm, 10));
            SimulateSeconds(game, win, 10);
            System.Diagnostics.Debug.WriteLine("DevChecks mission win: " + win.status + ", OP=" + win.operatorPoints + ", time=" + win.timeRemaining);
            Mission lose = new Mission(game, new MissionDef() { id = "debug_lose", durationSeconds = 60, startingOps = 30 });
            for (int i = 0; i < 3; i++)
                lose.moduleManager.Raise(new ModuleEvent(ModuleEventTypes.op_penalty, ModuleTypes.pm, 10));
            SimulateSeconds(game, lose, 1);
            System.Diagnostics.Debug.WriteLine("DevChecks mission lose: " + lose.status + ", OP=" + lose.operatorPoints + ", time=" + lose.timeRemaining);
        }
    }
}
