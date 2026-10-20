#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BombStack.UI
{
    /// <summary>Editor 메뉴에서만 사용하는 초기 UI 계층 제작기입니다. 플레이 중에는 포함/호출하지 않습니다.</summary>
    public static class InGameUIFactory
    {
        private static readonly Color BACKGROUND_COLOR = new Color(0.027f, 0.043f, 0.071f);
        private static readonly Color PANEL_COLOR = new Color(0.057f, 0.080f, 0.125f);
        private static readonly Color BORDER_COLOR = new Color(0.12f, 0.18f, 0.25f);
        private static readonly Color MUTED_COLOR = new Color(0.51f, 0.61f, 0.72f);
        private static readonly Color TEXT_COLOR = new Color(0.91f, 0.94f, 0.98f);
        private static readonly Color ACCENT_COLOR = new Color(0.31f, 0.91f, 0.74f);
        private static readonly Color GOLD_COLOR = new Color(1f, 0.76f, 0.32f);
        private const float REFERENCE_SCALE = 1.5f;

        public static InGameUI_view Create(Transform parentTrans, Font font, Sprite boardFrameSprite = null, Sprite emptyCellSprite = null)
        {
            Font assetFont = font;
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var rootObj = new GameObject("InGameUI", typeof(RectTransform));
            rootObj.SetActive(false);
            rootObj.transform.SetParent(parentTrans, false);
            Canvas canvas = rootObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            canvas.sortingOrder = 10;
            CanvasScaler scaler = rootObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            rootObj.AddComponent<GraphicRaycaster>();

            RectTransform backgroundRectTrans = Rect(rootObj.transform, "Background", 0, 0, 0, 0);
            backgroundRectTrans.anchorMin = Vector2.zero;
            backgroundRectTrans.anchorMax = Vector2.one;
            backgroundRectTrans.offsetMin = Vector2.zero;
            backgroundRectTrans.offsetMax = Vector2.zero;
            AddImage(backgroundRectTrans, BACKGROUND_COLOR);
            RectTransform frameRectTrans = Rect(rootObj.transform, "Content 1920x1080", 0, 0, 1280, 720);
            frameRectTrans.anchorMin = frameRectTrans.anchorMax = new Vector2(0.5f, 0.5f);
            frameRectTrans.pivot = new Vector2(0.5f, 0.5f);
            frameRectTrans.anchoredPosition = Vector2.zero;

            var references = new InGameUIReferences();
            Label(frameRectTrans, font, "Title", "BOMB STACK", 48, 28, 300, 40, 28, TEXT_COLOR, FontStyle.Bold);
            references.StageTxt = Label(frameRectTrans, font, "Stage", "STAGE 01 / 05", 322, 36, 300, 28, 18, ACCENT_COLOR, FontStyle.Bold);
            references.DangerTxt = Label(frameRectTrans, font, "Top Warning", "상단 적층 주의", 736, 36, 250, 28, 17, ACellGraphic.BASIC_COLOR, FontStyle.Bold);
            references.DangerTxt.gameObject.SetActive(false);
            references.PauseBtn = PauseButton(frameRectTrans, font);
            Label(frameRectTrans, font, "Cell Legend", "일반: 단색 셀   ·   기본: 원형   ·   가로/세로: 화살표   ·   중계: 안테나   ·   트리거: 마름모", 48, 67, 1184, 16, 12, MUTED_COLOR);
            AddImage(Rect(frameRectTrans, "Header Divider", 48, 85, 1184, 1), BORDER_COLOR);

            RectTransform scoreRectTrans = Panel(frameRectTrans, "Stage Score", 48, 114, 312, 198);
            Label(scoreRectTrans, font, "Caption", "스테이지 목표", 20, 18, 270, 25, 15, MUTED_COLOR);
            references.ScoreTxt = Label(scoreRectTrans, font, "Score", "0 / 200", 20, 48, 272, 48, 32, TEXT_COLOR, FontStyle.Bold);
            references.ScoreFillImg = Progress(scoreRectTrans, "Score Progress", 20, 107, 272, 5, ACCENT_COLOR);
            references.PiecesTxt = Label(scoreRectTrans, font, "Remaining Pieces", "남은 조각  20 / 20", 20, 130, 272, 34, 23, GOLD_COLOR, FontStyle.Bold);
            references.TotalScoreTxt = Label(scoreRectTrans, font, "Total Score", "TOTAL  0", 20, 166, 272, 24, 16, MUTED_COLOR);

            RectTransform buildRectTrans = Panel(frameRectTrans, "Run Build", 48, 328, 312, 220);
            Label(buildRectTrans, font, "Caption", "이번 판의 빌드", 20, 16, 272, 26, 18, TEXT_COLOR, FontStyle.Bold);
            references.PlayerLevelTxt = Label(buildRectTrans, font, "Player Level", "PLAYER  Lv.1", 20, 50, 272, 26, 20, ACCENT_COLOR, FontStyle.Bold);
            references.ExperienceTxt = Label(buildRectTrans, font, "Experience", "XP  0 / 8", 20, 80, 272, 22, 15, MUTED_COLOR);
            references.ExperienceFillImg = Progress(buildRectTrans, "Experience Progress", 20, 108, 272, 4, ACCENT_COLOR);
            references.BombTxts = new Text[3];
            for (int i = 0; i < references.BombTxts.Length; i++)
                references.BombTxts[i] = Label(buildRectTrans, font, "Bomb Slot " + (i + 1), i == 0 ? "B01  기본 폭탄   Lv.1" : "—  비어 있는 폭탄 슬롯", 20, 122 + i * 24, 272, 24, 16, TEXT_COLOR);
            references.MasteryTxt = Label(buildRectTrans, font, "Common Mastery", "회수 0   낙하 0   폭탄 0", 20, 194, 272, 22, 13, MUTED_COLOR);

            references.BoardGraphic = CreateBoard(frameRectTrans, boardFrameSprite, emptyCellSprite);

            RectTransform sidebarRectTrans = Rect(frameRectTrans, "Supply and Relics", 0, 0, 1280, 720);
            references.RightSidebarObj = sidebarRectTrans.gameObject;
            Label(sidebarRectTrans, font, "Next Caption", "NEXT  /  다음 조각", 870, 108, 362, 28, 18, TEXT_COLOR, FontStyle.Bold);
            references.PreviewTxts = new Text[3];
            references.PreviewBadgeTxts = new Text[3];
            references.PreviewGraphics = new NextPieceGraphic[3];
            for (int i = 0; i < 3; i++)
            {
                RectTransform previewRectTrans = Panel(sidebarRectTrans, "Next " + (i + 1), 870, 148 + i * 94, 362, 82);
                references.PreviewGraphics[i] = Rect(previewRectTrans, "Piece", 12, 6, 94, 70).gameObject.AddComponent<NextPieceGraphic>();
                references.PreviewGraphics[i].raycastTarget = false;
                references.PreviewTxts[i] = Label(previewRectTrans, font, "Description", "", 116, 10, 230, 40, 15, TEXT_COLOR);
                references.PreviewBadgeTxts[i] = Label(previewRectTrans, font, "First Appearance", "", 116, 50, 230, 22, 13, ACCENT_COLOR);
            }

            RectTransform relicRectTrans = Panel(frameRectTrans, "Owned Relics", 48, 564, 312, 100);
            Label(relicRectTrans, font, "Caption", "보유 유물", 16, 6, 280, 24, 15, TEXT_COLOR, FontStyle.Bold);
            references.RelicTxts = new Text[4];
            for (int i = 0; i < 4; i++)
            {
                int column = i % 2;
                int row = i / 2;
                references.RelicTxts[i] = Label(relicRectTrans, font, "Relic " + (i + 1), "—  비어 있음", 16 + column * 145, 36 + row * 28, 136, 26, 12, MUTED_COLOR);
            }
            references.InputHintTxt = Label(frameRectTrans, font, "Controls", "", 48, 674, 312, 46, 12, MUTED_COLOR);
            Label(sidebarRectTrans, font, "Placement Caption", "배치 정보", 870, 446, 362, 28, 18, TEXT_COLOR, FontStyle.Bold);
            references.StatusTxt = Label(sidebarRectTrans, font, "Placement Status", "", 870, 482, 362, 66, 16, GOLD_COLOR, FontStyle.Bold);
            references.BreakdownTxt = Label(sidebarRectTrans, font, "Score Breakdown", "", 870, 558, 362, 140, 14, MUTED_COLOR);
            CreateMenus(frameRectTrans, font, references);

            if (Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) == null)
            {
                var eventObj = new GameObject("EventSystem", typeof(EventSystem), typeof(InGamePointerInputModule));
                eventObj.transform.SetParent(parentTrans, false);
            }
            InGameUI_view view = rootObj.AddComponent<InGameUI_view>();
            view.Initialize(references, assetFont);
            return view;
        }

        private static void CreateMenus(Transform parentTrans, Font font, InGameUIReferences references)
        {
            RectTransform choiceRectTrans = Panel(parentTrans, "Growth and Relic Choices", 870, 100, 362, 620);
            references.ChoicePanelObj = choiceRectTrans.gameObject;
            references.ChoiceTitleTxt = Label(choiceRectTrans, font, "Title", "LEVEL UP", 16, 0, 330, 32, 22, ACCENT_COLOR, FontStyle.Bold);
            references.ChoiceSubtitleTxt = Label(choiceRectTrans, font, "Instructions", "선택 설명", 16, 38, 330, 50, 13, MUTED_COLOR);
            references.ChoiceBtns = new InGameMenuButton[3];
            for (int i = 0; i < 3; i++)
                references.ChoiceBtns[i] = MenuButton(choiceRectTrans, font, "Choice " + (i + 1), 8, 100 + i * 170, 346, 160);
            choiceRectTrans.gameObject.SetActive(false);

            RectTransform modalRectTrans = Rect(parentTrans, "Pause Guide Confirmation Result", 0, 0, 1280, 720);
            AddImage(modalRectTrans, new Color(0.02f, 0.035f, 0.06f, 0.97f)).raycastTarget = true;
            references.ModalPanelObj = modalRectTrans.gameObject;
            references.ModalTitleTxt = Label(modalRectTrans, font, "Title", "일시정지", 220, 64, 840, 64, 32, TEXT_COLOR, FontStyle.Bold);
            references.ModalSubtitleTxt = Label(modalRectTrans, font, "Body", "", 260, 148, 760, 216, 16, TEXT_COLOR);
            references.ModalSubtitleTxt.alignment = TextAnchor.UpperLeft;
            references.ModalBtns = new InGameMenuButton[5];
            for (int i = 0; i < references.ModalBtns.Length; i++)
                references.ModalBtns[i] = MenuButton(modalRectTrans, font, "Action " + (i + 1), 360, 380 + i * 58, 560, 52);
            modalRectTrans.gameObject.SetActive(false);
        }

        private static InGameMenuButton MenuButton(Transform parentTrans, Font font, string name, float x, float y, float width, float height)
        {
            RectTransform buttonRectTrans = Panel(parentTrans, name, x, y, width, height);
            Image backgroundImg = buttonRectTrans.GetComponent<Image>();
            backgroundImg.raycastTarget = true;
            Button button = buttonRectTrans.gameObject.AddComponent<Button>();
            button.targetGraphic = backgroundImg;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            Image selectionImg = AddImage(Rect(buttonRectTrans, "Selection", 0, 0, 3, height), ACCENT_COLOR);
            bool compact = height < 100;
            Text titleTxt = Label(buttonRectTrans, font, "Title", name, 16, compact ? 0 : 10, width - 32, compact ? height : 36, 17, TEXT_COLOR, FontStyle.Bold);
            Text descriptionTxt = Label(buttonRectTrans, font, "Description", "", 16, compact ? height : 52, width - 32, compact ? 0 : height - 60, 13, MUTED_COLOR);
            descriptionTxt.alignment = TextAnchor.UpperLeft;
            InGameMenuButton menuBtn = buttonRectTrans.gameObject.AddComponent<InGameMenuButton>();
            menuBtn.Initialize(titleTxt, descriptionTxt, selectionImg);
            return menuBtn;
        }

        private static InGameBoardGraphic CreateBoard(Transform parentTrans, Sprite boardFrameSprite, Sprite emptyCellSprite)
        {
            // 1920×1080 기준: BoardRoot 544×928 / Backdrop 496×880 / BoardArea 480×864.
            RectTransform boardRootRectTrans = Rect(parentTrans, "BoardRoot", 688f / REFERENCE_SCALE, 88, 544f / REFERENCE_SCALE, 928f / REFERENCE_SCALE);
            AddImage(Rect(boardRootRectTrans, "Backdrop", 16, 16, 496f / REFERENCE_SCALE, 880f / REFERENCE_SCALE), new Color(10f / 255, 16f / 255, 36f / 255));
            RectTransform areaRectTrans = Rect(boardRootRectTrans, "BoardArea", 32f / REFERENCE_SCALE, 32f / REFERENCE_SCALE, 320, 576);
            if (emptyCellSprite != null)
            {
                RectTransform gridRectTrans = Rect(areaRectTrans, "EmptyGrid", 0, 0, 320, 576);
                GridLayoutGroup grid = gridRectTrans.gameObject.AddComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(48, 48);
                grid.spacing = Vector2.zero;
                grid.padding = new RectOffset();
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 10;
                grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
                grid.startAxis = GridLayoutGroup.Axis.Horizontal;
                grid.childAlignment = TextAnchor.UpperLeft;
                for (int i = 0; i < 180; i++)
                {
                    Image cellImg = AddImage(Rect(gridRectTrans, "Empty " + i, 0, 0, 32, 32), Color.white);
                    cellImg.sprite = emptyCellSprite;
                }
            }
            InGameBoardGraphic boardGraphic = Rect(areaRectTrans, "Cells and Effects", 0, 0, 320, 576).gameObject.AddComponent<InGameBoardGraphic>();
            boardGraphic.raycastTarget = false;
            boardGraphic.SetEmptyCellBackground(emptyCellSprite == null);
            if (boardFrameSprite != null)
            {
                Image frameImg = AddImage(Rect(boardRootRectTrans, "Frame", 0, 0, 544f / REFERENCE_SCALE, 928f / REFERENCE_SCALE), Color.white);
                frameImg.sprite = boardFrameSprite;
                frameImg.type = Image.Type.Sliced;
                frameImg.pixelsPerUnitMultiplier = 6;
                frameImg.fillCenter = false;
            }
            return boardGraphic;
        }

        private static RectTransform Rect(Transform parentTrans, string name, float x, float y, float width, float height)
        {
            var rectTrans = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rectTrans.SetParent(parentTrans, false);
            rectTrans.anchorMin = rectTrans.anchorMax = new Vector2(0, 1);
            rectTrans.pivot = new Vector2(0, 1);
            rectTrans.anchoredPosition = new Vector2(x, -y) * REFERENCE_SCALE;
            rectTrans.sizeDelta = new Vector2(width, height) * REFERENCE_SCALE;
            return rectTrans;
        }

        private static Image AddImage(RectTransform rectTrans, Color color)
        {
            Image panelImg = rectTrans.gameObject.AddComponent<Image>();
            panelImg.color = color;
            panelImg.raycastTarget = false;
            return panelImg;
        }

        private static RectTransform Panel(Transform parentTrans, string name, float x, float y, float width, float height)
        {
            RectTransform panelRectTrans = Rect(parentTrans, name, x, y, width, height);
            AddImage(panelRectTrans, PANEL_COLOR);
            AddImage(Rect(panelRectTrans, "Top Border", 0, 0, width, 1), BORDER_COLOR);
            return panelRectTrans;
        }

        private static Text Label(Transform parentTrans, Font font, string name, string value, float x, float y, float width, float height, int size, Color color, FontStyle style = FontStyle.Normal)
        {
            Text labelTxt = Rect(parentTrans, name, x, y, width, height).gameObject.AddComponent<Text>();
            labelTxt.font = font;
            labelTxt.text = value;
            labelTxt.fontSize = Mathf.RoundToInt(size * REFERENCE_SCALE);
            labelTxt.fontStyle = style;
            labelTxt.color = color;
            labelTxt.alignment = TextAnchor.MiddleLeft;
            labelTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            labelTxt.verticalOverflow = VerticalWrapMode.Truncate;
            labelTxt.raycastTarget = false;
            return labelTxt;
        }

        private static Image Progress(Transform parentTrans, string name, float x, float y, float width, float height, Color color)
        {
            RectTransform trackRectTrans = Rect(parentTrans, name, x, y, width, height);
            AddImage(trackRectTrans, BORDER_COLOR);
            Image fillImg = AddImage(Rect(trackRectTrans, "Fill", 0, 0, width, height), color);
            RectTransform fillRectTrans = fillImg.rectTransform;
            fillRectTrans.anchorMin = Vector2.zero;
            fillRectTrans.anchorMax = Vector2.up;
            fillRectTrans.offsetMin = Vector2.zero;
            fillRectTrans.offsetMax = Vector2.zero;
            return fillImg;
        }

        private static Button PauseButton(Transform parentTrans, Font font)
        {
            RectTransform buttonRectTrans = Panel(parentTrans, "Pause Button", 1092, 28, 140, 40);
            Image panelImg = buttonRectTrans.GetComponent<Image>();
            panelImg.raycastTarget = true;
            Button pauseBtn = buttonRectTrans.gameObject.AddComponent<Button>();
            pauseBtn.targetGraphic = panelImg;
            pauseBtn.navigation = new Navigation { mode = Navigation.Mode.None };
            Text labelTxt = Label(buttonRectTrans, font, "Label", "Ⅱ  일시정지", 0, 0, 140, 40, 16, TEXT_COLOR);
            labelTxt.alignment = TextAnchor.MiddleCenter;
            return pauseBtn;
        }
    }
}
#endif
