using System;
using BombStack.Data;
using HM.CodeBase;
using UnityEngine;

namespace BombStack.UI
{
    /// <summary>UI 표시와 입력 요청만 관리합니다. 게임 진행과 생성/해제는 InGameController가 담당합니다.</summary>
    public sealed partial class InGameUIPresenter_presenter : APresenter
    {
        private readonly InGameUI_view VIEW;
        private readonly InGameUIModel_model MODEL;
        private static readonly Color MUTED_COLOR = new Color(0.51f, 0.61f, 0.72f);
        private static readonly Color TEXT_COLOR = new Color(0.91f, 0.94f, 0.98f);
        private bool _disposed;
        public bool IsOpen { get; private set; }
        public event Action<IN_GAME_ACTION, int> ActionRequested;

        public InGameUIPresenter_presenter(InGameUI_view view, InGameUIModel_model model)
        {
            VIEW = view != null ? view : throw new ArgumentNullException(nameof(view));
            MODEL = model ?? throw new ArgumentNullException(nameof(model));
        }

        public override void Open()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(InGameUIPresenter_presenter));
            if (IsOpen) return;
            VIEW.PauseRequested += OnPauseActioned;
            VIEW.MenuItemClicked += OnMenuItemClicked;
            VIEW.MenuItemFocused += OnMenuItemFocused;
            VIEW.Open();
            IsOpen = true;
        }

        public override void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (VIEW != null)
            {
                VIEW.PauseRequested -= OnPauseActioned;
                VIEW.MenuItemClicked -= OnMenuItemClicked;
                VIEW.MenuItemFocused -= OnMenuItemFocused;
                VIEW.Close();
            }
        }

        public override void Dispose()
        {
            if (_disposed) return;
            Close();
            if (VIEW != null) VIEW.Clear();
            ActionRequested = null;
            _disposed = true;
        }

        private void OnPauseActioned()
        {
            if (!_disposed && IsOpen && MODEL.InputEnabled && MODEL.Panel == IN_GAME_PANEL.NONE)
                ActionRequested?.Invoke(IN_GAME_ACTION.PAUSE, 0);
        }

        public void Refresh()
        {
            if (_disposed || !IsOpen || MODEL.Run == null) return;
            RunData run = MODEL.Run;
            GameDefinition definition = MODEL.Definition;
            StageDefinition stage = definition.GetStage(run.StageNumber);
            VIEW.SetStage($"STAGE {run.StageNumber:00} / {definition.Stages.Count:00}", $"{run.StageScore:N0} / {stage.TargetScore:N0}",
                $"남은 조각  {run.RemainingPieces} / {stage.PieceLimit}", $"TOTAL  {run.TotalScore:N0}", (float)run.StageScore / stage.TargetScore);
            VIEW.SetGrowth($"PLAYER  Lv.{run.Build.PlayerLevel}", $"XP  {run.Build.Experience} / {MODEL.RequiredExperience}",
                $"회수 {run.Build.RecoveryMasteryCount}   낙하 {run.Build.GravityMasteryCount}   폭탄 {run.Build.BombMasteryCount}", (float)run.Build.Experience / MODEL.RequiredExperience);

            for (int i = 0; i < definition.Growth.MaximumOwnedBombTypes; i++)
            {
                if (i >= run.Build.Bombs.Count) { VIEW.SetBomb(i, "—  비어 있는 폭탄 슬롯", MUTED_COLOR); continue; }
                BombProgressData bomb = run.Build.Bombs[i];
                VIEW.SetBomb(i, $"B{(int)bomb.Type}  {definition.GetBomb(bomb.Type).DisplayName}   Lv.{bomb.Level}", ACellGraphic.GetCellColor(new CellData(CELL_TYPE.BOMB, bomb.Type)));
            }
            for (int i = 0; i < definition.Stages.Count - 1; i++)
            {
                if (i >= run.Build.Relics.Count) { VIEW.SetRelic(i, "—  비어 있음", MUTED_COLOR); continue; }
                foreach (RelicDefinition relic in definition.Relics)
                    if (relic.Type == run.Build.Relics[i]) VIEW.SetRelic(i, $"{relic.Id}  {relic.DisplayName}", TEXT_COLOR);
            }
            bool heatproof = run.Build.Relics.Contains(RELIC_TYPE.R06_HEATPROOF_TRIGGER);
            for (int i = 0; i < definition.Supply.PreviewCount; i++)
            {
                QueuedPieceData queued = i < run.Supply.Preview.Count ? run.Supply.Preview[i] : null;
                if (queued == null) { VIEW.SetPreview(i, null, null, "", false, heatproof); continue; }
                PieceData piece = queued.Piece;
                string caption = piece.Attribute == CELL_TYPE.BOMB ? $"{definition.GetBomb(piece.BombType).DisplayName}  Lv.{BombLevel(run, piece.BombType)}"
                    : piece.Attribute == CELL_TYPE.TRIGGER ? "트리거 조각" : "일반 조각";
                bool reserved = run.Supply.FirstAppearances.Exists(item => item.QueuedPieceId == queued.Id);
                VIEW.SetPreview(i, definition.GetPiece(piece.Shape), piece, $"{i + 1:00}  /  {piece.Shape}   {caption}", reserved, heatproof);
            }
            VIEW.SetBoard(MODEL);
            string status = "쌓고 · 점화하고 · 다시 연결하세요";
            string breakdown = "금색 테두리: 첫 묶음의 제거 대상   /   초록 점선: 다른 B로 중계";
            if (MODEL.DisplayState == RUN_STATE.RESOLVING)
            {
                status = MODEL.PlaybackPhase == 0 ? "조각 고정" : MODEL.PlaybackPhase >= 5 ? "점수와 경험치 정산" : $"{MODEL.WaveNumber}차 폭발  /  " + (MODEL.PlaybackPhase == 3 ? "셀 낙하" : MODEL.PlaybackPhase == 4 ? "재점화 검사" : "폭탄 발동");
                breakdown = "모든 폭발과 낙하가 끝난 뒤 한 번 정산합니다.";
            }
            else if (run.LastPlacement != null && run.LastPlacement.IsSettled)
            {
                PlacementResultData result = run.LastPlacement;
                status = $"+{result.Score.TotalScore:N0}점   /   폭탄 {result.ActivatedBombCount}개 · 묶음 {result.WaveCount}회   /   XP +{result.TotalExperience}";
                string multiplier = (result.Score.MultiplierTwice / 2f).ToString("0.#");
                breakdown = result.ActivatedBombCount == 0 ? "준비 배치  /  이번 배치에서는 폭발이 발생하지 않았습니다."
                    : $"파괴 {result.Score.DestructionScore:N0} (×{multiplier})  +  중력 {result.Score.GravityBonus:N0}  +  폭탄 숙련 {result.Score.BombBonus:N0}  +  대량 파괴 {result.Score.MassDestructionBonus:N0}   |   N XP {result.NormalCellExperience} · 완료 XP {result.CompletionExperience}";
            }
            string hint = MODEL.UsingGamepad ? "D패드 이동·낙하   아래/왼쪽 버튼 회전\n오른쪽 숄더 고정   메뉴 버튼 일시정지"
                : "← → / A D 이동   ↑ X / Z 회전\n↓ / S 소프트 드롭   Space 고정   Esc 일시정지";
            VIEW.SetFooter(status, breakdown, hint, MODEL.HasTopWarning);
            RefreshMenu();
        }

        private static int BombLevel(RunData run, BOMB_TYPE type)
        {
            foreach (BombProgressData bomb in run.Build.Bombs) if (bomb.Type == type) return bomb.Level;
            return 1;
        }
    }
}
