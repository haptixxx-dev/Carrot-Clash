using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// Abstract base for every ability implementation (active, passive, momentum-passive).
    /// Concrete abilities are MonoBehaviours added to the player prefab and wired into
    /// <see cref="AbilityController"/>. Cooldown accounting lives in the controller; the ability
    /// only implements its effect.
    /// </summary>
    public abstract class AbilityBase : MonoBehaviour
    {
        [SerializeField] protected AbilityDataSO data;
        protected PlayerController Owner { get; private set; }

        public AbilityDataSO Data => data;
        public float Cooldown => data != null ? data.cooldown : 0f;

        /// <summary>Bind to its owner. Passives subscribe to events here; called once on spawn.</summary>
        public virtual void Bind(PlayerController owner, AbilityDataSO config)
        {
            Owner = owner;
            if (config != null) data = config;
            OnBind();
        }

        /// <summary>Override for passive setup (event subscriptions). Default no-op.</summary>
        protected virtual void OnBind() { }

        /// <summary>Detach (event unsubscription). Called on despawn/destroy.</summary>
        public virtual void Unbind() { }

        /// <summary>
        /// Activate the ability. Returns true if it actually fired (controller then starts the cooldown).
        /// Returning false (e.g. no valid target/surface) leaves the cooldown untouched.
        /// Active abilities override this; passives leave it returning false.
        /// </summary>
        public virtual bool Activate(PlayerController activator) => false;

        /// <summary>Optional early cancel (e.g. interrupting a channel). Default no-op.</summary>
        public virtual void Cancel() { }

        // ----- Shared helpers for implementations -----

        /// <summary>The owner's aim ray (camera centre) — used for placement and targeting.</summary>
        protected Ray AimRay => Owner != null && Owner.cam != null
            ? Owner.cam.AimRay
            : new Ray(transform.position, transform.forward);

        /// <summary>Layer mask of everything an ability raycast should hit for placement (environment + players).</summary>
        protected static int PlacementMask =>
            LayerMask.GetMask(GameConstants.LayerEnvironment, GameConstants.LayerPlayer);

        /// <summary>Play the configured activation SFX/VFX, if any.</summary>
        protected void PlayActivationFeedback()
        {
            if (data == null) return;
            if (!string.IsNullOrEmpty(data.sfxActivate))
                Audio.AudioManager.Instance?.PlaySfx(data.sfxActivate, transform.position);
            if (data.activationVfx != null)
                SpawnTimed(data.activationVfx, transform.position, transform.rotation, 2f);
        }

        /// <summary>Spawn a prefab and auto-destroy after a lifetime (utility for one-shot VFX).</summary>
        protected static GameObject SpawnTimed(GameObject prefab, Vector3 pos, Quaternion rot, float lifetime)
        {
            if (prefab == null) return null;
            var go = Instantiate(prefab, pos, rot);
            if (lifetime > 0f) Destroy(go, lifetime);
            return go;
        }
    }
}
