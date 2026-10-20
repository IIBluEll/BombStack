using System;
using BombStack.Core;
using BombStack.Data;
using BombStack.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace BombStack.Gameplay
{
    /// <summary>본 개발 인게임 진입점. Scene의 View를 사용하며 UI 계층을 런타임에 생성하지 않습니다.</summary>
    [DisallowMultipleComponent]
    public sealed class InGameController : MonoBehaviour
    {
        [SerializeField] private InGameUI_view _inGameView;
        [SerializeField] private bool _startOnEnable = true;
        [SerializeField] private bool _showGuideOnFirstStart = true;
        [SerializeField] private string _menuSceneName = "";
        [SerializeField] private GameSettingsData _settings = new GameSettingsData();
        private readonly GameDefinition DEFINITION = GameDefinition.CreateDefault();
        private GameInput _input;
        private GameSession _session;
        private ResolutionPlayback _playback;
        private PlacementResultData _seenPlacement;
        private InGameUIModel_model _model;
        private InGameUIPresenter_presenter _presenter;
        private ExplosionWaveData _preview;
        private ActivePieceData _previewPiece;
        private GridPosition _previewOrigin;
        private PIECE_ROTATION _previewRotation;
        private RUN_STATE _observedState;
        private IN_GAME_PANEL _confirmation;
        private bool _waitingForRelease;
        private bool _usingGamepad;
        private bool _finishedPublished;
        private bool _hasSeenGuide;
        private bool _guideBeforeRun;
        private bool _exiting;
        private int _guidePage = -1;
        private int _menuVersion;
        private int _gatedFrame;
        private int _actionFrame = -1;
        private int _heldDirection;
        private float _repeatElapsed;
        private float _repeatThreshold;

        public RunData Run => _session?.Run;
        public event Action<RunRecordData> RunFinished;
        public event Action<GameSettingsData> SettingsChanged;
        public event Action GuideCompleted;
        public event Action ReturnToMenuRequested;
        private bool CanExit => ReturnToMenuRequested != null || (!string.IsNullOrWhiteSpace(_menuSceneName) && Application.CanStreamedLevelBeLoaded(_menuSceneName));

#if UNITY_EDITOR
        public void Configure(InGameUI_view view) => _inGameView = view;
#endif

        private void Awake()
        {
            if (_inGameView == null || !_inGameView.IsConfigured)
            {
                Debug.LogError("InGameController에 완성된 InGameUI_view 참조가 필요합니다. Bomb Stack/UI/Create In-game Screen으로 계층을 만들거나 Inspector 참조를 연결해 주세요.", this);
                enabled = false;
                return;
            }
            if (_settings == null) _settings = new GameSettingsData();
            _input = new GameInput();
            _model = new InGameUIModel_model(DEFINITION);
            _presenter = new InGameUIPresenter_presenter(_inGameView, _model);
            _presenter.ActionRequested += OnUIActionRequested;
        }

        private void OnEnable()
        {
            if (_presenter == null) return;
            _input.Enable();
            InputSystem.onDeviceChange += OnDeviceChanged;
            if (_session == null && _startOnEnable) StartNewGame();
            else if (_session != null) { GateInput(); RefreshUI(); }
            else _inGameView.Close();
        }

        private void OnDisable()
        {
            InputSystem.onDeviceChange -= OnDeviceChanged;
            _input?.Disable();
            if (_session != null) _session.Pause();
            _presenter?.Close();
        }

        private void OnDestroy()
        {
            if (_presenter != null)
            {
                _presenter.ActionRequested -= OnUIActionRequested;
                _presenter.Dispose();
            }
            _input?.Dispose();
        }

        /// <summary>메뉴/저장 처리에서 시작 전에 설정과 최초 안내의 확인 여부를 전달합니다.</summary>
        public void ApplyPreferences(GameSettingsData settings, bool hasSeenGuide)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _hasSeenGuide = hasSeenGuide;
            _menuVersion++;
        }

        public void StartNewGame() => StartNewGame(unchecked(Environment.TickCount ^ Guid.NewGuid().GetHashCode()));

        public void StartNewGame(int seed)
        {
            if (_presenter == null) throw new InvalidOperationException("InGameController의 View 참조가 연결되지 않았습니다.");
            if (_session != null && _session.Run.State != RUN_STATE.RESULT)
            {
                _session.Abandon();
                PublishFinished();
            }
            _session = new GameSession(DEFINITION, seed);
            _playback = null;
            _seenPlacement = null;
            _previewPiece = null;
            _preview = null;
            _finishedPublished = false;
            _exiting = false;
            _confirmation = IN_GAME_PANEL.NONE;
            _guideBeforeRun = _showGuideOnFirstStart && !_hasSeenGuide;
            _guidePage = _guideBeforeRun ? 0 : -1;
            if (_guideBeforeRun) _session.Pause();
            _observedState = _session.Run.State;
            if (isActiveAndEnabled) _input.Enable();
            GateInput();
            RefreshUI();
        }

        private void Update()
        {
            if (_session == null || _exiting) return;
            if (_input.GamepadPressed) _usingGamepad = true;
            if (_input.KeyboardPressed) _usingGamepad = false;
            ObserveState();
            if (_waitingForRelease && Time.frameCount > _gatedFrame && !_input.AnyHeld) _waitingForRelease = false;
            if (!_waitingForRelease && _input.PausePressed) { HandleBackOrPause(); return; }
            float seconds = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            if (_session.Run.State == RUN_STATE.PIECE_CONTROL)
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
                if (_session.Run.State == RUN_STATE.RESOLVING) BeginPlayback();
            }
            else if (_session.Run.State == RUN_STATE.RESOLVING)
            {
                _session.Tick(seconds, false);
                if (!ReferenceEquals(_seenPlacement, _session.Run.LastPlacement)) BeginPlayback();
                _playback.Tick(seconds, _settings.PresentationSpeed == PRESENTATION_SPEED.FAST);
                if (_playback.Finished) _session.FinishResolution();
            }
            else if (!_waitingForRelease)
            {
                if (_input.BackPressed) HandleBackOrPause();
                else
                {
                    if (_input.PreviousPressed) _presenter.MoveSelection(-1);
                    if (_input.NextPressed) _presenter.MoveSelection(1);
                    if (_input.ConfirmPressed) _presenter.ConfirmSelection();
                }
            }
            ObserveState();
            PublishFinished();
        }

        private void LateUpdate()
        {
            if (_session != null && !_exiting) RefreshUI();
        }

        private void BeginPlayback()
        {
            _seenPlacement = _session.Run.LastPlacement;
            _playback = new ResolutionPlayback(_session.LastLockedBoard, _seenPlacement, DEFINITION.Presentation);
            _previewPiece = null;
            GateInput();
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

        private void GateInput()
        {
            _waitingForRelease = true;
            _gatedFrame = Time.frameCount;
            _heldDirection = 0;
            _repeatElapsed = 0;
            _menuVersion++;
        }

        private void ObserveState()
        {
            if (_observedState == _session.Run.State) return;
            _observedState = _session.Run.State;
            GateInput();
        }

        private IN_GAME_PANEL CurrentPanel()
        {
            if (_session.Run.State == RUN_STATE.PAUSED)
            {
                if (_confirmation != IN_GAME_PANEL.NONE) return _confirmation;
                return _guidePage >= 0 ? IN_GAME_PANEL.GUIDE : IN_GAME_PANEL.PAUSE;
            }
            switch (_session.Run.State)
            {
                case RUN_STATE.LEVEL_UP_CHOICE: return IN_GAME_PANEL.GROWTH;
                case RUN_STATE.RELIC_CHOICE: return IN_GAME_PANEL.RELIC;
                case RUN_STATE.RESULT: return IN_GAME_PANEL.RESULT;
                default: return IN_GAME_PANEL.NONE;
            }
        }

        private void RefreshUI()
        {
            RunData run = _session.Run;
            RUN_STATE displayState = run.State == RUN_STATE.PAUSED ? run.StateBeforePause : run.State;
            bool resolving = displayState == RUN_STATE.RESOLVING;
            bool hasPlayback = resolving && _playback != null && ReferenceEquals(_seenPlacement, run.LastPlacement);
            BoardData board = resolving ? (hasPlayback ? _playback.Board : _session.LastLockedBoard ?? run.Board) : run.Board;
            if (displayState == RUN_STATE.PIECE_CONTROL && run.ActivePiece != null)
            {
                ActivePieceData active = run.ActivePiece;
                if (run.State == RUN_STATE.PIECE_CONTROL && (_previewPiece != active || _previewOrigin != active.Origin || _previewRotation != active.Rotation))
                {
                    _preview = _session.GetLandingPreview();
                    _previewPiece = active;
                    _previewOrigin = active.Origin;
                    _previewRotation = active.Rotation;
                }
            }
            else _previewPiece = null;
            bool previewMatches = run.ActivePiece != null && _previewPiece == run.ActivePiece
                && _previewOrigin == run.ActivePiece.Origin && _previewRotation == run.ActivePiece.Rotation;
            ExplosionWaveData wave = resolving ? (hasPlayback ? _playback.Wave : null) : displayState == RUN_STATE.PIECE_CONTROL && previewMatches ? _preview : null;
            int phase = hasPlayback ? _playback.Phase : resolving ? 0 : -1;
            _model.Capture(run, board, wave, phase, hasPlayback ? _playback.WaveIndex + 1 : 0, hasPlayback ? _playback.GravityProgress : 0,
                _usingGamepad, CurrentPanel(), _menuVersion, Math.Max(0, _guidePage), _settings.PresentationSpeed == PRESENTATION_SPEED.FAST, CanExit, !_waitingForRelease);
            _presenter.Open();
            _presenter.Refresh();
        }

        private void OnUIActionRequested(IN_GAME_ACTION action, int index)
        {
            if (_session == null || _exiting || _waitingForRelease || _actionFrame == Time.frameCount) return;
            _actionFrame = Time.frameCount;
            IN_GAME_PANEL panel = CurrentPanel();
            switch (action)
            {
                case IN_GAME_ACTION.PAUSE: _session.Pause(); break;
                case IN_GAME_ACTION.GROWTH: if (panel == IN_GAME_PANEL.GROWTH) _session.ChooseGrowth(index); break;
                case IN_GAME_ACTION.RELIC: if (panel == IN_GAME_PANEL.RELIC) _session.ChooseRelic(index); break;
                case IN_GAME_ACTION.RESUME: if (panel == IN_GAME_PANEL.PAUSE) _session.Resume(); break;
                case IN_GAME_ACTION.TOGGLE_SPEED:
                    _settings.PresentationSpeed = _settings.PresentationSpeed == PRESENTATION_SPEED.FAST ? PRESENTATION_SPEED.NORMAL : PRESENTATION_SPEED.FAST;
                    SettingsChanged?.Invoke(_settings);
                    break;
                case IN_GAME_ACTION.GUIDE: _guidePage = 0; break;
                case IN_GAME_ACTION.GUIDE_NEXT:
                    if (++_guidePage >= 4) FinishGuide();
                    break;
                case IN_GAME_ACTION.REQUEST_RESTART: _confirmation = IN_GAME_PANEL.CONFIRM_RESTART; break;
                case IN_GAME_ACTION.REQUEST_EXIT: if (CanExit) _confirmation = IN_GAME_PANEL.CONFIRM_EXIT; break;
                case IN_GAME_ACTION.CANCEL: _confirmation = IN_GAME_PANEL.NONE; break;
                case IN_GAME_ACTION.RESTART: StartNewGame(); return;
                case IN_GAME_ACTION.EXIT: ExitToMenu(); return;
            }
            GateInput();
            ObserveState();
            RefreshUI();
        }

        private void HandleBackOrPause()
        {
            if (_session.Run.State == RUN_STATE.RESULT) return;
            if (_session.Run.State != RUN_STATE.PAUSED) _session.Pause();
            else if (_confirmation != IN_GAME_PANEL.NONE) _confirmation = IN_GAME_PANEL.NONE;
            else if (_guidePage >= 0)
            {
                if (_guideBeforeRun) return;
                _guidePage = -1;
            }
            else _session.Resume();
            GateInput();
            ObserveState();
            RefreshUI();
        }

        private void FinishGuide()
        {
            _guidePage = -1;
            _hasSeenGuide = true;
            bool resume = _guideBeforeRun;
            _guideBeforeRun = false;
            GuideCompleted?.Invoke();
            if (resume) _session.Resume();
        }

        private void PublishFinished()
        {
            if (_finishedPublished || _session == null || _session.Run.State != RUN_STATE.RESULT) return;
            _finishedPublished = true;
            RunFinished?.Invoke(RunRecordData.FromRun(_session.Run));
        }

        private void ExitToMenu()
        {
            if (!CanExit) return;
            if (_session.Run.State != RUN_STATE.RESULT) _session.Abandon();
            PublishFinished();
            _exiting = true;
            _input.Disable();
            _presenter.Close();
            if (ReturnToMenuRequested != null) ReturnToMenuRequested.Invoke();
            else SceneManager.LoadScene(_menuSceneName);
        }

        private void OnApplicationFocus(bool focused) { if (!focused) PauseForInterruption(); }
        private void OnApplicationPause(bool paused) { if (paused) PauseForInterruption(); }
        private void OnDeviceChanged(InputDevice device, InputDeviceChange change)
        {
            if (_usingGamepad && device is Gamepad && (change == InputDeviceChange.Disconnected || change == InputDeviceChange.Removed || change == InputDeviceChange.Disabled))
                PauseForInterruption();
        }

        private void PauseForInterruption()
        {
            if (_session == null || _exiting || _session.Run.State == RUN_STATE.PAUSED || _session.Run.State == RUN_STATE.RESULT) return;
            _session.Pause();
            GateInput();
            ObserveState();
            if (isActiveAndEnabled) RefreshUI();
        }
    }
}
