using UnityEngine;

namespace Networking.Models
{
    [CreateAssetMenu(fileName = "CardDatabase", menuName = "Networking/Card Database")]
    public class CardDatabase : ScriptableObject
    {
        [SerializeField] private CardDefinition[] _cards = new CardDefinition[0];

        public bool TryGetCard(int cardId, out CardDefinition card)
        {
            if (_cards != null)
            {
                for (int i = 0; i < _cards.Length; i++)
                {
                    if (_cards[i] != null && _cards[i].CardId == cardId)
                    {
                        card = _cards[i];
                        return true;
                    }
                }
            }

            card = null;
            return false;
        }
    }
}
