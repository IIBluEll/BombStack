namespace BombStack.UI
{
    public enum IN_GAME_PANEL { NONE, GROWTH, RELIC, PAUSE, GUIDE, CONFIRM_RESTART, CONFIRM_EXIT, RESULT }
    public enum IN_GAME_ACTION { PAUSE, GROWTH, RELIC, RESUME, TOGGLE_SPEED, GUIDE, GUIDE_NEXT, RESTART, REQUEST_RESTART, REQUEST_EXIT, EXIT, CANCEL }

    /// <summary>UI 항목의 표시와 요청입니다. 실제 게임 상태 변경은 컨트롤러가 처리합니다.</summary>
    public sealed class InGameMenuItem
    {
        public IN_GAME_ACTION Action { get; }
        public int Index { get; }
        public string Title { get; }
        public string Description { get; }
        public bool Interactable { get; }

        public InGameMenuItem(IN_GAME_ACTION action, string title, string description = "", int index = 0, bool interactable = true)
        {
            Action = action;
            Title = title;
            Description = description;
            Index = index;
            Interactable = interactable;
        }
    }
}
