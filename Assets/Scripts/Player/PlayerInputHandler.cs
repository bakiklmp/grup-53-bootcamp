
using UnityEngine;
using UnityEngine.InputSystem; 

public class PlayerInputHandler : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the PlayerWeaponManager script.")]
    [SerializeField] private PlayerWeaponManager weaponManager;
    [SerializeField] private PlayerController playerMovement;
    private PlayerCombatController playerCombatController;
    private Camera mainCamera;


    private double lastMouseAimInputTime = -1.0;
    private double lastGamepadAimInputTime = -1.0;
    public Vector2 CalculatedWorldAimDirection { get; private set; }


    private PlayerControls _playerControls;

    private bool _isFiring = false;
    private bool isBlocking = false;
    public bool IsParrying { get; private set; }
    public Vector2 MoveInput { get; private set; }
    public Vector2 AimPosition { get; private set; } 
    public bool IsAimingWithMouse { get; private set; }

    public float minMouseAimDistance = 0.2f;
    void Awake()
    {
        _playerControls = new PlayerControls();
        mainCamera = Camera.main;
        playerCombatController = GetComponent<PlayerCombatController>();

        if (playerCombatController == null)
        {
            Debug.LogError("PlayerCombatController not assigned in PlayerInputHandler!", this);
            enabled = false; 
            return;
        }
        if (weaponManager == null)
        {
            Debug.LogError("PlayerWeaponManager not assigned in PlayerInputHandler!", this);
            enabled = false; 
            return;
        }
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found! Assign a camera with the 'MainCamera' tag.", this);
        }
    }

    void OnEnable()
    {
        _playerControls.Gameplay.Enable();

        // Weapon Firing
        _playerControls.Gameplay.Fire.started += OnFireStarted;
        _playerControls.Gameplay.Fire.canceled += OnFireCanceled;

        // Weapon Changing
        _playerControls.Gameplay.QuickChange.started += OnQuickChangePerformed;

        // Aiming 
        _playerControls.Gameplay.AimMouse.performed += OnAimMouseInputPerformed; 
        _playerControls.Gameplay.AimGamepad.performed += OnAimGamepadInputPerformed;

        // Movement
        _playerControls.Gameplay.Move.performed += OnMovePerformed;
        _playerControls.Gameplay.Move.canceled += OnMoveCanceled;

        // Dash
        _playerControls.Gameplay.Dash.performed += OnDashPerformed;

        // Parry
        _playerControls.Gameplay.Parry.performed += OnParryPerformed;
        _playerControls.Gameplay.Parry.canceled += OnParryCanceled;

        // Block
        _playerControls.Gameplay.Block.performed += OnBlockStarted;
        _playerControls.Gameplay.Block.canceled += OnBlockCanceled;
    }

    void OnDisable()
    {
        _playerControls.Gameplay.Disable();

        _playerControls.Gameplay.Fire.started -= OnFireStarted;
        _playerControls.Gameplay.Fire.canceled -= OnFireCanceled;

        _playerControls.Gameplay.AimMouse.performed -= OnAimMouseInputPerformed;
        _playerControls.Gameplay.AimGamepad.performed -= OnAimGamepadInputPerformed;


        _playerControls.Gameplay.Move.performed -= OnMovePerformed;
        _playerControls.Gameplay.Move.canceled -= OnMoveCanceled;

        _playerControls.Gameplay.Dash.performed -= OnDashPerformed;

        _playerControls.Gameplay.Parry.performed -= OnParryPerformed;
        _playerControls.Gameplay.Parry.canceled -= OnParryCanceled;


        _playerControls.Gameplay.Block.performed -= OnBlockStarted;
        _playerControls.Gameplay.Block.canceled -= OnBlockCanceled;
    }

    private void OnQuickChangePerformed(InputAction.CallbackContext context)
    {
        weaponManager.CycleNextWeapon();
    }
    private void OnBlockStarted(InputAction.CallbackContext context)
    {
        isBlocking = true;
    }
    private void OnBlockCanceled(InputAction.CallbackContext context)
    {
        isBlocking = false;
        playerCombatController.EndBlock();
    }
    private void OnParryPerformed(InputAction.CallbackContext context)
    {
        IsParrying = true;
    }
    private void OnParryCanceled(InputAction.CallbackContext context)
    {
        IsParrying = false;
    }
    private void OnFireStarted(InputAction.CallbackContext context)
    {
        _isFiring = true;
    }

    private void OnFireCanceled(InputAction.CallbackContext context)
    {
        _isFiring = false;
    }

    private void OnAimMouseInputPerformed(InputAction.CallbackContext context)
    {
        // Always update the time for this input source
        lastMouseAimInputTime = context.time;
        // Only update AimPosition and IsAimingWithMouse if this device is now dominant
        UpdateAimingState();
    }

    private void OnAimGamepadInputPerformed(InputAction.CallbackContext context)
    {
        Vector2 gamepadStickValue = context.ReadValue<Vector2>();


        if (gamepadStickValue.sqrMagnitude > 0.01f)
        {
            lastGamepadAimInputTime = context.time;
            UpdateAimingState();
        }
 
    }
    private void UpdateAimingState()
    {
        // Determine which input was most recent
        if (lastMouseAimInputTime > lastGamepadAimInputTime)
        {


            IsAimingWithMouse = true;

            AimPosition = _playerControls.Gameplay.AimMouse.ReadValue<Vector2>();
        }
        else if (lastGamepadAimInputTime > lastMouseAimInputTime)
        {
            // Gamepad was last active
            IsAimingWithMouse = false;
            // The actual gamepad stick value is read continuously.
            AimPosition = _playerControls.Gameplay.AimGamepad.ReadValue<Vector2>();
        }
        else
        {

            if (lastMouseAimInputTime < 0 && lastGamepadAimInputTime < 0)
            {
                IsAimingWithMouse = true; 
                AimPosition = _playerControls.Gameplay.AimMouse.ReadValue<Vector2>(); 
            }
        }
        // Debug.Log($"Aiming with Mouse: {IsAimingWithMouse}, Pos: {AimPosition}, MouseTime: {lastMouseAimInputTime}, GamepadTime: {lastGamepadAimInputTime}");
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();

        if (playerMovement != null)
        {
            playerMovement.SetMoveInput(MoveInput);

        }
    }
    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        MoveInput = Vector2.zero;
         if (playerMovement != null)
        {
            playerMovement.SetMoveInput(Vector2.zero);
        }
    }
    private void OnDashPerformed(InputAction.CallbackContext context)
    {
        if (playerMovement != null)
        {
            playerMovement.TryDash();
        }
    }
    


    void Update()
    {
        RecalculateWorldAimDirection();

        if (_isFiring)
        {
            if (weaponManager != null)
            {
                weaponManager.AttemptFireCurrentWeapon();
            }
        }
        if (isBlocking)
        {
            if (playerCombatController != null)
            {
                playerCombatController.StartBlock();
            }
        }

    }
    private void RecalculateWorldAimDirection()
    {
        if (IsAimingWithMouse)// Mouse Aiming
        {
            if (mainCamera == null || weaponManager == null)
            {
                Debug.LogWarning("Main Camera not found for aiming.");
                CalculatedWorldAimDirection = transform.right; // Fallback
                return;
            }
            
            
            Transform muzzle = transform;
            if (muzzle == null)
            {
   
                Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, mainCamera.nearClipPlane);
                Vector3 worldPoint = mainCamera.ScreenToWorldPoint(screenCenter);
                CalculatedWorldAimDirection = (worldPoint - transform.position).normalized; 
                if (mainCamera.orthographic) CalculatedWorldAimDirection = mainCamera.transform.up; 

                Debug.LogWarning($"MUZZLE EMPTY IN {this}");

                Vector3 mouseScreenP = AimPosition; 
                mouseScreenP.z = mainCamera.nearClipPlane + 10f; 
                Vector3 mouseW = mainCamera.ScreenToWorldPoint(mouseScreenP);
                CalculatedWorldAimDirection = (mouseW - mainCamera.transform.position).normalized; 
                return;
            }

            Vector3 mouseScreenPos = AimPosition; // This is the raw mouse screen position
            mouseScreenPos.z = mainCamera.WorldToScreenPoint(muzzle.position).z;
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);

            Vector3 direction3D = (new Vector3(mouseWorldPos.x, mouseWorldPos.y, muzzle.position.z) - muzzle.position).normalized;
            CalculatedWorldAimDirection = new Vector2(direction3D.x, direction3D.y);
        }
        else // Gamepad Aiming
        {
            if (AimPosition.sqrMagnitude > 0.01f) // AimPosition stores raw gamepad vector
            {
                CalculatedWorldAimDirection = AimPosition.normalized;
            }
            else
            {

                CalculatedWorldAimDirection = transform.right; 
            }
        }
    }

}