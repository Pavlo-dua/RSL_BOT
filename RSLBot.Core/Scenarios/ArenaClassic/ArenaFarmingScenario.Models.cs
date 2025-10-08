using System;
using System.Drawing;

namespace RSLBot.Core.Scenarios.ArenaClassic
{
    public partial class ArenaFarmingScenario
    {
        public enum FightStatus
        {
            NotFought,
            Fighting,
            Won,
            Lost
        }

        public class Opponent : IDisposable
        {
            /// <summary>
            /// A unique bitmap snapshot of the opponent's area.
            /// </summary>
            public Bitmap Snapshot { get; set; }

            /// <summary>
            /// The status of the fight against this opponent.
            /// </summary>
            public FightStatus Status { get; set; }

            public void Dispose()
            {
                // Safely dispose the bitmap to free up memory.
                Snapshot?.Dispose();
                Snapshot = null;
            }
        }
    }
}