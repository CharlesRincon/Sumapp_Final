using UnityEngine;

namespace Networking.Services
{
    /// <summary>
    /// Small host-side service that tracks shared basin health and defeat threshold.
    /// </summary>
    public class BasinService
    {
        private int _basinHealth;

        public int BasinHealth => _basinHealth;
        public bool IsDefeated => _basinHealth <= 0;

        public void Initialize(int initialHealth)
        {
            _basinHealth = Mathf.Clamp(initialHealth, 0, 100);
        }

        public int ApplyDelta(int delta)
        {
            _basinHealth = Mathf.Clamp(_basinHealth + delta, 0, 100);
            return _basinHealth;
        }
    }
}
