using TMPro;
using UnityEngine;

namespace Player.UI
{
    public class CharacterHealthUI : MonoBehaviour
    {
        public TextMeshProUGUI healthText;

        private void Start()
        {
            healthText.enabled = false;
        }

        public void ShowHealth(int health)
        {
            healthText.enabled = true;
            healthText.text = health.ToString();
        }
        public void HideHealth()
        {
            healthText.enabled = false;
        }
    }
}