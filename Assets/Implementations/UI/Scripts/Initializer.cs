using N2K;
using UnityEngine;

namespace UI_Example
{
    public class Initializer : MonoBehaviour
    {
        [SerializeField] private UIManager _uiManager;

        private void Start()
        {
            _uiManager.ShowScreen(ScreenType.Screen1);
        }
    }
}