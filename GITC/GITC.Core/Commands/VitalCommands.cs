using System;

namespace GITC.Core
{
    // TIM commands that act on the field agent's vitals (calm, stimulate, warm, cool). Each is a vital_nudge event
    // sent to the Vital Monitor; cooldowns come from commands.json (CommandDef.cooldownSeconds).
    internal static class VitalCommands
    {
        public static void Register(CommandRegistry registry)
        {
            registry.handlers["calm"] = context => Apply(context, "Calming signal sent.",
                (Vitals.heart_rate, -300), (Vitals.sanity, 150));
            registry.handlers["stimulate"] = context => Apply(context, "Stimulant sent.",
                (Vitals.heart_rate, 300));
            registry.handlers["warm"] = context => Apply(context, "Heating the suit.",
                (Vitals.temperature, 15));
            registry.handlers["cool"] = context => Apply(context, "Cooling the suit.",
                (Vitals.temperature, -15));
        }

        // The command that fixes a vital that went out of range, or "" if none does (a vital_critical event's
        // amount is +1 for too high, -1 for too low)
        public static string CorrectiveFor(Vitals vital, bool tooHigh)
        {
            switch (vital)
            {
                case Vitals.heart_rate:
                    return tooHigh ? "calm" : "stimulate";
                case Vitals.sanity:
                    return tooHigh ? "" : "calm";
                default:
                    return tooHigh ? "cool" : "warm";
            }
        }

        // Sends one vital_nudge per change (amounts in tenths of a unit, see VitalsModel.NUDGEUNIT)
        private static string[] Apply(CommandContext context, string message, params (Vitals vital, int tenths)[] changes)
        {
            ModuleManager manager = context.tim.manager;
            VMModule vm = manager?.Get(ModuleTypes.vm) as VMModule;
            if (vm == null)
                return new[] { "No vital monitor connected." };
            if (vm.model.isDead)
                return new[] { "No signal from the agent." };

            foreach ((Vitals vital, int tenths) change in changes)
                manager.Raise(new ModuleEvent(ModuleEventTypes.vital_nudge, ModuleTypes.tim, change.tenths, change.vital.ToString()));
            return new[] { message };
        }
    }
}
