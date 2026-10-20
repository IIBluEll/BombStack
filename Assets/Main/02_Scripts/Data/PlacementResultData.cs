using System.Collections.Generic;

namespace BombStack.Data
{
    public readonly struct BombActivationData
    {
        public GridPosition Position { get; }
        public BOMB_TYPE BombType { get; }
        public int Level { get; }

        public BombActivationData(GridPosition position, BOMB_TYPE bombType, int level)
        {
            Position = position;
            BombType = bombType;
            Level = level;
        }
    }

    public readonly struct RelayConnectionData
    {
        public GridPosition Source { get; }
        public GridPosition Target { get; }

        public RelayConnectionData(GridPosition source, GridPosition target)
        {
            Source = source;
            Target = target;
        }
    }

    public readonly struct RemovedCellData
    {
        public GridPosition Position { get; }
        public CellData Cell { get; }

        public RemovedCellData(GridPosition position, CellData cell)
        {
            Position = position;
            Cell = cell;
        }
    }

    public readonly struct CellMovementData
    {
        public GridPosition From { get; }
        public GridPosition To { get; }
        public CellData Cell { get; }

        public CellMovementData(GridPosition from, GridPosition to, CellData cell)
        {
            From = from;
            To = to;
            Cell = cell;
        }
    }

    public sealed class ExplosionWaveData
    {
        // 발동/제거는 중력 전, 이동은 제거 후 기록입니다. 다른 묶음의 같은 좌표는 다른 셀입니다.
        public List<BombActivationData> ActivatedBombs { get; } = new List<BombActivationData>();
        public List<RelayConnectionData> RelayConnections { get; } = new List<RelayConnectionData>();
        public List<RemovedCellData> RemovedCells { get; } = new List<RemovedCellData>();
        public List<CellMovementData> Movements { get; } = new List<CellMovementData>();
    }

    public sealed class PlacementScoreData
    {
        public int MultiplierTwice { get; set; }
        public long DestructionScore { get; set; }
        public long GravityBonus { get; set; }
        public long BombBonus { get; set; }
        public long MassDestructionBonus { get; set; }
        public long TotalScore => DestructionScore + GravityBonus + BombBonus + MassDestructionBonus;
    }

    /// <summary>판정 결과와 재생 기록만 보유합니다. 집계/중복 제거/정산은 구현하지 않습니다.</summary>
    public sealed class PlacementResultData
    {
        public List<ExplosionWaveData> Waves { get; } = new List<ExplosionWaveData>();
        public int DestroyedNormalCellCount { get; set; }
        public int ActivatedBombCount { get; set; }
        public int WaveCount => Waves.Count;
        public PlacementScoreData Score { get; } = new PlacementScoreData();
        public int NormalCellExperience { get; set; }
        public int CompletionExperience { get; set; }
        public int TotalExperience => NormalCellExperience + CompletionExperience;
        public bool IsSettled { get; set; }
    }
}
