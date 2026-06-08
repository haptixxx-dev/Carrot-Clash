using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// Static configuration for one hero class (Characters doc). Bundles stats, weapon, abilities,
    /// and colour language. The player prefab reads this on spawn to configure all subsystems.
    /// </summary>
    [CreateAssetMenu(menuName = "Carrot Clash/Character Data", fileName = "CharacterData")]
    public class CharacterDataSO : ScriptableObject
    {
        [Header("Identity")]
        public ClassId classId = ClassId.Carrot;
        public string characterName = "Carrot";
        [TextArea] public string tagline = "Fast, quiet, and always behind you.";
        [Tooltip("Difficulty rating 1-5 for the class-select screen.")]
        [Range(1, 5)] public int difficulty = 3;

        [Header("Stats")]
        public int baseHP = 90;
        public float baseMoveSpeed = 7.5f;     // m/s
        public float sprintMultiplier = 1.4f;  // sprint = base * this
        public float jumpHeight = 1.4f;        // metres

        [Header("Loadout")]
        public WeaponDataSO primaryWeapon;
        public WeaponDataSO secondaryWeapon;   // shared Pistol "The Pip"

        [Header("Abilities")]
        public AbilityDataSO active1;
        public AbilityDataSO active2;
        public AbilityDataSO passive;
        public AbilityDataSO momentumPassive;

        [Header("Presentation")]
        public Color primaryColor = new Color(1f, 0.5f, 0f);  // momentum VFX tint
        public Color accentColor = Color.yellow;
        public GameObject playerModelPrefab;

        /// <summary>Sprint speed derived from base.</summary>
        public float SprintSpeed => baseMoveSpeed * sprintMultiplier;
    }
}
