using System;
using System.Collections.Generic;
using BombStack.Data;
using HM.CodeBase;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BombStack.UI
{
    [Serializable]
    public sealed class InGameUIReferences
    {
        public Text StageTxt;
        public Text ScoreTxt;
        public Text PiecesTxt;
        public Text TotalScoreTxt;
        public Text PlayerLevelTxt;
        public Text ExperienceTxt;
        public Text MasteryTxt;
        public Text InputHintTxt;
        public Text StatusTxt;
        public Text BreakdownTxt;
        public Text DangerTxt;
        public Image ScoreFillImg;
        public Image ExperienceFillImg;
        public Text[] BombTxts;
        public Text[] RelicTxts;
        public Text[] PreviewTxts;
        public Text[] PreviewBadgeTxts;
        public NextPieceGraphic[] PreviewGraphics;
        public Button PauseBtn;
        public InGameBoardGraphic BoardGraphic;
        public GameObject RightSidebarObj;
        public GameObject ChoicePanelObj;
        public Text ChoiceTitleTxt;
        public Text ChoiceSubtitleTxt;
        public InGameMenuButton[] ChoiceBtns;
        public GameObject ModalPanelObj;
        public Text ModalTitleTxt;
        public Text ModalSubtitleTxt;
        public InGameMenuButton[] ModalBtns;
    }

    public sealed class InGameUI_view : AView
    {
        [SerializeField] private InGameUIReferences _references;
        [SerializeField] private Font _font;
        private bool _isBound;
        private bool _fontsAssigned;
        private Font _runtimeFont;
        public event Action PauseRequested;
        public event Action<int> MenuItemClicked;
        public event Action<int> MenuItemFocused;

        public bool IsConfigured => _references != null && _references.StageTxt != null && _references.ScoreTxt != null
            && _references.PiecesTxt != null && _references.TotalScoreTxt != null && _references.PlayerLevelTxt != null
            && _references.ExperienceTxt != null && _references.MasteryTxt != null && _references.InputHintTxt != null
            && _references.StatusTxt != null && _references.BreakdownTxt != null && _references.DangerTxt != null
            && _references.ScoreFillImg != null && _references.ExperienceFillImg != null && _references.PauseBtn != null
            && _references.BoardGraphic != null && _references.RightSidebarObj != null && _references.ChoicePanelObj != null
            && _references.ChoiceTitleTxt != null && _references.ChoiceSubtitleTxt != null && _references.ModalPanelObj != null
            && _references.ModalTitleTxt != null && _references.ModalSubtitleTxt != null
            && HasReferences(_references.BombTxts, 3) && HasReferences(_references.RelicTxts, 4)
            && HasReferences(_references.PreviewTxts, 3) && HasReferences(_references.PreviewBadgeTxts, 3)
            && HasReferences(_references.PreviewGraphics, 3) && HasMenuButtons(_references.ChoiceBtns, 3)
            && HasMenuButtons(_references.ModalBtns, 5);

        private static bool HasReferences<T>(T[] references, int count) where T : UnityEngine.Object
        {
            if (references == null || references.Length < count) return false;
            foreach (T reference in references) if (reference == null) return false;
            return true;
        }

        private static bool HasMenuButtons(InGameMenuButton[] buttons, int count)
        {
            if (!HasReferences(buttons, count)) return false;
            foreach (InGameMenuButton button in buttons) if (!button.IsConfigured) return false;
            return true;
        }

        public void Initialize(InGameUIReferences references, Font font = null)
        {
            UnbindButton();
            _references = references ?? throw new ArgumentNullException(nameof(references));
            _font = font;
        }

        public override void Open()
        {
            if (!IsConfigured) throw new InvalidOperationException("인게임 UI의 HUD/메뉴 참조를 먼저 연결해야 합니다.");
            AssignFallbackFont();
            if (!_isBound)
            {
                _references.PauseBtn.onClick.AddListener(OnPauseActioned);
                BindMenuButtons(_references.ChoiceBtns);
                BindMenuButtons(_references.ModalBtns);
                _isBound = true;
            }
            base.Open();
        }

        public override void Close()
        {
            UnbindButton();
            base.Close();
        }

        public override void Clear()
        {
            if (_references == null) return;
            if (_references.BoardGraphic != null) _references.BoardGraphic.SetModel(null);
            foreach (NextPieceGraphic graphic in _references.PreviewGraphics)
                if (graphic != null) graphic.SetPiece(null, null, false);
            SetText(_references.StatusTxt, "");
            SetText(_references.BreakdownTxt, "");
        }

        private void OnDestroy()
        {
            UnbindButton();
            if (_runtimeFont != null) Destroy(_runtimeFont);
        }

        private void AssignFallbackFont()
        {
            if (_fontsAssigned) return;
            Font font = _font;
            if (font == null)
            {
                _runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Noto Sans CJK KR", "Arial" }, 24);
                font = _runtimeFont;
            }
            Font builtInFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            foreach (Text labelTxt in GetComponentsInChildren<Text>(true))
                if (labelTxt.font == null || labelTxt.font == builtInFont) labelTxt.font = font;
            _fontsAssigned = true;
        }

        private void UnbindButton()
        {
            if (!_isBound) return;
            if (_references.PauseBtn != null) _references.PauseBtn.onClick.RemoveListener(OnPauseActioned);
            UnbindMenuButtons(_references.ChoiceBtns);
            UnbindMenuButtons(_references.ModalBtns);
            _isBound = false;
        }

        private void BindMenuButtons(InGameMenuButton[] buttons)
        {
            foreach (InGameMenuButton button in buttons)
            {
                button.Clicked += OnMenuItemClicked;
                button.Focused += OnMenuItemFocused;
            }
        }

        private void UnbindMenuButtons(InGameMenuButton[] buttons)
        {
            foreach (InGameMenuButton button in buttons)
                if (button != null)
                {
                    button.Clicked -= OnMenuItemClicked;
                    button.Focused -= OnMenuItemFocused;
                }
        }

        private void OnMenuItemClicked(int index) => MenuItemClicked?.Invoke(index);
        private void OnMenuItemFocused(int index) => MenuItemFocused?.Invoke(index);

        private void OnPauseActioned()
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            PauseRequested?.Invoke();
        }

        public void SetStage(string stage, string score, string pieces, string total, float progress)
        {
            SetText(_references.StageTxt, stage);
            SetText(_references.ScoreTxt, score);
            SetText(_references.PiecesTxt, pieces);
            SetText(_references.TotalScoreTxt, total);
            SetProgress(_references.ScoreFillImg, progress);
        }

        public void SetGrowth(string level, string experience, string mastery, float progress)
        {
            SetText(_references.PlayerLevelTxt, level);
            SetText(_references.ExperienceTxt, experience);
            SetText(_references.MasteryTxt, mastery);
            SetProgress(_references.ExperienceFillImg, progress);
        }

        public void SetBomb(int index, string text, Color color)
        {
            SetText(_references.BombTxts[index], text);
            _references.BombTxts[index].color = color;
        }

        public void SetRelic(int index, string text, Color color)
        {
            SetText(_references.RelicTxts[index], text);
            _references.RelicTxts[index].color = color;
        }

        public void SetPreview(int index, PieceDefinition definition, PieceData piece, string caption, bool reserved, bool heatproof)
        {
            _references.PreviewGraphics[index].SetPiece(definition, piece, heatproof);
            SetText(_references.PreviewTxts[index], caption);
            SetText(_references.PreviewBadgeTxts[index], reserved ? "새 폭탄 첫 등장" : "");
        }

        public void SetBoard(InGameUIModel_model model) => _references.BoardGraphic.SetModel(model);

        public void SetMenu(IN_GAME_PANEL panel, string title, string subtitle, IReadOnlyList<InGameMenuItem> items, int selection, bool inputEnabled)
        {
            bool choices = panel == IN_GAME_PANEL.GROWTH || panel == IN_GAME_PANEL.RELIC;
            bool modal = panel != IN_GAME_PANEL.NONE && !choices;
            SetActive(_references.RightSidebarObj, !choices);
            SetActive(_references.ChoicePanelObj, choices);
            SetActive(_references.ModalPanelObj, modal);
            _references.PauseBtn.interactable = panel == IN_GAME_PANEL.NONE && inputEnabled;
            if (panel == IN_GAME_PANEL.NONE) return;
            SetText(choices ? _references.ChoiceTitleTxt : _references.ModalTitleTxt, title);
            SetText(choices ? _references.ChoiceSubtitleTxt : _references.ModalSubtitleTxt, subtitle);
            InGameMenuButton[] buttons = choices ? _references.ChoiceBtns : _references.ModalBtns;
            for (int i = 0; i < buttons.Length; i++)
            {
                SetActive(buttons[i].gameObject, i < items.Count);
                if (i < items.Count) buttons[i].SetItem(i, items[i], i == selection, inputEnabled);
            }
        }

        private static void SetActive(GameObject targetObj, bool active)
        {
            if (targetObj.activeSelf != active) targetObj.SetActive(active);
        }

        public void SetFooter(string status, string breakdown, string inputHint, bool warning)
        {
            SetText(_references.StatusTxt, status);
            SetText(_references.BreakdownTxt, breakdown);
            SetText(_references.InputHintTxt, inputHint);
            _references.DangerTxt.gameObject.SetActive(warning);
        }

        private static void SetText(Text targetTxt, string value)
        {
            if (targetTxt != null && targetTxt.text != value) targetTxt.text = value;
        }

        private static void SetProgress(Image fillImg, float value)
        {
            fillImg.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(value), 1);
        }
    }
}
