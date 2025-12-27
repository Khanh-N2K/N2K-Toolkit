using N2K;
using UnityEngine;
using UnityEngine.UI;

namespace UI_Example
{
    public class Screen1 : ScreenBase
    {
        [Header("=== SCREEN1 ===")]

        [SerializeField] 
        private Button _goToScreen2Btn;

        [SerializeField] 
        private Button _openPopup1Btn;

        protected override void Initialize()
        {
            base.Initialize();

            _goToScreen2Btn.onClick.AddListener(() => UIManager.Instance.ShowScreen<Screen2>());
            _openPopup1Btn.onClick.AddListener(() => UIManager.Instance.ShowPopup<Popup1>());
        }
    }
}