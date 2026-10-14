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
            tim.SubmitLine("clear");
            System.Diagnostics.Debug.WriteLine("DevChecks TIM T-5.4: help lines in overworld scope=" + overworldHelp);

            // T-5.7: typing hook and scroll indicator state
            string hookKeys = "";
            tim.onKeyTyped = k => hookKeys += k == (char)8 ? "<B>" : (k == (char)10 ? "<E>" : k.ToString());
            foreach (char c in "hi")
                game.currGameState.input.typedChars.Enqueue(new Microsoft.Xna.Framework.TextInputEventArgs(c, Microsoft.Xna.Framework.Input.Keys.None));
            game.currGameState.input.typedChars.Enqueue(new Microsoft.Xna.Framework.TextInputEventArgs((char)8, Microsoft.Xna.Framework.Input.Keys.Back));
            tim.HandleInput(game.currGameState.input);
            game.currGameState.input.ClearTyped();
            tim.onKeyTyped = null;
            tim.inputText.Clear();
            tim.ClearHistory();
            bool noIndicators = !tim.hasMoreAbove && !tim.isScrolledBack;
            for (int i = 0; i < 40; i++)
                tim.AddHistoryLines("line " + i);
            bool moreAbove = tim.hasMoreAbove;
            tim.Scroll(5);
            System.Diagnostics.Debug.WriteLine("DevChecks TIM T-5.7: hook saw '" + hookKeys + "' (expect hi<B>), indicators none=" + noIndicators + ", moreAbove=" + moreAbove + ", scrolledBack=" + tim.isScrolledBack);
            tim.Scroll(-1000);
            tim.ClearHistory();

            // Visual aid: GITC_DEVSCROLL=1 fills the running debug mission's console so the scroll indicators can be seen
            if (Environment.GetEnvironmentVariable("GITC_DEVSCROLL") == "1")
            {
                TIMModule liveTim = (TIMModule)game.currGameState.currMission.moduleManager.Get(ModuleTypes.tim);
                for (int i = 0; i < 40; i++)
                    liveTim.AddHistoryLines("history line " + i);
                liveTim.Scroll(5);
            }

            // T-6.1: vitals go critical once, kill the agent after deathSeconds, and recover
            VitalsConfig vcfg = new VitalsConfig();
            vcfg.heartRate.deathSeconds = 5f;
            vcfg.temperature.driftPerSecond = -0.5f;
            VitalsModel vm = new VitalsModel(vcfg);
            int criticals = 0, deaths = 0; string critWhat = "", deathWhat = "";
            vm.onCritical = (v, high) => { criticals++; critWhat += v.name + (high ? "+" : "-") + " "; };
            vm.onDeath = v => { deaths++; deathWhat = v.name; };
            vm.Update(1f);
            string tempAt1 = vm.Get(Vitals.temperature).value.ToString("0.0");
            vm.Nudge(Vitals.heart_rate, 60f);          // 130 bpm: critical
            VitalStates hrState = vm.Get(Vitals.heart_rate).state;
            for (int i = 0; i < 3; i++) vm.Update(1f); // 3 s out of range, not dead yet
            bool aliveAt3 = !vm.isDead;
            vm.Nudge(Vitals.heart_rate, -60f);         // recovered
            vm.Update(1f);
            float outAfter = vm.Get(Vitals.heart_rate).outOfRangeSeconds;
            vm.Nudge(Vitals.heart_rate, 200f);         // clamped to the hard max (220)
            float clamped = vm.Get(Vitals.heart_rate).value;
            for (int i = 0; i < 10; i++) vm.Update(1f);
            System.Diagnostics.Debug.WriteLine("DevChecks vitals: temp after 1s=" + tempAt1 + ", HR state=" + hrState + ", alive at 3s=" + aliveAt3 + ", outOfRange after recovery=" + outAfter + ", clamped HR=" + clamped + ", criticals=" + criticals + " (" + critWhat.Trim() + "), deaths=" + deaths + " (" + deathWhat + "), dead=" + vm.isDead);

            // T-6.2: the VM module turns the model into events and takes nudges from events
            Mission vmMission = new Mission(game, new MissionDef() { id = "debug_vm", durationSeconds = 60, startingOps = 30 });
            string vmSeen = "";
            System.Action<ModuleEvent> vmPrev = vmMission.moduleManager.eventHandler;
            vmMission.moduleManager.eventHandler = e => { vmSeen += e.type + (e.tag.Length > 0 ? ":" + e.tag : "") + " "; vmPrev(e); };
            VMModule vmModule = (VMModule)vmMission.moduleManager.Get(ModuleTypes.vm);
            vmMission.moduleManager.Raise(new ModuleEvent(ModuleEventTypes.vital_nudge, ModuleTypes.fav, 600, "heart_rate"));
            SimulateSeconds(game, vmMission, 1);
            float hrAfterNudge = vmModule.model.Get(Vitals.heart_rate).value;
            SimulateSeconds(game, vmMission, 12);
            System.Diagnostics.Debug.WriteLine("DevChecks VM module: HR after +60 nudge=" + hrAfterNudge + ", events seen: " + vmSeen.Trim() + ", dead=" + vmModule.model.isDead + ", ECG wave peak=" + VMModule.EcgWave(0.235f).ToString("0.00") + " baseline=" + VMModule.EcgWave(0.7f).ToString("0.00"));

            // T-6.3: a critical vital raises the right command challenge; the command fixes it and then has a cooldown
            Mission fix = new Mission(game, new MissionDef() { id = "debug_fix", durationSeconds = 120, startingOps = 30 });
            TIMModule fixTim = (TIMModule)fix.moduleManager.Get(ModuleTypes.tim);
            VMModule fixVm = (VMModule)fix.moduleManager.Get(ModuleTypes.vm);
            fix.moduleManager.Raise(new ModuleEvent(ModuleEventTypes.vital_nudge, ModuleTypes.fav, 600, "heart_rate"));
            SimulateSeconds(game, fix, 2);
            string asked = fixTim.challenges.Count > 0 ? fixTim.challenges[0].command + " in " + fixTim.challenges[0].timeLimit + "s" : "<none>";
            fixTim.SubmitLine("calm");
            fix.Update(new Microsoft.Xna.Framework.GameTime(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1)), game.currGameState.input);
            float hrFixed = fixVm.model.Get(Vitals.heart_rate).value;
            float sanityAfter = fixVm.model.Get(Vitals.sanity).value;
            string defaultStates = fixVm.model.Get(Vitals.heart_rate).state + "/" + fixVm.model.Get(Vitals.sanity).state + "/" + fixVm.model.Get(Vitals.temperature).state;
            fixTim.ClearHistory();
            fixTim.SubmitLine("calm");
            string coolOut = fixTim.history[0];
            fixTim.ClearHistory();
            fix.Update(new Microsoft.Xna.Framework.GameTime(TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(1)), game.currGameState.input);
            fixTim.SubmitLine("warm");
            fix.Update(new Microsoft.Xna.Framework.GameTime(TimeSpan.FromSeconds(13), TimeSpan.FromSeconds(1)), game.currGameState.input);
            // Sanity is fixed by its own command and never warns at its maximum
            fix.moduleManager.Raise(new ModuleEvent(ModuleEventTypes.vital_nudge, ModuleTypes.fav, -700, "sanity"));
            fix.Update(new Microsoft.Xna.Framework.GameTime(TimeSpan.FromSeconds(14), TimeSpan.FromSeconds(1)), game.currGameState.input);
            fix.Update(new Microsoft.Xna.Framework.GameTime(TimeSpan.FromSeconds(14), TimeSpan.FromSeconds(1)), game.currGameState.input);
            string sanityAsk = fixTim.challenges.Exists(c => c.command == "reassure") ? "reassure" : "<none>";
            float sanityLow = fixVm.model.Get(Vitals.sanity).value;
            float hrBefore = fixVm.model.Get(Vitals.heart_rate).value;
            fixTim.SubmitLine("reassure");
            fix.Update(new Microsoft.Xna.Framework.GameTime(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(1)), game.currGameState.input);
            System.Diagnostics.Debug.WriteLine("DevChecks sanity: default states HR/sanity/temp=" + defaultStates + " (expect normal/normal/normal), low sanity=" + sanityLow + " asks '" + sanityAsk + "', after reassure=" + fixVm.model.Get(Vitals.sanity).value + " (expect 50), HR untouched=" + (fixVm.model.Get(Vitals.heart_rate).value == hrBefore));
            System.Diagnostics.Debug.WriteLine("DevChecks vital commands: challenge='" + asked + "' (expect calm in 6s), HR after calm=" + hrFixed + " (expect 100), sanity after calm=" + sanityAfter + " (expect 100, calm leaves it alone), second calm='" + coolOut + "', challenges left=" + fixTim.challenges.Count + ", OP=" + fix.operatorPoints + ", temp after warm=" + fixVm.model.Get(Vitals.temperature).value.ToString("0.0") + " (expect 38.5)");

            // Visual aid: GITC_DEVVITALS=1 pushes the running debug mission's vitals into warning/critical so the monitor can be seen
            if (Environment.GetEnvironmentVariable("GITC_DEVVITALS") == "1")
            {
                game.currGameState.currMission.moduleManager.Raise(new ModuleEvent(ModuleEventTypes.vital_nudge, ModuleTypes.fav, 450, "heart_rate"));
                game.currGameState.currMission.moduleManager.Raise(new ModuleEvent(ModuleEventTypes.vital_nudge, ModuleTypes.fav, -15, "temperature"));
                game.currGameState.currMission.moduleManager.Raise(new ModuleEvent(ModuleEventTypes.vital_nudge, ModuleTypes.fav, -400, "sanity"));
            }

            // T-5.6: timed command challenges (simulated, 1 second per frame)
            Mission chal = new Mission(game, new MissionDef() { id = "debug_challenge", durationSeconds = 60, startingOps = 30 });
            TIMModule chalTim = (TIMModule)chal.moduleManager.Get(ModuleTypes.tim);
            chal.moduleManager.SetFocus(ModuleTypes.fav);
            chal.moduleManager.Raise(new ModuleEvent(ModuleEventTypes.command_required, ModuleTypes.fav, 8, "unlock barrier"));
            SimulateSeconds(game, chal, 1);
            string focusAfter = chal.moduleManager.focusedModule.ToString();
            int pending = chalTim.challenges.Count;
            chalTim.SubmitLine("  UNLOCK   Barrier ");
            SimulateSeconds(game, chal, 1);
            System.Diagnostics.Debug.WriteLine("DevChecks challenge answered: focus=" + focusAfter + ", pending before=" + pending + ", after=" + chalTim.challenges.Count + ", OP=" + chal.operatorPoints + " (expect tim, 1, 0, 30), history[0]='" + chalTim.history[0] + "'");
            chal.moduleManager.Raise(new ModuleEvent(ModuleEventTypes.command_required, ModuleTypes.fav, 3, "calm"));
            SimulateSeconds(game, chal, 5);
            System.Diagnostics.Debug.WriteLine("DevChecks challenge timed out: pending=" + chalTim.challenges.Count + ", OP=" + chal.operatorPoints + " (expect 0, 20), history[0]='" + chalTim.history[0] + "'");

            // T-5.5: navigate the 3-level sample tree, read a file, unlock a hidden one
            SaveManager fsSaves = game.currGameState.saveManager;
            bool hadFlag = fsSaves.HasFlag("found_archive");
            bool hadSecret = fsSaves.current.discoveredSecrets.Contains("archive_note");
            fsSaves.current.flags.Remove("found_archive");
            fsSaves.current.discoveredSecrets.Remove("archive_note");
            tim.SubmitLine("listdir"); string root1 = string.Join(",", tim.history.GetRange(0, 3)); tim.ClearHistory();
            tim.SubmitLine("cd logs"); tim.SubmitLine("cd archive"); string path3 = tim.history[0]; tim.ClearHistory();
            tim.SubmitLine("listdir"); string archiveBefore = string.Join(",", tim.history.GetRange(0, tim.history.Count)); tim.ClearHistory();
            tim.SubmitLine("read note.txt"); string hiddenRead = tim.history[0]; tim.ClearHistory();
            tim.SubmitLine("cd /logs"); tim.SubmitLine("read session.log"); tim.ClearHistory();
            tim.SubmitLine("ls archive"); string after = string.Join(",", tim.history.GetRange(0, tim.history.Count)); tim.ClearHistory();
            tim.SubmitLine("cat archive/note.txt"); string noteRead = tim.history[0]; tim.ClearHistory();
            tim.SubmitLine("cd /ops"); tim.SubmitLine("cd .."); string upOne = tim.history[0]; tim.ClearHistory();
            tim.SubmitLine("cd nowhere"); string badCd = tim.history[0]; tim.ClearHistory();
            System.Diagnostics.Debug.WriteLine("DevChecks VFS: root (newest first)=" + root1 + " | depth3=" + path3 + " | archive before=" + archiveBefore + " | read hidden='" + hiddenRead + "' | archive after session.log=" + after + " | note='" + noteRead + "' | back=" + upOne + " | bad cd='" + badCd + "'");
            // Leave the real save as it was
            fsSaves.current.flags.Remove("found_archive"); fsSaves.current.discoveredSecrets.Remove("archive_note");
            if (hadFlag) fsSaves.SetFlag("found_archive");
            if (hadSecret) fsSaves.AddSecret("archive_note");
            tim.fileSystem.ChangeDir("/");
            System.Diagnostics.Debug.WriteLine("DevChecks TIM commands: echo='" + echoOut + "', echo2='" + echoOut2 + "', unknown='" + unknownOut + "', nohandler='" + noHandlerOut + "', help lists echo/help/clear=" + helpLines + ", after clear history=" + tim.history.Count);
            for (int i = 0; i < 40; i++)
                tim.AddHistoryLines("line " + i);
            tim.Scroll(1000);
            System.Diagnostics.Debug.WriteLine("DevChecks TIM scroll clamp: " + tim.scrollOffset + " of " + (tim.history.Count - TIMModule.CONSOLEHISTORY_SIZE) + " max");
        }
    }
}
