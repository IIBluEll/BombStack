using System;
using System.Collections.Generic;

namespace BombStack.Data
{
    public sealed class BoardDefinition
    {
        public int Width { get; } = 10;
        public int Height { get; } = 18;
        public int WarningStartRow { get; } = 14;
        public GridPosition SpawnOrigin { get; } = new GridPosition(3, 14);
    }

    public sealed class SupplyDefinition
    {
        public int PreviewCount { get; } = 3;
        public IReadOnlyList<PIECE_SHAPE> ShapeBag { get; } = Array.AsReadOnly(new[]
        {
            PIECE_SHAPE.I, PIECE_SHAPE.O, PIECE_SHAPE.T, PIECE_SHAPE.S, PIECE_SHAPE.Z, PIECE_SHAPE.J, PIECE_SHAPE.L
        });
        public IReadOnlyList<CELL_TYPE> AttributeBag { get; } = Array.AsReadOnly(new[]
        {
            CELL_TYPE.BOMB, CELL_TYPE.BOMB, CELL_TYPE.TRIGGER, CELL_TYPE.NORMAL
        });
        public IReadOnlyList<GridPosition> RotationKicks { get; } = Array.AsReadOnly(new[]
        {
            new GridPosition(0, 0), new GridPosition(-1, 0), new GridPosition(1, 0),
            new GridPosition(-2, 0), new GridPosition(2, 0), new GridPosition(0, 1), new GridPosition(0, 2)
        });
    }

    public sealed class TimingDefinition
    {
        public float AutomaticFallSeconds { get; } = 1f;
        public float SoftDropSeconds { get; } = 0.05f;
        public float HorizontalRepeatDelaySeconds { get; } = 0.15f;
        public float HorizontalRepeatIntervalSeconds { get; } = 0.05f;
        public float LockDelaySeconds { get; } = 0.8f;
    }

    public sealed class ScoringDefinition
    {
        public int NormalCellUnitScore { get; } = 10;
        public int BaseMultiplierTwice { get; } = 2;
        public int MultiplierStepTwice { get; } = 1;
        public int MaximumBaseMultiplierTwice { get; } = 6;
        public int AdditionalWaveBonus { get; } = 30;
    }

    public sealed class GrowthDefinition
    {
        public BOMB_TYPE StartingBombType { get; } = BOMB_TYPE.B01_BASIC;
        public int StartingBombLevel { get; } = 1;
        public int StartingPlayerLevel { get; } = 1;
        public int MaximumOwnedBombTypes { get; } = 3;
        public int MaximumBombLevel { get; } = 3;
        public int ChoiceCount { get; } = 3;
        public int MaximumBombChoiceCount { get; } = 2;
        public int RelicChoiceCount { get; } = 3;
        public int ExperiencePerNormalCell { get; } = 1;
        public int FirstLevelExperience { get; } = 8;
        public int ExperienceIncreasePerLevel { get; } = 4;
    }

    public sealed class PresentationDefinition
    {
        public float PieceLockSeconds { get; } = 0.06f;
        public float WaveActivationSeconds { get; } = 0.12f;
        public float CellRemovalSeconds { get; } = 0.12f;
        public float GravitySeconds { get; } = 0.16f;
        public float LandingSeconds { get; } = 0.05f;
        public float ScoreTallySeconds { get; } = 0.20f;
        public float FastSpeedTimeScale { get; } = 0.5f;
    }
}
