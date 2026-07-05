using N2K;
using UnityEngine;
using UnityEngine.UI;

namespace UI_Example
{
    public class Popup2 : Base_Popup
    {
        [Header("=== POPUP2 ===")]

        [SerializeField] 
        private Button closeBtn;

        protected override void Initialize()
        {
            base.Initialize();

            closeBtn.onClick.AddListener(() => UIManager.Instance.HidePopup(this));
        }
    }
}