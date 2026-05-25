using UnityEngine;

namespace Networking.Services
{
    public static class MinigameDebugService
    {
        public static bool IsEnabled = false;
        public static string ForcedMinigameScene = "";

        public static string GetMinigameScene(string fallback)
        {
            if (IsEnabled && !string.IsNullOrEmpty(ForcedMinigameScene))
            {
                Debug.Log($"[MinigameDebugService] Forcing minigame scene: {ForcedMinigameScene} (original: {fallback})");
                return ForcedMinigameScene;
            }
            return fallback;
        }
    }
}
