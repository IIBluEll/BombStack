using System;
using BombStack.Core;
using BombStack.Data;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace BombStack.Prototype
{
    /// <summary>SampleScene에서만 자동 생성되는 임시 프로토타입. Scene/Prefab 참조가 없습니다.</summary>
    public sealed partial class BombStackPrototype : MonoBehaviour
    {
        private enum SCREEN { MENU, GAME, GUIDE, CONFIRM }
        private readonly GameDefinition DEFINITION = GameDefinition.CreateDefault();
        private PrototypeInput _input;
        private GameSession _session;
        private PersistentData _persistent;
        private PrototypePlayback _playback;
        private PlacementResultData _seenPlacement;
        private ExplosionWaveData _preview;
        private SCREEN _screen;
        private SCREEN _returnScreen;
        private RUN_STATE _observedState;
        private bool _waitingForRelease = true;
        private bool _usedGamepad;
        private bool _resultSaved;
        private bool _restartConfirmed;
        private bool _startAfterGuide;
        private bool _smokeMode;
        private InputSettings.BackgroundBehavior _backgroundBehaviorBeforeSmoke;
        private int _selection;
        private int _guidePage;
        private int _heldDirection;
        private float _repeatElapsed;
        private float _repeatThreshold;
        private string _notice = "";
        private Font _font;
        private GUIStyle _labelStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (SceneManager.GetActiveScene().name != "SampleScene" || FindFirstObjectByType<BombStackPrototype>() != null) return;
            new GameObject("Bomb Stack Prototype (Runtime)").AddComponent<BombStackPrototype>();
        }

        private void Awake()
        {
            _input = new PrototypeInput();
            _smokeMode = Array.IndexOf(Environment.GetCommandLineArgs(), "--prototype-smoke") >= 0;
            if (_smokeMode)
            {
                _backgroundBehaviorBeforeSmoke = InputSystem.settings.backgroundBehavior;
                Application.runInBackground = true;
                Application.targetFrameRate = 60;
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            }
            _persistent = _smokeMode ? new PersistentData { HasSeenTutorial = true } : PrototypeSave.Load(out _notice);
            _font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Noto Sans CJK KR", "Arial" }, 18);
            InputSystem.onDeviceChange += OnDeviceChanged;
            if (_smokeMode) StartCoroutine(RunSmoke_cor());
        }

        private void OnDestroy()
        {
            InputSystem.onDeviceChange -= OnDeviceChanged;
            _input?.Dispose();
            if (_font != null) Destroy(_font);
        }

        private void OnGUI() => DrawPrototype();

        private void OnDeviceChanged(InputDevice device, InputDeviceChange change)
        {
            if (_usedGamepad && device is Gamepad && (change == InputDeviceChange.Disconnected || change == InputDeviceChange.Removed || change == InputDeviceChange.Disabled)) Pause();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused && !_smokeMode) Pause();
        }

        private void Pause()
        {
            if (_screen != SCREEN.GAME || _session == null) return;
            _session.Pause();
            GateInput();
        }

        private void GateInput()
        {
            _waitingForRelease = true;
            _selection = 0;
            _heldDirection = 0;
            _repeatElapsed = 0;
        }

        private void StartGame()
        {
            int seed = unchecked(Environment.TickCount ^ Guid.NewGuid().GetHashCode());
            _session = new GameSession(DEFINITION, seed);
            _playback = null;
            _seenPlacement = null;
            _resultSaved = false;
            _screen = SCREEN.GAME;
            _observedState = _session.Run.State;
            GateInput();
        }

        private void BeginNewGame()
        {
            if (_persistent.HasSeenTutorial) StartGame();
            else { _startAfterGuide = true; ShowGuide(); }
        }

        private void ShowGuide()
        {
            _returnScreen = _screen;
            _screen = SCREEN.GUIDE;
            _guidePage = 0;
            GateInput();
        }

        private void Update()
        {
            if (_input.GamepadPressed) _usedGamepad = true;
            if (_input.KeyboardPressed) _usedGamepad = false;
            if (_screen != SCREEN.GAME) { HandleMenuInput(); return; }
            RunData run = _session.Run;
            if (_observedState != run.State) { _observedState = run.State; GateInput(); }
            if (_waitingForRelease && !_input.AnyHeld) _waitingForRelease = false;
            if (!_waitingForRelease && _input.PausePressed && run.State != RUN_STATE.RESULT)
            {
                if (run.State == RUN_STATE.PAUSED) _session.Resume();
                else _session.Pause();
                GateInput();
                return;
            }
            float seconds = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            if (run.State == RUN_STATE.PIECE_CONTROL)
            {
                if (!_waitingForRelease)
                {
                    if (_input.HardDropPressed) _session.HardDrop();
                    else
                    {
                        if (_input.RotationPressed) _session.Rotate(_input.RotationClockwise);
                        UpdateHorizontal(seconds);
                    }
                }
                _session.Tick(seconds, !_waitingForRelease && _input.SoftDropHeld);
                _preview = _session.GetLandingPreview();
                if (run.State == RUN_STATE.RESOLVING)
                {
                    _seenPlacement = run.LastPlacement;
                    _playback = new PrototypePlayback(_session.LastLockedBoard, run.LastPlacement, DEFINITION.Presentation);
                    GateInput();
                }
            }
            else if (run.State == RUN_STATE.RESOLVING)
            {
                _session.Tick(seconds, false);
                if (!ReferenceEquals(_seenPlacement, run.LastPlacement))
                {
                    _seenPlacement = run.LastPlacement;
                    _playback = new PrototypePlayback(_session.LastLockedBoard, run.LastPlacement, DEFINITION.Presentation);
                }
                _playback.Tick(seconds, _persistent.Settings.PresentationSpeed == PRESENTATION_SPEED.FAST);
                if (_playback.Finished) { _session.FinishResolution(); GateInput(); }
            }
            else
            {
                if (run.State == RUN_STATE.RESULT && !_resultSaved) SaveResult();
                HandleMenuInput();
            }
        }

        private void UpdateHorizontal(float seconds)
        {
            int direction = _input.HorizontalDirection;
            if (direction != _heldDirection)
            {
                _heldDirection = direction;
                _repeatElapsed = 0;
                _repeatThreshold = DEFINITION.Timing.HorizontalRepeatDelaySeconds;
                if (direction != 0) _session.Move(direction);
            }
            else if (direction != 0)
            {
                _repeatElapsed += seconds;
                while (_repeatElapsed >= _repeatThreshold)
                {
                    _repeatElapsed -= _repeatThreshold;
                    _repeatThreshold = DEFINITION.Timing.HorizontalRepeatIntervalSeconds;
                    _session.Move(direction);
                }
            }
        }

        private void HandleMenuInput()
        {
            if (_waitingForRelease) { if (!_input.AnyHeld) _waitingForRelease = false; return; }
            int count = MenuItemCount();
            if (_input.PreviousPressed) _selection = (_selection + count - 1) % count;
            if (_input.NextPressed) _selection = (_selection + 1) % count;
            if (_input.BackPressed)
            {
                if (_screen == SCREEN.GUIDE) CloseGuide();
                else if (_screen == SCREEN.CONFIRM) { _screen = SCREEN.GAME; GateInput(); }
                else if (_screen == SCREEN.GAME && _session.Run.State == RUN_STATE.PAUSED) { _session.Resume(); GateInput(); }
            }
            else if (_input.ConfirmPressed) ActivateMenu(_selection);
        }

        private int MenuItemCount()
        {
            if (_screen == SCREEN.MENU) return 3;
            if (_screen == SCREEN.GUIDE) return 1;
            if (_screen == SCREEN.CONFIRM) return 2;
            switch (_session.Run.State)
            {
                case RUN_STATE.LEVEL_UP_CHOICE: return _session.Run.GrowthChoices.Count;
                case RUN_STATE.RELIC_CHOICE: return _session.Run.RelicChoices.Count;
                case RUN_STATE.PAUSED: return 5;
                case RUN_STATE.RESULT: return 2;
                default: return 1;
            }
        }

        private void ActivateMenu(int index)
        {
            if (_screen == SCREEN.MENU)
            {
                if (index == 0) BeginNewGame();
                else if (index == 1) { _startAfterGuide = false; ShowGuide(); }
                else Quit();
            }
            else if (_screen == SCREEN.GUIDE)
            {
                if (++_guidePage >= 4) CloseGuide();
                GateInput();
            }
            else if (_screen == SCREEN.CONFIRM)
            {
                _screen = SCREEN.GAME;
                if (index == 0)
                {
                    _session.Abandon();
                    if (_restartConfirmed) StartGame();
                    else { _session = null; _screen = SCREEN.MENU; }
                }
                GateInput();
            }
            else if (_session.Run.State == RUN_STATE.LEVEL_UP_CHOICE) { _session.ChooseGrowth(index); GateInput(); }
            else if (_session.Run.State == RUN_STATE.RELIC_CHOICE) { _session.ChooseRelic(index); GateInput(); }
            else if (_session.Run.State == RUN_STATE.PAUSED)
            {
                if (index == 0) _session.Resume();
                else if (index == 1)
                {
                    _persistent.Settings.PresentationSpeed = _persistent.Settings.PresentationSpeed == PRESENTATION_SPEED.NORMAL ? PRESENTATION_SPEED.FAST : PRESENTATION_SPEED.NORMAL;
                    if (!_smokeMode) PrototypeSave.Save(_persistent, out _notice);
                }
                else if (index == 2) { _startAfterGuide = false; ShowGuide(); }
                else { _restartConfirmed = index == 3; _screen = SCREEN.CONFIRM; }
                GateInput();
            }
            else if (_session.Run.State == RUN_STATE.RESULT)
            {
                if (index == 0) StartGame();
                else { _session = null; _screen = SCREEN.MENU; GateInput(); }
            }
        }

        private void CloseGuide()
        {
            if (_startAfterGuide)
            {
                _persistent.HasSeenTutorial = true;
                if (!_smokeMode) PrototypeSave.Save(_persistent, out _notice);
                _startAfterGuide = false;
                StartGame();
            }
            else _screen = _returnScreen;
            GateInput();
        }

        private void SaveResult()
        {
            _resultSaved = true;
            RunRecordData record = RunRecordData.FromRun(_session.Run);
            _persistent.TotalPlayCount++;
            if (record.EndReason == RUN_END_REASON.VICTORY) _persistent.TotalVictoryCount++;
            if (RecordRules.IsBetter(record, _persistent.BestRecord)) _persistent.BestRecord = record;
            if (!_smokeMode) PrototypeSave.Save(_persistent, out _notice);
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
