using N2K;
using UnityEngine;
using UnityEngine.UI;

namespace UI_Example
{
    public class Popup1 : PopupBase
    {
        [Header("=== POPUP1 ===")]

        [SerializeField] 
        private Button closeBtn;

        [SerializeField] 
        private Button openPopup2Btn;

        protected override void Initialize()
        {
            base.Initialize();

            closeBtn.onClick.AddListener(() => UIManager.Instance.HidePopup(this));
            openPopup2Btn.onClick.AddListener(() => UIManager.Instance.ShowPopup<Popup2>());
        }
    }
}