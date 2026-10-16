using System;
using System.Collections.Generic;

namespace GITC.Core
{
    //TESTING, REMOVE AFTER: startup self-checks for the data layer. Set env var GITC_DEVCHECKS=1 to also echo Debug output to the console.
    internal static class DevChecks
    {
        private static void StepTo(LVMModule lvm, Microsoft.Xna.Framework.Point target)
        {
            foreach (int dir in new[] { MazeGenerator.NORTH, MazeGenerator.SOUTH, MazeGenerator.EAST, MazeGenerator.WEST })
                if (lvm.playerCell.X + MazeGenerator.OffsetX(dir) == target.X && lvm.playerCell.Y + MazeGenerator.OffsetY(dir) == target.Y)
                    lvm.TryMove(dir);
        }

        // Runs a FAV world at 60 fps with the same intent every frame (move -1/0/1, jump pressed on the first frame only)
        private static void RunFav(FavWorld world, float seconds, int move, bool jump = false)
        {
            for (int f = 0; f < (int)(seconds * 60); f++)
            {
                world.moveDirection = move;
                world.jumpRequested = jump && f == 0;
                world.Update(1f / 60f);
            }
            world.moveDirection = 0;
        }

        // Runs a whole mission at 60 fps while the FAV agent is steered directly (no keyboard needed)
        private static void RunMissionFav(GITCGame game, Mission mission, FavWorld world, float seconds, int move)
        {
            for (int f = 0; f < (int)(seconds * 60); f++)
            {
                world.moveDirection = move;
                mission.Update(new Microsoft.Xna.Framework.GameTime(TimeSpan.FromSeconds(f / 60.0), TimeSpan.FromSeconds(1.0 / 60.0)), game.currGameState.input);
            }
        }

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

            // T-7.1: perfect mazes from 5x5 to 25x25 (n*m-1 passages = connected with no loops), start left, exit right, one solution
            string mazeReport = "";
            foreach (int side in new[] { 5, 9, 15, 25 })
            {
                MazeGenerator mg = new MazeGenerator(side, side, 100 + side);
                mazeReport += side + "x" + side + ": passages=" + mg.CountPassages() + " (expect " + (side * side - 1) + "), solution=" + mg.solution.Count + ", start.X=" + mg.start.X + ", exit.X=" + mg.exit.X + "; ";
            }
            MazeGenerator again = new MazeGenerator(9, 9, 109);
            MazeGenerator same = new MazeGenerator(9, 9, 109);
            System.Diagnostics.Debug.WriteLine("DevChecks maze: " + mazeReport.Trim() + " same seed identical=" + (again.ToAscii() == same.ToAscii()) + ", difficulty 1/5/20 sizes=" + MazeGenerator.SizeFor(1) + "/" + MazeGenerator.SizeFor(5) + "/" + MazeGenerator.SizeFor(20));
            System.Diagnostics.Debug.WriteLine("DevChecks maze 5x5 ASCII:" + Environment.NewLine + new MazeGenerator(5, 5, 7).ToAscii());
            if (Environment.GetEnvironmentVariable("GITC_DEVMAZE") == "1")
                System.Diagnostics.Debug.WriteLine("DevChecks maze 25x25 ASCII:" + Environment.NewLine + new MazeGenerator(25, 25, 25).ToAscii());

            // T-7.2: wrong turn costs OPs and resets; walls block silently; reaching the exit builds a bigger maze
            MazeConfig lvmCfg = new MazeConfig() { seed = 42 };
            Mission lvmMission = new Mission(game, new MissionDef() { id = "debug_lvm", durationSeconds = 120, startingOps = 30, modules = new List<ModuleConfig>() { new ModuleConfig() { module = "lvm", maze = lvmCfg } } });
            string lvmSeen = "";
            System.Action<ModuleEvent> lvmPrev = lvmMission.moduleManager.eventHandler;
            lvmMission.moduleManager.eventHandler = e => { lvmSeen += e.type + " "; lvmPrev(e); };
            LVMModule lvm = (LVMModule)lvmMission.moduleManager.Get(ModuleTypes.lvm);
            bool idleAtStart = !lvm.isChallengeActive;
            lvmMission.moduleManager.Raise(new ModuleEvent(ModuleEventTypes.maze_required, ModuleTypes.pm, 30));
            SimulateSeconds(game, lvmMission, 1);
            bool activeAfterRequest = lvm.isChallengeActive;
            string focusLvm = lvmMission.moduleManager.focusedModule.ToString();
            MazeGenerator firstMaze = lvm.maze;
            // Walk the solution until a cell has an open side that leaves it, then step off
            int wallBlocks = 0;
            foreach (int dir in new[] { MazeGenerator.NORTH, MazeGenerator.SOUTH, MazeGenerator.EAST, MazeGenerator.WEST })
                if (!lvm.maze.IsOpen(lvm.playerCell, dir)) { Microsoft.Xna.Framework.Point wallFrom = lvm.playerCell; lvm.TryMove(dir); if (lvm.playerCell == wallFrom) wallBlocks++; }
            bool wrongDone = false;
            for (int i = 0; i < lvm.maze.solution.Count && !wrongDone; i++)
            {
                Microsoft.Xna.Framework.Point cell = lvm.maze.solution[i];
                foreach (int dir in new[] { MazeGenerator.NORTH, MazeGenerator.SOUTH, MazeGenerator.EAST, MazeGenerator.WEST })
                {
                    Microsoft.Xna.Framework.Point off = new Microsoft.Xna.Framework.Point(cell.X + MazeGenerator.OffsetX(dir), cell.Y + MazeGenerator.OffsetY(dir));
                    if (lvm.maze.IsOpen(cell, dir) && !lvm.maze.solutionSet.Contains(off))
                    {
                        for (int s = 1; s <= i; s++) StepTo(lvm, lvm.maze.solution[s]);
                        lvm.TryMove(dir);
                        wrongDone = true;
                        break;
                    }
                }
            }
            SimulateSeconds(game, lvmMission, 1);
            bool resetToStart = lvm.playerCell == lvm.maze.start && lvm.path.Count == 1;
            bool sameMaze = lvm.maze == firstMaze;
            int opsAfterWrong = lvmMission.operatorPoints;
            for (int s = 1; s < lvm.maze.solution.Count; s++) StepTo(lvm, lvm.maze.solution[s]);
            SimulateSeconds(game, lvmMission, 1);
            System.Diagnostics.Debug.WriteLine("DevChecks LVM: walls blocked=" + wallBlocks + " with no penalty, wrong turn done=" + wrongDone + ", reset to start=" + resetToStart + ", same maze kept=" + sameMaze + ", OP=" + opsAfterWrong + " (expect 20), after solving: completed=" + lvm.mazesCompleted + ", standing by=" + !lvm.isChallengeActive + ", difficulty=" + lvm.difficulty + " (expect 1, True, 1), idle at start=" + idleAtStart + ", active after request=" + activeAfterRequest + ", focus=" + focusLvm + " (expect True, True, lvm), events: " + lvmSeen.Trim());
            // A request that is not answered in time costs OPs; requests while a maze is running are ignored
            int opsBeforeTimeout = lvmMission.operatorPoints;
            lvmMission.moduleManager.Raise(new ModuleEvent(ModuleEventTypes.maze_required, ModuleTypes.pm, 3));
            SimulateSeconds(game, lvmMission, 1);
            MazeGenerator runningMaze = lvm.maze;
            lvmMission.moduleManager.Raise(new ModuleEvent(ModuleEventTypes.maze_required, ModuleTypes.pm, 99));
            SimulateSeconds(game, lvmMission, 1);
            bool ignoredWhileActive = lvm.maze == runningMaze;
            SimulateSeconds(game, lvmMission, 3);
            System.Diagnostics.Debug.WriteLine("DevChecks LVM timeout: failed=" + lvm.mazesFailed + ", standing by=" + !lvm.isChallengeActive + ", OP " + opsBeforeTimeout + " -> " + lvmMission.operatorPoints + " (expect 1, True, 10 less), second request ignored while active=" + ignoredWhileActive + ", events: " + lvmSeen.Trim());

            // T-8.1: the agent lands on the ground, is stopped by the 2-tile block, jumps over it, and can't move while hidden
            FavWorld fav = new FavWorld(FAVModule.LoadLevel(FAVModule.DEFAULTLEVELFILE));
            RunFav(fav, 0.5f, 0);
            bool landed = fav.isGrounded;
            RunFav(fav, 3f, 1);
            float blockedAt = fav.agentPosition.X + fav.agentWidth;
            RunFav(fav, 0.1f, -1);
            RunFav(fav, 1.2f, 1, true);
            bool overBlock = fav.agentPosition.X > 22 * fav.tileSize && fav.isGrounded;
            fav.ToggleHide();
            float hiddenX = fav.agentPosition.X;
            RunFav(fav, 1f, 1, true);
            System.Diagnostics.Debug.WriteLine("DevChecks FAV physics: level " + fav.columns + "x" + fav.rows + ", landed=" + landed + ", stopped at x=" + blockedAt + " (expect " + 20 * fav.tileSize + "), jumped over block=" + overBlock + ", hidden=" + fav.isHidden + " stayed put=" + (fav.agentPosition.X == hiddenX) + " (expect True, True, True)");

            // T-8.2: a guard 6 tiles from the spawn. Seen in the open -> dead; hidden, facing away, or behind a wall -> safe; a non-lethal one costs OPs
            Func<int, bool, int, FavLevelDef> guardLevel = (facing, lethal, wallX) =>
            {
                FavLevelDef d = FavLevelDef.Flat(30, 8);
                d.enemies.Add(new FavEnemyDef() { id = "g", x = 8, y = 5, patrolFromX = 8, patrolToX = 8, facing = facing, sightRange = 8, detectSeconds = 0.5f, lethal = lethal, damage = 10 });
                if (wallX > 0)
                    for (int y = 2; y <= 5; y++)
                        d.tiles[y] = d.tiles[y].Substring(0, wallX) + "#" + d.tiles[y].Substring(wallX + 1);
                return d;
            };
            Mission favMission = new Mission(game, new MissionDef() { id = "debug_fav", durationSeconds = 120, startingOps = 30 });
            string favSeen = "";
            System.Action<ModuleEvent> favPrev = favMission.moduleManager.eventHandler;
            favMission.moduleManager.eventHandler = e => { if (e.source == ModuleTypes.fav) favSeen += e.type + "(" + e.tag + ") "; favPrev(e); };
            FAVModule favModule = (FAVModule)favMission.moduleManager.Get(ModuleTypes.fav);
            favModule.SetWorld(new FavWorld(guardLevel(-1, true, 0)));
            SimulateSeconds(game, favMission, 1);
            bool spottedDead = favModule.world.isAgentDown;
            SimulateSeconds(game, favMission, 3);
            bool respawned = !favModule.world.isAgentDown;
            FavWorld hideWorld = new FavWorld(guardLevel(-1, true, 0));
            RunFav(hideWorld, 0.2f, 0); hideWorld.ToggleHide(); RunFav(hideWorld, 2f, 0);
            FavWorld awayWorld = new FavWorld(guardLevel(1, true, 0));
            RunFav(awayWorld, 2f, 0);
            FavWorld wallWorld = new FavWorld(guardLevel(-1, true, 5));
            RunFav(wallWorld, 2f, 0);
            favModule.SetWorld(new FavWorld(guardLevel(-1, false, 0)));
            int opsBeforeHit = favMission.operatorPoints;
            SimulateSeconds(game, favMission, 1);
            bool hitAlive = !favModule.world.isAgentDown && favModule.world.graceSeconds > 0f;
            System.Diagnostics.Debug.WriteLine("DevChecks FAV enemies: in the open dead=" + spottedDead + ", respawned after 3 s=" + respawned + ", hidden safe=" + !hideWorld.isAgentDown + " (alert " + hideWorld.enemies[0].alert + "), facing away safe=" + !awayWorld.isAgentDown + ", behind wall safe=" + !wallWorld.isAgentDown + ", non-lethal: alive with grace=" + hitAlive + ", OP " + opsBeforeHit + " -> " + favMission.operatorPoints + " (expect all True, 10 less), fav events: " + favSeen.Trim());

            // T-8.3: spikes and a pit kill; both can be jumped; a barrier asks the TIM, opens when answered, stays shut when the time runs out
            Func<int, int, char, FavLevelDef> hazardLevel = (fromX, toX, c) =>
            {
                FavLevelDef d = FavLevelDef.Flat(30, 8);
                int row = c == '^' ? 5 : 6;
                for (int r = row; r <= (c == '^' ? 5 : 7); r++)
                    d.tiles[r] = d.tiles[r].Substring(0, fromX) + new string(c, toX - fromX + 1) + d.tiles[r].Substring(toX + 1);
                return d;
            };
            string favDeath = "";
            FavWorld spikeWorld = new FavWorld(hazardLevel(7, 7, '^')); spikeWorld.onDeath = c => favDeath += c + " ";
            RunFav(spikeWorld, 2f, 1);
            FavWorld pitWorld = new FavWorld(hazardLevel(7, 9, '.')); pitWorld.onDeath = c => favDeath += c + " ";
            RunFav(pitWorld, 3f, 1);
            FavWorld spikeJump = new FavWorld(hazardLevel(7, 7, '^'));
            RunFav(spikeJump, 0.3f, 0); RunFav(spikeJump, 0.42f, 1); RunFav(spikeJump, 1f, 1, true);
            FavWorld pitJump = new FavWorld(hazardLevel(7, 9, '.'));
            RunFav(pitJump, 0.3f, 0); RunFav(pitJump, 0.42f, 1); RunFav(pitJump, 1f, 1, true);
            System.Diagnostics.Debug.WriteLine("DevChecks FAV hazards: deaths='" + favDeath.Trim() + "' (expect spikes pit), jumped spikes alive=" + !spikeJump.isAgentDown + " at x=" + (int)(spikeJump.agentPosition.X / spikeJump.tileSize) + ", jumped pit alive=" + !pitJump.isAgentDown + " at x=" + (int)(pitJump.agentPosition.X / pitJump.tileSize) + " (expect True >7, True >9)");

            Func<FavLevelDef> gateLevel = () =>
            {
                FavLevelDef d = FavLevelDef.Flat(30, 8);
                d.barriers.Add(new FavBarrierDef() { id = "gate", x = 8, y = 0, height = 6, command = "override gate", seconds = 3 });
                return d;
            };
            Mission gateMission = new Mission(game, new MissionDef() { id = "debug_gate", durationSeconds = 120, startingOps = 30 });
            string gateSeen = "";
            System.Action<ModuleEvent> gatePrev = gateMission.moduleManager.eventHandler;
            gateMission.moduleManager.eventHandler = e => { gateSeen += e.type + " "; gatePrev(e); };
            FAVModule gateFav = (FAVModule)gateMission.moduleManager.Get(ModuleTypes.fav);
            TIMModule gateTim = (TIMModule)gateMission.moduleManager.Get(ModuleTypes.tim);
            gateFav.SetWorld(new FavWorld(gateLevel()));
            RunMissionFav(game, gateMission, gateFav.world, 2f, 1);
            bool blocked = gateFav.world.agentPosition.X + gateFav.world.agentWidth <= 8 * gateFav.world.tileSize + 0.01f;
            string gateAsked = gateTim.challenges.Count > 0 ? gateTim.challenges[0].command : "<none>";
            gateTim.SubmitLine("Override  Gate");
            RunMissionFav(game, gateMission, gateFav.world, 1.5f, 1);
            bool passed = gateFav.world.agentPosition.X > 9 * gateFav.world.tileSize;
            System.Diagnostics.Debug.WriteLine("DevChecks FAV barrier answered: blocked=" + blocked + ", TIM asked='" + gateAsked + "', focus=" + gateMission.moduleManager.focusedModule + ", open=" + gateFav.world.openBarriers.Contains("gate") + ", walked through=" + passed + ", OP=" + gateMission.operatorPoints + " (expect True, override gate, tim, True, True, 30), events: " + gateSeen.Trim());
            gateSeen = "";
            gateFav.SetWorld(new FavWorld(gateLevel()));
            RunMissionFav(game, gateMission, gateFav.world, 6f, 1);
            bool stillShut = !gateFav.world.openBarriers.Contains("gate") && gateFav.world.agentPosition.X < 8 * gateFav.world.tileSize;
            System.Diagnostics.Debug.WriteLine("DevChecks FAV barrier missed: still shut=" + stillShut + ", OP=" + gateMission.operatorPoints + " (expect True, 20), asked again after retry=" + (gateTim.challenges.Count == 1) + ", events: " + gateSeen.Trim());

            // T-8.4: death costs OPs and the respawn revives the VM; the goal pays a bonus and reloads; a VM death drops the agent with no FAV penalty;
            // the heart rate climbs near an enemy and comes back down to resting (not below) while hidden
            Mission lifeMission = new Mission(game, new MissionDef() { id = "debug_fav_life", durationSeconds = 120, startingOps = 30 });
            string lifeSeen = "";
            System.Action<ModuleEvent> lifePrev = lifeMission.moduleManager.eventHandler;
            lifeMission.moduleManager.eventHandler = e => { if ((e.source == ModuleTypes.fav || e.type == ModuleEventTypes.agent_died) && e.type != ModuleEventTypes.vital_nudge) lifeSeen += e.type + "(" + e.tag + ") "; lifePrev(e); };
            FAVModule lifeFav = (FAVModule)lifeMission.moduleManager.Get(ModuleTypes.fav);
            VMModule lifeVm = (VMModule)lifeMission.moduleManager.Get(ModuleTypes.vm);
            lifeFav.SetWorld(new FavWorld(guardLevel(-1, true, 0)));
            RunMissionFav(game, lifeMission, lifeFav.world, 1f, 0);
            int opsAfterDeath = lifeMission.operatorPoints;
            RunMissionFav(game, lifeMission, lifeFav.world, 2.5f, 0);
            System.Diagnostics.Debug.WriteLine("DevChecks FAV death: OP 30 -> " + opsAfterDeath + " (expect 20), alive again=" + !lifeFav.world.isAgentDown + ", events: " + lifeSeen.Trim());
            lifeSeen = "";
            FavLevelDef goalDef = FavLevelDef.Flat(30, 8);
            goalDef.goalX = 10;
            lifeFav.SetWorld(new FavWorld(goalDef));
            FavWorld beforeGoal = lifeFav.world;
            RunMissionFav(game, lifeMission, lifeFav.world, 1.5f, 1);
            System.Diagnostics.Debug.WriteLine("DevChecks FAV goal: segments=" + lifeFav.segmentsCompleted + ", OP=" + lifeMission.operatorPoints + " (expect 1, 25), new world=" + (lifeFav.world != beforeGoal) + ", back near spawn=" + (lifeFav.world.agentPosition.X < 5 * lifeFav.world.tileSize) + ", events: " + lifeSeen.Trim());
            lifeSeen = "";
            int opsBeforeVmDeath = lifeMission.operatorPoints;
            lifeMission.moduleManager.Raise(new ModuleEvent(ModuleEventTypes.agent_died, ModuleTypes.vm, 0, "heart_rate"));
            RunMissionFav(game, lifeMission, lifeFav.world, 0.5f, 0);
            bool vmDropped = lifeFav.world.isAgentDown;
            RunMissionFav(game, lifeMission, lifeFav.world, 2f, 0);
            System.Diagnostics.Debug.WriteLine("DevChecks FAV vitals death: agent dropped=" + vmDropped + ", OP unchanged=" + (lifeMission.operatorPoints == opsBeforeVmDeath) + ", alive again=" + !lifeFav.world.isAgentDown + ", VM alive=" + !lifeVm.model.isDead + " (expect all True), events: " + lifeSeen.Trim());
            FavLevelDef nearDef = FavLevelDef.Flat(30, 8);
            nearDef.enemies.Add(new FavEnemyDef() { id = "watcher", x = 6, y = 5, patrolFromX = 6, patrolToX = 6, facing = 1 });
            lifeFav.SetWorld(new FavWorld(nearDef));
            float hrStart = lifeVm.model.Get(Vitals.heart_rate).value;
            RunMissionFav(game, lifeMission, lifeFav.world, 3f, 0);
            float hrNear = lifeVm.model.Get(Vitals.heart_rate).value;
            lifeFav.SetWorld(new FavWorld(FavLevelDef.Flat(30, 8)));
            RunMissionFav(game, lifeMission, lifeFav.world, 0.3f, 0);
            lifeFav.world.ToggleHide();
            RunMissionFav(game, lifeMission, lifeFav.world, 2f, 0);
            float hrHiding = lifeVm.model.Get(Vitals.heart_rate).value;
            RunMissionFav(game, lifeMission, lifeFav.world, 6f, 0);
            System.Diagnostics.Debug.WriteLine("DevChecks FAV heart rate: start " + hrStart + ", 3 s four tiles from an enemy " + hrNear.ToString("0.0") + " (expect about +18), hidden 2 s " + hrHiding.ToString("0.0") + " (expect about -12), hidden 8 s " + lifeVm.model.Get(Vitals.heart_rate).value.ToString("0.0") + " (expect resting " + lifeVm.model.Get(Vitals.heart_rate).range.start + ")");

            // Regression (owner report: S did not hide): standing still must read as grounded after EVERY frame for any frame length,
            // including MonoGame's 0.0166667 s, which used to leave a ~3e-8 s physics slice that ended the frame 'in the air'
            string groundReport = "";
            foreach (float frame in new[] { 1f / 60f, (float)TimeSpan.FromTicks(166667).TotalSeconds, 0.0166666f, 1f / 144f, 0.0333334f })
            {
                FavWorld stand = new FavWorld(FavLevelDef.Flat(30, 8));
                int airborneFrames = 0;
                for (int f = 0; f < 120; f++)
                {
                    stand.Update(frame);
                    if (f >= 30 && !stand.isGrounded) airborneFrames++;
                }
                stand.ToggleHide();
                groundReport += frame.ToString("0.0000000") + "s: airborne frames=" + airborneFrames + " hides=" + stand.isHidden + "; ";
            }
            System.Diagnostics.Debug.WriteLine("DevChecks FAV standing/hide per frame length (expect 0 and True each): " + groundReport.Trim());

            // Visual aid: GITC_DEVFAV=1 puts the running mission's agent, hidden, a few tiles from guard_a so the enemy and its cone are on screen;
            // =2 puts it at column 70 so the spikes, drone_b and the barrier are in view
            string devFav = Environment.GetEnvironmentVariable("GITC_DEVFAV");
            if (devFav == "1" || devFav == "2")
            {
                FavWorld runFav = ((FAVModule)game.currGameState.currMission.moduleManager.Get(ModuleTypes.fav)).world;
                runFav.agentPosition = new Microsoft.Xna.Framework.Vector2((devFav == "1" ? 41 : 70) * runFav.tileSize, 17 * runFav.tileSize - runFav.agentHeight);
                runFav.isGrounded = true;
                runFav.isHidden = true;
            }

            // Visual aid: GITC_DEVLIDAR=1 walks the running mission's LVM six steps along the solution; =2 also turns the lidar radius on
            string devLidar = Environment.GetEnvironmentVariable("GITC_DEVLIDAR");
            if (devLidar == "1" || devLidar == "2")
            {
                LVMModule runLvm = (LVMModule)game.currGameState.currMission.moduleManager.Get(ModuleTypes.lvm);
                runLvm.StartChallenge(120);
                if (devLidar == "2") runLvm.config.revealRadius = 4;
                for (int s = 1; s <= 6; s++) StepTo(runLvm, runLvm.maze.solution[s]);
            }

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
