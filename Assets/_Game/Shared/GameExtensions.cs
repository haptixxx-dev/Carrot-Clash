using UnityEngine;

namespace CarrotClash
{
    /// <summary>Small shared helpers used across gameplay systems. Allocation-free where it matters.</summary>
    public static class GameExtensions
    {
        /// <summary>Flatten a world point to the XZ plane (drops Y) for distance/dot checks.</summary>
        public static Vector3 Flat(this Vector3 v) => new Vector3(v.x, 0f, v.z);

        /// <summary>Horizontal distance ignoring height — used for capture radius and aura checks.</summary>
        public static float FlatDistance(Vector3 a, Vector3 b) => (a.Flat() - b.Flat()).magnitude;

        public static float FlatSqrDistance(Vector3 a, Vector3 b) => (a.Flat() - b.Flat()).sqrMagnitude;

        /// <summary>True if <paramref name="b"/> is roughly behind <paramref name="forward"/>'s owner — Carrot backstab arc.</summary>
        public static bool IsInRearArc(Vector3 victimForward, Vector3 victimToKiller, float dotThreshold)
        {
            // victimToKiller points from victim to the killer. If the killer is behind the victim,
            // it opposes the victim's forward, giving a negative dot.
            Vector3 vf = victimForward.Flat().normalized;
            Vector3 vk = victimToKiller.Flat().normalized;
            return Vector3.Dot(vf, vk) < dotThreshold;
        }

        /// <summary>The team layer mask helper — convenience over LayerMask name lookups.</summary>
        public static int ToLayerMask(params string[] layerNames) => LayerMask.GetMask(layerNames);

        /// <summary>Remap a value from one range to another, unclamped.</summary>
        public static float Remap(this float value, float inMin, float inMax, float outMin, float outMax)
        {
            if (Mathf.Approximately(inMax, inMin)) return outMin;
            return outMin + (value - inMin) * (outMax - outMin) / (inMax - inMin);
        }

        /// <summary>Team id as int (0/1). None maps to -1.</summary>
        public static int Id(this Team t) => (int)t;

        /// <summary>True if two teams are valid and opposed.</summary>
        public static bool IsEnemyOf(this Team a, Team b)
            => a != Team.None && b != Team.None && a != b;
    }
}
