using UnityEngine;
#if NETCODE_PRESENT
using Unity.Netcode;

namespace CarrotClash.Net
{
    /// <summary>
    /// Network mirror for the spine <see cref="AbilityController"/>.
    ///
    /// SERVER-AUTHORITATIVE gameplay, CLIENT-SIDE visuals: the owner requests an activation; the
    /// server validates the cooldown against the real AbilityController and (if ready) runs the
    /// authoritative gameplay effect, then broadcasts a visual-only ClientRpc so every client plays
    /// the activation VFX/SFX locally. The server never trusts the client for cooldown state.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [DisallowMultipleComponent]
    public class NetworkedAbilityController : NetworkBehaviour
    {
        [Header("Spine references")]
        [SerializeField] PlayerController controller;
        [SerializeField] AbilityController abilities;

        void Awake()
        {
            if (controller == null) controller = GetComponent<PlayerController>();
            if (abilities == null && controller != null) abilities = controller.abilities;
        }

        /// <summary>
        /// Local owner entry point: request activation of an ability slot. Routes to the server which
        /// is the single source of truth for whether the ability actually fires.
        /// </summary>
        public void RequestActivate(int slot)
        {
            if (slot < 0 || slot > 1) return;
            ActivateAbilityServerRpc(slot);
        }

        /// <summary>
        /// Client → server activation request. The server validates cooldown/readiness via the real
        /// AbilityController and only then runs the authoritative effect and triggers client VFX.
        /// </summary>
        [ServerRpc]
        public void ActivateAbilityServerRpc(int slot, ServerRpcParams rpcParams = default)
        {
            if (!IsServer || abilities == null || slot < 0 || slot > 1) return;

            // Server-side validation: don't fire if on cooldown / dead / staggered. ActivateAbility
            // performs all those checks and returns false if it didn't fire.
            if (!abilities.IsReady(slot)) return;

            bool fired = abilities.ActivateAbility(slot);
            if (!fired) return;

            // Visual-only broadcast — gameplay already resolved authoritatively above.
            PlayAbilityVfxClientRpc(slot);
        }

        /// <summary>
        /// Visual-only: every client plays the activation VFX/SFX for the given slot. No gameplay state
        /// is mutated here; the authoritative effect already ran on the server.
        /// </summary>
        [ClientRpc]
        public void PlayAbilityVfxClientRpc(int slot, ClientRpcParams rpcParams = default)
        {
            // The owning client already saw its prediction via the local AbilityController firing path,
            // but re-playing here keeps remote observers in sync. Resolve the data for SFX/VFX keys.
            if (abilities == null) return;
            AbilityBase ability = abilities.GetActive(slot);
            if (ability == null || ability.Data == null) return;

            AbilityDataSO data = ability.Data;
            if (!string.IsNullOrEmpty(data.sfxActivate))
            {
                CarrotClash.Audio.AudioManager.Instance?.PlaySfx(data.sfxActivate, transform.position);
            }
            if (data.activationVfx != null)
            {
                Transform origin = slot == 0 && controller != null && controller.abilityOrigin1 != null
                    ? controller.abilityOrigin1
                    : (controller != null && controller.abilityOrigin2 != null ? controller.abilityOrigin2 : transform);
                // One-shot VFX: auto-destroy after a fixed lifetime so activation effects don't leak
                // a permanent GameObject on every client (mirrors AbilityBase.SpawnTimed's 2f lifetime).
                GameObject vfx = Instantiate(data.activationVfx, origin.position, origin.rotation);
                Destroy(vfx, 2f);
            }
        }
    }
}
#endif
