using System;
using System.Collections.Generic;

namespace BombStack.Data
{
    /// <summary>기획서 v1.2의 초기 기준값. 매 판 상태와 공유해도 변경되지 않는 정의입니다.</summary>
    public sealed class GameDefinition
    {
        public string DesignVersion { get; } = "1.2";
        public BoardDefinition Board { get; } = new BoardDefinition();
        public SupplyDefinition Supply { get; } = new SupplyDefinition();
        public TimingDefinition Timing { get; } = new TimingDefinition();
        public ScoringDefinition Scoring { get; } = new ScoringDefinition();
        public GrowthDefinition Growth { get; } = new GrowthDefinition();
        public PresentationDefinition Presentation { get; } = new PresentationDefinition();
        public IReadOnlyList<BombDefinition> Bombs { get; }
        public IReadOnlyList<PieceDefinition> Pieces { get; }
        public IReadOnlyList<StageDefinition> Stages { get; }
        public IReadOnlyList<CommonUpgradeDefinition> CommonUpgrades { get; }
        public IReadOnlyList<RelicDefinition> Relics { get; }

        private GameDefinition()
        {
            Bombs = Array.AsReadOnly(CreateBombs());
            Pieces = Array.AsReadOnly(CreatePieces());
            Stages = Array.AsReadOnly(new[]
            {
                new StageDefinition(1, 200, 20, 10, true),
                new StageDefinition(2, 350, 20, 10, true),
                new StageDefinition(3, 500, 20, 10, true),
                new StageDefinition(4, 650, 20, 10, true),
                new StageDefinition(5, 800, 20, 0, false)
            });
            CommonUpgrades = Array.AsReadOnly(new[]
            {
                new CommonUpgradeDefinition(COMMON_UPGRADE_TYPE.RECOVERY_MASTERY, "회수 숙련", 1),
                new CommonUpgradeDefinition(COMMON_UPGRADE_TYPE.GRAVITY_MASTERY, "낙하 숙련", 5),
                new CommonUpgradeDefinition(COMMON_UPGRADE_TYPE.BOMB_MASTERY, "폭탄 숙련", 5)
            });
            Relics = Array.AsReadOnly(new[]
            {
                new RelicDefinition(RELIC_TYPE.R01_PRECISION_BLAST, "R01", "정밀 폭파", RELIC_EFFECT.NORMAL_CELL_UNIT_BONUS, 2),
                new RelicDefinition(RELIC_TYPE.R02_CHAIN_AMPLIFIER, "R02", "연쇄 증폭기", RELIC_EFFECT.CHAIN_MULTIPLIER_BONUS_TWICE, 1, 2),
                new RelicDefinition(RELIC_TYPE.R03_IGNITION_AMPLIFIER, "R03", "점화 증폭기", RELIC_EFFECT.DIAGONAL_IGNITION, 0),
                new RelicDefinition(RELIC_TYPE.R04_GRAVITY_CHARGER, "R04", "낙하 충전기", RELIC_EFFECT.ADDITIONAL_WAVE_BONUS, 30),
                new RelicDefinition(RELIC_TYPE.R05_DEMOLITION_BONUS, "R05", "폭파 보너스", RELIC_EFFECT.MASS_DESTRUCTION_BONUS, 75, 10),
                new RelicDefinition(RELIC_TYPE.R06_HEATPROOF_TRIGGER, "R06", "내열 트리거", RELIC_EFFECT.PRESERVE_TRIGGERS, 0)
            });
        }

        public static GameDefinition CreateDefault() => new GameDefinition();

        public BombDefinition GetBomb(BOMB_TYPE type)
        {
            foreach (BombDefinition bomb in Bombs)
                if (bomb.Type == type) return bomb;
            throw new ArgumentOutOfRangeException(nameof(type));
        }

        public PieceDefinition GetPiece(PIECE_SHAPE shape)
        {
            foreach (PieceDefinition piece in Pieces)
                if (piece.Shape == shape) return piece;
            throw new ArgumentOutOfRangeException(nameof(shape));
        }

        public StageDefinition GetStage(int number)
        {
            if (number < 1 || number > Stages.Count) throw new ArgumentOutOfRangeException(nameof(number));
            return Stages[number - 1];
        }

        private static BombDefinition[] CreateBombs()
        {
            return new[]
            {
                new BombDefinition(BOMB_TYPE.B01_BASIC, "B01", "기본 폭탄",
                    new BombLevelDefinition(1, CreateBasicOffsets(1)),
                    new BombLevelDefinition(2, CreateBasicOffsets(2)),
                    new BombLevelDefinition(3, CreateBasicOffsets(3))),
                new BombDefinition(BOMB_TYPE.B02_HORIZONTAL, "B02", "가로 폭탄",
                    new BombLevelDefinition(1, CreateLineOffsets(3, true)),
                    new BombLevelDefinition(2, CreateLineOffsets(4, true)),
                    new BombLevelDefinition(3, CreateLineOffsets(5, true))),
                new BombDefinition(BOMB_TYPE.B03_VERTICAL, "B03", "세로 폭탄",
                    new BombLevelDefinition(1, CreateLineOffsets(3, false)),
                    new BombLevelDefinition(2, CreateLineOffsets(4, false)),
                    new BombLevelDefinition(3, CreateLineOffsets(5, false))),
                new BombDefinition(BOMB_TYPE.B04_RELAY, "B04", "중계 폭탄",
                    new BombLevelDefinition(1, CreateBasicOffsets(1), 3),
                    new BombLevelDefinition(2, CreateBasicOffsets(1), 4),
                    new BombLevelDefinition(3, CreateBasicOffsets(1), 5))
            };
        }

        private static GridPosition[] CreateBasicOffsets(int level)
        {
            var offsets = new List<GridPosition>();
            for (int y = -1; y <= 1; y++)
                for (int x = -1; x <= 1; x++) offsets.Add(new GridPosition(x, y));

            for (int distance = 2; distance <= level; distance++)
            {
                offsets.Add(new GridPosition(-distance, 0));
                offsets.Add(new GridPosition(distance, 0));
                offsets.Add(new GridPosition(0, -distance));
                offsets.Add(new GridPosition(0, distance));
            }
            return offsets.ToArray();
        }

        private static GridPosition[] CreateLineOffsets(int radius, bool horizontal)
        {
            var offsets = new GridPosition[radius * 2 + 1];
            for (int distance = -radius; distance <= radius; distance++)
                offsets[distance + radius] = horizontal ? new GridPosition(distance, 0) : new GridPosition(0, distance);
            return offsets;
        }

        private static PieceDefinition[] CreatePieces()
        {
            return new[]
            {
                new PieceDefinition(PIECE_SHAPE.I, new GridPosition(3, 3),
                    new GridPosition(0, 2), new GridPosition(1, 2), new GridPosition(2, 2), new GridPosition(3, 2)),
                new PieceDefinition(PIECE_SHAPE.O, new GridPosition(3, 3),
                    new GridPosition(1, 2), new GridPosition(2, 2), new GridPosition(1, 1), new GridPosition(2, 1)),
                new PieceDefinition(PIECE_SHAPE.T, new GridPosition(2, 4),
                    new GridPosition(1, 3), new GridPosition(0, 2), new GridPosition(1, 2), new GridPosition(2, 2)),
                new PieceDefinition(PIECE_SHAPE.S, new GridPosition(2, 4),
                    new GridPosition(1, 3), new GridPosition(2, 3), new GridPosition(0, 2), new GridPosition(1, 2)),
                new PieceDefinition(PIECE_SHAPE.Z, new GridPosition(2, 4),
                    new GridPosition(0, 3), new GridPosition(1, 3), new GridPosition(1, 2), new GridPosition(2, 2)),
                new PieceDefinition(PIECE_SHAPE.J, new GridPosition(2, 4),
                    new GridPosition(0, 3), new GridPosition(0, 2), new GridPosition(1, 2), new GridPosition(2, 2)),
                new PieceDefinition(PIECE_SHAPE.L, new GridPosition(2, 4),
                    new GridPosition(2, 3), new GridPosition(0, 2), new GridPosition(1, 2), new GridPosition(2, 2))
            };
        }
    }
}
