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

            // T-4.4: the NES palette loads and every company colour is an entry of it
            System.Diagnostics.Debug.WriteLine("DevChecks palette entries = " + Palette.nes.Count);
            if (companies != null)
            {
                System.Diagnostics.Debug.WriteLine("DevChecks company colours = " + string.Join(", ", companies.ConvertAll(c => c.id + " " + c.color + (Palette.Contains(Palette.ParseHex(c.color)) ? " ok" : " OFF-PALETTE"))));
                System.Diagnostics.Debug.WriteLine("DevChecks off-palette companies = " + Palette.ValidateCompanies(companies));
            }

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

            // T-5.1: typed characters reach the TIM through InputManager.typedChars and echo works
            Mission typing = new Mission(game, new MissionDef() { id = "debug_typing", durationSeconds = 60 });
            TIMModule tim = (TIMModule)typing.moduleManager.Get(ModuleTypes.tim);
            foreach (char c in "echo hi")
                game.currGameState.input.typedChars.Enqueue(new Microsoft.Xna.Framework.TextInputEventArgs(c, Microsoft.Xna.Framework.Input.Keys.None));
            game.currGameState.input.typedChars.Enqueue(new Microsoft.Xna.Framework.TextInputEventArgs('\r', Microsoft.Xna.Framework.Input.Keys.Enter));
            tim.HandleInput(game.currGameState.input);
            game.currGameState.input.ClearTyped();
            System.Diagnostics.Debug.WriteLine("DevChecks TIM history [0]='" + tim.history[0] + "' [1]='" + tim.history[1] + "'");

            // T-5.2: wrapping, backspace, command recall, scrollback
            int before = tim.history.Count;
            tim.SubmitLine("echo " + new string('x', 200));
            System.Diagnostics.Debug.WriteLine("DevChecks TIM wrap: " + (tim.history.Count - before) + " lines added (expect 6), newest length " + tim.history[0].Length + " (expect 14)");
            foreach (char c in "abc")
                game.currGameState.input.typedChars.Enqueue(new Microsoft.Xna.Framework.TextInputEventArgs(c, Microsoft.Xna.Framework.Input.Keys.None));
            game.currGameState.input.typedChars.Enqueue(new Microsoft.Xna.Framework.TextInputEventArgs((char)8, Microsoft.Xna.Framework.Input.Keys.Back));
            tim.HandleInput(game.currGameState.input);
            game.currGameState.input.ClearTyped();
            System.Diagnostics.Debug.WriteLine("DevChecks TIM backspace: '" + tim.inputText + "' (expect 'ab')");
            tim.inputText.Clear();
            tim.SubmitLine("echo a");
            tim.SubmitLine("echo b");
            tim.inputText.Append("draft");
            tim.Recall(1); string r1 = tim.inputText.ToString();
            tim.Recall(1); string r2 = tim.inputText.ToString();
            tim.Recall(-1); string r3 = tim.inputText.ToString();
            tim.Recall(-1); string r4 = tim.inputText.ToString();
            System.Diagnostics.Debug.WriteLine("DevChecks TIM recall: '" + r1 + "', '" + r2 + "', '" + r3 + "', '" + r4 + "' (expect 'echo b', 'echo a', 'echo b', 'draft')");
            tim.inputText.Clear();

            // T-5.3: commands run through the registry
            tim.ClearHistory();
            tim.SubmitLine("echo hello world");
            string echoOut = tim.history[0];
            tim.SubmitLine("ECHO   spaced   out");
            string echoOut2 = tim.history[0];
            tim.SubmitLine("nosuchcommand");
            string unknownOut = tim.history[0];
            tim.SubmitLine("whispers");
            string noHandlerOut = tim.history[0];
            tim.SubmitLine("help");
            int helpLines = 0;
            foreach (string h in tim.history) { if (h.StartsWith("echo ") || h.StartsWith("help ") || h.StartsWith("clear ")) helpLines++; }
            tim.scope = "overworld";
            tim.SubmitLine("help");
            int overworldHelp = tim.history.Count;
            tim.scope = "mission";
            tim.SubmitLine("listdir");
            string listOut = tim.history[0];
            tim.SubmitLine("cd somewhere");
            string cdOut = tim.history[0];
            tim.SubmitLine("clear");
            System.Diagnostics.Debug.WriteLine("DevChecks TIM T-5.4: listdir='" + listOut + "', cd='" + cdOut + "', help lines in overworld scope=" + overworldHelp);
            System.Diagnostics.Debug.WriteLine("DevChecks TIM commands: echo='" + echoOut + "', echo2='" + echoOut2 + "', unknown='" + unknownOut + "', nohandler='" + noHandlerOut + "', help lists echo/help/clear=" + helpLines + ", after clear history=" + tim.history.Count);
            for (int i = 0; i < 40; i++)
                tim.AddHistoryLines("line " + i);
            tim.Scroll(1000);
            System.Diagnostics.Debug.WriteLine("DevChecks TIM scroll clamp: " + tim.scrollOffset + " of " + (tim.history.Count - TIMModule.CONSOLEHISTORY_SIZE) + " max");
        }
    }
}
