using System;
using System.Collections.Generic;

namespace BombStack.Data
{
    public sealed class BombProgressData
    {
        public BOMB_TYPE Type { get; }
        public int Level { get; set; }

        public BombProgressData(BOMB_TYPE type, int level)
        {
            if (type == BOMB_TYPE.NONE || !Enum.IsDefined(typeof(BOMB_TYPE), type)) throw new ArgumentOutOfRangeException(nameof(type));
            if (level < 1) throw new ArgumentOutOfRangeException(nameof(level));
            Type = type;
            Level = level;
        }
    }

    public sealed class GrowthChoiceData
    {
        public GROWTH_CHOICE_TYPE Type { get; }
        public BOMB_TYPE BombType { get; }

        /// <summary>공통 강화 선택일 때만 값이 있습니다.</summary>
        public COMMON_UPGRADE_TYPE? CommonUpgradeType { get; }

        private GrowthChoiceData(GROWTH_CHOICE_TYPE type, BOMB_TYPE bombType, COMMON_UPGRADE_TYPE? commonUpgradeType)
        {
            Type = type;
            BombType = bombType;
            CommonUpgradeType = commonUpgradeType;
        }

        public static GrowthChoiceData ForBomb(BOMB_TYPE bombType, bool isAcquisition)
        {
            if (bombType == BOMB_TYPE.NONE || !Enum.IsDefined(typeof(BOMB_TYPE), bombType)) throw new ArgumentOutOfRangeException(nameof(bombType));
            return new GrowthChoiceData(isAcquisition ? GROWTH_CHOICE_TYPE.ACQUIRE_BOMB : GROWTH_CHOICE_TYPE.UPGRADE_BOMB, bombType, null);
        }

        public static GrowthChoiceData ForCommonUpgrade(COMMON_UPGRADE_TYPE type)
        {
            if (!Enum.IsDefined(typeof(COMMON_UPGRADE_TYPE), type)) throw new ArgumentOutOfRangeException(nameof(type));
            return new GrowthChoiceData(GROWTH_CHOICE_TYPE.COMMON_UPGRADE, BOMB_TYPE.NONE, type);
        }
    }

    /// <summary>이번 판에만 유지되는 빌드. 후보 자격/상한/획득 처리는 향후 규칙 코드의 책임입니다.</summary>
    public sealed class RunBuildData
    {
        public int PlayerLevel { get; set; }
        public long Experience { get; set; }
        public List<BombProgressData> Bombs { get; } = new List<BombProgressData>();
        public long RecoveryMasteryCount { get; set; }
        public long GravityMasteryCount { get; set; }
        public long BombMasteryCount { get; set; }
        public List<RELIC_TYPE> Relics { get; } = new List<RELIC_TYPE>();

        public RunBuildData(GrowthDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            PlayerLevel = definition.StartingPlayerLevel;
            Bombs.Add(new BombProgressData(definition.StartingBombType, definition.StartingBombLevel));
        }
    }
}
