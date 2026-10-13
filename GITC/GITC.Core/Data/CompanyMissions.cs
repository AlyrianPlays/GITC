using System.Collections.Generic;

namespace GITC.Core
{
    // Root of a per-company mission file, e.g. Content/Data/companies/swiftail.json
    internal class CompanyMissions
    {
        public string companyId { get; set; }
        public List<MissionDef> missions { get; set; }

        public CompanyMissions()
        {
            missions = new List<MissionDef>();
        }
    }
}
