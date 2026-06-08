using UnityEngine;
#if NETCODE_PRESENT
using Unity.Netcode;

namespace CarrotClash.Net
{
    /// <summary>
    /// Network mirror for the spine <see cref="HealthController"/>.
    ///
    /// SERVER-AUTHORITATIVE: only the server applies damage to the spine HealthController. Clients
    /// request damage through <see cref="TakeDamageServerRpc"/>; the server resolves it via the real
    /// logic and the resulting HP is replicated through <see cref="netHp"/>. Clients mirror the
    /// authoritative HP onto their local spine so HUD/feedback stays consistent without ever owning HP.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [DisallowMultipleComponent]
    public class NetworkedHealthController : NetworkBehaviour
    {
        [Header("Spine references")]
        [SerializeField] PlayerController controller;
        [SerializeField] HealthController health;

        /// <summary>Authoritative current HP. Written by the server only; read by everyone.</summary>
        readonly NetworkVariable<int> netHp = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        bool subscribed;

        void Awake()
        {
            if (controller == null) controller = GetComponent<PlayerController>();
            if (health == null && controller != null) health = controller.health;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer && health != null)
            {
                netHp.Value = health.CurrentHP;
                // Server publishes authoritative HP whenever the local logic changes it.
                health.OnHealthChanged += OnServerHealthChanged;
                subscribed = true;
            }

            netHp.OnValueChanged += OnNetHpChanged;
        }

        public override void OnNetworkDespawn()
        {
            netHp.OnValueChanged -= OnNetHpChanged;
            if (subscribed && health != null)
            {
                health.OnHealthChanged -= OnServerHealthChanged;
                subscribed = false;
            }
        }

        // Belt-and-braces cleanup in case despawn ordering differs.
        public override void OnDestroy()
        {
            netHp.OnValueChanged -= OnNetHpChanged;
            if (subscribed && health != null)
            {
                health.OnHealthChanged -= OnServerHealthChanged;
                subscribed = false;
            }
            base.OnDestroy();
        }

        void OnServerHealthChanged(int current, int max)
        {
            // Server is the only writer.
            if (IsServer) netHp.Value = current;
        }

        void OnNetHpChanged(int previous, int current)
        {
            // On clients (non-server, or server's own non-authoritative listeners) reconcile the local
            // spine. The server already mutated its own HealthController, so skip there.
            if (IsServer || health == null) return;

            int delta = current - previous;
            if (delta < 0)
            {
                // Apply the replicated loss as Ability damage with no instigator context (purely visual
                // reconciliation; the authoritative resolution already happened on the server).
                health.TakeDamage(DamageInfo.Environmental(-delta, DamageType.Ability));
            }
            else if (delta > 0)
            {
                health.Heal(delta);
            }
        }

        /// <summary>
        /// Client → server damage request. The server applies it through the real HealthController so
        /// all interactions (temp HP, thick skin, death/kill events) run exactly once, authoritatively.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void TakeDamageServerRpc(int amount, int damageType, Vector3 hitPoint, Vector3 hitDirection,
            bool isHeadshot, ulong instigatorId, ServerRpcParams rpcParams = default)
        {
            if (!IsServer || health == null || amount <= 0) return;

            PlayerController instigator = ResolveInstigator(instigatorId);
            var info = new DamageInfo(amount, (DamageType)damageType, instigator, hitPoint, hitDirection, isHeadshot);
            health.TakeDamage(info);
            netHp.Value = health.CurrentHP;
        }

        PlayerController ResolveInstigator(ulong instigatorId)
        {
            if (NetworkManager.Singleton == null) return null;
            var objects = NetworkManager.Singleton.SpawnManager;
            if (objects == null) return null;
            if (objects.SpawnedObjects.TryGetValue(instigatorId, out NetworkObject netObj) && netObj != null)
                return netObj.GetComponent<PlayerController>();
            return null;
        }
    }
}
#endif
