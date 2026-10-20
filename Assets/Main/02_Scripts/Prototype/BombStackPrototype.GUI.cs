using System;
using System.Text;
using BombStack.Core;
using BombStack.Data;
using UnityEngine;

namespace BombStack.Prototype
{
    public sealed partial class BombStackPrototype
    {
        private static readonly Color BACKGROUND = new Color(0.035f, 0.05f, 0.08f);
        private static readonly Color PANEL = new Color(0.065f, 0.09f, 0.14f);
        private static readonly Color MUTED = new Color(0.53f, 0.63f, 0.74f);
        private static readonly Color ACCENT = new Color(0.3f, 0.92f, 0.77f);
        private static readonly Color GOLD = new Color(1f, 0.78f, 0.34f);
        private const float CELL_SIZE = 28;
        private const float BOARD_X = 450;
        private const float BOARD_Y = 122;
        private int _guiRepaintCount;

        private void DrawPrototype()
        {
            if (Event.current.type == EventType.Repaint) _guiRepaintCount++;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280 * scale) / 2, (Screen.height - 720 * scale) / 2, 0), Quaternion.identity, Vector3.one * scale);
            if (_labelStyle == null) _labelStyle = new GUIStyle(GUI.skin.label) { font = _font, wordWrap = true };
            Fill(new Rect(-5000, -5000, 10000, 10000), BACKGROUND);
            Text(new Rect(48, 28, 600, 36), "BOMB STACK", 28, Color.white, FontStyle.Bold);
            Text(new Rect(48, 67, 760, 24), "쌓고 · 점화하고 · 다시 연결하세요", 15, MUTED);
            Text(new Rect(920, 40, 310, 28), "PLAYABLE PROTOTYPE  /  v1.2", 13, ACCENT);
            if (_session != null && _screen != SCREEN.MENU) DrawGame();
            else DrawMainMenu();
            if (_screen == SCREEN.GUIDE) DrawGuide();
            else if (_screen == SCREEN.CONFIRM) DrawConfirmation();
            else if (_screen == SCREEN.GAME)
            {
                switch (_session.Run.State)
                {
                    case RUN_STATE.LEVEL_UP_CHOICE: DrawGrowthChoices(); break;
                    case RUN_STATE.RELIC_CHOICE: DrawRelicChoices(); break;
                    case RUN_STATE.PAUSED: DrawPause(); break;
                    case RUN_STATE.RESULT: DrawResult(); break;
                }
            }
            Text(new Rect(48, 682, 1180, 26), string.IsNullOrEmpty(_notice) ? "N 일반   /   B1 기본   B2 가로   B3 세로   B4 중계   /   T 트리거   ·   줄은 지워지지 않습니다" : _notice, 13, MUTED);
            GUI.matrix = Matrix4x4.identity;
        }

        private static void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static void Outline(Rect rect, Color color, float thickness = 2)
        {
            Fill(new Rect(rect.x, rect.y, rect.width, thickness), color);
            Fill(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            Fill(new Rect(rect.x, rect.y, thickness, rect.height), color);
            Fill(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private void Text(Rect rect, string text, int size, Color color, FontStyle fontStyle = FontStyle.Normal, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            _labelStyle.fontSize = size;
            _labelStyle.fontStyle = fontStyle;
            _labelStyle.alignment = alignment;
            _labelStyle.normal.textColor = color;
            GUI.Label(rect, text, _labelStyle);
        }

        private bool MenuButton(Rect rect, string text, int index)
        {
            bool selected = _selection == index;
            Fill(rect, selected ? new Color(0.10f, 0.22f, 0.25f) : PANEL);
            Outline(rect, selected ? ACCENT : new Color(0.15f, 0.21f, 0.29f), selected ? 2 : 1);
            Text(new Rect(rect.x + 16, rect.y + 12, rect.width - 32, rect.height - 16), text, 18, selected ? ACCENT : Color.white);
            bool clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none);
            return clicked && !_waitingForRelease;
        }

        private void DrawMainMenu()
        {
            Text(new Rect(70, 160, 570, 100), "한 번의 폭발보다\n다음 폭발을 설계하세요.", 34, Color.white, FontStyle.Bold);
            Text(new Rect(70, 280, 540, 70), "폭탄을 연결해 공간을 비우고, 남은 셀의 낙하로 연쇄를 만드세요. 한 판 안에서 새로운 폭탄과 강화를 선택합니다.", 17, MUTED);
            string[] buttons = { "새 게임", "게임 방법", "종료" };
            for (int i = 0; i < buttons.Length; i++)
                if (MenuButton(new Rect(70, 385 + 62 * i, 460, 50), buttons[i], i) && _screen == SCREEN.MENU) ActivateMenu(i);
            Fill(new Rect(720, 152, 500, 445), PANEL);
            Text(new Rect(750, 180, 430, 36), "5 STAGES  /  ONE BUILD", 23, ACCENT, FontStyle.Bold);
            Text(new Rect(750, 240, 430, 180), "01   테트로미노를 배치합니다.\n\n02   T를 B에 접촉시켜 점화합니다.\n\n03   폭발 후 낙하로 재점화합니다.\n\n04   XP로 폭탄을 획득·강화합니다.", 18, Color.white);
            RunRecordData best = _persistent.BestRecord;
            Text(new Rect(750, 465, 430, 90), best == null ? "아직 최고 기록이 없습니다." : $"BEST  {(best.EndReason == RUN_END_REASON.VICTORY ? "승리" : "스테이지 " + best.ReachedStage)}\n{best.TotalScore:N0}점 · {best.PlacedPieceCount}조각", 19, GOLD);
            Text(new Rect(70, 590, 550, 40), "방향키 / D패드 이동 · Enter / 아래쪽 버튼 확인", 14, MUTED);
        }

        private void DrawGame()
        {
            RunData run = _session.Run;
            StageDefinition stage = DEFINITION.GetStage(run.StageNumber);
            Fill(new Rect(48, 122, 350, 504), PANEL);
            Text(new Rect(70, 142, 300, 25), $"STAGE  {run.StageNumber:00} / 05", 19, ACCENT, FontStyle.Bold);
            Text(new Rect(70, 180, 300, 48), $"{run.StageScore:N0} / {stage.TargetScore}", 32, Color.white, FontStyle.Bold);
            Fill(new Rect(70, 239, 300, 5), new Color(0.15f, 0.2f, 0.27f));
            Fill(new Rect(70, 239, 300 * Mathf.Clamp01((float)run.StageScore / stage.TargetScore), 5), ACCENT);
            Text(new Rect(70, 265, 300, 33), $"남은 조각  {run.RemainingPieces} / {stage.PieceLimit}", 23, GOLD);
            Text(new Rect(70, 309, 300, 25), $"총점  {run.TotalScore:N0}     Lv.{run.Build.PlayerLevel}", 17, Color.white);
            Text(new Rect(70, 340, 300, 25), $"XP  {run.Build.Experience} / {_session.RequiredExperience}", 16, ACCENT);
            int row = 0;
            foreach (BombProgressData bomb in run.Build.Bombs)
            {
                Text(new Rect(70, 383 + row * 26, 300, 26), $"B{(int)bomb.Type}  {DEFINITION.GetBomb(bomb.Type).DisplayName}  Lv.{bomb.Level}", 16, CellColor(new CellData(CELL_TYPE.BOMB, bomb.Type)));
                row++;
            }
            Text(new Rect(70, 472, 300, 50), $"회수 {run.Build.RecoveryMasteryCount}   낙하 {run.Build.GravityMasteryCount}   폭탄 {run.Build.BombMasteryCount}", 14, MUTED);
            Text(new Rect(70, 520, 300, 88), RelicSummary(), 14, Color.white);
            DrawBoard();
            if (run.State == RUN_STATE.PIECE_CONTROL || run.State == RUN_STATE.RESOLVING)
            {
                Text(new Rect(790, 122, 400, 32), "NEXT  /  다음 3개", 19, Color.white, FontStyle.Bold);
                for (int i = 0; i < run.Supply.Preview.Count; i++) DrawNext(run.Supply.Preview[i], i);
                Text(new Rect(790, 475, 420, 140), _usedGamepad
                    ? "D패드 좌우  이동\n아래 버튼 / 왼쪽 버튼  회전\nD패드 아래  빠르게 내리기\n오른쪽 숄더  즉시 고정\n메뉴 버튼  일시정지"
                    : "← → / A D  이동\n↑ X / Z  시계 / 반시계 회전\n↓ / S  빠르게 내리기\nSpace  즉시 고정\nEsc  일시정지", 17, MUTED);
            }
            string status = "다음 폭발을 연결해 보세요";
            if (run.State == RUN_STATE.RESOLVING) status = _playback != null && _playback.Wave != null ? $"{_playback.WaveIndex + 1}차 폭발  ·  셀 낙하 후 재검사" : "배치 정산 중";
            else if (run.LastPlacement != null && run.LastPlacement.IsSettled)
                status = $"+{run.LastPlacement.Score.TotalScore}점  ·  B {run.LastPlacement.ActivatedBombCount}  /  묶음 {run.LastPlacement.WaveCount}  ·  XP +{run.LastPlacement.TotalExperience}";
            Text(new Rect(400, 640, 820, 30), status, 16, GOLD, alignment: TextAnchor.MiddleCenter);
        }

        private void DrawBoard()
        {
            RunData run = _session.Run;
            bool resolving = run.State == RUN_STATE.RESOLVING || (run.State == RUN_STATE.PAUSED && run.StateBeforePause == RUN_STATE.RESOLVING);
            BoardData board = resolving ? (_playback?.Board ?? _session.LastLockedBoard) : run.Board;
            Fill(new Rect(BOARD_X - 8, BOARD_Y - 8, 296, 520), new Color(0.10f, 0.14f, 0.20f));
            for (int y = 0; y < board.Height; y++)
                for (int x = 0; x < board.Width; x++)
                {
                    var p = new GridPosition(x, y);
                    Fill(CellRect(p), new Color(0.045f, 0.065f, 0.10f));
                    CellData cell = board.GetCell(p);
                    if (cell.Type != CELL_TYPE.EMPTY) DrawCell(CellRect(p), cell);
                }
            Outline(new Rect(BOARD_X, BOARD_Y, 280, 112), new Color(0.54f, 0.26f, 0.25f, 0.45f), 1);
            if (run.ActivePiece != null && (run.State == RUN_STATE.PIECE_CONTROL || run.State == RUN_STATE.PAUSED))
            {
                ActivePieceData active = run.ActivePiece;
                GridPosition landing = BoardRules.GetLandingOrigin(DEFINITION, run.Board, active);
                GridPosition[] ghost = BoardRules.GetPositions(DEFINITION, active.Piece, landing, active.Rotation);
                GridPosition[] actual = BoardRules.GetPositions(DEFINITION, active.Piece, active.Origin, active.Rotation);
                for (int i = 0; i < 4; i++) DrawCell(CellRect(ghost[i]), active.Piece.GetCell(i), true);
                if (_preview != null && run.State == RUN_STATE.PIECE_CONTROL)
                {
                    foreach (RemovedCellData removed in _preview.RemovedCells) Outline(CellRect(removed.Position), GOLD, 1);
                    foreach (BombActivationData bomb in _preview.ActivatedBombs) Outline(CellRect(bomb.Position), ACCENT, 2);
                    DrawRelayLines(_preview);
                }
                for (int i = 0; i < 4; i++) { DrawCell(CellRect(actual[i]), active.Piece.GetCell(i)); Outline(CellRect(actual[i]), Color.white, 1); }
            }
            if (resolving && _playback != null && _playback.Wave != null)
            {
                ExplosionWaveData wave = _playback.Wave;
                if (_playback.Phase == 1)
                {
                    foreach (RemovedCellData removed in wave.RemovedCells) Outline(CellRect(removed.Position), GOLD);
                    foreach (BombActivationData bomb in wave.ActivatedBombs) Outline(CellRect(bomb.Position), Color.white, 3);
                    DrawRelayLines(wave);
                }
                else if (_playback.Phase == 2)
                    foreach (RemovedCellData removed in wave.RemovedCells) Fill(CellRect(removed.Position), new Color(1f, 0.7f, 0.3f, 0.28f));
                else if (_playback.Phase == 3)
                    foreach (CellMovementData movement in wave.Movements)
                    {
                        Rect from = CellRect(movement.From);
                        Rect to = CellRect(movement.To);
                        DrawCell(new Rect(from.x, Mathf.Lerp(from.y, to.y, _playback.GravityProgress), from.width, from.height), movement.Cell);
                    }
            }
        }

        private static Rect CellRect(GridPosition p) => new Rect(BOARD_X + p.X * CELL_SIZE + 1, BOARD_Y + (17 - p.Y) * CELL_SIZE + 1, CELL_SIZE - 2, CELL_SIZE - 2);

        private static Color CellColor(CellData cell)
        {
            if (cell.Type == CELL_TYPE.NORMAL) return new Color(0.30f, 0.39f, 0.52f);
            if (cell.Type == CELL_TYPE.TRIGGER) return new Color(1f, 0.72f, 0.26f);
            switch (cell.BombType)
            {
                case BOMB_TYPE.B02_HORIZONTAL: return new Color(0.35f, 0.73f, 1f);
                case BOMB_TYPE.B03_VERTICAL: return new Color(0.70f, 0.55f, 1f);
                case BOMB_TYPE.B04_RELAY: return new Color(0.3f, 0.9f, 0.72f);
                default: return new Color(1f, 0.39f, 0.39f);
            }
        }

        private void DrawCell(Rect rect, CellData cell, bool ghost = false)
        {
            Color color = CellColor(cell);
            Fill(rect, ghost ? new Color(color.r, color.g, color.b, 0.18f) : color);
            Outline(rect, ghost ? color : new Color(1, 1, 1, 0.12f), 1);
            string symbol = cell.Type == CELL_TYPE.BOMB ? "B" + (int)cell.BombType : cell.Type == CELL_TYPE.TRIGGER ? "T" : "";
            Text(rect, symbol, rect.width < 23 ? 10 : 12, ghost ? color : BACKGROUND, FontStyle.Bold, TextAnchor.MiddleCenter);
            if (cell.Type == CELL_TYPE.TRIGGER && _session != null && _session.Run.Build.Relics.Contains(RELIC_TYPE.R06_HEATPROOF_TRIGGER))
                Outline(new Rect(rect.x + 3, rect.y + 3, rect.width - 6, rect.height - 6), Color.white, 1);
        }

        private void DrawRelayLines(ExplosionWaveData wave)
        {
            foreach (RelayConnectionData link in wave.RelayConnections)
            {
                Vector2 start = CellRect(link.Source).center;
                Vector2 end = CellRect(link.Target).center;
                int steps = Mathf.CeilToInt(Vector2.Distance(start, end) / 7);
                for (int i = 0; i <= steps; i++)
                {
                    Vector2 p = Vector2.Lerp(start, end, (float)i / steps);
                    Fill(new Rect(p.x - 1, p.y - 1, 3, 3), ACCENT);
                }
            }
        }

        private void DrawNext(QueuedPieceData queued, int index)
        {
            float y = 167 + index * 98;
            Fill(new Rect(790, y, 430, 90), PANEL);
            PieceDefinition shape = DEFINITION.GetPiece(queued.Piece.Shape);
            for (int i = 0; i < 4; i++) DrawCell(new Rect(805 + shape.Cells[i].X * 18, y + 10 + (3 - shape.Cells[i].Y) * 18, 16, 16), queued.Piece.GetCell(i));
            PieceData piece = queued.Piece;
            string name = piece.Attribute == CELL_TYPE.BOMB ? DEFINITION.GetBomb(piece.BombType).DisplayName + $" Lv.{ExplosionResolver.GetBombLevel(_session.Run.Build, piece.BombType)}" : piece.Attribute == CELL_TYPE.TRIGGER ? "트리거 조각" : "일반 조각";
            Text(new Rect(905, y + 15, 300, 30), $"{index + 1}.  {piece.Shape}  /  {name}", 16, Color.white);
            bool reserved = _session.Run.Supply.FirstAppearances.Exists(item => item.QueuedPieceId == queued.Id);
            if (reserved) Text(new Rect(905, y + 50, 300, 24), "새 폭탄 첫 등장", 14, ACCENT);
        }

        private string RelicSummary()
        {
            if (_session.Run.Build.Relics.Count == 0) return "보유 유물 없음";
            var text = new StringBuilder();
            foreach (RELIC_TYPE type in _session.Run.Build.Relics)
                foreach (RelicDefinition relic in DEFINITION.Relics)
                    if (relic.Type == type) text.Append(relic.DisplayName).Append("  ");
            return text.ToString();
        }

        private void DrawGrowthChoices()
        {
            Text(new Rect(780, 122, 450, 35), $"LEVEL UP  /  Lv.{_session.Run.Build.PlayerLevel + 1}", 23, ACCENT, FontStyle.Bold);
            for (int i = 0; i < _session.Run.GrowthChoices.Count; i++)
            {
                GrowthChoiceData choice = _session.Run.GrowthChoices[i];
                string title;
                string description;
                if (choice.Type == GROWTH_CHOICE_TYPE.COMMON_UPGRADE)
                {
                    COMMON_UPGRADE_TYPE type = choice.CommonUpgradeType.Value;
                    title = type == COMMON_UPGRADE_TYPE.RECOVERY_MASTERY ? "회수 숙련" : type == COMMON_UPGRADE_TYPE.GRAVITY_MASTERY ? "낙하 숙련" : "폭탄 숙련";
                    description = type == COMMON_UPGRADE_TYPE.RECOVERY_MASTERY ? "파괴한 N의 기본 단가 +1점" : type == COMMON_UPGRADE_TYPE.GRAVITY_MASTERY ? "추가 폭발 묶음당 +5점" : "발동한 폭탄 하나당 +5점";
                    description += "\n현재 폭탄 공급 구성을 유지합니다.";
                }
                else
                {
                    bool acquire = choice.Type == GROWTH_CHOICE_TYPE.ACQUIRE_BOMB;
                    int level = acquire ? 1 : ExplosionResolver.GetBombLevel(_session.Run.Build, choice.BombType) + 1;
                    title = (acquire ? "획득  " : "강화  ") + DEFINITION.GetBomb(choice.BombType).DisplayName + $"  Lv.{level}";
                    description = acquire ? BombRange(choice.BombType, level) : BombRange(choice.BombType, level - 1) + " → " + BombRange(choice.BombType, level);
                    int count = _session.Run.Build.Bombs.Count;
                    description += acquire ? $"\n기존 각 {100f / count:0.#}% → 각 {100f / (count + 1):0.#}% · 다음 B 첫 등장 예약" : $"\n공급 비중 각 {100f / count:0.#}% 유지 · 기존 B에도 적용";
                }
                DrawChoiceCard(i, title, description, false);
            }
        }

        private static string BombRange(BOMB_TYPE type, int level)
        {
            if (type == BOMB_TYPE.B01_BASIC) return level == 1 ? "파괴 3×3" : $"파괴 3×3 + 축 끝점 거리 2{(level == 3 ? ", 3" : "")}";
            if (type == BOMB_TYPE.B04_RELAY) return $"파괴 3×3 / 중계 거리 {level + 2} (다른 B만)";
            return type == BOMB_TYPE.B02_HORIZONTAL ? $"파괴 가로 {5 + level * 2}×1" : $"파괴 세로 1×{5 + level * 2}";
        }

        private void DrawRelicChoices()
        {
            Text(new Rect(780, 122, 450, 38), "STAGE CLEAR  /  유물 선택", 23, GOLD, FontStyle.Bold);
            for (int i = 0; i < _session.Run.RelicChoices.Count; i++)
            {
                RELIC_TYPE type = _session.Run.RelicChoices[i];
                foreach (RelicDefinition relic in DEFINITION.Relics)
                    if (relic.Type == type) DrawChoiceCard(i, relic.DisplayName, RelicDescription(type), true);
            }
        }

        private void DrawChoiceCard(int index, string title, string description, bool relic)
        {
            Rect rect = new Rect(780, 175 + index * 148, 450, 134);
            Fill(rect, PANEL);
            Outline(rect, _selection == index ? ACCENT : new Color(0.18f, 0.24f, 0.31f), _selection == index ? 2 : 1);
            Text(new Rect(rect.x + 15, rect.y + 12, 420, 34), $"0{index + 1}  {title}", 19, Color.white, FontStyle.Bold);
            Text(new Rect(rect.x + 15, rect.y + 52, 420, 75), description, 15, MUTED);
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none) && !_waitingForRelease) ActivateMenu(index);
            Text(new Rect(780, 625, 450, 30), relic ? "선택 후 다음 스테이지로 진행합니다." : "방향키 / D패드 이동 · 확인 버튼으로 선택", 14, ACCENT);
        }

        private static string RelicDescription(RELIC_TYPE type)
        {
            switch (type)
            {
                case RELIC_TYPE.R01_PRECISION_BLAST: return "파괴한 N의 기본 단가 +2점";
                case RELIC_TYPE.R02_CHAIN_AMPLIFIER: return "한 배치 폭탄 2개 이상 발동 시 배율 +0.5\n기본 배율 상한 적용 뒤 더합니다.";
                case RELIC_TYPE.R03_IGNITION_AMPLIFIER: return "T와 B가 대각선으로 접촉해도 점화합니다.\n파괴 범위와 중계 거리는 유지합니다.";
                case RELIC_TYPE.R04_GRAVITY_CHARGER: return "추가 폭발 묶음당 +30점";
                case RELIC_TYPE.R05_DEMOLITION_BONUS: return "한 배치에서 N 10개 이상 파괴 시 +75점";
                default: return "T가 폭발 범위에 들어가도 살아남습니다.\n남은 T도 중력으로 낙하하고 점화합니다.";
            }
        }

        private void Modal(string title, string subtitle)
        {
            Fill(new Rect(0, 102, 1280, 570), new Color(0.02f, 0.035f, 0.06f, 0.94f));
            Text(new Rect(330, 125, 680, 50), title, 30, Color.white, FontStyle.Bold);
            Text(new Rect(330, 185, 680, 55), subtitle, 17, MUTED);
        }

        private void DrawPause()
        {
            Modal("일시정지", "조작과 연출 시간이 멈췄습니다.");
            string[] buttons = { "계속", "연출 속도  /  " + (_persistent.Settings.PresentationSpeed == PRESENTATION_SPEED.FAST ? "빠르게" : "기본"), "게임 방법", "다시 시작", "판 포기 후 메뉴" };
            for (int i = 0; i < buttons.Length; i++)
                if (MenuButton(new Rect(330, 250 + i * 65, 620, 52), buttons[i], i)) ActivateMenu(i);
        }

        private void DrawConfirmation()
        {
            Modal(_restartConfirmed ? "다시 시작하시겠습니까?" : "메뉴로 돌아가시겠습니까?", "현재 진행 중인 판은 저장되지 않습니다.");
            if (MenuButton(new Rect(330, 310, 620, 58), "확인", 0)) ActivateMenu(0);
            if (MenuButton(new Rect(330, 390, 620, 58), "취소", 1)) ActivateMenu(1);
        }

        private void DrawGuide()
        {
            string[] titles = { "쌓기", "터뜨리기", "이어 터뜨리기", "빌드 만들기" };
            string[] bodies =
            {
                "조각은 네 칸입니다.\n\n좌우로 이동하고 시계·반시계로 회전합니다.\nSpace / 오른쪽 숄더로 고스트 위치에 즉시 고정합니다.\n\nN은 일반 셀, B는 폭탄, T는 트리거입니다.\n줄을 완성해도 지워지지 않습니다.",
                "조각이 고정된 뒤, T에 상하좌우로 닿은 B가 점화합니다.\n\n기본 폭탄은 주변 3×3을 파괴합니다.\n범위에 들어간 다른 B도 같은 묶음으로 발동합니다.\n\n가로·세로 폭탄의 방향은 조각을 돌려도 유지합니다.",
                "폭발 후 보드 전체의 셀이 열별로 아래로 떨어집니다.\n\n착지 후 T와 B가 닿으면 다음 묶음이 시작됩니다.\n금색 테두리는 첫 묶음의 실제 파괴 대상입니다.\n초록 점선은 중계이며, 중간 N/T를 지우지 않습니다.\n\n20조각 안에 목표 점수를 채우세요. 보드는 다음 스테이지로 이어집니다.",
                "N 파괴와 중간 스테이지 완료로 XP를 얻습니다.\n\n레벨업마다 3개 중 새 폭탄·종류별 강화·공통 강화를 고릅니다.\n새 폭탄은 다음 B에 첫 등장 예약이 표시됩니다.\n스테이지 1~4 완료 후에는 유물도 하나 선택합니다.\n\n5스테이지를 완료하면 승리합니다. 새 판은 기본 폭탄부터 시작합니다."
            };
            Modal($"게임 방법  {_guidePage + 1}/4  /  {titles[_guidePage]}", "확인 버튼으로 다음 안내를 읽습니다.");
            Text(new Rect(330, 260, 690, 300), bodies[_guidePage], 20, Color.white);
            if (MenuButton(new Rect(330, 587, 620, 55), _guidePage == 3 ? "확인" : "다음", 0)) ActivateMenu(0);
        }

        private void DrawResult()
        {
            RunData run = _session.Run;
            string reason = run.EndReason == RUN_END_REASON.VICTORY ? "5스테이지 완료" : run.EndReason == RUN_END_REASON.SPAWN_BLOCKED ? "다음 조각을 생성할 공간이 없습니다." : "조각 제한 안에 목표에 도달하지 못했습니다.";
            Modal(run.EndReason == RUN_END_REASON.VICTORY ? "VICTORY  /  승리" : "RUN OVER  /  이번 판 종료", reason);
            Text(new Rect(330, 250, 690, 42), $"총점 {run.TotalScore:N0}  ·  도달 스테이지 {run.StageNumber}  ·  Lv.{run.Build.PlayerLevel}", 24, GOLD, FontStyle.Bold);
            int minutes = (int)run.PlaySeconds / 60;
            int seconds = (int)run.PlaySeconds % 60;
            Text(new Rect(330, 310, 690, 60), $"배치 {run.PlacedPieceCount}조각  /  최대 폭탄 {run.MaximumBombCount}  /  최대 묶음 {run.MaximumWaveCount}\n플레이 {minutes:00}:{seconds:00}  ·  시드 {run.Seed}", 17, Color.white);
            var build = new StringBuilder();
            foreach (BombProgressData bomb in run.Build.Bombs) build.Append(DEFINITION.GetBomb(bomb.Type).DisplayName).Append(" Lv.").Append(bomb.Level).Append("   ");
            Text(new Rect(330, 395, 690, 100), build + "\n" + RelicSummary(), 17, MUTED);
            if (MenuButton(new Rect(330, 520, 300, 55), "다시 시작", 0)) ActivateMenu(0);
            if (MenuButton(new Rect(650, 520, 300, 55), "메인 메뉴", 1)) ActivateMenu(1);
        }
    }
}
