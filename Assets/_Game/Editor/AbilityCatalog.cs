using System.Collections.Generic;

namespace CarrotClash.EditorTools
{
    /// <summary>
    /// Editor-only mirror of the frozen ability-name → concrete-behaviour mapping defined in
    /// CONTRACTS.md (and <see cref="AbilityFactory"/>). The runtime factory keeps its <c>Map</c>
    /// private, so this catalog lets the authoring tools (validator + inspector hints) report which
    /// behaviour a given <see cref="AbilityDataSO.abilityName"/> resolves to without reaching into
    /// the spine. The names here MUST stay in lock-step with the contract.
    /// </summary>
    public static class AbilityCatalog
    {
        // Canonical ability name -> concrete AbilityBase behaviour class name (CONTRACTS.md / AbilityFactory).
        static readonly Dictionary<string, string> NameToBehaviour = new Dictionary<string, string>
        {
            // Carrot
            { "Sprint Dash", "Ability_SprintDash" },
            { "Radar Pulse", "Ability_RadarPulse" },
            { "Silent Steps", "Passive_SilentSteps" },
            { "Backstab Momentum", "MomentumPassive_Backstab" },
            // Jalapeño
            { "Spice Burst", "Ability_SpiceBurst" },
            { "Heat Trail", "Ability_HeatTrail" },
            { "Burn Streak", "Passive_BurnStreak" },
            { "Extended Streak", "MomentumPassive_ExtendedStreak" },
            // Broccoli
            { "Leaf Shield", "Ability_LeafShield" },
            { "Spore Cloud", "Ability_SporeCloud" },
            { "Regen Aura", "Passive_RegenAura" },
            { "Shared Harvest", "MomentumPassive_SharedHarvest" },
            // Potato
            { "Starch Armor", "Ability_StarchArmor" },
            { "Earthen Slam", "Ability_EarthenSlam" },
            { "Thick Skin", "Passive_ThickSkin" },
            { "Stubborn Root", "MomentumPassive_StubbornRoot" },
        };

        /// <summary>
        /// Concrete behaviour class name the factory would attach for <paramref name="abilityName"/>,
        /// or <c>null</c> if the name is not in the canonical map.
        /// </summary>
        public static string ResolveBehaviour(string abilityName)
        {
            if (string.IsNullOrEmpty(abilityName)) return null;
            return NameToBehaviour.TryGetValue(abilityName, out string behaviour) ? behaviour : null;
        }
    }
}
