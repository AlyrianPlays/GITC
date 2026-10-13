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
        }
    }
}
