using System;

namespace BombStack.Data
{
    // 저장 DTO는 직렬화 가능한 public 필드로 구성하며, 진행 중 RunData는 포함하지 않습니다.
    [Serializable]
    public sealed class GameSettingsData
    {
        // 음량/언어/초기 창 크기는 기획서에 없으므로 정한 기본값입니다. 적용/범위 보정은 설정 처리에서 합니다.
        public GAME_LANGUAGE Language = GAME_LANGUAGE.KOREAN;
        public WINDOW_MODE WindowMode = WINDOW_MODE.WINDOWED;
        public int WindowWidth = 1280;
        public int WindowHeight = 720;
        public int MasterVolume = 100;
        public int MusicVolume = 100;
        public int SoundEffectVolume = 100;
        public bool ScreenShakeEnabled = true;
        public FLASH_MODE FlashMode = FLASH_MODE.LOW;
        public PRESENTATION_SPEED PresentationSpeed = PRESENTATION_SPEED.NORMAL;
    }

    [Serializable]
    public sealed class BombRecordData
    {
        public BOMB_TYPE Type;
        public int Level;
    }

    [Serializable]
    public sealed class RunRecordData
    {
        public string DesignVersion;
        public RUN_END_REASON EndReason;
        public int ReachedStage;
        public long TotalScore;
        public int PlacedPieceCount;
        public int MaximumBombCount;
        public int MaximumWaveCount;
        public double PlaySeconds;
        public int PlayerLevel;
        public BombRecordData[] Bombs = Array.Empty<BombRecordData>();
        public long RecoveryMasteryCount;
        public long GravityMasteryCount;
        public long BombMasteryCount;
        public RELIC_TYPE[] Relics = Array.Empty<RELIC_TYPE>();

        /// <summary>결과 화면에서 사용할 독립 복사본. 최고 기록 비교/저장은 별도 구현합니다.</summary>
        public static RunRecordData FromRun(RunData run)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));
            if (run.State != RUN_STATE.RESULT || run.EndReason == RUN_END_REASON.NONE)
                throw new InvalidOperationException("종료된 판만 기록으로 만들 수 있습니다.");

            var bombs = new BombRecordData[run.Build.Bombs.Count];
            for (int i = 0; i < bombs.Length; i++)
                bombs[i] = new BombRecordData { Type = run.Build.Bombs[i].Type, Level = run.Build.Bombs[i].Level };

            return new RunRecordData
            {
                DesignVersion = run.DesignVersion,
                EndReason = run.EndReason,
                ReachedStage = run.StageNumber,
                TotalScore = run.TotalScore,
                PlacedPieceCount = run.PlacedPieceCount,
                MaximumBombCount = run.MaximumBombCount,
                MaximumWaveCount = run.MaximumWaveCount,
                PlaySeconds = run.PlaySeconds,
                PlayerLevel = run.Build.PlayerLevel,
                Bombs = bombs,
                RecoveryMasteryCount = run.Build.RecoveryMasteryCount,
                GravityMasteryCount = run.Build.GravityMasteryCount,
                BombMasteryCount = run.Build.BombMasteryCount,
                Relics = run.Build.Relics.ToArray()
            };
        }
    }

    [Serializable]
    public sealed class PersistentData
    {
        public int Version = 1;
        public GameSettingsData Settings = new GameSettingsData();
        public bool HasSeenTutorial;
        public RunRecordData BestRecord;
        public int TotalPlayCount;
        public int TotalVictoryCount;
    }
}
