using System;
using System.IO;
using BombStack.Core;
using BombStack.Data;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BombStack.Editor
{
    public static class PrototypeEditorValidation
    {
        public static void CompileAndBuild()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--prototype-isolated") >= 0)
            {
                // 원본 프로젝트가 아닌 별도 검증 복사본에서만 기본 Scene을 구성합니다.
                UnityEngine.SceneManagement.Scene scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, "Assets/Main/01_Scene/SampleScene.unity");
            }
            var definition = GameDefinition.CreateDefault();
            var session = new GameSession(definition, 12345);
            session.HardDrop();
            if (!session.FinishResolution()) throw new Exception("배치 정산 실패");
            var record = new RunRecordData { TotalScore = 123, Bombs = new[] { new BombRecordData { Type = BOMB_TYPE.B04_RELAY, Level = 3 } } };
            var data = new PersistentData { BestRecord = record, HasSeenTutorial = true };
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".utmp"));
            Directory.CreateDirectory(Path.Combine(root, "UnityVerification"));
            string path = Path.Combine(root, "UnityVerification", "save-roundtrip.json");
            File.WriteAllText(path, JsonUtility.ToJson(data, true));
            PersistentData restored = JsonUtility.FromJson<PersistentData>(File.ReadAllText(path));
            if (!restored.HasSeenTutorial || restored.BestRecord.TotalScore != 123 || restored.BestRecord.Bombs[0].Level != 3)
                throw new Exception("Unity JSON 필드 왕복 실패");
            Debug.Log("PROTOTYPE_UNITY_COMPILE_AND_JSON_PASS");
            string output = Path.Combine(root, "PrototypeBuild", "BombStack.exe");
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Main/01_Scene/SampleScene.unity" },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("프로토타입 빌드 실패: " + report.summary.result);
            Debug.Log("PROTOTYPE_BUILD_PASS: " + output);
        }

        [MenuItem("Bomb Stack/Play Prototype")]
        public static void PlayPrototype()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "SampleScene")
            {
                EditorUtility.DisplayDialog("Bomb Stack", "기존 SampleScene을 연 뒤 실행해 주세요. 이 메뉴는 Scene을 변경하거나 저장하지 않습니다.", "확인");
                return;
            }
            EditorApplication.isPlaying = true;
        }
    }
}
