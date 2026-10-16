namespace GITC.Core
{
    // A FAV barrier: a column of tiles that blocks the agent until the TIM answers a command.
    // Touching it raises command_required (tag = command, amount = seconds); command_satisfied opens it for good.
    internal class FavBarrierDef
    {
        public string id { get; set; }
        // Column, top row, and how many rows tall
        public int x { get; set; }
        public int y { get; set; }
        public int height { get; set; }
        // What the player must type in the TIM, and how long they have
        public string command { get; set; }
        public int seconds { get; set; }

        public FavBarrierDef()
        {
            height = 1;
            seconds = 10;
        }
    }
}
