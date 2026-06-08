using UnityEngine;
using UnityEngine.InputSystem;

namespace CarrotClash
{
    /// <summary>
    /// Routes new-InputSystem actions to the local <see cref="PlayerController"/>. Reads the
    /// "Player" action map from <see cref="InputSystem_Actions"/> (loaded by name; no generated
    /// wrapper is present) and subscribes to the actions that exist (Move, Look, Attack, Sprint,
    /// Crouch, Jump, Interact, Next/Previous for weapon swap).
    ///
    /// The action asset currently lacks Reload / ADS / Ability1 / Ability2, so those use a direct
    /// device-polling fallback (Keyboard.current / Mouse.current) in <see cref="Update"/>:
    /// R = Reload, RightMouse = ADS, Q = Ability1, E = Ability2, mouse-wheel / Q-row = SwapWeapon.
    ///
    /// Only attached to the local player (PlayerSpawner adds it). Enables the map while the
    /// component is enabled and locks + hides the hardware cursor while bound.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController))]
    public class PlayerInputBinder : MonoBehaviour
    {
        [Header("Input asset")]
        [Tooltip("Optional explicit reference. If null, the asset is loaded by name 'InputSystem_Actions' from Resources or located in memory.")]
        [SerializeField] InputActionAsset actionAssetOverride;
        [SerializeField] string actionMapName = "Player";

        [Header("Cursor")]
        [SerializeField] bool lockCursor = true;

        PlayerController player;
        InputActionAsset assetInstance;
        InputActionMap playerMap;

        InputAction moveAction;
        InputAction lookAction;
        InputAction attackAction;
        InputAction sprintAction;
        InputAction crouchAction;
        InputAction jumpAction;
        InputAction nextAction;       // weapon swap (next)
        InputAction previousAction;   // weapon swap (previous)

        // Fallback edge-tracking for actions missing from the asset.
        bool prevReloadKey;
        bool prevAds;
        bool prevAbility1Key;
        bool prevAbility2Key;
        bool prevSwapKey;

        void Awake()
        {
            player = GetComponent<PlayerController>();
            ResolveAsset();
            ResolveActions();
        }

        void OnEnable()
        {
            if (playerMap == null) return;

            if (moveAction != null) { moveAction.performed += OnMove; moveAction.canceled += OnMove; }
            if (lookAction != null) { lookAction.performed += OnLook; lookAction.canceled += OnLook; }
            if (attackAction != null) { attackAction.started += OnAttackStarted; attackAction.canceled += OnAttackCanceled; }
            if (sprintAction != null) { sprintAction.started += OnSprintStarted; sprintAction.canceled += OnSprintCanceled; }
            if (crouchAction != null) { crouchAction.started += OnCrouchStarted; crouchAction.canceled += OnCrouchCanceled; }
            if (jumpAction != null) jumpAction.performed += OnJump;
            if (nextAction != null) nextAction.performed += OnSwap;
            if (previousAction != null) previousAction.performed += OnSwap;

            playerMap.Enable();
            ApplyCursor(lockCursor);
        }

        void OnDisable()
        {
            if (moveAction != null) { moveAction.performed -= OnMove; moveAction.canceled -= OnMove; }
            if (lookAction != null) { lookAction.performed -= OnLook; lookAction.canceled -= OnLook; }
            if (attackAction != null) { attackAction.started -= OnAttackStarted; attackAction.canceled -= OnAttackCanceled; }
            if (sprintAction != null) { sprintAction.started -= OnSprintStarted; sprintAction.canceled -= OnSprintCanceled; }
            if (crouchAction != null) { crouchAction.started -= OnCrouchStarted; crouchAction.canceled -= OnCrouchCanceled; }
            if (jumpAction != null) jumpAction.performed -= OnJump;
            if (nextAction != null) nextAction.performed -= OnSwap;
            if (previousAction != null) previousAction.performed -= OnSwap;

            if (playerMap != null) playerMap.Disable();
            ApplyCursor(false);
        }

        void OnDestroy()
        {
            // The map may belong to a cloned asset instance we own; clean it up.
            if (assetInstance != null && assetInstance != actionAssetOverride)
            {
                Destroy(assetInstance);
                assetInstance = null;
            }
        }

        // ----- Setup -----
        void ResolveAsset()
        {
            InputActionAsset source = actionAssetOverride;
            if (source == null)
            {
                // No generated wrapper in this project; locate the asset by name. Try Resources first,
                // then any asset already loaded in memory (e.g. assigned via PlayerInput elsewhere).
                source = Resources.Load<InputActionAsset>("InputSystem_Actions");
                if (source == null)
                {
                    var all = Resources.FindObjectsOfTypeAll<InputActionAsset>();
                    foreach (var a in all)
                    {
                        if (a != null && a.name == "InputSystem_Actions") { source = a; break; }
                    }
                }
            }

            // Clone so per-player enable/disable + rebinds never mutate the shared asset.
            assetInstance = source != null ? Instantiate(source) : null;
            if (assetInstance != null) assetInstance.name = "InputSystem_Actions";
        }

        void ResolveActions()
        {
            if (assetInstance == null)
            {
                Debug.LogWarning("[PlayerInputBinder] No InputSystem_Actions asset found; using fallback device polling only.", this);
                return;
            }

            playerMap = assetInstance.FindActionMap(actionMapName, throwIfNotFound: false);
            if (playerMap == null)
            {
                Debug.LogWarning($"[PlayerInputBinder] Action map '{actionMapName}' not found; using fallback device polling only.", this);
                return;
            }

            moveAction = playerMap.FindAction("Move", throwIfNotFound: false);
            lookAction = playerMap.FindAction("Look", throwIfNotFound: false);
            attackAction = playerMap.FindAction("Attack", throwIfNotFound: false);
            sprintAction = playerMap.FindAction("Sprint", throwIfNotFound: false);
            crouchAction = playerMap.FindAction("Crouch", throwIfNotFound: false);
            jumpAction = playerMap.FindAction("Jump", throwIfNotFound: false);
            nextAction = playerMap.FindAction("Next", throwIfNotFound: false);
            previousAction = playerMap.FindAction("Previous", throwIfNotFound: false);
        }

        // ----- Action callbacks -----
        void OnMove(InputAction.CallbackContext ctx) => player.InputMove(ctx.ReadValue<Vector2>());
        void OnLook(InputAction.CallbackContext ctx) => player.InputLook(ctx.ReadValue<Vector2>());
        void OnAttackStarted(InputAction.CallbackContext ctx) => player.InputFire(true);
        void OnAttackCanceled(InputAction.CallbackContext ctx) => player.InputFire(false);
        void OnSprintStarted(InputAction.CallbackContext ctx) => player.InputSprint(true);
        void OnSprintCanceled(InputAction.CallbackContext ctx) => player.InputSprint(false);
        void OnCrouchStarted(InputAction.CallbackContext ctx) => player.InputCrouch(true);
        void OnCrouchCanceled(InputAction.CallbackContext ctx) => player.InputCrouch(false);
        void OnJump(InputAction.CallbackContext ctx) => player.InputJump();
        void OnSwap(InputAction.CallbackContext ctx) => player.InputSwapWeapon();

        // ----- Direct-key fallback for actions missing from the asset -----
        void Update()
        {
            if (player == null) return;

            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;

            // Reload (R) — edge-triggered.
            if (kb != null)
            {
                bool reload = kb.rKey.isPressed;
                if (reload && !prevReloadKey) player.InputReload();
                prevReloadKey = reload;

                // Ability 1 (Q) and Ability 2 (E) — edge-triggered.
                bool ab1 = kb.qKey.isPressed;
                if (ab1 && !prevAbility1Key) player.InputAbility((int)AbilitySlot.Active1);
                prevAbility1Key = ab1;

                bool ab2 = kb.eKey.isPressed;
                if (ab2 && !prevAbility2Key) player.InputAbility((int)AbilitySlot.Active2);
                prevAbility2Key = ab2;

                // SwapWeapon fallback (X) if the Next/Previous actions are unbound.
                if (nextAction == null && previousAction == null)
                {
                    bool swap = kb.xKey.isPressed;
                    if (swap && !prevSwapKey) player.InputSwapWeapon();
                    prevSwapKey = swap;
                }
            }

            // ADS (Right Mouse) — held bool, only push on change.
            if (mouse != null)
            {
                bool ads = mouse.rightButton.isPressed;
                if (ads != prevAds) player.InputAds(ads);
                prevAds = ads;

                // Mouse-wheel weapon swap fallback when actions are unbound.
                if (nextAction == null && previousAction == null)
                {
                    float scroll = mouse.scroll.ReadValue().y;
                    if (Mathf.Abs(scroll) > 0.01f) player.InputSwapWeapon();
                }
            }
        }

        static void ApplyCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
