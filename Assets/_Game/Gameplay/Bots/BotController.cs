using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// Replaces the human input layer for a bot-controlled <see cref="PlayerController"/>. Holds an
    /// <see cref="IBotBrain"/> chosen from <see cref="BotDifficulty"/> and, each frame, ticks the brain
    /// then feeds its outputs into the player's input API (move / look / fire / abilities). The bot
    /// uses the exact same movement, weapon, ability and health subsystems as a real player; only the
    /// source of intent differs.
    /// </summary>
    [DisallowMultipleComponent]
    public class BotController : MonoBehaviour
    {
        [SerializeField] BotDifficulty difficulty = BotDifficulty.Medium;

        IBotBrain brain;
        PlayerController player;

        // Edge-detected ability triggers so a held "true" only fires once per press.
        bool prevAbility1;
        bool prevAbility2;

        public BotDifficulty Difficulty => difficulty;

        /// <summary>Bind this controller to a player and pick the brain matching the difficulty.</summary>
        public void Configure(PlayerController owner, BotDifficulty botDifficulty)
        {
            player = owner;
            difficulty = botDifficulty;
            brain = CreateBrain(botDifficulty);
            prevAbility1 = false;
            prevAbility2 = false;
        }

        static IBotBrain CreateBrain(BotDifficulty difficulty)
        {
            switch (difficulty)
            {
                case BotDifficulty.Dummy:
                    return new DummyBrain();
                case BotDifficulty.Easy:
                    return new CombatBotBrain(0.40f, 0.6f);
                case BotDifficulty.Medium:
                    return new CombatBotBrain(0.65f, 0.3f);
                case BotDifficulty.Hard:
                    return new CombatBotBrain(0.85f, 0.15f);
                default:
                    return new DummyBrain();
            }
        }

        void Update()
        {
            if (player == null || brain == null) return;
            if (player.health != null && player.health.IsDead)
            {
                // Dead bots produce no input; stop firing so the weapon doesn't auto-fire on respawn.
                player.InputMove(Vector2.zero);
                player.InputLook(Vector2.zero);
                player.InputFire(false);
                prevAbility1 = false;
                prevAbility2 = false;
                return;
            }

            brain.Tick(player);

            player.InputMove(brain.GetMoveInput());

            // Bots have no PlayerCamera (cam is null on bot players, per the contract), so routing the
            // look intent through PlayerController.InputLook is a no-op: the body would never turn and the
            // brain's facing/aim math (which reads transform.forward / eulerAngles.y) would be dead-ended.
            // The body yaw is normally applied by PlayerCamera.ApplyLook; for a bot we apply it here.
            // The brain already produces an AimTurnSpeed-clamped, per-frame yaw delta (degrees) in look.x,
            // so we apply that directly as a world-Y rotation. Pitch (look.y) is irrelevant to a bot since
            // hitscan uses the head/forward and there is no camera to pitch.
            Vector2 look = brain.GetLookInput();
            player.transform.Rotate(0f, look.x, 0f, Space.World);

            player.InputFire(brain.GetFireInput());

            bool a1 = brain.GetAbility1Input();
            if (a1 && !prevAbility1) player.InputAbility((int)AbilitySlot.Active1);
            prevAbility1 = a1;

            bool a2 = brain.GetAbility2Input();
            if (a2 && !prevAbility2) player.InputAbility((int)AbilitySlot.Active2);
            prevAbility2 = a2;
        }
    }
}
