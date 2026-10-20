using BombStack.Data;

namespace BombStack.Core
{
    public static class RecordRules
    {
        public static bool IsBetter(RunRecordData candidate, RunRecordData current)
        {
            if (candidate == null || candidate.EndReason == RUN_END_REASON.NONE || candidate.EndReason == RUN_END_REASON.ABANDONED) return false;
            if (current == null) return true;
            bool victory = candidate.EndReason == RUN_END_REASON.VICTORY;
            bool previousVictory = current.EndReason == RUN_END_REASON.VICTORY;
            if (victory != previousVictory) return victory;
            if (!victory && candidate.ReachedStage != current.ReachedStage) return candidate.ReachedStage > current.ReachedStage;
            if (candidate.TotalScore != current.TotalScore) return candidate.TotalScore > current.TotalScore;
            return candidate.PlacedPieceCount < current.PlacedPieceCount;
        }
    }
}
