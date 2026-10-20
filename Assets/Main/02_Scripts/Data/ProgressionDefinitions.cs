namespace BombStack.Data
{
    public sealed class StageDefinition
    {
        public int Number { get; }
        public int TargetScore { get; }
        public int PieceLimit { get; }
        public int CompletionExperience { get; }
        public bool GrantsRelicChoice { get; }

        internal StageDefinition(int number, int targetScore, int pieceLimit, int completionExperience, bool grantsRelicChoice)
        {
            Number = number;
            TargetScore = targetScore;
            PieceLimit = pieceLimit;
            CompletionExperience = completionExperience;
            GrantsRelicChoice = grantsRelicChoice;
        }
    }

    public sealed class CommonUpgradeDefinition
    {
        public COMMON_UPGRADE_TYPE Type { get; }
        public string DisplayName { get; }
        public int BonusPerStack { get; }

        internal CommonUpgradeDefinition(COMMON_UPGRADE_TYPE type, string displayName, int bonusPerStack)
        {
            Type = type;
            DisplayName = displayName;
            BonusPerStack = bonusPerStack;
        }
    }

    public sealed class RelicDefinition
    {
        public RELIC_TYPE Type { get; }
        public string Id { get; }
        public string DisplayName { get; }
        public RELIC_EFFECT Effect { get; }

        /// <summary>효과량. 배율은 두 배 정수 단위이며, 접촉/보존 효과는 보유 여부만 사용합니다.</summary>
        public int Value { get; }

        /// <summary>R02는 발동 B 수, R05는 파괴 N 수의 하한. 다른 효과는 0입니다.</summary>
        public int Threshold { get; }

        internal RelicDefinition(RELIC_TYPE type, string id, string displayName, RELIC_EFFECT effect, int value, int threshold = 0)
        {
            Type = type;
            Id = id;
            DisplayName = displayName;
            Effect = effect;
            Value = value;
            Threshold = threshold;
        }
    }
}
