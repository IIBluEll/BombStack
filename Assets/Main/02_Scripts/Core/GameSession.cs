using System;
using System.Collections.Generic;
using BombStack.Data;

namespace BombStack.Core
{
    /// <summary>조작/판정/보상 순서를 관리합니다. Unity 객체나 연출 시간에 의존하지 않습니다.</summary>
    public sealed class GameSession
    {
        private readonly GameDefinition DEFINITION;
        private readonly ExplosionResolver RESOLVER;
        private bool _softDrop;
        public RunData Run { get; }
        public BoardData LastLockedBoard { get; private set; }
        public long RequiredExperience => DEFINITION.Growth.FirstLevelExperience + (long)DEFINITION.Growth.ExperienceIncreasePerLevel * (Run.Build.PlayerLevel - 1);

        public GameSession(GameDefinition definition, int seed)
        {
            DEFINITION = definition ?? throw new ArgumentNullException(nameof(definition));
            RESOLVER = new ExplosionResolver(definition);
            Run = new RunData(definition, seed);
            SupplyRules.Initialize(Run);
            SpawnNext();
        }

        public bool Move(int direction)
        {
            if (Run.State != RUN_STATE.PIECE_CONTROL || Math.Abs(direction) != 1) return false;
            return TryMove(direction, 0);
        }

        public bool Rotate(bool clockwise)
        {
            if (Run.State != RUN_STATE.PIECE_CONTROL) return false;
            ActivePieceData active = Run.ActivePiece;
            PIECE_ROTATION rotation = (PIECE_ROTATION)(((int)active.Rotation + (clockwise ? 1 : 3)) % 4);
            foreach (GridPosition kick in DEFINITION.Supply.RotationKicks)
            {
                var origin = new GridPosition(active.Origin.X + kick.X, active.Origin.Y + kick.Y);
                if (!BoardRules.CanPlace(DEFINITION, Run.Board, active.Piece, origin, rotation)) continue;
                active.Origin = origin;
                active.Rotation = rotation;
                UpdateGroundContact();
                return true;
            }
            return false;
        }

        public void Tick(float seconds, bool softDrop)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            if (Run.State == RUN_STATE.RESOLVING) { Run.PlaySeconds += seconds; return; }
            if (Run.State != RUN_STATE.PIECE_CONTROL) return;
            Run.PlaySeconds += seconds;
            ActivePieceData active = Run.ActivePiece;
            if (_softDrop != softDrop) { active.FallElapsedSeconds = 0; _softDrop = softDrop; }
            if (active.HasTouchedGround) active.LockElapsedSeconds += seconds;
            active.FallElapsedSeconds += seconds;
            float interval = softDrop ? DEFINITION.Timing.SoftDropSeconds : DEFINITION.Timing.AutomaticFallSeconds;
            while (active.FallElapsedSeconds >= interval)
            {
                active.FallElapsedSeconds -= interval;
                if (!TryMove(0, -1)) { active.FallElapsedSeconds = 0; break; }
            }
            UpdateGroundContact();
            if (active.HasTouchedGround && active.LockElapsedSeconds >= DEFINITION.Timing.LockDelaySeconds && IsGrounded()) LockPiece();
        }

        public void HardDrop()
        {
            if (Run.State != RUN_STATE.PIECE_CONTROL) return;
            Run.ActivePiece.Origin = BoardRules.GetLandingOrigin(DEFINITION, Run.Board, Run.ActivePiece);
            LockPiece();
        }

        public ExplosionWaveData GetLandingPreview()
        {
            if (Run.State != RUN_STATE.PIECE_CONTROL) return null;
            BoardData board = BoardRules.Copy(Run.Board);
            ActivePieceData active = Run.ActivePiece;
            BoardRules.Place(DEFINITION, board, active.Piece, BoardRules.GetLandingOrigin(DEFINITION, board, active), active.Rotation);
            return RESOLVER.FindFirstWave(board, Run.Build);
        }

        private bool TryMove(int dx, int dy)
        {
            ActivePieceData active = Run.ActivePiece;
            var origin = new GridPosition(active.Origin.X + dx, active.Origin.Y + dy);
            if (!BoardRules.CanPlace(DEFINITION, Run.Board, active.Piece, origin, active.Rotation)) return false;
            active.Origin = origin;
            UpdateGroundContact();
            return true;
        }

        private bool IsGrounded()
        {
            ActivePieceData active = Run.ActivePiece;
            return !BoardRules.CanPlace(DEFINITION, Run.Board, active.Piece, new GridPosition(active.Origin.X, active.Origin.Y - 1), active.Rotation);
        }

        private void UpdateGroundContact()
        {
            if (!Run.ActivePiece.HasTouchedGround && IsGrounded()) Run.ActivePiece.HasTouchedGround = true;
        }

        private void LockPiece()
        {
            ActivePieceData active = Run.ActivePiece;
            BoardRules.Place(DEFINITION, Run.Board, active.Piece, active.Origin, active.Rotation);
            Run.ActivePiece = null;
            Run.RemainingPieces--;
            Run.PlacedPieceCount++;
            Run.State = RUN_STATE.RESOLVING;
            LastLockedBoard = BoardRules.Copy(Run.Board);
            Run.LastPlacement = RESOLVER.Resolve(Run.Board, Run.Build);
        }

        /// <summary>연출이 끝난 뒤 한 번 호출합니다. 중복 호출로 점수/XP를 지급하지 않습니다.</summary>
        public bool FinishResolution()
        {
            if (Run.State != RUN_STATE.RESOLVING || Run.LastPlacement == null || Run.LastPlacement.IsSettled) return false;
            PlacementResultData result = Run.LastPlacement;
            result.IsSettled = true;
            Run.StageScore += result.Score.TotalScore;
            Run.TotalScore += result.Score.TotalScore;
            Run.Build.Experience += result.NormalCellExperience;
            Run.MaximumBombCount = Math.Max(Run.MaximumBombCount, result.ActivatedBombCount);
            Run.MaximumWaveCount = Math.Max(Run.MaximumWaveCount, result.WaveCount);
            StageDefinition stage = DEFINITION.GetStage(Run.StageNumber);
            bool achieved = Run.StageScore >= stage.TargetScore;
            if (achieved && Run.StageNumber == DEFINITION.Stages.Count) { End(RUN_END_REASON.VICTORY); return true; }
            if (!achieved && Run.RemainingPieces == 0) { End(RUN_END_REASON.PIECE_LIMIT); return true; }
            PieceData next = Run.Supply.Preview[0].Piece;
            if (!BoardRules.CanPlace(DEFINITION, Run.Board, next, DEFINITION.Board.SpawnOrigin, PIECE_ROTATION.SPAWN))
            { End(RUN_END_REASON.SPAWN_BLOCKED); return true; }
            if (achieved)
            {
                Run.StageCompletionPending = true;
                if (!Run.StageCompletionExperienceGranted)
                {
                    result.CompletionExperience = stage.CompletionExperience;
                    Run.Build.Experience += result.CompletionExperience;
                    Run.StageCompletionExperienceGranted = true;
                }
            }
            AdvanceRewards();
            return true;
        }

        private void AdvanceRewards()
        {
            Run.GrowthChoices.Clear();
            if (Run.Build.Experience >= RequiredExperience)
            {
                var core = new List<GrowthChoiceData>();
                foreach (BombDefinition bomb in DEFINITION.Bombs)
                {
                    BombProgressData owned = Run.Build.Bombs.Find(item => item.Type == bomb.Type);
                    if (owned == null && Run.Build.Bombs.Count < DEFINITION.Growth.MaximumOwnedBombTypes) core.Add(GrowthChoiceData.ForBomb(bomb.Type, true));
                    else if (owned != null && owned.Level < DEFINITION.Growth.MaximumBombLevel) core.Add(GrowthChoiceData.ForBomb(bomb.Type, false));
                }
                SupplyRules.Shuffle(core, Run.Supply.RandomStreams.GrowthChoice);
                int coreCount = Math.Min(core.Count, DEFINITION.Growth.MaximumBombChoiceCount);
                for (int i = 0; i < coreCount; i++) Run.GrowthChoices.Add(core[i]);
                var common = new List<CommonUpgradeDefinition>(DEFINITION.CommonUpgrades);
                SupplyRules.Shuffle(common, Run.Supply.RandomStreams.GrowthChoice);
                for (int i = 0; Run.GrowthChoices.Count < DEFINITION.Growth.ChoiceCount; i++)
                    Run.GrowthChoices.Add(GrowthChoiceData.ForCommonUpgrade(common[i].Type));
                Run.State = RUN_STATE.LEVEL_UP_CHOICE;
                return;
            }
            if (Run.StageCompletionPending)
            {
                Run.RelicChoices.Clear();
                foreach (RelicDefinition relic in DEFINITION.Relics)
                    if (!Run.Build.Relics.Contains(relic.Type)) Run.RelicChoices.Add(relic.Type);
                SupplyRules.Shuffle(Run.RelicChoices, Run.Supply.RandomStreams.RelicChoice);
                if (Run.RelicChoices.Count > DEFINITION.Growth.RelicChoiceCount)
                    Run.RelicChoices.RemoveRange(DEFINITION.Growth.RelicChoiceCount, Run.RelicChoices.Count - DEFINITION.Growth.RelicChoiceCount);
                Run.State = RUN_STATE.RELIC_CHOICE;
                return;
            }
            SpawnNext();
        }

        public bool ChooseGrowth(int index)
        {
            if (Run.State != RUN_STATE.LEVEL_UP_CHOICE || index < 0 || index >= Run.GrowthChoices.Count) return false;
            GrowthChoiceData choice = Run.GrowthChoices[index];
            if (choice.Type == GROWTH_CHOICE_TYPE.ACQUIRE_BOMB)
            {
                Run.Build.Bombs.Add(new BombProgressData(choice.BombType, 1));
                SupplyRules.OnBombAcquired(Run, choice.BombType);
            }
            else if (choice.Type == GROWTH_CHOICE_TYPE.UPGRADE_BOMB) Run.Build.Bombs.Find(item => item.Type == choice.BombType).Level++;
            else
            {
                switch (choice.CommonUpgradeType.Value)
                {
                    case COMMON_UPGRADE_TYPE.RECOVERY_MASTERY: Run.Build.RecoveryMasteryCount++; break;
                    case COMMON_UPGRADE_TYPE.GRAVITY_MASTERY: Run.Build.GravityMasteryCount++; break;
                    case COMMON_UPGRADE_TYPE.BOMB_MASTERY: Run.Build.BombMasteryCount++; break;
                }
            }
            Run.Build.Experience -= RequiredExperience;
            Run.Build.PlayerLevel++;
            AdvanceRewards();
            return true;
        }

        public bool ChooseRelic(int index)
        {
            if (Run.State != RUN_STATE.RELIC_CHOICE || index < 0 || index >= Run.RelicChoices.Count) return false;
            Run.Build.Relics.Add(Run.RelicChoices[index]);
            Run.RelicChoices.Clear();
            Run.StageNumber++;
            Run.StageScore = 0;
            Run.RemainingPieces = DEFINITION.GetStage(Run.StageNumber).PieceLimit;
            Run.StageCompletionPending = false;
            Run.StageCompletionExperienceGranted = false;
            SpawnNext();
            return true;
        }

        private void SpawnNext()
        {
            PieceData piece = SupplyRules.ConsumeNext(DEFINITION, Run);
            if (!BoardRules.CanPlace(DEFINITION, Run.Board, piece, DEFINITION.Board.SpawnOrigin, PIECE_ROTATION.SPAWN))
            { End(RUN_END_REASON.SPAWN_BLOCKED); return; }
            Run.ActivePiece = new ActivePieceData(piece, DEFINITION.Board.SpawnOrigin);
            Run.State = RUN_STATE.PIECE_CONTROL;
            _softDrop = false;
            UpdateGroundContact();
        }

        public void Pause()
        {
            if (Run.State == RUN_STATE.PAUSED || Run.State == RUN_STATE.RESULT) return;
            Run.StateBeforePause = Run.State;
            Run.State = RUN_STATE.PAUSED;
        }

        public void Resume()
        {
            if (Run.State == RUN_STATE.PAUSED) Run.State = Run.StateBeforePause;
        }

        public void Abandon() => End(RUN_END_REASON.ABANDONED);

        private void End(RUN_END_REASON reason)
        {
            Run.EndReason = reason;
            Run.State = RUN_STATE.RESULT;
            Run.ActivePiece = null;
            Run.GrowthChoices.Clear();
            Run.RelicChoices.Clear();
        }
    }
}
