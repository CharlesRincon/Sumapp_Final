using UnityEngine;

namespace Networking.Models
{
    [CreateAssetMenu(fileName = "Card_", menuName = "Networking/Card Definition")]
    public class CardDefinition : ScriptableObject
    {
        [SerializeField] private int _cardId = 1;
        [SerializeField] private string _displayName = "New Card";
        [SerializeField] private int _waterDelta;
        [SerializeField] private int _moneyDelta;
        [SerializeField] private int _basinDelta;

        public int CardId       => _cardId;
        public string DisplayName => _displayName;
        public int WaterDelta   => _waterDelta;
        public int MoneyDelta   => _moneyDelta;
        public int BasinDelta   => _basinDelta;
    }
}
