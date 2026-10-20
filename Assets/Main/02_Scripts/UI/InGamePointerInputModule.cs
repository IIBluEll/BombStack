using UnityEngine.InputSystem.UI;

namespace BombStack.UI
{
    /// <summary>포인터만 uGUI로 전달합니다. 키보드/패드 메뉴 입력은 GameInput에서 한 번만 처리합니다.</summary>
    public sealed class InGamePointerInputModule : InputSystemUIInputModule
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            move = null;
            submit = null;
            cancel = null;
        }
    }
}
