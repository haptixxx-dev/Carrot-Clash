using UnityEngine;
#if NETCODE_PRESENT
using Unity.Netcode;

namespace CarrotClash.Net
{
    /// <summary>
    /// Network mirror for the spine <see cref="CaptureZone"/>.
    ///
    /// SERVER-AUTHORITATIVE: the host runs the real CaptureZone simulation (occupant counting, progress
    /// advance, scoring). This component samples the spine each server tick and replicates owner team +
    /// capture progress through NetworkVariables. Clients raise the same local GameEvents so HUD zone
    /// rings / banners update identically to a host. Clients never run the capture simulation.
    /// </summary>
    [RequireComponent(typeof(CaptureZone))]
    [DisallowMultipleComponent]
    public class CaptureZoneNetwork : NetworkBehaviour
    {
        [Header("Spine reference")]
        [SerializeField] CaptureZone zone;

        /// <summary>Authoritative owning team (cast of <see cref="Team"/>). Server writes; everyone reads.</summary>
        readonly NetworkVariable<int> netOwningTeam = new NetworkVariable<int>(
            (int)Team.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        /// <summary>Authoritative capture progress 0..1. Server writes; everyone reads.</summary>
        readonly NetworkVariable<float> netCaptureProgress = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        [Tooltip("Minimum progress delta before the server pushes an update (reduces bandwidth).")]
        [SerializeField] float progressEpsilon = 0.01f;

        float lastPushedProgress = -1f;

        void Awake()
        {
            if (zone == null) zone = GetComponent<CaptureZone>();
        }

        public override void OnNetworkSpawn()
        {
            netOwningTeam.OnValueChanged += OnOwnerChanged;
            netCaptureProgress.OnValueChanged += OnProgressChanged;

            if (IsServer && zone != null)
            {
                netOwningTeam.Value = (int)zone.OwningTeam;
                netCaptureProgress.Value = zone.Progress;
                lastPushedProgress = zone.Progress;
            }
        }

        public override void OnNetworkDespawn()
        {
            netOwningTeam.OnValueChanged -= OnOwnerChanged;
            netCaptureProgress.OnValueChanged -= OnProgressChanged;
        }

        public override void OnDestroy()
        {
            netOwningTeam.OnValueChanged -= OnOwnerChanged;
            netCaptureProgress.OnValueChanged -= OnProgressChanged;
            base.OnDestroy();
        }

        void Update()
        {
            // Only the server samples the authoritative spine simulation and publishes deltas.
            if (!IsServer || zone == null) return;

            int owner = (int)zone.OwningTeam;
            if (netOwningTeam.Value != owner) netOwningTeam.Value = owner;

            float progress = zone.Progress;
            if (Mathf.Abs(progress - lastPushedProgress) >= progressEpsilon || progress >= 1f || progress <= 0f)
            {
                if (!Mathf.Approximately(netCaptureProgress.Value, progress))
                {
                    netCaptureProgress.Value = progress;
                    lastPushedProgress = progress;
                }
            }
        }

        void OnOwnerChanged(int previous, int current)
        {
            // Clients reflect the authoritative ownership into a local capture event so HUD reacts.
            if (IsServer || zone == null) return;
            var evt = new ZoneCaptureEvent(zone.zoneId, (Team)current, (Team)previous, wasContested: false);
            GameEvents.RaiseZoneCaptured(evt);
        }

        void OnProgressChanged(float previous, float current)
        {
            if (IsServer || zone == null) return;
            GameEvents.RaiseZoneProgress(zone.zoneId, current);
        }
    }
}
#endif
