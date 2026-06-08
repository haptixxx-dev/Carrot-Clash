using System;
using System.Collections;
using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// Standalone HP for the Phase-1 test dummy. Deliberately independent of
    /// <see cref="HealthController"/> / the player pipeline (no PlayerController owner) so it can be
    /// dropped on a static prop. Accepts <see cref="DamageInfo"/> via <see cref="TakeDamage"/> and
    /// raises local events the dummy uses to drive its die/reset cycle.
    /// </summary>
    [DisallowMultipleComponent]
    public class DummyHealth : MonoBehaviour
    {
        [SerializeField] int maxHP = 100;

        int currentHP;
        bool isDead;

        public int CurrentHP => currentHP;
        public int MaxHP => maxHP;
        public bool IsDead => isDead;
        public float HealthFraction => maxHP > 0 ? (float)currentHP / maxHP : 0f;

        /// <summary>Fired when HP changes (current, max).</summary>
        public event Action<int, int> OnHealthChanged;
        /// <summary>Fired when the dummy is reduced to 0 HP. Argument is the killing damage info.</summary>
        public event Action<DamageInfo> OnDied;

        void Awake() => ResetHealth();

        /// <summary>Restore to full and clear the dead flag.</summary>
        public void ResetHealth()
        {
            maxHP = Mathf.Max(1, maxHP);
            currentHP = maxHP;
            isDead = false;
            OnHealthChanged?.Invoke(currentHP, maxHP);
        }

        /// <summary>Apply damage. Returns the resolved result for hit-marker feedback.</summary>
        public DamageResult TakeDamage(in DamageInfo info)
        {
            if (isDead || info.Amount <= 0)
                return DamageResult.None;

            int applied = Mathf.Min(currentHP, info.Amount);
            currentHP = Mathf.Max(0, currentHP - info.Amount);
            OnHealthChanged?.Invoke(currentHP, maxHP);

            bool lethal = currentHP <= 0;
            var result = new DamageResult(applied, false, lethal, info.IsHeadshot);

            if (lethal)
            {
                isDead = true;
                OnDied?.Invoke(info);
            }

            return result;
        }
    }

    /// <summary>
    /// Phase-1 testing target (Milestones doc: "static; takes damage; dies; resets after 3s"). Wraps
    /// a <see cref="DummyHealth"/>, hides/disables its colliders + renderers on death, and re-enables
    /// at full HP after a delay so the player can verify weapons, hit-markers, and momentum gain.
    /// Standalone — never goes through the player spawn / respawn pipeline.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DummyHealth))]
    public class TargetDummy : MonoBehaviour
    {
        [Header("Respawn")]
        [SerializeField] float respawnDelay = 3f;

        [Header("Visuals (auto-collected if empty)")]
        [SerializeField] Renderer[] renderers;
        [SerializeField] Collider[] colliders;

        [Header("SFX (optional)")]
        [SerializeField] string deathSfxKey = "impact_surface";

        DummyHealth dummyHealth;
        Coroutine respawnRoutine;

        void Awake()
        {
            dummyHealth = GetComponent<DummyHealth>();
            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
            if (colliders == null || colliders.Length == 0)
                colliders = GetComponentsInChildren<Collider>(includeInactive: true);
        }

        void OnEnable()
        {
            dummyHealth.OnDied += HandleDied;
        }

        void OnDisable()
        {
            dummyHealth.OnDied -= HandleDied;
            if (respawnRoutine != null)
            {
                StopCoroutine(respawnRoutine);
                respawnRoutine = null;
            }
        }

        void HandleDied(DamageInfo killingBlow)
        {
            CarrotClash.Audio.AudioManager.Instance?.PlaySfx(deathSfxKey, transform.position);
            SetVisible(false);
            if (respawnRoutine != null) StopCoroutine(respawnRoutine);
            respawnRoutine = StartCoroutine(RespawnAfterDelay());
        }

        IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(respawnDelay);
            dummyHealth.ResetHealth();
            SetVisible(true);
            respawnRoutine = null;
        }

        void SetVisible(bool visible)
        {
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].enabled = visible;
            for (int i = 0; i < colliders.Length; i++)
                if (colliders[i] != null) colliders[i].enabled = visible;
        }
    }
}
