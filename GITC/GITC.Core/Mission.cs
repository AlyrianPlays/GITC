using Microsoft.Xna.Framework;
using System;

namespace GITC.Core
{
    // One running level: the clock, the Operator Points, and the modules.
    // A mission is won by reaching the end of its time with OPs left, lost when OPs reach zero.
    internal class Mission
    {
        public GITCGame currGame { get; set; }
        public MissionDef def { get; set; }
        public ModuleManager moduleManager { get; set; }
        public MissionStatus status { get; set; }
        // Border colour and logo for the mission's company (neutral if the company is unknown)
        public CompanyTheme theme { get; set; }
        // Seconds left on the clock (counts down while the mission is running)
        public float timeRemaining { get; set; }
        public int operatorPoints { get; set; }
        // OP cost of a missed command challenge unless the mission's penalties say otherwise
        public const int DEFAULTCOMMANDPENALTY = 10;
        // OPs cannot be gained above the starting amount
        public int maxOperatorPoints { get; set; }

        public Mission(GITCGame currGame, MissionDef def)
        {
            this.currGame = currGame;
            this.def = def;
            timeRemaining = def.durationSeconds;
            operatorPoints = def.startingOps;
            maxOperatorPoints = def.startingOps;
            status = MissionStatus.running;
            theme = CompanyTheme.FromCompany(def.companyId);

            moduleManager = new ModuleManager(currGame);
            moduleManager.eventHandler = HandleEvent;
            moduleManager.AddModule(new PMModule(currGame, this));
        }

        // Frozen once the mission has ended; call from the play state only (pause simply stops calling it)
        public void Update(GameTime gameTime, InputManager input)
        {
            if (status != MissionStatus.running)
                return;

            timeRemaining = Math.Max(0f, timeRemaining - (float)gameTime.ElapsedGameTime.TotalSeconds);

            // Modules run and their events are delivered to HandleEvent during this call
            moduleManager.Update(gameTime, input);

            // Checks: losing takes priority, so zero OPs on the last frame is still a loss
            if (operatorPoints <= 0)
                status = MissionStatus.lost;
            else if (timeRemaining <= 0f)
                status = MissionStatus.won;
        }

        // Mission-level reactions to module events (penalty rules grow in T-9.3)
        private void HandleEvent(ModuleEvent moduleEvent)
        {
            switch (moduleEvent.type)
            {
                case ModuleEventTypes.op_penalty:
                    operatorPoints = Math.Max(0, operatorPoints - moduleEvent.amount);
                    break;
                case ModuleEventTypes.command_failed:
                    // A command challenge ran out of time (raised by the TIM)
                    int penalty = def.penalties.TryGetValue("command_failed", out int custom) ? custom : DEFAULTCOMMANDPENALTY;
                    operatorPoints = Math.Max(0, operatorPoints - penalty);
                    break;
                case ModuleEventTypes.op_gain:
                    operatorPoints = Math.Min(maxOperatorPoints, operatorPoints + moduleEvent.amount);
                    break;
                default:
                    break;
            }
        }
    }
}
