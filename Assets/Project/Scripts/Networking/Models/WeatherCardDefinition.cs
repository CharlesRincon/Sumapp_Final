using UnityEngine;

namespace Networking.Models
{
    [CreateAssetMenu(fileName = "WeatherCard_", menuName = "Networking/Weather Card Definition")]
    public class WeatherCardDefinition : ScriptableObject
    {
        public Sprite Illustration;
        public string Description;
        public bool IsElNino; // true = El Niño, false = La Niña
    }
}
