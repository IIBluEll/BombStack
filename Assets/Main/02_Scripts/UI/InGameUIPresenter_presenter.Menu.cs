using System.Collections.Generic;
using System.Text;
using BombStack.Data;

namespace BombStack.UI
{
    public sealed partial class InGameUIPresenter_presenter
    {
        private readonly List<InGameMenuItem> MENU_ITEMS = new List<InGameMenuItem>();
        private int _menuVersion = -1;
        private int _selection;
        private string _menuTitle;
        private string _menuSubtitle;

        private void RefreshMenu()
        {
            if (_menuVersion != MODEL.MenuVersion)
            {
                _menuVersion = MODEL.MenuVersion;
                _selection = 0;
                BuildMenu();
            }
            VIEW.SetMenu(MODEL.Panel, _menuTitle, _menuSubtitle, MENU_ITEMS, _selection, MODEL.InputEnabled);
        }

        public void MoveSelection(int direction)
        {
            if (_disposed || !IsOpen || !MODEL.InputEnabled || MENU_ITEMS.Count == 0) return;
            for (int i = 0; i < MENU_ITEMS.Count; i++)
            {
                _selection = (_selection + direction + MENU_ITEMS.Count) % MENU_ITEMS.Count;
                if (MENU_ITEMS[_selection].Interactable) break;
            }
            RefreshMenu();
        }

        public void ConfirmSelection() => OnMenuItemClicked(_selection);

        private void OnMenuItemClicked(int index)
        {
            if (_disposed || !IsOpen || !MODEL.InputEnabled || index < 0 || index >= MENU_ITEMS.Count) return;
            InGameMenuItem item = MENU_ITEMS[index];
            if (item.Interactable) ActionRequested?.Invoke(item.Action, item.Index);
        }

        private void OnMenuItemFocused(int index)
        {
            if (_disposed || !IsOpen || !MODEL.InputEnabled || index < 0 || index >= MENU_ITEMS.Count) return;
            _selection = index;
            RefreshMenu();
        }

        private void BuildMenu()
        {
            MENU_ITEMS.Clear();
            _menuTitle = "";
            _menuSubtitle = "";
            RunData run = MODEL.Run;
            switch (MODEL.Panel)
            {
                case IN_GAME_PANEL.GROWTH:
                    _menuTitle = $"LEVEL UP  /  Lv.{run.Build.PlayerLevel + 1}";
                    _menuSubtitle = $"폭탄 {run.Build.Bombs.Count}/{MODEL.Definition.Growth.MaximumOwnedBombTypes}종 보유 · 이번 판에 적용\n방향키 / D패드로 이동하고 확인하여 선택하세요.";
                    for (int i = 0; i < run.GrowthChoices.Count; i++) AddGrowth(run.GrowthChoices[i], i);
                    break;
                case IN_GAME_PANEL.RELIC:
                    _menuTitle = "STAGE CLEAR  /  유물 선택";
                    _menuSubtitle = "보드와 빌드를 유지합니다.\n하나를 선택하면 다음 스테이지로 진행합니다.";
                    for (int i = 0; i < run.RelicChoices.Count; i++)
                    {
                        RelicDefinition relic = GetRelic(run.RelicChoices[i]);
                        MENU_ITEMS.Add(new InGameMenuItem(IN_GAME_ACTION.RELIC, relic.Id + "  " + relic.DisplayName, RelicDescription(relic), i));
                    }
                    break;
                case IN_GAME_PANEL.PAUSE:
                    _menuTitle = "일시정지";
                    _menuSubtitle = "조작과 폭발·낙하 연출이 멈췄습니다.";
                    MENU_ITEMS.Add(new InGameMenuItem(IN_GAME_ACTION.RESUME, "계속"));
                    MENU_ITEMS.Add(new InGameMenuItem(IN_GAME_ACTION.TOGGLE_SPEED, "연출 속도  /  " + (MODEL.FastPresentation ? "빠르게" : "기본")));
                    MENU_ITEMS.Add(new InGameMenuItem(IN_GAME_ACTION.GUIDE, "게임 방법"));
                    MENU_ITEMS.Add(new InGameMenuItem(IN_GAME_ACTION.REQUEST_RESTART, "다시 시작"));
                    MENU_ITEMS.Add(new InGameMenuItem(IN_GAME_ACTION.REQUEST_EXIT, "판 포기 후 메뉴", interactable: MODEL.CanExit));
                    break;
                case IN_GAME_PANEL.GUIDE:
                    BuildGuide();
                    break;
                case IN_GAME_PANEL.CONFIRM_RESTART:
                case IN_GAME_PANEL.CONFIRM_EXIT:
                    bool restart = MODEL.Panel == IN_GAME_PANEL.CONFIRM_RESTART;
                    _menuTitle = restart ? "다시 시작하시겠습니까?" : "메뉴로 돌아가시겠습니까?";
                    _menuSubtitle = "현재 진행 중인 판은 저장되지 않습니다.";
                    MENU_ITEMS.Add(new InGameMenuItem(restart ? IN_GAME_ACTION.RESTART : IN_GAME_ACTION.EXIT, "확인"));
                    MENU_ITEMS.Add(new InGameMenuItem(IN_GAME_ACTION.CANCEL, "취소"));
                    break;
                case IN_GAME_PANEL.RESULT:
                    BuildResult();
                    MENU_ITEMS.Add(new InGameMenuItem(IN_GAME_ACTION.RESTART, "다시 시작"));
                    MENU_ITEMS.Add(new InGameMenuItem(IN_GAME_ACTION.EXIT, "메인 메뉴", interactable: MODEL.CanExit));
                    break;
            }
        }

        private void AddGrowth(GrowthChoiceData choice, int index)
        {
            if (choice.Type == GROWTH_CHOICE_TYPE.COMMON_UPGRADE)
            {
                foreach (CommonUpgradeDefinition upgrade in MODEL.Definition.CommonUpgrades)
                    if (upgrade.Type == choice.CommonUpgradeType.Value)
                    {
                        long stacks = upgrade.Type == COMMON_UPGRADE_TYPE.RECOVERY_MASTERY ? MODEL.Run.Build.RecoveryMasteryCount
                            : upgrade.Type == COMMON_UPGRADE_TYPE.GRAVITY_MASTERY ? MODEL.Run.Build.GravityMasteryCount : MODEL.Run.Build.BombMasteryCount;
                        string target = upgrade.Type == COMMON_UPGRADE_TYPE.RECOVERY_MASTERY ? "파괴 N의 기본 단가"
                            : upgrade.Type == COMMON_UPGRADE_TYPE.GRAVITY_MASTERY ? "추가 폭발 묶음당 점수" : "발동 폭탄 하나당 점수";
                        MENU_ITEMS.Add(new InGameMenuItem(IN_GAME_ACTION.GROWTH, "공통 강화  /  " + upgrade.DisplayName,
                            $"{target} +{upgrade.BonusPerStack}점\n획득 횟수 {stacks} → {stacks + 1}\n현재 폭탄 공급 구성을 유지합니다.", index));
                    }
                return;
            }
            bool acquire = choice.Type == GROWTH_CHOICE_TYPE.ACQUIRE_BOMB;
            BombDefinition bomb = MODEL.Definition.GetBomb(choice.BombType);
            int level = acquire ? 1 : BombLevel(MODEL.Run, choice.BombType);
            string title = (acquire ? "신규 획득  /  " : "강화  /  ") + bomb.DisplayName;
            int count = MODEL.Run.Build.Bombs.Count;
            string description = acquire
                ? $"미보유 → Lv.1\n{BombRange(bomb, 1)}\n기존 각 {100f / count:0.#}% → 각 {100f / (count + 1):0.#}%\n다음 B 첫 등장 예약 · 이후 종류 Bag에 편입"
                : $"Lv.{level} → Lv.{level + 1}\n{BombRange(bomb, level)}\n→ {BombRange(bomb, level + 1)}\n공급 각 {100f / count:0.#}% 유지 · 보드/대기열에 적용";
            MENU_ITEMS.Add(new InGameMenuItem(IN_GAME_ACTION.GROWTH, title, description, index));
        }

        private static string BombRange(BombDefinition bomb, int level)
        {
            BombLevelDefinition definition = bomb.GetLevel(level);
            int minX = 0, minY = 0, maxX = 0, maxY = 0;
            foreach (GridPosition offset in definition.DestructionOffsets)
            {
                if (offset.X < minX) minX = offset.X;
                if (offset.X > maxX) maxX = offset.X;
                if (offset.Y < minY) minY = offset.Y;
                if (offset.Y > maxY) maxY = offset.Y;
            }
            if (definition.RelayDistance > 0) return $"파괴 3×3 / 다른 B만 중계 거리 {definition.RelayDistance}";
            if (bomb.Type == BOMB_TYPE.B01_BASIC && maxX > 1)
                return $"파괴 3×3 + 축 끝점 거리 {(maxX == 2 ? "2" : "2, 3")}";
            return $"파괴 {maxX - minX + 1}×{maxY - minY + 1}";
        }

        private RelicDefinition GetRelic(RELIC_TYPE type)
        {
            foreach (RelicDefinition relic in MODEL.Definition.Relics) if (relic.Type == type) return relic;
            throw new System.ArgumentOutOfRangeException(nameof(type));
        }

        private static string RelicDescription(RelicDefinition relic)
        {
            switch (relic.Effect)
            {
                case RELIC_EFFECT.NORMAL_CELL_UNIT_BONUS: return $"파괴 N의 기본 단가 +{relic.Value}점";
                case RELIC_EFFECT.CHAIN_MULTIPLIER_BONUS_TWICE: return $"한 배치 폭탄 {relic.Threshold}개 이상 발동 시 배율 +{relic.Value / 2f:0.#}\n기본 배율 상한 적용 뒤 더합니다.";
                case RELIC_EFFECT.DIAGONAL_IGNITION: return "T–B가 대각선으로 접촉해도 점화합니다.\n파괴 범위와 중계 거리는 유지합니다.";
                case RELIC_EFFECT.ADDITIONAL_WAVE_BONUS: return $"추가 폭발 묶음당 +{relic.Value}점";
                case RELIC_EFFECT.MASS_DESTRUCTION_BONUS: return $"한 배치 N {relic.Threshold}개 이상 파괴 시 +{relic.Value}점 1회";
                default: return "T가 폭발 범위에 들어가도 살아남습니다.\n남은 T도 중력으로 낙하하고 점화합니다.";
            }
        }

        private void BuildGuide()
        {
            string[] titles = { "쌓기", "터뜨리기", "이어 터뜨리기", "빌드 만들기" };
            string[] pages =
            {
                "조각은 네 칸입니다. 좌우로 이동하고 시계·반시계로 회전합니다.\n\nSpace / 오른쪽 숄더로 고스트 위치에 즉시 고정합니다.\nN은 일반 셀, B는 폭탄, T는 트리거입니다.\n줄을 완성해도 지워지지 않습니다.",
                "고정 후 T에 상하좌우로 닿은 B가 점화합니다.\n기본 폭탄 Lv.1은 주변 3×3을 파괴하고 다른 B에도 전달합니다.\n\n심볼: 기본 원형 · 가로/세로 화살표 · 중계 안테나 · T 마름모\n가로·세로 폭탄의 방향은 조각을 돌려도 유지합니다.",
                "폭발 후 남은 셀이 열별로 아래로 떨어집니다.\n착지 후 T–B가 다시 접촉하면 다음 폭발 묶음이 시작됩니다.\n\n금색 테두리는 첫 묶음의 파괴 대상입니다.\n초록 중계 점선은 중간 N/T를 파괴하지 않습니다.\n목표 점수와 남은 조각을 확인하세요. 보드는 다음 스테이지로 이어집니다.",
                "N 파괴와 중간 스테이지 완료로 XP를 얻습니다.\n레벨업마다 신규 폭탄·종류별 강화·공통 강화 중 하나를 고릅니다.\n\n새 폭탄은 다음 B에 첫 등장이 예약되고 이후 공급에 편입됩니다.\n1~4스테이지 완료 뒤에는 보조 유물을 하나 고릅니다.\n5스테이지를 완료하면 승리하며 새 판은 기본 폭탄부터 시작합니다."
            };
            _menuTitle = $"게임 방법  {MODEL.GuidePage + 1}/4  /  {titles[MODEL.GuidePage]}";
            _menuSubtitle = pages[MODEL.GuidePage];
            MENU_ITEMS.Add(new InGameMenuItem(IN_GAME_ACTION.GUIDE_NEXT, MODEL.GuidePage == 3 ? "확인" : "다음"));
        }

        private void BuildResult()
        {
            RunData run = MODEL.Run;
            string reason = run.EndReason == RUN_END_REASON.VICTORY ? "5스테이지 완료" : run.EndReason == RUN_END_REASON.SPAWN_BLOCKED
                ? "다음 조각을 생성할 공간이 없습니다." : run.EndReason == RUN_END_REASON.ABANDONED ? "현재 판을 포기했습니다." : "조각 제한 안에 목표에 도달하지 못했습니다.";
            _menuTitle = run.EndReason == RUN_END_REASON.VICTORY ? "VICTORY  /  승리" : "RUN OVER  /  이번 판 종료";
            var text = new StringBuilder(reason);
            text.Append($"\n총점 {run.TotalScore:N0}  ·  도달 스테이지 {run.StageNumber}  ·  Lv.{run.Build.PlayerLevel}");
            text.Append($"\n배치 {run.PlacedPieceCount}조각  /  최대 폭탄 {run.MaximumBombCount}개  /  최대 묶음 {run.MaximumWaveCount}회");
            long seconds = (long)run.PlaySeconds;
            text.Append($"\n플레이 {seconds / 60:00}:{seconds % 60:00}");
            text.Append("\n최종 폭탄: ");
            foreach (BombProgressData bomb in run.Build.Bombs) text.Append($"{MODEL.Definition.GetBomb(bomb.Type).DisplayName} Lv.{bomb.Level}  ");
            text.Append($"\n숙련: 회수 {run.Build.RecoveryMasteryCount} / 낙하 {run.Build.GravityMasteryCount} / 폭탄 {run.Build.BombMasteryCount}");
            text.Append("\n유물: ");
            if (run.Build.Relics.Count == 0) text.Append("없음");
            foreach (RELIC_TYPE type in run.Build.Relics) text.Append(GetRelic(type).DisplayName).Append("  ");
            _menuSubtitle = text.ToString();
        }
    }
}
