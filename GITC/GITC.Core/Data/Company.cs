using System.Collections.Generic;

namespace GITC.Core
{
    // One entry of Content/Data/companies.json
    internal class Company
    {
        // Lookup key, matches the Companies enum member (lower_snake_case)
        public string id { get; set; }
        // Display name, e.g. "Wisdus" (previous iterations share the name and set isPrior)
        public string name { get; set; }
        // True for a previous iteration of the company (new CEO took over, company no longer exists, etc.)
        public bool isPrior { get; set; }
        public string ceo { get; set; }
        // The CEO's real name as an elder god (hidden from the player until lore reveals it)
        public string godName { get; set; }
        // Brand colour as "#RRGGBB"
        public string color { get; set; }
        // ContentRegistry texture key for the logo
        public string logoKey { get; set; }
        // Optional ContentRegistry song key for this company's missions
        public string musicKey { get; set; }
        // Mission file relative to Content/Data/ (a CompanyMissions json), empty if the company has no missions yet
        public string missionFile { get; set; }
        // Event/difficulty multiplier per module name ("fav", "tim", ...); missing = 1.0. Used by the mission director.
        public Dictionary<string, float> moduleWeights { get; set; }

        public Company()
        {
            moduleWeights = new Dictionary<string, float>();
        }
    }
}
