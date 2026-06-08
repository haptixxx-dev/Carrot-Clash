using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// AI decision source for a bot. A brain produces the same kind of input a human would
    /// (move/look axes + fire / ability buttons); <see cref="BotController"/> reads those each
    /// frame and routes them through the owning <see cref="PlayerController"/>'s input API, so the
    /// bot drives the exact same movement / weapon / ability / health subsystems as a real player.
    /// </summary>
    public interface IBotBrain
    {
        /// <summary>Desired planar move input, components in [-1, 1] (x = strafe, y = forward).</summary>
        Vector2 GetMoveInput();

        /// <summary>Desired look delta for this frame (yaw, pitch). Bots fed via look "delta" like mouse.</summary>
        Vector2 GetLookInput();

        /// <summary>True while the brain wants to hold fire this frame.</summary>
        bool GetFireInput();

        /// <summary>True on the frame the brain wants to trigger active ability slot 1.</summary>
        bool GetAbility1Input();

        /// <summary>True on the frame the brain wants to trigger active ability slot 2.</summary>
        bool GetAbility2Input();

        /// <summary>Advance the brain's decision logic. Called once per frame before the getters are read.</summary>
        void Tick(PlayerController self);
    }
}
