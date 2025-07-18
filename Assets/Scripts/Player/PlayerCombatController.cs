using System;
using UnityEngine;

public class PlayerCombatController : MonoBehaviour
{

    private PlayerInputHandler playerInputHandler;

    [Header("Blocking")]
    public KeyCode blockKey = KeyCode.Mouse1; // Right Mouse Button
    public bool isBlocking { get; private set; }
    // public float blockStaminaCostPerSecond = 10f; // Optional for later

    [Header("Parrying")]
    public KeyCode parryKey = KeyCode.Q;
    public float parryAttemptDuration = 0.3f; // How long the parry window stays active
    public float parryCooldown = 1.0f; // Cooldown after a parry attempt (success or fail)
    public bool isParryAttemptActive { get; private set; }
    private float parryAttemptTimer;
    private float parryCooldownTimer;

    [Header("Parry Meter")]
    public int maxParryCount = 3;
    public int CurrentParryCount { get; private set; } // The UI will read this value
    public event Action OnParryCountChanged;

    SpriteRenderer spriteRenderer;



    private void Awake()
    {
        playerInputHandler = GetComponent<PlayerInputHandler>();
        if (playerInputHandler == null)
        {
            Debug.LogError("PlayerInputHandler component not found on this GameObject.", this);
            enabled = false;
            return;
        }
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("SpriteRenderer component not found on this GameObject.", this);
            enabled = false;
            return;
        }
    }

    void Update()
    {
        HandleParrying();
    }

    //Handles in InputHandler
    public void StartBlock()
    {
        if (isParryAttemptActive) return; // Cannot block during parry attempt

        isBlocking = true;
        Debug.Log("Player started blocking.");
    }

    public void EndBlock()
    {
        isBlocking = false;
        Debug.Log("Player stopped blocking.");
    }


    void HandleParrying()
    {
        if (parryCooldownTimer > 0)
        {
            parryCooldownTimer -= Time.deltaTime;
        }

        if (parryAttemptTimer > 0)
        {
            parryAttemptTimer -= Time.deltaTime;
            if (parryAttemptTimer <= 0)
            {
                isParryAttemptActive = false;
                spriteRenderer.color = Color.white;
                Debug.Log("Player parry attempt window closed.");
            }
        }

        if (playerInputHandler.IsParrying && parryCooldownTimer <= 0 && !isBlocking)
        {
            AttemptParry();
        }
    }

    void AttemptParry()
    {
        spriteRenderer.color = Color.yellow;
        isParryAttemptActive = true;
        parryAttemptTimer = parryAttemptDuration;
        parryCooldownTimer = parryCooldown; // Start cooldown for next parry attempt

        Debug.Log("Player attempting parry!");
    }

    public void NotifySuccessfulParry()
    {
        Debug.LogWarning("PLAYER SUCCESSFULLY PARRIED!");
        if (CurrentParryCount < maxParryCount)
        {
            CurrentParryCount++;
            OnParryCountChanged?.Invoke(); // Notify UI that the value changed

            if (CurrentParryCount >= maxParryCount)
            {
                Debug.LogWarning("PARRY SPECIAL ABILITY IS READY!");
            }
        }

    }
    public void UseAndResetParryMeter()
    {
        if (CurrentParryCount >= maxParryCount)
        {
            Debug.Log("SPECIAL ABILITY USED!");

            CurrentParryCount = 0;
            OnParryCountChanged?.Invoke(); // Notify UI that the meter is now empty
        }
    }
}