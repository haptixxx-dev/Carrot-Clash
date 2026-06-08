using System.Collections.Generic;
using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// Maps an <see cref="AbilityDataSO"/> to the concrete <see cref="AbilityBase"/> behaviour that
    /// implements it, attaching the component to the player GameObject on demand. This lets the
    /// player prefab stay class-agnostic: the kit is driven entirely by the CharacterDataSO.
    ///
    /// Matching is by ability name (canonical, doc-driven). Add new abilities to <see cref="Map"/>.
    /// </summary>
    public static class AbilityFactory
    {
        // Canonical ability name -> component type. Names match AbilityDataSO.abilityName.
        static readonly Dictionary<string, System.Type> Map = new Dictionary<string, System.Type>
        {
            // Carrot
            { "Sprint Dash", typeof(Ability_SprintDash) },
            { "Radar Pulse", typeof(Ability_RadarPulse) },
            { "Silent Steps", typeof(Passive_SilentSteps) },
            { "Backstab Momentum", typeof(MomentumPassive_Backstab) },
            // Jalapeño
            { "Spice Burst", typeof(Ability_SpiceBurst) },
            { "Heat Trail", typeof(Ability_HeatTrail) },
            { "Burn Streak", typeof(Passive_BurnStreak) },
            { "Extended Streak", typeof(MomentumPassive_ExtendedStreak) },
            // Broccoli
            { "Leaf Shield", typeof(Ability_LeafShield) },
            { "Spore Cloud", typeof(Ability_SporeCloud) },
            { "Regen Aura", typeof(Passive_RegenAura) },
            { "Shared Harvest", typeof(MomentumPassive_SharedHarvest) },
            // Potato
            { "Starch Armor", typeof(Ability_StarchArmor) },
            { "Earthen Slam", typeof(Ability_EarthenSlam) },
            { "Thick Skin", typeof(Passive_ThickSkin) },
            { "Stubborn Root", typeof(MomentumPassive_StubbornRoot) },
        };

        /// <summary>Attach (or find) the behaviour for this ability data on the given player object.</summary>
        public static AbilityBase Attach(GameObject playerObject, AbilityDataSO config)
        {
            if (config == null || playerObject == null) return null;

            if (!Map.TryGetValue(config.abilityName, out var type))
            {
                Debug.LogWarning($"[AbilityFactory] No behaviour mapped for ability '{config.abilityName}'.");
                return null;
            }

            // Reuse an existing matching component if present.
            foreach (var existing in playerObject.GetComponentsInChildren(type, true))
                if (existing is AbilityBase ab && ab.Data == config) return ab;

            var comp = (AbilityBase)playerObject.AddComponent(type);
            return comp;
        }
    }
}
