using System.Drawing;

namespace RSLBot.Core.Scenarios.ArenaClassic
{
    public partial class ArenaFarmingScenario
    {
        private enum FightStatus
        {
            NotFought,
            Won,
            Lost
        }

        private class Opponent
        {
            public Rectangle Area { get; set; }
            public FightStatus Status { get; set; }

            public override bool Equals(object obj)
            {
                if (obj is Opponent other)
                {
                    // Opponents are the same if their areas are very close.
                    return System.Math.Abs(Area.Y - other.Area.Y) < 10;
                }
                return false;
            }

            public override int GetHashCode()
            {
                return Area.Y.GetHashCode();
            }
        }
    }
}
