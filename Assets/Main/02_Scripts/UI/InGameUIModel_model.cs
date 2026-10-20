using System;
using BombStack.Core;
using BombStack.Data;

namespace BombStack.UI
{
    /// <summary>현재 프레임에서 표시할 데이터만 보관합니다. 게임 진행과 정산은 GameSession의 책임입니다.</summary>
    public sealed class InGameUIModel_model
    {
        public GameDefinition Definition { get; }
        public RunData Run { get; private set; }
        public BoardData Board { get; private set; }
        public ExplosionWaveData Wave { get; private set; }
        public int PlaybackPhase { get; private set; }
        public int WaveNumber { get; private set; }
        public float GravityProgress { get; private set; }
        public bool UsingGamepad { get; private set; }
        public bool HasTopWarning { get; private set; }
        public IN_GAME_PANEL Panel { get; private set; }
        public int MenuVersion { get; private set; }
        public int GuidePage { get; private set; }
        public bool FastPresentation { get; private set; }
        public bool CanExit { get; private set; }
        public bool InputEnabled { get; private set; }
        public RUN_STATE DisplayState => Run.State == RUN_STATE.PAUSED ? Run.StateBeforePause : Run.State;
        public GridPosition[] ActivePositions { get; private set; }
        public GridPosition[] GhostPositions { get; private set; }
        public long RequiredExperience => Definition.Growth.FirstLevelExperience + (long)Definition.Growth.ExperienceIncreasePerLevel * (Run.Build.PlayerLevel - 1);

        public InGameUIModel_model(GameDefinition definition) => Definition = definition ?? throw new ArgumentNullException(nameof(definition));

        public void Capture(RunData run, BoardData board, ExplosionWaveData wave, int playbackPhase, int waveNumber, float gravityProgress,
            bool usingGamepad, IN_GAME_PANEL panel, int menuVersion, int guidePage, bool fastPresentation, bool canExit, bool inputEnabled)
        {
            Run = run;
            Board = board;
            Wave = wave;
            PlaybackPhase = playbackPhase;
            WaveNumber = waveNumber;
            GravityProgress = gravityProgress;
            UsingGamepad = usingGamepad;
            Panel = panel;
            MenuVersion = menuVersion;
            GuidePage = guidePage;
            FastPresentation = fastPresentation;
            CanExit = canExit;
            InputEnabled = inputEnabled;
            ActivePositions = null;
            GhostPositions = null;
            if (DisplayState == RUN_STATE.PIECE_CONTROL && run.ActivePiece != null)
            {
                ActivePieceData active = run.ActivePiece;
                ActivePositions = BoardRules.GetPositions(Definition, active.Piece, active.Origin, active.Rotation);
                GridPosition landing = BoardRules.GetLandingOrigin(Definition, run.Board, active);
                GhostPositions = BoardRules.GetPositions(Definition, active.Piece, landing, active.Rotation);
            }
            HasTopWarning = false;
            for (int y = Definition.Board.WarningStartRow; y < board.Height && !HasTopWarning; y++)
                for (int x = 0; x < board.Width; x++)
                    if (board.GetCell(new GridPosition(x, y)).Type != CELL_TYPE.EMPTY) { HasTopWarning = true; break; }
        }
    }
}
