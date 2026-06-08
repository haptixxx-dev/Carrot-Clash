using System.Collections.Generic;
using UnityEngine;

namespace CarrotClash.Audio
{
    /// <summary>
    /// Key → AudioClip lookup asset. Keys are the string identifiers used throughout gameplay code
    /// (e.g. "weapon_The Nub_fire", "spice_slow_apply", "ui_button_confirm"). Multiple clips under a
    /// key are randomly varied to avoid repetition (footsteps, weapon fire).
    /// </summary>
    [CreateAssetMenu(menuName = "Carrot Clash/Audio Library", fileName = "AudioLibrary")]
    public class AudioLibrary : ScriptableObject
    {
        [System.Serializable]
        public struct Entry
        {
            public string key;
            public AudioClip[] variants;
        }

        [SerializeField] Entry[] entries = new Entry[0];

        Dictionary<string, AudioClip[]> map;

        void BuildMap()
        {
            map = new Dictionary<string, AudioClip[]>(entries.Length);
            foreach (var e in entries)
                if (!string.IsNullOrEmpty(e.key) && e.variants != null && e.variants.Length > 0)
                    map[e.key] = e.variants;
        }

        /// <summary>Resolve a key to a clip (random variant). Returns null if missing — callers no-op gracefully.</summary>
        public AudioClip Resolve(string key)
        {
            if (map == null) BuildMap();
            if (map.TryGetValue(key, out var variants) && variants.Length > 0)
                return variants[Random.Range(0, variants.Length)];
            return null;
        }
    }
}
