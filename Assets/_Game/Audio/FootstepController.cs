using UnityEngine;

namespace CarrotClash.Audio
{
    /// <summary>
    /// Drives footstep SFX for a player (Audio doc — Footsteps). Each step raycasts straight down
    /// from the feet, reads the hit collider's <c>PhysicsMaterial</c> name, maps it to a
    /// <see cref="SurfaceType"/> through a <see cref="FootstepBank"/>, and plays
    /// <c>footstep_{surface}</c> via <see cref="AudioManager"/>.
    ///
    /// Step cadence is derived from <see cref="PlayerMovement.HorizontalSpeed"/>: faster movement →
    /// shorter interval between steps. A travelled-distance threshold is the fallback so steps stay
    /// in lockstep with actual displacement even at variable frame rates.
    ///
    /// Carrot "Silent Steps": when this player is a Carrot moving slower than
    /// <c>SprintSpeed * SilentStepsSpeedFraction</c>, steps are muted (the passive's stealth value).
    /// Crouching attenuates step volume by 80%.
    /// </summary>
    [DisallowMultipleComponent]
    public class FootstepController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] PlayerController player;
        [SerializeField] PlayerMovement movement;
        [SerializeField] FootstepBank bank;

        [Header("Raycast")]
        [Tooltip("Origin offset above the player root for the downward surface probe.")]
        [SerializeField] float rayOriginHeight = 0.4f;
        [Tooltip("How far below the origin to probe for ground.")]
        [SerializeField] float rayLength = 1.5f;
        [SerializeField] LayerMask groundMask = ~0;

        [Header("Cadence")]
        [Tooltip("Seconds between steps at full sprint speed.")]
        [SerializeField] float minStepInterval = 0.3f;
        [Tooltip("Seconds between steps at the walk threshold speed.")]
        [SerializeField] float maxStepInterval = 0.55f;
        [Tooltip("Below this horizontal speed (m/s) the player is considered not stepping.")]
        [SerializeField] float minWalkSpeed = 0.6f;
        [Tooltip("Metres travelled that always forces a step, independent of the timer.")]
        [SerializeField] float stepDistanceThreshold = 2.2f;

        [Header("Volume")]
        [Range(0f, 1f)]
        [SerializeField] float baseVolume = 1f;
        [Tooltip("Crouch multiplier — design: crouch reduces footstep volume by 80%.")]
        [Range(0f, 1f)]
        [SerializeField] float crouchVolumeMultiplier = 0.2f;

        float stepTimer;
        float distanceAccumulator;
        Vector3 lastStepPosition;
        readonly RaycastHit[] hitBuffer = new RaycastHit[4];

        void Awake()
        {
            if (player == null) player = GetComponentInParent<PlayerController>();
            if (movement == null && player != null) movement = player.movement;
            if (movement == null) movement = GetComponentInParent<PlayerMovement>();
            lastStepPosition = transform.position;
        }

        void Update()
        {
            if (movement == null) return;
            if (player != null && player.health != null && player.health.IsDead) return;

            float speed = movement.HorizontalSpeed;

            // Track travelled distance for the displacement-based fallback trigger.
            Vector3 flatNow = transform.position.Flat();
            distanceAccumulator += (flatNow - lastStepPosition.Flat()).magnitude;
            lastStepPosition = transform.position;

            if (!movement.IsGrounded || speed < minWalkSpeed)
            {
                // Reset the timer so the first step after standing still fires promptly.
                stepTimer = 0f;
                distanceAccumulator = 0f;
                return;
            }

            stepTimer -= Time.deltaTime;

            bool timerStep = stepTimer <= 0f;
            bool distanceStep = distanceAccumulator >= stepDistanceThreshold;
            if (timerStep || distanceStep)
            {
                EmitStep(speed);
                stepTimer = StepInterval(speed);
                distanceAccumulator = 0f;
            }
        }

        /// <summary>Map current speed onto the [min,max] cadence range (faster = quicker steps).</summary>
        float StepInterval(float speed)
        {
            float sprintSpeed = movement.SprintSpeed;
            if (sprintSpeed <= minWalkSpeed) return maxStepInterval;
            float t = Mathf.InverseLerp(minWalkSpeed, sprintSpeed, speed);
            return Mathf.Lerp(maxStepInterval, minStepInterval, Mathf.Clamp01(t));
        }

        void EmitStep(float speed)
        {
            AudioManager am = AudioManager.Instance;
            if (am == null) return;

            // Carrot Silent Steps: muted while moving below half sprint speed.
            if (player != null && player.ClassId == ClassId.Carrot)
            {
                float silentThreshold = movement.SprintSpeed * GameConstants.SilentStepsSpeedFraction;
                if (speed < silentThreshold) return;
            }

            SurfaceType surface = ProbeSurface();
            string key = "footstep_" + SurfaceKey(surface);

            float volume = baseVolume;
            if (movement.IsCrouching) volume *= crouchVolumeMultiplier;

            am.PlaySfx(key, transform.position, volume);
        }

        /// <summary>Raycast down, resolve the hit material name to a surface category.</summary>
        SurfaceType ProbeSurface()
        {
            SurfaceType fallback = bank != null ? bank.DefaultSurface : SurfaceType.Stone;

            Vector3 origin = transform.position + Vector3.up * rayOriginHeight;
            int count = Physics.RaycastNonAlloc(origin, Vector3.down, hitBuffer,
                rayLength, groundMask, QueryTriggerInteraction.Ignore);
            if (count <= 0) return fallback;

            // Choose the nearest hit that is not this player's own collider.
            float bestDist = float.MaxValue;
            Collider bestCollider = null;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = hitBuffer[i];
                if (hit.collider == null) continue;
                if (player != null && hit.collider.transform.IsChildOf(player.transform)) continue;
                if (hit.distance < bestDist)
                {
                    bestDist = hit.distance;
                    bestCollider = hit.collider;
                }
            }

            if (bestCollider == null) return fallback;
            if (bank == null) return fallback;

            PhysicsMaterial mat = bestCollider.sharedMaterial;
            return bank.Resolve(mat != null ? mat.name : null);
        }

        static string SurfaceKey(SurfaceType surface)
        {
            switch (surface)
            {
                case SurfaceType.Wood: return "wood";
                case SurfaceType.Metal: return "metal";
                case SurfaceType.Dirt: return "dirt";
                default: return "stone";
            }
        }
    }
}
