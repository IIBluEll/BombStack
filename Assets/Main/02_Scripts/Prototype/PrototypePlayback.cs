using BombStack.Core;
using BombStack.Data;
using UnityEngine;

namespace BombStack.Prototype
{
    internal sealed class PrototypePlayback
    {
        private readonly PlacementResultData RESULT;
        private readonly PresentationDefinition TIMING;
        private float _elapsed;
        public BoardData Board { get; }
        public int Phase { get; private set; }
        public int WaveIndex { get; private set; }
        public bool Finished { get; private set; }
        public ExplosionWaveData Wave => WaveIndex < RESULT.Waves.Count ? RESULT.Waves[WaveIndex] : null;
        public float GravityProgress => Mathf.Clamp01(_elapsed / TIMING.GravitySeconds);

        public PrototypePlayback(BoardData lockedBoard, PlacementResultData result, PresentationDefinition timing)
        {
            Board = BoardRules.Copy(lockedBoard);
            RESULT = result;
            TIMING = timing;
        }

        public void Tick(float seconds, bool fast)
        {
            if (Finished) return;
            _elapsed += seconds / (fast ? TIMING.FastSpeedTimeScale : 1f);
            while (!Finished && _elapsed >= Duration())
            {
                _elapsed -= Duration();
                Advance();
            }
        }

        private float Duration()
        {
            switch (Phase)
            {
                case 0: return TIMING.PieceLockSeconds;
                case 1: return TIMING.WaveActivationSeconds;
                case 2: return TIMING.CellRemovalSeconds;
                case 3: return TIMING.GravitySeconds;
                case 4: return TIMING.LandingSeconds;
                default: return TIMING.ScoreTallySeconds;
            }
        }

        private void Advance()
        {
            switch (Phase)
            {
                case 0:
                    if (RESULT.WaveCount == 0) Finished = true;
                    else Phase = 1;
                    break;
                case 1:
                    foreach (RemovedCellData removed in Wave.RemovedCells) Board.SetCell(removed.Position, default);
                    Phase = 2;
                    break;
                case 2:
                    foreach (CellMovementData movement in Wave.Movements) Board.SetCell(movement.From, default);
                    Phase = 3;
                    break;
                case 3:
                    foreach (CellMovementData movement in Wave.Movements) Board.SetCell(movement.To, movement.Cell);
                    Phase = 4;
                    break;
                case 4:
                    WaveIndex++;
                    Phase = WaveIndex < RESULT.WaveCount ? 1 : 5;
                    break;
                default: Finished = true; break;
            }
        }
    }
}
