using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// A small standalone destructible obstacle (Broccoli's Leaf Shield). It is intentionally
    /// independent of <see cref="HealthController"/> — that component is player-coupled (owner,
    /// momentum transfer, assist bookkeeping) — so cover gets its own minimal HP pool that takes
    /// integer damage and shatters (self-destructs) when depleted. Weapons / explosions can locate
    /// it via <see cref="GetComponentInParent{T}"/> on the collider they hit, exactly like a player.
    /// </summary>
    [DisallowMultipleComponent]
    public class DestructibleCover : MonoBehaviour
    {
        [Header("Runtime (read-only)")]
        [SerializeField] int maxHP = 80;
        [SerializeField] int currentHP = 80;

        [Header("Shatter")]
        [Tooltip("Optional shatter VFX spawned on destruction; auto-destroyed after its lifetime.")]
        [SerializeField] GameObject shatterVfx;
        [SerializeField] float shatterVfxLifetime = 2f;
        [Tooltip("SFX key resolved by AudioManager when the cover shatters.")]
        [SerializeField] string shatterSfxKey = "impact_surface";

        bool shattered;

        /// <summary>Current cover health.</summary>
        public int CurrentHP => currentHP;

        /// <summary>Maximum cover health.</summary>
        public int MaxHP => maxHP;

        /// <summary>Fraction of health remaining (0..1).</summary>
        public float HealthFraction => maxHP > 0 ? (float)currentHP / maxHP : 0f;

        /// <summary>True once the cover has shattered (pending destruction).</summary>
        public bool IsShattered => shattered;

        /// <summary>Configure the cover's HP pool (defaults to 80 if not called).</summary>
        public void Initialize(int hp)
        {
            maxHP = Mathf.Max(1, hp);
            currentHP = maxHP;
            shattered = false;
        }

        /// <summary>Apply integer damage. Shatters (and destroys the GameObject) when HP reaches 0.</summary>
        public void TakeDamage(int amount)
        {
            if (shattered || amount <= 0) return;

            currentHP = Mathf.Max(0, currentHP - amount);

            if (currentHP <= 0)
                Shatter();
        }

        void Shatter()
        {
            if (shattered) return;
            shattered = true;

            if (shatterVfx != null)
            {
                GameObject vfx = Instantiate(shatterVfx, transform.position, transform.rotation);
                if (shatterVfxLifetime > 0f) Destroy(vfx, shatterVfxLifetime);
            }

            if (!string.IsNullOrEmpty(shatterSfxKey))
                Audio.AudioManager.Instance?.PlaySfx(shatterSfxKey, transform.position);

            Destroy(gameObject);
        }
    }
}
