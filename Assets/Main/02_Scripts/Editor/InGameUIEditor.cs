using BombStack.Gameplay;
using BombStack.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BombStack.Editor
{
    /// <summary>사용자가 메뉴를 실행할 때만 편집 가능한 인게임 계층을 만듭니다. 자동 실행/저장하지 않습니다.</summary>
    public sealed class InGameUIEditor : EditorWindow
    {
        [SerializeField] private Font _font;
        [SerializeField] private Sprite _boardFrameSprite;
        [SerializeField] private Sprite _emptyCellSprite;

        [MenuItem("Bomb Stack/UI/Create In-game Screen")]
        public static void OpenWindow() => GetWindow<InGameUIEditor>("In-game UI");

        private void OnEnable()
        {
            if (_boardFrameSprite == null) _boardFrameSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Main/05_Assets/UI/Board/BoardFrame_9Slice_v1.png");
            if (_emptyCellSprite == null) _emptyCellSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Main/05_Assets/UI/Board/BoardEmptyCell_v1.png");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("본 개발용 인게임 UI", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("편집 가능한 Canvas, HUD, 선택/일시정지/결과 패널, InGameController를 한 번 생성합니다. Play 중 UI를 재생성하지 않습니다. 기존 오브젝트와 에셋을 덮어쓰지 않습니다.", MessageType.Info);
            _font = (Font)EditorGUILayout.ObjectField("Font (optional)", _font, typeof(Font), false);
            _boardFrameSprite = (Sprite)EditorGUILayout.ObjectField("Board Frame Sprite", _boardFrameSprite, typeof(Sprite), false);
            _emptyCellSprite = (Sprite)EditorGUILayout.ObjectField("Empty Cell Sprite", _emptyCellSprite, typeof(Sprite), false);
            if (_font == null) EditorGUILayout.HelpBox("한글 Font 에셋을 지정하면 편집 화면에서도 한글을 볼 수 있습니다. 미지정 시 플레이에서 OS 한글 폰트를 사용합니다.", MessageType.Info);
            if (_boardFrameSprite == null || _emptyCellSprite == null)
                EditorGUILayout.HelpBox("Sprite가 없는 부분은 기본 도형으로 생성됩니다. 이미지 임포트 설정은 자동 변경하지 않습니다.", MessageType.Warning);
            if (_boardFrameSprite != null && _boardFrameSprite.border == Vector4.zero)
                EditorGUILayout.HelpBox("프레임 Sprite의 Border가 없습니다. Sprite Editor에서 네 방향 192px를 적용한 뒤 생성해 주세요.", MessageType.Warning);

            Scene scene = SceneManager.GetActiveScene();
            bool canCreate = !EditorApplication.isPlayingOrWillChangePlaymode && scene.IsValid() && scene.isLoaded
                && scene.name != "SampleScene" && PrefabStageUtility.GetCurrentPrefabStage() == null;
            if (scene.name == "SampleScene")
                EditorGUILayout.HelpBox("SampleScene은 프로토타입용입니다. 본 개발용 InGame Scene을 열고 생성해 주세요.", MessageType.Info);
            using (new EditorGUI.DisabledScope(!canCreate))
                if (GUILayout.Button("현재 Scene에 인게임 화면 생성", GUILayout.Height(36))) CreateScreen();
        }

        private void CreateScreen()
        {
            InGameController existing = Object.FindFirstObjectByType<InGameController>(FindObjectsInactive.Include);
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                EditorUtility.DisplayDialog("In-game UI", "이미 InGameController가 있습니다. 기존 화면을 선택했습니다. 배치나 참조를 덮어쓰지 않습니다.", "확인");
                return;
            }
            GameObject rootObj = null;
            try
            {
                rootObj = new GameObject("InGame");
                SceneManager.MoveGameObjectToScene(rootObj, SceneManager.GetActiveScene());
                InGameUI_view view = InGameUIFactory.Create(rootObj.transform, _font, _boardFrameSprite, _emptyCellSprite);
                rootObj.AddComponent<InGameController>().Configure(view);
                view.gameObject.SetActive(true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(view.GetComponent<RectTransform>());
                Undo.RegisterCreatedObjectUndo(rootObj, "Create In-game Screen");
                EditorSceneManager.MarkSceneDirty(rootObj.scene);
                Selection.activeGameObject = rootObj;
            }
            catch
            {
                if (rootObj != null) Object.DestroyImmediate(rootObj);
                throw;
            }
        }
    }
}
