using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// A damageable region on a player body. Multiple hitboxes (head, body) point back to the same
    /// <see cref="PlayerController"/>; the head hitbox flags <see cref="isHead"/> for the headshot
    /// multiplier. Hitscan raycasts hit these colliders and resolve the owner + headshot flag.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerHitbox : MonoBehaviour
    {
        [SerializeField] PlayerController owner;
        [SerializeField] bool isHead;

        public PlayerController Owner => owner;
        public bool IsHead => isHead;
        public Team Team => owner != null ? owner.Team : Team.None;

        public void Bind(PlayerController player) => owner = player;
    }
}
