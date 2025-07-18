using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerUIController : MonoBehaviour
{
    [Header("Health UI")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("Parry UI")]
    [SerializeField] private Slider parrySlider;
    [SerializeField] private GameObject parryReadyIndicator;

    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerCombatController playerCombat;

    void Start()
    {


        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateHealthUI;
            UpdateHealthUI(); 
        }

        if (playerCombat != null)
        {
            playerCombat.OnParryCountChanged += UpdateParryUI;
            UpdateParryUI(); 
        }

        if (parryReadyIndicator != null)
        {
            parryReadyIndicator.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHealthUI;
        }
        if (playerCombat != null)
        {
            playerCombat.OnParryCountChanged -= UpdateParryUI;
        }
    }

    private void UpdateHealthUI()
    {
        if (playerHealth == null) return;

        healthSlider.value = playerHealth.currentHealth / playerHealth.maxHealth;

        if (healthText != null)
        {
            healthText.text = $"{Mathf.Ceil(playerHealth.currentHealth)} / {playerHealth.maxHealth}";
        }
    }

    private void UpdateParryUI()
    {
        if (playerCombat == null) return;

        parrySlider.value = (float)playerCombat.CurrentParryCount / playerCombat.maxParryCount;

        if (parryReadyIndicator != null)
        {
            bool isReady = playerCombat.CurrentParryCount >= playerCombat.maxParryCount;
            parryReadyIndicator.SetActive(isReady);
        }
    }
}