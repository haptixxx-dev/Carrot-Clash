using UnityEngine;
#if NETCODE_PRESENT
using Unity.Netcode;
using Unity.Netcode.Components;

namespace CarrotClash.Net
{
    /// <summary>
    /// Network mirror for the gameplay-spine <see cref="PlayerController"/>. Drives transform sync
    /// (owner-authoritative, client-predicted movement) and flags the local/owning player so the
    /// spine wires up camera + input only on the machine that owns this object.
    ///
    /// Authority model: movement is client-predicted (owner writes its own transform), everything
    /// damage/score/momentum/capture related stays SERVER-AUTHORITATIVE and is handled by the other
    /// mirror components. This component only owns position/rotation presentation.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [DisallowMultipleComponent]
    public class NetworkedPlayerController : NetworkBehaviour
    {
        [Header("Spine reference")]
        [SerializeField] PlayerController controller;

        [Header("Transform sync")]
        [Tooltip("Optional NGO NetworkTransform. If assigned it owns transform sync and the fallback NetworkVariables are unused.")]
        [SerializeField] NetworkTransform networkTransform;

        [Tooltip("Interpolation speed for the NetworkVariable fallback path (no NetworkTransform).")]
        [SerializeField] float interpolationSpeed = 14f;

        // Fallback owner-authoritative transform sync when no NetworkTransform component is present.
        // Owner writes; everyone reads. (Server still validates damage etc. elsewhere.)
        readonly NetworkVariable<Vector3> netPosition = new NetworkVariable<Vector3>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        readonly NetworkVariable<Quaternion> netRotation = new NetworkVariable<Quaternion>(
            Quaternion.identity, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        bool useFallbackSync;

        void Awake()
        {
            if (controller == null) controller = GetComponent<PlayerController>();
            if (networkTransform == null) networkTransform = GetComponent<NetworkTransform>();
        }

        public override void OnNetworkSpawn()
        {
            useFallbackSync = networkTransform == null;

            // IsOwner is true on the machine that controls this player; mirror it onto the spine so
            // camera + local HUD + input binding activate only here. Bots are owned by the host.
            bool local = IsOwner;
            if (controller != null && controller.Data != null && controller.IsLocal != local)
            {
                // The spawner (PlayerSpawner / BotSpawner) already ran Initialize with full identity, but
                // it could not know per-machine ownership before this object networked-spawned. Only when
                // the spawner-set locality actually disagrees with IsOwner do we re-run Initialize to flip
                // it. In every normal flow (owner spawned local:true, remotes/bots local:false) the values
                // already agree, so we skip the redundant call.
                //
                // NOTE: PlayerController.Initialize is not idempotent (it does `health.OnDeath += HandleDeath`
                // and RaisePlayerSpawned each call). Gating on a genuine mismatch keeps it to at most one
                // extra call and avoids double-subscribing HandleDeath / firing a spurious OnPlayerSpawned
                // in the common case. A dedicated spine locality setter would remove the residual case, but
                // that is outside this mirror's contract surface.
                controller.Initialize(
                    controller.Data,
                    controller.momentum != null ? controller.momentum.Config : null,
                    controller.Team,
                    controller.PlayerId,
                    local,
                    controller.IsBot);
            }

            if (useFallbackSync && IsOwner)
            {
                netPosition.Value = transform.position;
                netRotation.Value = transform.rotation;
            }
        }

        void Update()
        {
            if (!useFallbackSync) return;

            if (IsOwner)
            {
                // Client-predicted: owner publishes its locally-simulated pose.
                if (netPosition.Value != transform.position) netPosition.Value = transform.position;
                if (netRotation.Value != transform.rotation) netRotation.Value = transform.rotation;
            }
            else
            {
                // Remote view: interpolate toward the latest replicated pose to smooth jitter.
                float t = 1f - Mathf.Exp(-interpolationSpeed * Time.deltaTime);
                transform.position = Vector3.Lerp(transform.position, netPosition.Value, t);
                transform.rotation = Quaternion.Slerp(transform.rotation, netRotation.Value, t);
            }
        }
    }
}
#endif
