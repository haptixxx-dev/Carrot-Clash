using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// CharacterController-based FPS movement (Milestones doc: CharacterController, not Rigidbody).
    /// Applies the momentum speed multiplier, slow status, sprint, crouch, jump, gravity, dash, and
    /// knockback impulse. Movement is client-predicted; the networked transform syncs the result.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        CharacterController cc;
        PlayerController owner;

        [Header("Config (from CharacterDataSO)")]
        float baseSpeed = 7.5f;
        float sprintMultiplier = 1.4f;
        float jumpHeight = 1.4f;

        [Header("Runtime input")]
        Vector2 moveInput;
        bool sprintHeld;
        bool crouchHeld;
        bool jumpQueued;

        [Header("State")]
        Vector3 verticalVelocity;     // gravity / jump on Y
        Vector3 externalImpulse;      // knockback, decays over time
        float slowMultiplier = 1f;    // 1 = no slow; 0.65 when Spice-Burst slowed
        float slowExpireTime = -1f;
        bool staggered;
        float staggerExpireTime = -1f;
        float firingSpeedMultiplier = 1f;

        float standHeight;
        float crouchHeight;
        float currentHeight;
        float lastMoveTime;           // for Potato Thick Skin stationary check
        Vector3 lastPosition;

        public bool IsSprinting { get; private set; }
        public bool IsCrouching { get; private set; }
        public bool IsGrounded => cc != null && cc.isGrounded;
        public bool IsStaggered => staggered;
        public float CurrentSpeed { get; private set; }
        /// <summary>Horizontal speed magnitude this frame (m/s) — used by footsteps / Silent Steps.</summary>
        public float HorizontalSpeed { get; private set; }
        public float SprintSpeed => baseSpeed * sprintMultiplier;

        /// <summary>Potato Thick Skin: -8% incoming damage while stationary &gt; 1s.</summary>
        public float IncomingDamageMultiplier
        {
            get
            {
                if (owner != null && owner.ClassId == ClassId.Potato &&
                    Time.time - lastMoveTime > 1f)
                    return 0.92f;
                return 1f;
            }
        }

        public void Initialize(PlayerController player, CharacterDataSO data)
        {
            owner = player;
            cc = GetComponent<CharacterController>();
            baseSpeed = data.baseMoveSpeed;
            sprintMultiplier = data.sprintMultiplier;
            jumpHeight = data.jumpHeight;

            standHeight = cc.height;
            crouchHeight = Mathf.Max(0.8f, standHeight - GameConstants.CrouchHeightDelta);
            currentHeight = standHeight;
            lastPosition = transform.position;
            lastMoveTime = Time.time;
        }

        // ----- Input setters (called by PlayerController from Input System callbacks) -----
        public void SetMoveInput(Vector2 v) => moveInput = v;
        public void SetSprint(bool held) => sprintHeld = held;
        public void SetCrouch(bool held) => crouchHeld = held;
        public void Jump() => jumpQueued = true;
        public void SetFiring(float speedMultiplier) => firingSpeedMultiplier = Mathf.Clamp01(speedMultiplier);

        void Update()
        {
            if (cc == null || !cc.enabled) return;
            if (owner != null && owner.health != null && owner.health.IsDead) return;

            UpdateStatusTimers();
            UpdateCrouch();

            // ----- Horizontal movement -----
            float momentumMult = owner != null && owner.momentum != null ? owner.momentum.SpeedMultiplier : 1f;
            bool wantsSprint = sprintHeld && !IsCrouching && moveInput.y > 0.1f && !staggered;
            IsSprinting = wantsSprint;

            float speed = baseSpeed * (wantsSprint ? sprintMultiplier : 1f);
            speed *= momentumMult;        // momentum applies to base, before slow (doc)
            speed *= slowMultiplier;      // Spice Burst slow
            speed *= firingSpeedMultiplier; // Potato LMG move penalty while firing
            if (IsCrouching) speed *= 0.6f;
            if (staggered) speed = 0f;    // stagger stops directed movement; impulse still applies
            CurrentSpeed = speed;

            Vector3 wishDir = (transform.right * moveInput.x + transform.forward * moveInput.y);
            wishDir = wishDir.Flat();
            if (wishDir.sqrMagnitude > 1f) wishDir.Normalize();
            Vector3 horizontal = wishDir * speed;

            // ----- Gravity / jump -----
            if (cc.isGrounded)
            {
                if (verticalVelocity.y < 0f) verticalVelocity.y = -2f; // stick to ground
                if (jumpQueued && !IsCrouching && !staggered)
                {
                    verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * GameConstants.Gravity);
                    owner?.cam?.OnJump();
                }
            }
            jumpQueued = false;
            verticalVelocity.y = Mathf.Max(GameConstants.TerminalVelocity,
                                           verticalVelocity.y + GameConstants.Gravity * Time.deltaTime);

            // ----- External impulse (knockback) decay -----
            if (externalImpulse.sqrMagnitude > 0.01f)
                externalImpulse = Vector3.Lerp(externalImpulse, Vector3.zero, Time.deltaTime / GameConstants.KnockbackDuration);
            else
                externalImpulse = Vector3.zero;

            Vector3 motion = (horizontal + externalImpulse + verticalVelocity) * Time.deltaTime;
            bool wasGrounded = cc.isGrounded;
            cc.Move(motion);

            if (!wasGrounded && cc.isGrounded)
                owner?.cam?.OnLand();

            // ----- Bookkeeping -----
            HorizontalSpeed = (transform.position.Flat() - lastPosition.Flat()).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
            if (HorizontalSpeed > 0.15f) lastMoveTime = Time.time;
            lastPosition = transform.position;
        }

        void UpdateCrouch()
        {
            bool wantCrouch = crouchHeld;
            IsCrouching = wantCrouch;
            float target = wantCrouch ? crouchHeight : standHeight;
            currentHeight = Mathf.MoveTowards(currentHeight, target,
                                              (GameConstants.CrouchHeightDelta / GameConstants.CrouchTransitionTime) * Time.deltaTime);
            cc.height = currentHeight;
            cc.center = new Vector3(0f, currentHeight * 0.5f, 0f);
        }

        void UpdateStatusTimers()
        {
            if (slowMultiplier < 1f && Time.time >= slowExpireTime) slowMultiplier = 1f;
            if (staggered && Time.time >= staggerExpireTime) staggered = false;
        }

        /// <summary>Sprint Dash: instant directional displacement preserving momentum. Usable mid-air.</summary>
        public void Dash(Vector3 direction, float distance)
        {
            if (cc == null) return;
            Vector3 dir = direction.Flat();
            if (dir.sqrMagnitude < 0.01f) dir = transform.forward.Flat();
            dir.Normalize();
            // Apply as a single Move so it respects collisions (no clipping through walls).
            cc.Move(dir * distance);
        }

        /// <summary>Spice Burst slow. Reapplication resets duration; floor is 65% (no compounding).</summary>
        public void ApplySlow(float amount, float duration)
        {
            slowMultiplier = 1f - Mathf.Clamp01(amount);
            slowExpireTime = Time.time + duration;
        }

        /// <summary>Earthen Slam knockback impulse + stagger. Direction includes upward bounce.</summary>
        public void ApplyKnockback(Vector3 force, float stagger)
        {
            externalImpulse = force;
            if (stagger > 0f)
            {
                staggered = true;
                staggerExpireTime = Time.time + stagger;
            }
        }

        public void ResetForRespawn(Vector3 position, Quaternion rotation)
        {
            bool wasEnabled = cc.enabled;
            cc.enabled = false;                  // disable so we can teleport without collision fighting
            transform.SetPositionAndRotation(position, rotation);
            cc.enabled = wasEnabled;
            verticalVelocity = Vector3.zero;
            externalImpulse = Vector3.zero;
            slowMultiplier = 1f;
            staggered = false;
            lastPosition = position;
        }
    }
}
