using UnityEngine;
#if NETCODE_PRESENT
using Unity.Netcode;

namespace CarrotClash.Net
{
    /// <summary>
    /// Network mirror for the spine <see cref="MomentumController"/>.
    ///
    /// SERVER-AUTHORITATIVE: the server owns the momentum tier. Kills are reported via
    /// <see cref="RegisterKillServerRpc"/>; the server runs the real MomentumController logic and the
    /// resulting tier replicates through <see cref="netTier"/>. Clients force their local spine to the
    /// authoritative tier so VFX / speed / cooldown multipliers stay in sync without owning the value.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [DisallowMultipleComponent]
    public class NetworkedMomentumController : NetworkBehaviour
    {
        [Header("Spine references")]
        [SerializeField] PlayerController controller;
        [SerializeField] MomentumController momentum;

        /// <summary>Authoritative momentum tier (0..3). Server writes; everyone reads.</summary>
        readonly NetworkVariable<int> netTier = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        bool subscribed;

        void Awake()
        {
            if (controller == null) controller = GetComponent<PlayerController>();
            if (momentum == null && controller != null) momentum = controller.momentum;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer && momentum != null)
            {
                netTier.Value = momentum.Tier;
                momentum.OnTierChanged += OnServerTierChanged;
                subscribed = true;
            }

            netTier.OnValueChanged += OnNetTierChanged;
        }

        public override void OnNetworkDespawn()
        {
            netTier.OnValueChanged -= OnNetTierChanged;
            if (subscribed && momentum != null)
            {
                momentum.OnTierChanged -= OnServerTierChanged;
                subscribed = false;
            }
        }

        public override void OnDestroy()
        {
            netTier.OnValueChanged -= OnNetTierChanged;
            if (subscribed && momentum != null)
            {
                momentum.OnTierChanged -= OnServerTierChanged;
                subscribed = false;
            }
            base.OnDestroy();
        }

        void OnServerTierChanged(MomentumTier oldTier, MomentumTier newTier)
        {
            if (IsServer) netTier.Value = (int)newTier;
        }

        void OnNetTierChanged(int previous, int current)
        {
            // Clients reconcile their local spine to the authoritative tier. The server already mutated
            // its own MomentumController, so don't double-apply there.
            if (IsServer || momentum == null) return;
            if (momentum.Tier != current) momentum.ForceTier(current);
        }

        /// <summary>
        /// Report a kill by this player to the server. The server runs the authoritative momentum logic
        /// (backstab = +2 tiers for Carrot, decay extension for Jalapeño, etc.) on the real component.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RegisterKillServerRpc(bool backstab, ServerRpcParams rpcParams = default)
        {
            if (!IsServer || momentum == null) return;
            momentum.RegisterKill(backstab);
            netTier.Value = momentum.Tier;
        }
    }
}
#endif
