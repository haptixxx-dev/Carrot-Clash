using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// Static configuration shared by every ability (active, passive, momentum-passive).
    /// The behaviour lives in an <see cref="AbilityBase"/> implementation; this is the tunable data.
    /// Generic numeric fields cover the spread of ability needs without per-ability subclasses of the SO.
    /// </summary>
    [CreateAssetMenu(menuName = "Carrot Clash/Ability Data", fileName = "AbilityData")]
    public class AbilityDataSO : ScriptableObject
    {
        public enum AbilityKind { Active, Passive, MomentumPassive }

        [Header("Identity")]
        public string abilityName = "Sprint Dash";
        [TextArea] public string description;
        public Sprite icon;
        public AbilityKind kind = AbilityKind.Active;

        [Header("Activation")]
        [Tooltip("Base cooldown in seconds (before momentum cooldown reduction). Actives only.")]
        public float cooldown = 6f;
        [Tooltip("How long the ability's effect persists, if it has a duration (trail, cloud, shield, reveal).")]
        public float duration = 0f;

        [Header("Geometry")]
        [Tooltip("Primary range — dash distance, throw range, reveal radius, etc.")]
        public float range = 8f;
        [Tooltip("Effect radius for AOE abilities (Spice Burst, Earthen Slam, Radar Pulse, Spore Cloud).")]
        public float radius = 0f;

        [Header("Magnitudes (interpreted per-ability)")]
        [Tooltip("Generic primary magnitude: dash force, slow %, DPS, knockback m, temp HP, heal, etc.")]
        public float magnitude = 0f;
        [Tooltip("Generic secondary magnitude: e.g. direct impact damage on a slow grenade.")]
        public float magnitudeSecondary = 0f;

        [Header("Prefabs / VFX")]
        [Tooltip("Spawned effect prefab (trail segment, shield object, cloud volume, slam decal).")]
        public GameObject effectPrefab;
        public GameObject activationVfx;

        [Header("Audio (keys resolved by AudioManager / AudioLibrary)")]
        public string sfxActivate;
        public string sfxImpact;
        public string sfxLoop;
    }
}
