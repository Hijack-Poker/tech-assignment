using System.Collections.Generic;
using UnityEngine;

namespace HijackPoker.UI
{
    public class CommunityCardsView : MonoBehaviour
    {
        [SerializeField] private CardView[] _slots;

        public void Refresh(List<string> cards)
        {
            int count = cards?.Count ?? 0;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (i < count)
                    _slots[i].SetCard(cards[i], faceUp: true);
                else
                    _slots[i].SetBlueBack();
            }
        }
    }
}
