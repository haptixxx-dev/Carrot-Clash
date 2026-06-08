using System;
using System.IO;
using UnityEngine;

namespace CarrotClash.Progression
{
    /// <summary>
    /// Persists the <see cref="PlayerProfile"/> to a JSON file in <see cref="Application.persistentDataPath"/>,
    /// falling back to PlayerPrefs if the file system is unavailable (e.g. WebGL). Single-player,
    /// main-thread usage; a lock guards against re-entrant Save/Load while still being lightweight.
    /// </summary>
    public static class SaveSystem
    {
        const string FileName = "profile.json";
        const string PrefsKey = "carrotclash.profile";
        static readonly object Gate = new object();

        static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        /// <summary>Write the profile to disk (or PlayerPrefs on failure). Never throws to callers.</summary>
        public static void Save(PlayerProfile profile)
        {
            if (profile == null) return;
            lock (Gate)
            {
                string json = JsonUtility.ToJson(profile, prettyPrint: true);
                try
                {
                    File.WriteAllText(FilePath, json);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SaveSystem] File write failed ({e.Message}); falling back to PlayerPrefs.");
                    PlayerPrefs.SetString(PrefsKey, json);
                    PlayerPrefs.Save();
                }
            }
        }

        /// <summary>
        /// Load the profile from disk (or PlayerPrefs fallback). Returns a fresh default profile if
        /// nothing is stored or the data is corrupt. Never returns null.
        /// </summary>
        public static PlayerProfile Load()
        {
            lock (Gate)
            {
                string json = null;
                try
                {
                    if (File.Exists(FilePath))
                        json = File.ReadAllText(FilePath);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SaveSystem] File read failed ({e.Message}); trying PlayerPrefs.");
                }

                if (string.IsNullOrEmpty(json) && PlayerPrefs.HasKey(PrefsKey))
                    json = PlayerPrefs.GetString(PrefsKey);

                if (string.IsNullOrEmpty(json))
                    return NewProfile();

                try
                {
                    PlayerProfile profile = JsonUtility.FromJson<PlayerProfile>(json);
                    return profile != null ? Sanitize(profile) : NewProfile();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SaveSystem] Profile parse failed ({e.Message}); creating a new profile.");
                    return NewProfile();
                }
            }
        }

        /// <summary>True if a saved profile exists in either backing store.</summary>
        public static bool HasSave()
        {
            lock (Gate)
            {
                try { if (File.Exists(FilePath)) return true; }
                catch { /* ignore — fall through to prefs */ }
                return PlayerPrefs.HasKey(PrefsKey);
            }
        }

        /// <summary>Delete the stored profile from both backing stores (debug / reset support).</summary>
        public static void Delete()
        {
            lock (Gate)
            {
                try { if (File.Exists(FilePath)) File.Delete(FilePath); }
                catch (Exception e) { Debug.LogWarning($"[SaveSystem] Delete failed ({e.Message})."); }
                if (PlayerPrefs.HasKey(PrefsKey)) { PlayerPrefs.DeleteKey(PrefsKey); PlayerPrefs.Save(); }
            }
        }

        static PlayerProfile NewProfile()
        {
            return new PlayerProfile { level = 1, lastDailyResetUtcTicks = 0L };
        }

        /// <summary>Repair any nulls that an older/partial save might leave behind.</summary>
        static PlayerProfile Sanitize(PlayerProfile p)
        {
            if (p.level < 1) p.level = 1;
            if (p.ownedCosmetics == null) p.ownedCosmetics = new System.Collections.Generic.List<string>();
            if (p.dailyChallenges == null) p.dailyChallenges = new System.Collections.Generic.List<DailyChallenge>();
            return p;
        }
    }
}
