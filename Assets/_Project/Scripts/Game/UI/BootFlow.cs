using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeonHorde
{
    /// <summary>Boot scene: services are already up (GameBootstrap); route to the menu.</summary>
    public sealed class BootFlow : MonoBehaviour
    {
        void Start()
        {
            if (Application.CanStreamedLevelBeLoaded("MainMenu"))
                SceneManager.LoadScene("MainMenu");
            else if (Application.CanStreamedLevelBeLoaded("Game"))
                SceneManager.LoadScene("Game");
        }
    }
}
