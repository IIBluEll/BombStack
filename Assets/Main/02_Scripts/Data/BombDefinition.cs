using System;
using System.Collections.Generic;

namespace BombStack.Data
{
    public sealed class BombLevelDefinition
    {
        public int Level { get; }
        public IReadOnlyList<GridPosition> DestructionOffsets { get; }

        /// <summary>같은 행/열의 다른 B만 점화하는 거리. 0이면 중계하지 않습니다.</summary>
        public int RelayDistance { get; }

        internal BombLevelDefinition(int level, GridPosition[] destructionOffsets, int relayDistance = 0)
        {
            Level = level;
            DestructionOffsets = Array.AsReadOnly(destructionOffsets);
            RelayDistance = relayDistance;
        }
    }

    public sealed class BombDefinition
    {
        public BOMB_TYPE Type { get; }
        public string Id { get; }
        public string DisplayName { get; }
        public IReadOnlyList<BombLevelDefinition> Levels { get; }

        internal BombDefinition(BOMB_TYPE type, string id, string displayName, params BombLevelDefinition[] levels)
        {
            Type = type;
            Id = id;
            DisplayName = displayName;
            Levels = Array.AsReadOnly(levels);
        }

        public BombLevelDefinition GetLevel(int level)
        {
            if (level < 1 || level > Levels.Count) throw new ArgumentOutOfRangeException(nameof(level));
            return Levels[level - 1];
        }
    }
}
