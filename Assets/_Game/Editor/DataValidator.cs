using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CarrotClash.EditorTools
{
    /// <summary>
    /// Sanity-checks every <see cref="CharacterDataSO"/> in the project against the design docs:
    /// each must have both weapons and all four ability slots assigned, an HP/move-speed inside the
    /// documented range, the matching ability kinds, and abilities whose names resolve in
    /// <see cref="AbilityFactory"/>. Logs a warning per problem and a single pass/fail summary.
    /// </summary>
    public static class DataValidator
    {
        /// <summary>Documented per-class stat envelope (docs/characters.md).</summary>
        struct StatRange
        {
            public int hp;
            public float speed;
            public StatRange(int hp, float speed)
            {
                this.hp = hp;
                this.speed = speed;
            }
        }

        static readonly Dictionary<ClassId, StatRange> Expected = new Dictionary<ClassId, StatRange>
        {
            { ClassId.Carrot,   new StatRange(90, 7.5f) },
            { ClassId.Jalapeno, new StatRange(110, 6.5f) },
            { ClassId.Broccoli, new StatRange(100, 6.0f) },
            { ClassId.Potato,   new StatRange(140, 5.0f) },
        };

        [MenuItem("Carrot Clash/Validate Data")]
        public static void Validate()
        {
            string[] guids = AssetDatabase.FindAssets("t:CharacterDataSO");
            if (guids.Length == 0)
            {
                Debug.LogWarning("[DataValidator] No CharacterDataSO assets found. " +
                                 "Run 'Carrot Clash/Generate Default Data Assets' first.");
                return;
            }

            int warnings = 0;
            HashSet<ClassId> seenClasses = new HashSet<ClassId>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CharacterDataSO c = AssetDatabase.LoadAssetAtPath<CharacterDataSO>(path);
                if (c == null) continue;

                string label = string.IsNullOrEmpty(c.characterName) ? path : c.characterName;
                seenClasses.Add(c.classId);

                // ----- Loadout completeness -----
                if (c.primaryWeapon == null)
                    warnings += Warn(label, "missing primaryWeapon.");
                if (c.secondaryWeapon == null)
                    warnings += Warn(label, "missing secondaryWeapon (shared pistol 'The Pip').");

                warnings += CheckAbility(label, "active1", c.active1, AbilityDataSO.AbilityKind.Active);
                warnings += CheckAbility(label, "active2", c.active2, AbilityDataSO.AbilityKind.Active);
                warnings += CheckAbility(label, "passive", c.passive, AbilityDataSO.AbilityKind.Passive);
                warnings += CheckAbility(label, "momentumPassive", c.momentumPassive, AbilityDataSO.AbilityKind.MomentumPassive);

                // ----- Stat ranges (docs/characters.md) -----
                if (Expected.TryGetValue(c.classId, out StatRange range))
                {
                    if (c.baseHP != range.hp)
                        warnings += Warn(label, $"baseHP is {c.baseHP}, doc expects {range.hp}.");
                    if (!Mathf.Approximately(c.baseMoveSpeed, range.speed))
                        warnings += Warn(label, $"baseMoveSpeed is {c.baseMoveSpeed}, doc expects {range.speed}.");
                }
                else
                {
                    warnings += Warn(label, $"classId '{c.classId}' has no documented stat range.");
                }

                // ----- General sanity -----
                if (c.sprintMultiplier <= 1f)
                    warnings += Warn(label, $"sprintMultiplier {c.sprintMultiplier} should be > 1.");
                if (c.jumpHeight <= 0f)
                    warnings += Warn(label, $"jumpHeight {c.jumpHeight} should be > 0.");
                if (c.difficulty < 1 || c.difficulty > 5)
                    warnings += Warn(label, $"difficulty {c.difficulty} out of 1..5 range.");
            }

            // ----- Coverage: all four documented classes present and unique -----
            foreach (KeyValuePair<ClassId, StatRange> kvp in Expected)
            {
                if (!seenClasses.Contains(kvp.Key))
                    warnings += Warn("Project", $"no CharacterDataSO found for class '{kvp.Key}'.");
            }

            if (warnings == 0)
                Debug.Log($"[DataValidator] PASS — {guids.Length} character asset(s) validated cleanly.");
            else
                Debug.LogWarning($"[DataValidator] FAIL — {warnings} issue(s) found across {guids.Length} character asset(s).");
        }

        /// <summary>
        /// Validates one ability slot: assigned, correct kind, and a name the factory can resolve.
        /// Returns the number of warnings emitted (0 or 1).
        /// </summary>
        static int CheckAbility(string owner, string slot, AbilityDataSO ability, AbilityDataSO.AbilityKind expectedKind)
        {
            if (ability == null)
                return Warn(owner, $"slot '{slot}' has no ability assigned.");

            if (ability.kind != expectedKind)
                return Warn(owner, $"slot '{slot}' ability '{ability.abilityName}' is kind {ability.kind}, expected {expectedKind}.");

            if (AbilityCatalog.ResolveBehaviour(ability.abilityName) == null)
                return Warn(owner, $"slot '{slot}' ability name '{ability.abilityName}' does not resolve to any AbilityFactory behaviour.");

            return 0;
        }

        static int Warn(string owner, string message)
        {
            Debug.LogWarning($"[DataValidator] {owner}: {message}");
            return 1;
        }
    }
}
