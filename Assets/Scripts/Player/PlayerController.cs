using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed;
    public float rotationSpeed = 15f; // Adjust for how fast you want the rotation to be

    [Header("Dash Settings")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 1f;

    [Header("Layer Names")]
    [SerializeField] private string playerLayerName = "Player";
    [SerializeField] private string playerDashingLayerName = "PlayerDashing";

    public bool IsDashing { get; private set; }
    public bool CanDash { get; private set; } = true;

    public Vector2 MoveInput { get; private set; }
    public Vector2 LastMoveDirection { get; private set; } = Vector2.right;

    private Rigidbody2D rb;
    private PlayerInputHandler playerInputHandler;
    private SpriteRenderer spriteRenderer;

    [SerializeField] private SpriteRenderer dashCooldownColor;

    private int _playerLayerValue;
    private int _playerDashingLayerValue;
    private Coroutine _dashCoroutine;
    private Coroutine _dashCooldownCoroutine;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody2D component not found on this GameObject.", this);
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

        playerInputHandler = GetComponent<PlayerInputHandler>();
        if (playerInputHandler == null)
        {
            Debug.LogError("PlayerInputHandler component not found on this GameObject.", this);
            enabled = false;
            return;
        }

        // Get Layer integer values from names
        _playerLayerValue = LayerMask.NameToLayer(playerLayerName);
        _playerDashingLayerValue = LayerMask.NameToLayer(playerDashingLayerName);

        if (_playerLayerValue == -1)
        {
            Debug.LogError($"Player layer '{playerLayerName}' not found! Please define it in Project Settings > Tags and Layers.", this);
            enabled = false;
            return;
        }
        if (_playerDashingLayerValue == -1)
        {
            Debug.LogError($"Player Dashing layer '{playerDashingLayerName}' not found! Please define it in Project Settings > Tags and Layers.", this);
            enabled = false;
            return;
        }

        // Set initial layer
        gameObject.layer = _playerLayerValue;
    }
    private void OnDisable()
    {
        if (_dashCoroutine != null) StopCoroutine(_dashCoroutine);
        if (_dashCooldownCoroutine != null) StopCoroutine(_dashCooldownCoroutine);
    }

    public void SetMoveInput(Vector2 moveInput)
    {
        MoveInput = moveInput.normalized;

        if (MoveInput.sqrMagnitude > 0.01f)
        {
            LastMoveDirection = MoveInput.normalized;
        }
    }

    void FixedUpdate()
    {
        HandleMovement();
       // RotatePlayer();
        RotatePlayerTowardsAim();
    }
    private void Update()
    {
        //rotation should be here but there are some jittering problems with fps and stuff
    }
    private void HandleMovement()
    {
        // No movement while dashing
        if (IsDashing)
        {
            return;
        }
        rb.linearVelocity = MoveInput * moveSpeed;
    }

    private void RotatePlayerTowardsAim()
    {
        if (playerInputHandler.CalculatedWorldAimDirection == Vector2.zero)
        {
            return; // Don't rotate if there's no specific aim direction
        }

        float targetAngle = Mathf.Atan2(playerInputHandler.CalculatedWorldAimDirection.y, playerInputHandler.CalculatedWorldAimDirection.x) * Mathf.Rad2Deg;
        //targetAngle += spriteAngleOffset;

        Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
    }

    public void TryDash()
    {
        if (CanDash && !IsDashing)
        {
            Vector2 directionToDash = MoveInput.normalized;

            if (directionToDash == Vector2.zero) // If no current movement input
            {
                directionToDash = LastMoveDirection.normalized; // Use last known movement direction
            }

            if (directionToDash == Vector2.zero)
            {
                Debug.LogWarning("Cannot dash with zero direction. Defaulting to right.", this);
                directionToDash = Vector2.right;
            }

            if (_dashCoroutine != null) StopCoroutine(_dashCoroutine);
            _dashCoroutine = StartCoroutine(DashCoroutine(directionToDash));

            if (_dashCooldownCoroutine != null) StopCoroutine(_dashCooldownCoroutine);
            _dashCooldownCoroutine = StartCoroutine(DashCooldownCoroutine());
        }
    }

    private IEnumerator DashCoroutine(Vector2 dashDirection)
    {
        IsDashing = true;
        CanDash = false;

        int originalLayer = gameObject.layer;
        gameObject.layer = _playerDashingLayerValue;

        spriteRenderer.color = Color.red;

        rb.linearVelocity = dashDirection.normalized * dashSpeed;

        yield return new WaitForSeconds(dashDuration);

        // Only revert layer if it's still the dashing layer could be changed by other effects
        if (gameObject.layer == _playerDashingLayerValue)
        {
            gameObject.layer = originalLayer;
        }

        IsDashing = false;
        spriteRenderer.color = Color.white;

        _dashCoroutine = null;
    }

    private IEnumerator DashCooldownCoroutine()
    {
        dashCooldownColor.enabled = false;
        yield return new WaitForSeconds(dashCooldown);
        CanDash = true;
        _dashCooldownCoroutine = null;
        dashCooldownColor.enabled = true;

    }


}
