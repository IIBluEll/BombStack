using System;
using System.Collections;
using System.IO;
using BombStack.Data;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace BombStack.Prototype
{
    public sealed partial class BombStackPrototype
    {
        // 빌드 검증용 명시적 실행 옵션에서만 동작하며 일반 플레이/저장에는 관여하지 않습니다.
        private IEnumerator RunSmoke_cor()
        {
            string output = Path.Combine(Application.dataPath, "..", "Smoke");
            foreach (string argument in Environment.GetCommandLineArgs())
                if (argument.StartsWith("--prototype-smoke-output=", StringComparison.Ordinal)) output = argument.Substring("--prototype-smoke-output=".Length);
            Directory.CreateDirectory(output);
            for (int frame = 0; frame < 10; frame++) yield return null;
            yield return Capture_cor(Path.Combine(output, "01-menu.png"));
            StartGame();
            for (int frame = 0; frame < 10; frame++) yield return null;
            yield return Capture_cor(Path.Combine(output, "02-game.png"));
            Debug.Log($"PROTOTYPE_SMOKE_RENDER: repaints={_guiRepaintCount}, focus={Application.isFocused}, size={Screen.width}x{Screen.height}");

            Keyboard keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
            _session.Run.Build.Experience = 20; // 화면 분기 검증 입력. 실제 플레이 성장 검증으로 간주하지 않습니다.
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
            double inputDeadline = Time.realtimeSinceStartupAsDouble + 2;
            while (_session.Run.PlacedPieceCount == 0 && Time.realtimeSinceStartupAsDouble < inputDeadline) yield return null;
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            if (_session.Run.PlacedPieceCount != 1)
            {
                Debug.LogError("PROTOTYPE_SMOKE_FAIL: hard drop input");
                Application.Quit(1);
                yield break;
            }
            double deadline = Time.realtimeSinceStartupAsDouble + 10;
            while (_session.Run.State == RUN_STATE.RESOLVING && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            if (_session.Run.State != RUN_STATE.LEVEL_UP_CHOICE)
            {
                Debug.LogError("PROTOTYPE_SMOKE_FAIL: growth state");
                Application.Quit(1);
                yield break;
            }
            yield return Capture_cor(Path.Combine(output, "03-growth.png"));
            while (_session.Run.State == RUN_STATE.LEVEL_UP_CHOICE) _session.ChooseGrowth(_session.Run.GrowthChoices.Count - 1);
            _session.Pause();
            double before = _session.Run.PlaySeconds;
            yield return Capture_cor(Path.Combine(output, "04-pause.png"));
            if (_session.Run.PlaySeconds != before)
            {
                Debug.LogError("PROTOTYPE_SMOKE_FAIL: paused time changed");
                Application.Quit(1);
                yield break;
            }
            _session.Resume();
            _session.Run.StageScore = DEFINITION.GetStage(1).TargetScore;
            _session.HardDrop();
            deadline = Time.realtimeSinceStartupAsDouble + 10;
            while (_session.Run.State == RUN_STATE.RESOLVING && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            while (_session.Run.State == RUN_STATE.LEVEL_UP_CHOICE) _session.ChooseGrowth(_session.Run.GrowthChoices.Count - 1);
            if (_session.Run.State != RUN_STATE.RELIC_CHOICE)
            {
                Debug.LogError("PROTOTYPE_SMOKE_FAIL: relic state " + _session.Run.State);
                Application.Quit(1);
                yield break;
            }
            yield return Capture_cor(Path.Combine(output, "05-relic.png"));
            _session.ChooseRelic(0);
            _session.Run.RemainingPieces = 1;
            _session.HardDrop();
            deadline = Time.realtimeSinceStartupAsDouble + 10;
            while (_session.Run.State == RUN_STATE.RESOLVING && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            yield return Capture_cor(Path.Combine(output, "06-result.png"));
            bool passed = _guiRepaintCount > 0 && _session.Run.State == RUN_STATE.RESULT && _session.Run.StageNumber == 2 && _session.Run.Build.Relics.Count == 1;
            File.WriteAllText(Path.Combine(output, "smoke-result.txt"), passed ? "PASS: runtime bootstrap, IMGUI, InputSystem hard drop, resolution, growth, pause, relic, stage transition, result. Branch inputs include injected XP/score; not a balance test." : "FAIL");
            Debug.Log(passed ? "PROTOTYPE_SMOKE_PASS" : "PROTOTYPE_SMOKE_FAIL");
            if (passed && Array.IndexOf(Environment.GetCommandLineArgs(), "--prototype-stay-open") >= 0)
            {
                _smokeMode = false;
                Application.runInBackground = false;
                InputSystem.settings.backgroundBehavior = _backgroundBehaviorBeforeSmoke;
                _persistent = PrototypeSave.Load(out _notice);
                _session = null;
                _screen = SCREEN.MENU;
                GateInput();
                yield break;
            }
            Application.Quit(passed ? 0 : 1);
        }

        private static IEnumerator Capture_cor(string path)
        {
            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForSecondsRealtime(0.3f);
        }
    }
}
