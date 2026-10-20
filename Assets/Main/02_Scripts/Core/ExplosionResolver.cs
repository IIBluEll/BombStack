using System;
using System.Collections.Generic;
using BombStack.Data;

namespace BombStack.Core
{
    public sealed class ExplosionResolver
    {
        private readonly GameDefinition DEFINITION;

        public ExplosionResolver(GameDefinition definition) => DEFINITION = definition ?? throw new ArgumentNullException(nameof(definition));

        public PlacementResultData Resolve(BoardData board, RunBuildData build)
        {
            var result = new PlacementResultData();
            ExplosionWaveData wave;
            while ((wave = FindFirstWave(board, build)) != null)
            {
                foreach (RemovedCellData removed in wave.RemovedCells)
                {
                    if (removed.Cell.Type == CELL_TYPE.NORMAL) result.DestroyedNormalCellCount++;
                    board.SetCell(removed.Position, default);
                }
                ApplyGravity(board, wave);
                result.ActivatedBombCount += wave.ActivatedBombs.Count;
                result.Waves.Add(wave);
            }
            CalculateScore(result, build);
            return result;
        }

        public ExplosionWaveData FindFirstWave(BoardData board, RunBuildData build)
        {
            var bombs = new List<GridPosition>();
            var found = new HashSet<GridPosition>();
            bool diagonal = build.Relics.Contains(RELIC_TYPE.R03_IGNITION_AMPLIFIER);
            for (int y = 0; y < board.Height; y++)
                for (int x = 0; x < board.Width; x++)
                {
                    var p = new GridPosition(x, y);
                    if (board.GetCell(p).Type != CELL_TYPE.BOMB) continue;
                    bool touching = false;
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if ((dx == 0 && dy == 0) || (!diagonal && Math.Abs(dx) + Math.Abs(dy) != 1)) continue;
                            var neighbor = new GridPosition(x + dx, y + dy);
                            if (board.Contains(neighbor) && board.GetCell(neighbor).Type == CELL_TYPE.TRIGGER) touching = true;
                        }
                    if (touching) { found.Add(p); bombs.Add(p); }
                }
            if (bombs.Count == 0) return null;

            var wave = new ExplosionWaveData();
            var destruction = new HashSet<GridPosition>();
            // 발견 순서대로 확장하므로 HashSet 열거 순서에 판정/재생 순서가 의존하지 않습니다.
            for (int index = 0; index < bombs.Count; index++)
            {
                GridPosition p = bombs[index];
                BOMB_TYPE type = board.GetCell(p).BombType;
                int level = GetBombLevel(build, type);
                BombLevelDefinition bomb = DEFINITION.GetBomb(type).GetLevel(level);
                wave.ActivatedBombs.Add(new BombActivationData(p, type, level));
                foreach (GridPosition offset in bomb.DestructionOffsets)
                {
                    var target = new GridPosition(p.X + offset.X, p.Y + offset.Y);
                    if (!board.Contains(target)) continue;
                    destruction.Add(target);
                    if (board.GetCell(target).Type == CELL_TYPE.BOMB && found.Add(target)) bombs.Add(target);
                }
                for (int distance = 1; distance <= bomb.RelayDistance; distance++)
                    for (int direction = 0; direction < 4; direction++)
                    {
                        int dx = direction == 0 ? -distance : direction == 1 ? distance : 0;
                        int dy = direction == 2 ? -distance : direction == 3 ? distance : 0;
                        var target = new GridPosition(p.X + dx, p.Y + dy);
                        if (!board.Contains(target) || board.GetCell(target).Type != CELL_TYPE.BOMB) continue;
                        wave.RelayConnections.Add(new RelayConnectionData(p, target));
                        if (found.Add(target)) bombs.Add(target);
                    }
            }

            bool preserve = build.Relics.Contains(RELIC_TYPE.R06_HEATPROOF_TRIGGER);
            for (int y = 0; y < board.Height; y++)
                for (int x = 0; x < board.Width; x++)
                {
                    var p = new GridPosition(x, y);
                    CellData cell = board.GetCell(p);
                    if (!destruction.Contains(p) || cell.Type == CELL_TYPE.EMPTY || (preserve && cell.Type == CELL_TYPE.TRIGGER)) continue;
                    wave.RemovedCells.Add(new RemovedCellData(p, cell));
                }
            return wave;
        }

        public static int GetBombLevel(RunBuildData build, BOMB_TYPE type)
        {
            foreach (BombProgressData bomb in build.Bombs)
                if (bomb.Type == type) return bomb.Level;
            throw new InvalidOperationException($"미보유 폭탄이 보드/공급에 있습니다: {type}");
        }

        private static void ApplyGravity(BoardData board, ExplosionWaveData wave)
        {
            for (int x = 0; x < board.Width; x++)
            {
                int destination = 0;
                for (int y = 0; y < board.Height; y++)
                {
                    var from = new GridPosition(x, y);
                    CellData cell = board.GetCell(from);
                    if (cell.Type == CELL_TYPE.EMPTY) continue;
                    if (destination != y)
                    {
                        var to = new GridPosition(x, destination);
                        board.SetCell(from, default);
                        board.SetCell(to, cell);
                        wave.Movements.Add(new CellMovementData(from, to, cell));
                    }
                    destination++;
                }
            }
        }

        public void CalculateScore(PlacementResultData result, RunBuildData build)
        {
            if (result.ActivatedBombCount == 0) return;
            int b = result.ActivatedBombCount;
            long unit = DEFINITION.Scoring.NormalCellUnitScore + build.RecoveryMasteryCount * UpgradeValue(COMMON_UPGRADE_TYPE.RECOVERY_MASTERY)
                + RelicValue(build, RELIC_EFFECT.NORMAL_CELL_UNIT_BONUS);
            int multiplier = Math.Min(DEFINITION.Scoring.MaximumBaseMultiplierTwice,
                DEFINITION.Scoring.BaseMultiplierTwice + DEFINITION.Scoring.MultiplierStepTwice * (b - 1));
            foreach (RelicDefinition relic in DEFINITION.Relics)
                if (relic.Effect == RELIC_EFFECT.CHAIN_MULTIPLIER_BONUS_TWICE && build.Relics.Contains(relic.Type) && b >= relic.Threshold)
                    multiplier += relic.Value;
            result.Score.MultiplierTwice = multiplier;
            result.Score.DestructionScore = result.DestroyedNormalCellCount * unit * multiplier / 2;
            result.Score.GravityBonus = Math.Max(0, result.WaveCount - 1) * (DEFINITION.Scoring.AdditionalWaveBonus
                + build.GravityMasteryCount * UpgradeValue(COMMON_UPGRADE_TYPE.GRAVITY_MASTERY) + RelicValue(build, RELIC_EFFECT.ADDITIONAL_WAVE_BONUS));
            result.Score.BombBonus = b * build.BombMasteryCount * UpgradeValue(COMMON_UPGRADE_TYPE.BOMB_MASTERY);
            foreach (RelicDefinition relic in DEFINITION.Relics)
                if (relic.Effect == RELIC_EFFECT.MASS_DESTRUCTION_BONUS && build.Relics.Contains(relic.Type) && result.DestroyedNormalCellCount >= relic.Threshold)
                    result.Score.MassDestructionBonus += relic.Value;
            result.NormalCellExperience = result.DestroyedNormalCellCount * DEFINITION.Growth.ExperiencePerNormalCell;
        }

        private int UpgradeValue(COMMON_UPGRADE_TYPE type)
        {
            foreach (CommonUpgradeDefinition upgrade in DEFINITION.CommonUpgrades)
                if (upgrade.Type == type) return upgrade.BonusPerStack;
            throw new ArgumentOutOfRangeException(nameof(type));
        }

        private int RelicValue(RunBuildData build, RELIC_EFFECT effect)
        {
            foreach (RelicDefinition relic in DEFINITION.Relics)
                if (relic.Effect == effect && build.Relics.Contains(relic.Type)) return relic.Value;
            return 0;
        }
    }
}
