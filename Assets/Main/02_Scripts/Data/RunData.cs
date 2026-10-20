using System;
using System.Collections.Generic;

namespace BombStack.Data
{
    /// <summary>한 판의 상태 컨테이너. 생성만으로 조각 추첨이나 게임 진행을 시작하지 않습니다.</summary>
    public sealed class RunData
    {
        public string DesignVersion { get; }
        public int Seed { get; }
        public BoardData Board { get; }
        public SupplyData Supply { get; } = new SupplyData();
        public RunBuildData Build { get; }
        public ActivePieceData ActivePiece { get; set; }
        public RUN_STATE State { get; set; } = RUN_STATE.PREPARING;
        public RUN_STATE StateBeforePause { get; set; } = RUN_STATE.PREPARING;
        public RUN_END_REASON EndReason { get; set; }
        public int StageNumber { get; set; } = 1;
        public long StageScore { get; set; }
        public int RemainingPieces { get; set; }
        public long TotalScore { get; set; }
        public int PlacedPieceCount { get; set; }
        public int MaximumBombCount { get; set; }
        public int MaximumWaveCount { get; set; }
        public double PlaySeconds { get; set; }
        public bool StageCompletionPending { get; set; }
        public bool StageCompletionExperienceGranted { get; set; }
        public List<GrowthChoiceData> GrowthChoices { get; } = new List<GrowthChoiceData>();
        public List<RELIC_TYPE> RelicChoices { get; } = new List<RELIC_TYPE>();
        public PlacementResultData LastPlacement { get; set; }

        public RunData(GameDefinition definition, int seed)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            DesignVersion = definition.DesignVersion;
            Seed = seed;
            Board = new BoardData(definition.Board.Width, definition.Board.Height);
            Build = new RunBuildData(definition.Growth);
            RemainingPieces = definition.GetStage(1).PieceLimit;
        }
    }
}
