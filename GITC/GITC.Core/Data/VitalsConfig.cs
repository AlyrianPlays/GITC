namespace GITC.Core
{
    // Vital Monitor settings: the three vital signs and how they behave
    internal class VitalsConfig
    {
        public VitalRange heartRate { get; set; }
        public VitalRange sanity { get; set; }
        public VitalRange temperature { get; set; }

        public VitalsConfig()
        {
            heartRate = new VitalRange(70f, 50f, 110f);
            sanity = new VitalRange(100f, 40f, 100f);
            temperature = new VitalRange(37f, 35f, 39f);
        }
    }
}
