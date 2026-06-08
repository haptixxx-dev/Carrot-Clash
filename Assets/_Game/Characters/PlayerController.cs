using System.Collections.Generic;
using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// Top-level player orchestrator. Holds subsystem references, configures them from
    /// <see cref="CharacterDataSO"/>, routes input to them, and assembles cross-system events
    /// (notably death → KillEvent with backstab/tier context). One per player; the local player
    /// additionally owns camera + HUD, remote players do not.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerController : MonoBehaviour
    {
        [Header("Subsystems")]
        public PlayerMovement movement;
        public PlayerCamera cam;             // null on remote / bot players
        public WeaponController weapon;
        public AbilityController abilities;
        public HealthController health;
        public MomentumController momentum;

        [Header("Identity")]
        [SerializeField] CharacterDataSO characterData;
        [SerializeField] MomentumConfigSO momentumConfig;

        [Header("Aim origins")]
        public Transform abilityOrigin1;
        public Transform abilityOrigin2;
        public Transform headTransform;       // headshot hitbox reference / corpse anchor

        Team team = Team.None;
        int playerId = -1;
        bool isLocal;
        bool isBot;

        static readonly List<PlayerController> assistBuffer = new List<PlayerController>(8);

        public CharacterDataSO Data => characterData;
        public ClassId ClassId => characterData != null ? characterData.classId : ClassId.Carrot;
        public Team Team => team;
        public int PlayerId => playerId;
        public bool IsLocal => isLocal;
        public bool IsBot => isBot;
        public Color TeamColor => team == Team.A ? new Color(0.3f, 0.6f, 1f) : new Color(1f, 0.4f, 0.3f);

        /// <summary>Configure the whole player from data. Call once after instantiation / network spawn.</summary>
        public void Initialize(CharacterDataSO data, MomentumConfigSO momentumCfg, Team playerTeam, int id, bool local, bool bot = false)
        {
            characterData = data;
            momentumConfig = momentumCfg;
            team = playerTeam;
            playerId = id;
            isLocal = local;
            isBot = bot;

            health.Initialize(this, data.baseHP);
            momentum.Initialize(this, momentumCfg);
            movement.Initialize(this, data);
            weapon.Initialize(this, data);
            abilities.Initialize(this, data);
            if (isLocal && cam != null) cam.Initialize(this);

            health.OnDeath += HandleDeath;

            GameEvents.RaisePlayerSpawned(this);
        }

        void OnDestroy()
        {
            if (health != null) health.OnDeath -= HandleDeath;
        }

        // ----- Input routing (wired by PlayerInputBinder for local player; BotController for bots) -----
        public void InputMove(Vector2 v) => movement.SetMoveInput(v);
        public void InputLook(Vector2 v) { if (cam != null) cam.SetLookInput(v); }
        public void InputSprint(bool held) => movement.SetSprint(held);
        public void InputCrouch(bool held) => movement.SetCrouch(held);
        public void InputJump() => movement.Jump();
        public void InputFire(bool held) => weapon.SetFiring(held);
        public void InputAds(bool held) => weapon.SetADS(held);
        public void InputReload() => weapon.Reload();
        public void InputAbility(int slot) => abilities.ActivateAbility(slot);
        public void InputSwapWeapon() => weapon.SwapWeapon();

        /// <summary>
        /// Death handler. Builds the KillEvent with full context (backstab arc, victim tier),
        /// applies momentum transfer to the killer, credits assists, and raises the global kill event.
        /// </summary>
        void HandleDeath(PlayerController killer)
        {
            MomentumTier victimTier = momentum.TierEnum;

            bool isBackstab = false;
            if (killer != null && killer.ClassId == ClassId.Carrot)
            {
                Vector3 victimToKiller = killer.transform.position - transform.position;
                isBackstab = GameExtensions.IsInRearArc(transform.forward, victimToKiller, GameConstants.CarrotBackstabDot);
            }

            // Momentum transfer: victim relinquishes charge; killer absorbs (Potato override handled inside).
            float transferred = momentum.HandleDeath();
            if (killer != null && killer != this && killer.momentum != null)
            {
                killer.momentum.AbsorbTransfer(transferred);
                killer.momentum.RegisterKill(isBackstab);
            }

            // Assist credit (everyone who damaged the victim in the window except the killer).
            health.CollectAssisters(assistBuffer, killer);
            foreach (var assister in assistBuffer)
            {
                if (assister == null || assister.momentum == null) continue;
                assister.momentum.RegisterAssist();
                GameEvents.RaiseAssist(assister, 1);
            }

            var killEvent = new KillEvent(killer, this, DamageType.Bullet, isBackstab, victimTier, false);
            GameEvents.RaiseKill(killEvent);
            GameEvents.RaisePlayerDied(this);

            // Local presentation: death tilt + audio handled by feedback listeners + camera.
            if (isLocal && cam != null) cam.DeathTilt();
        }

        /// <summary>Respawn at a given pose: revive health, reset momentum tier, reset cooldowns, re-enable input.</summary>
        public void Respawn(Vector3 position, Quaternion rotation)
        {
            health.Revive();
            momentum.ForceTier(0);
            abilities.ResetCooldowns();
            movement.ResetForRespawn(position, rotation);
            weapon.ResetForRespawn();
            if (isLocal && cam != null) cam.ResetForRespawn();
            GameEvents.RaisePlayerSpawned(this);
        }

        /// <summary>Convenience: apply damage to this player from a source (used by weapons/abilities/environment).</summary>
        public DamageResult ApplyDamage(in DamageInfo info) => health.TakeDamage(info);
    }
}
