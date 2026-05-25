using UnityEngine;

namespace Networking.Services
{
    public static class DiceDebugService
    {
        public static bool IsEnabled = false;
        public static int ForcedValue = 1;

        public static int GetRoll(int fallback)
        {
            if (IsEnabled)
            {
                Debug.Log($"[DiceDebugService] Forcing dice roll: {ForcedValue} (original: {fallback})");
                return ForcedValue;
            }
            return fallback;
        }
    }
}
