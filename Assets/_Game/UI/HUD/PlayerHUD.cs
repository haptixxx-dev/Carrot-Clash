using UnityEngine;

namespace CarrotClash.UI
{
    /// <summary>
    /// Top-level in-match HUD orchestrator (UI/UX HUD spec). Holds references to every HUD sub-widget
    /// and wires them to the local player on <see cref="BindLocalPlayer"/>: per-player widgets bind to
    /// the player's subsystem events (health, momentum, abilities, weapon), while match-global widgets
    /// (score bar, zone indicators) subscribe to <see cref="GameEvents"/>. The HUD re-binds the
    /// player-driven widgets when the local player respawns so post-death visuals (a shattered momentum
    /// ring, a death-tilted crosshair) reset cleanly.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerHUD : MonoBehaviour, IHudBindable
    {
        [Header("Per-player widgets")]
        [SerializeField] HealthBarUI healthBar;
        [SerializeField] MomentumRingUI momentumRing;
        [SerializeField] AbilitySlotUI abilitySlot1;   // Active1 / Q
        [SerializeField] AbilitySlotUI abilitySlot2;   // Active2 / E
        [SerializeField] AmmoUI ammo;
        [SerializeField] CrosshairUI crosshair;

        [Header("Match-global widgets")]
        [SerializeField] ScoreBarUI scoreBar;
        [SerializeField] ZoneIndicatorUI zoneIndicator;

        [Header("Ability key labels")]
        [SerializeField] string keyLabelSlot1 = "Q";
        [SerializeField] string keyLabelSlot2 = "E";

        PlayerController localPlayer;
        bool globalsBound;

        /// <summary>
        /// Wire the entire HUD to the local player. Binds per-player widgets to that player's
        /// subsystems and (once) subscribes the global widgets. Safe to call again to rebind to a
        /// different local player.
        /// </summary>
        public void BindLocalPlayer(PlayerController player)
        {
            localPlayer = player;
            BindGlobals();
            BindPlayerWidgets();
        }

        void OnEnable()
        {
            // Respawn rebinds the player's subsystems' state; refresh widgets that hold death/shatter state.
            GameEvents.OnPlayerSpawned += HandlePlayerSpawned;
        }

        void OnDisable()
        {
            GameEvents.OnPlayerSpawned -= HandlePlayerSpawned;
            UnbindAll();
        }

        void OnDestroy()
        {
            GameEvents.OnPlayerSpawned -= HandlePlayerSpawned;
            UnbindAll();
        }

        void HandlePlayerSpawned(PlayerController player)
        {
            // Only react to the local player respawning; re-seed the player-driven widgets.
            if (player == null || player != localPlayer) return;
            BindPlayerWidgets();
        }

        void BindGlobals()
        {
            if (globalsBound) return;

            if (scoreBar != null) scoreBar.Bind();
            if (zoneIndicator != null)
            {
                zoneIndicator.Bind();
                zoneIndicator.SetLocalTeam(localPlayer != null ? localPlayer.Team : Team.None);
            }
            globalsBound = true;
        }

        void BindPlayerWidgets()
        {
            if (localPlayer == null) return;

            if (healthBar != null) healthBar.Bind(localPlayer.health);
            if (momentumRing != null) momentumRing.Bind(localPlayer.momentum, localPlayer.health);
            if (ammo != null) ammo.Bind(localPlayer.weapon);
            if (crosshair != null) crosshair.Bind(localPlayer);

            CharacterDataSO data = localPlayer.Data;
            if (abilitySlot1 != null)
                abilitySlot1.Bind(localPlayer.abilities, data != null ? data.active1 : null, (int)AbilitySlot.Active1, keyLabelSlot1);
            if (abilitySlot2 != null)
                abilitySlot2.Bind(localPlayer.abilities, data != null ? data.active2 : null, (int)AbilitySlot.Active2, keyLabelSlot2);

            // Keep zone colouring in sync with the (possibly newly known) local team.
            if (zoneIndicator != null)
                zoneIndicator.SetLocalTeam(localPlayer.Team);
        }

        void UnbindAll()
        {
            // Widgets each own their own unsubscribe in OnDisable/OnDestroy; calling here is idempotent
            // and ensures a clean detach if the HUD is torn down before the widgets.
            if (healthBar != null) healthBar.Unbind();
            if (momentumRing != null) momentumRing.Unbind();
            if (ammo != null) ammo.Unbind();
            if (crosshair != null) crosshair.Unbind();
            if (abilitySlot1 != null) abilitySlot1.Unbind();
            if (abilitySlot2 != null) abilitySlot2.Unbind();
            globalsBound = false;
            localPlayer = null;
        }
    }
}
