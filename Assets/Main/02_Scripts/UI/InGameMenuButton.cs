using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BombStack.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class InGameMenuButton : MonoBehaviour, IPointerEnterHandler
    {
        [SerializeField] private Text _titleTxt;
        [SerializeField] private Text _descriptionTxt;
        [SerializeField] private Image _selectionImg;
        private Button _menuBtn;
        private int _index;
        public event Action<int> Clicked;
        public event Action<int> Focused;

        public bool IsConfigured => _titleTxt != null && _descriptionTxt != null && _selectionImg != null;

        public void Initialize(Text titleTxt, Text descriptionTxt, Image selectionImg)
        {
            _titleTxt = titleTxt;
            _descriptionTxt = descriptionTxt;
            _selectionImg = selectionImg;
        }

        private void Awake() => _menuBtn = GetComponent<Button>();
        private void OnEnable() => GetComponent<Button>().onClick.AddListener(OnClicked);
        private void OnDisable() => GetComponent<Button>().onClick.RemoveListener(OnClicked);

        public void SetItem(int index, InGameMenuItem item, bool selected, bool inputEnabled)
        {
            _index = index;
            if (_menuBtn == null) _menuBtn = GetComponent<Button>();
            _titleTxt.text = item.Title;
            _descriptionTxt.text = item.Description;
            _selectionImg.enabled = selected;
            _menuBtn.interactable = inputEnabled && item.Interactable;
        }

        private void OnClicked()
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            Clicked?.Invoke(_index);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_menuBtn != null && _menuBtn.interactable) Focused?.Invoke(_index);
        }
    }
}
