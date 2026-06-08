using System.Collections.Generic;
using UnityEngine;

namespace CarrotClash.Audio
{
    /// <summary>
    /// Maps a collider's <c>PhysicsMaterial</c> name (set per-surface in the level) to a
    /// <see cref="SurfaceType"/> footstep category (Audio doc — surface detection table).
    /// Lookup is case-insensitive and substring-tolerant so designers can name materials
    /// "Courtyard_Stone" / "Market Wood Floor" and still resolve cleanly. Resolved category
    /// drives the <c>footstep_{surface}</c> SFX key in <see cref="FootstepController"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Carrot Clash/Footstep Bank", fileName = "FootstepBank")]
    public class FootstepBank : ScriptableObject
    {
        /// <summary>One designer-authored mapping from a material name to a surface category.</summary>
        [System.Serializable]
        public struct Mapping
        {
            [Tooltip("PhysicsMaterial name (or a substring of it) to match, e.g. \"Stone\".")]
            public string materialName;
            public SurfaceType surface;
        }

        [Header("Material name -> Surface category")]
        [SerializeField] Mapping[] mappings = new Mapping[0];

        [Header("Fallback when no mapping matches")]
        [SerializeField] SurfaceType defaultSurface = SurfaceType.Stone;

        // Exact-match cache (lower-cased) for fast common-case lookups.
        Dictionary<string, SurfaceType> exact;

        void BuildMap()
        {
            exact = new Dictionary<string, SurfaceType>(mappings.Length);
            foreach (Mapping m in mappings)
            {
                if (string.IsNullOrEmpty(m.materialName)) continue;
                string key = m.materialName.ToLowerInvariant();
                exact[key] = m.surface;
            }
        }

        /// <summary>
        /// Resolve a material name to a surface category. Tries exact (case-insensitive) match
        /// first, then substring containment, then falls back to <c>defaultSurface</c>.
        /// </summary>
        public SurfaceType Resolve(string materialName)
        {
            if (exact == null) BuildMap();
            if (string.IsNullOrEmpty(materialName)) return defaultSurface;

            string name = materialName.ToLowerInvariant();
            if (exact.TryGetValue(name, out SurfaceType direct)) return direct;

            // Substring containment: material "Courtyard_Stone" contains mapping "stone".
            foreach (Mapping m in mappings)
            {
                if (string.IsNullOrEmpty(m.materialName)) continue;
                if (name.Contains(m.materialName.ToLowerInvariant())) return m.surface;
            }

            return defaultSurface;
        }

        /// <summary>The configured fallback category (used when a surface has no PhysicsMaterial).</summary>
        public SurfaceType DefaultSurface => defaultSurface;
    }
}
