using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// Marker placed on a player while they stand inside a Broccoli Spore Cloud. The reveal systems
    /// (Carrot Radar Pulse, minimap blips, outline rendering) treat a player whose
    /// <see cref="IsObscured"/> is true as not-revealable. The cloud volume increments a reference
    /// count on enter and decrements on exit / despawn so overlapping clouds don't prematurely clear
    /// the flag, and the marker auto-removes itself once no cloud is covering the player.
    /// </summary>
    [DisallowMultipleComponent]
    public class VisionObscured : MonoBehaviour
    {
        int sourceCount;

        /// <summary>True while at least one fog source is covering this player — suppress reveals.</summary>
        public bool IsObscured => sourceCount > 0;

        /// <summary>Register a covering fog source. Idempotent per source via paired calls.</summary>
        public void AddSource()
        {
            sourceCount++;
        }

        /// <summary>Unregister a covering fog source. Self-destructs when the last one leaves.</summary>
        public void RemoveSource()
        {
            sourceCount = Mathf.Max(0, sourceCount - 1);
            if (sourceCount == 0)
                Destroy(this);
        }
    }
}
