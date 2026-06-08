using System.Collections;
using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// First-person camera: mouse/stick look, FOV transitions (sprint/ADS/dash), camera bob,
    /// landing bob, and screen shake. Local player only — remote players have no PlayerCamera.
    /// Shake honours the "reduce motion" accessibility setting.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerCamera : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Transform cameraRig;     // yaw/pitch pivot (the player capsule rotates yaw, rig pitches)
        [SerializeField] Camera mainCamera;

        [Header("Look")]
        [SerializeField] float lookSensitivity = 0.1f;
        [SerializeField] float pitchClamp = 89f;

        PlayerController owner;
        Vector2 lookInput;
        float yaw;
        float pitch;

        // FOV
        float baseFov = GameConstants.DefaultFov;
        float fovTarget;
        float fovVelocity;
        float fovExtra;            // transient additive (dash surge), decays

        // Bob
        float bobTimer;
        Vector3 bobOffset;

        // Shake
        Vector3 shakeOffset;
        Coroutine shakeRoutine;

        public Camera Camera => mainCamera;
        public Transform CameraRig => cameraRig;
        public bool ShakeEnabled { get; set; } = true;   // bound to accessibility "reduce motion"

        public void Initialize(PlayerController player)
        {
            owner = player;
            baseFov = SettingsService.FieldOfView;
            fovTarget = baseFov;
            if (mainCamera != null) mainCamera.fieldOfView = baseFov;
            yaw = transform.eulerAngles.y;
            ShakeEnabled = !SettingsService.ReduceMotion;
        }

        public void SetLookInput(Vector2 v) => lookInput = v;

        void Update()
        {
            if (owner != null && owner.health != null && owner.health.IsDead) return;
            ApplyLook();
            ApplyFov();
            ApplyBob();
            ComposeRig();
        }

        void ApplyLook()
        {
            float sens = lookSensitivity * SettingsService.MouseSensitivity;
            yaw += lookInput.x * sens;
            pitch -= lookInput.y * sens;
            pitch = Mathf.Clamp(pitch, -pitchClamp, pitchClamp);

            // Yaw on the body (so movement & weapons inherit it), pitch on the rig.
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        void ApplyFov()
        {
            if (mainCamera == null) return;
            float target = fovTarget + fovExtra;
            float current = Mathf.SmoothDamp(mainCamera.fieldOfView, target, ref fovVelocity, GameConstants.SprintFovTime);
            mainCamera.fieldOfView = current;
            fovExtra = Mathf.MoveTowards(fovExtra, 0f, (GameConstants.DashFovSurge / 0.2f) * Time.deltaTime);

            // Resolve target from movement state.
            if (owner != null && owner.movement != null)
            {
                if (owner.weapon != null && owner.weapon.IsAiming) fovTarget = GameConstants.AdsFov;
                else if (owner.movement.IsSprinting) fovTarget = GameConstants.SprintFov;
                else fovTarget = baseFov;
            }
        }

        void ApplyBob()
        {
            if (SettingsService.ReduceMotion || owner == null || owner.movement == null)
            {
                bobOffset = Vector3.MoveTowards(bobOffset, Vector3.zero, Time.deltaTime);
                return;
            }

            float speed = owner.movement.HorizontalSpeed;
            if (speed > 0.2f && owner.movement.IsGrounded && !owner.movement.IsCrouching)
            {
                bool sprinting = owner.movement.IsSprinting;
                float amplitude = sprinting ? 0.05f : 0.03f;
                float frequency = sprinting ? 2.5f : 1.5f;
                bobTimer += Time.deltaTime * frequency * Mathf.PI * 2f;
                bobOffset = new Vector3(
                    Mathf.Cos(bobTimer * 0.5f) * (sprinting ? 0.02f : 0f),
                    Mathf.Sin(bobTimer) * amplitude,
                    0f);
            }
            else
            {
                bobOffset = Vector3.MoveTowards(bobOffset, Vector3.zero, Time.deltaTime * 0.5f);
            }
        }

        void ComposeRig()
        {
            if (cameraRig == null) return;
            cameraRig.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            cameraRig.localPosition = bobOffset + shakeOffset;
        }

        // ----- Feel hooks (called by movement / weapon / abilities) -----
        public void OnJump() { /* small upward head bob handled by bob curve; hook kept for SFX timing */ }

        public void OnLand()
        {
            // single downward landing snap, spring recovery
            StartCoroutine(LandBob());
        }

        IEnumerator LandBob()
        {
            float t = 0f;
            const float dur = 0.15f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = 1f - (t / dur);
                shakeOffset.y = -0.08f * k * k;
                yield return null;
            }
            shakeOffset.y = 0f;
        }

        /// <summary>Transient FOV punch (Sprint Dash).</summary>
        public void FovSurge(float amount) => fovExtra = amount;

        /// <summary>Screen shake. Honours ShakeEnabled (reduce-motion). See Game Feel screen-shake table.</summary>
        public void Shake(float magnitude, float duration, ShakeType type = ShakeType.Random, Vector3 direction = default)
        {
            if (!ShakeEnabled || SettingsService.ReduceMotion || magnitude <= 0f) return;
            if (shakeRoutine != null) StopCoroutine(shakeRoutine);
            shakeRoutine = StartCoroutine(ShakeRoutine(magnitude, duration, type, direction));
        }

        IEnumerator ShakeRoutine(float magnitude, float duration, ShakeType type, Vector3 direction)
        {
            float t = 0f;
            Vector3 dir = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.right;
            while (t < duration)
            {
                t += Time.deltaTime;
                float falloff = 1f - (t / duration);
                float m = magnitude * falloff;
                switch (type)
                {
                    case ShakeType.Directional:
                        shakeOffset = dir * (Mathf.Sin(t * 60f) * m);
                        break;
                    case ShakeType.Downward:
                        shakeOffset = Vector3.down * (Mathf.Abs(Mathf.Sin(t * 50f)) * m);
                        break;
                    case ShakeType.Radial:
                    case ShakeType.Random:
                    default:
                        shakeOffset = (Vector3)(Random.insideUnitCircle * m);
                        break;
                }
                yield return null;
            }
            shakeOffset = Vector3.zero;
            shakeRoutine = null;
        }

        /// <summary>Camera tilt on death (ragdoll-view feel without ragdoll).</summary>
        public void DeathTilt()
        {
            StartCoroutine(DeathTiltRoutine());
        }

        IEnumerator DeathTiltRoutine()
        {
            float t = 0f;
            const float dur = 0.5f;
            float startRoll = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float roll = Mathf.Lerp(startRoll, 90f, t / dur);
                if (cameraRig != null)
                    cameraRig.localRotation = Quaternion.Euler(pitch, 0f, roll);
                yield return null;
            }
        }

        public void ResetForRespawn()
        {
            pitch = 0f;
            shakeOffset = Vector3.zero;
            bobOffset = Vector3.zero;
            if (cameraRig != null) cameraRig.localRotation = Quaternion.identity;
        }

        /// <summary>Current world aim ray from the camera centre — the authoritative shooting ray.</summary>
        public Ray AimRay => mainCamera != null
            ? mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
            : new Ray(transform.position, transform.forward);
    }
}
