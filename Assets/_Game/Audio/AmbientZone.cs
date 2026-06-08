using System.Collections;
using UnityEngine;

namespace CarrotClash.Audio
{
    /// <summary>
    /// A trigger volume that owns one ambient loop (Audio doc — Ambient per zone). When the local
    /// player enters, it starts the loop via <see cref="AudioManager.StartLoop"/>; when they leave,
    /// it stops it. Stops are deferred by a short crossfade grace so that walking across a zone
    /// boundary doesn't hard-cut the bed — re-entering (or entering an overlapping zone) within the
    /// grace cancels the pending stop, giving the ~1.5s smooth-transition feel from the doc.
    ///
    /// The loop is positioned at this zone's centre so spatial ambience attenuates naturally with
    /// distance; set <see cref="spatial"/> false for global, non-positional beds.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class AmbientZone : MonoBehaviour
    {
        [Header("Ambient loop")]
        [Tooltip("AudioLibrary key for this zone's ambient bed, e.g. \"ambient_courtyard\".")]
        [SerializeField] string ambientKey = "ambient_zone";
        [Tooltip("Spatialise the bed (positional) or play it 2D as a global atmosphere.")]
        [SerializeField] bool spatial = true;

        [Header("Transition")]
        [Tooltip("Seconds the loop keeps playing after the local player leaves before it stops.")]
        [SerializeField] float crossfadeSeconds = 1.5f;

        bool localPlayerInside;
        Coroutine pendingStop;

        void OnTriggerEnter(Collider other)
        {
            if (!IsLocalPlayer(other)) return;
            localPlayerInside = true;
            CancelPendingStop();
            StartAmbient();
        }

        void OnTriggerExit(Collider other)
        {
            if (!IsLocalPlayer(other)) return;
            localPlayerInside = false;
            ScheduleStop();
        }

        void OnDisable()
        {
            CancelPendingStop();
            if (localPlayerInside)
            {
                StopAmbient();
                localPlayerInside = false;
            }
        }

        void StartAmbient()
        {
            if (string.IsNullOrEmpty(ambientKey)) return;
            AudioManager.Instance?.StartLoop(ambientKey, transform.position, spatial);
        }

        void StopAmbient()
        {
            if (string.IsNullOrEmpty(ambientKey)) return;
            AudioManager.Instance?.StopLoop(ambientKey);
        }

        void ScheduleStop()
        {
            CancelPendingStop();
            if (isActiveAndEnabled)
                pendingStop = StartCoroutine(StopAfterGrace());
            else
                StopAmbient();
        }

        IEnumerator StopAfterGrace()
        {
            yield return new WaitForSeconds(crossfadeSeconds);
            pendingStop = null;
            if (!localPlayerInside)
                StopAmbient();
        }

        void CancelPendingStop()
        {
            if (pendingStop != null)
            {
                StopCoroutine(pendingStop);
                pendingStop = null;
            }
        }

        static bool IsLocalPlayer(Collider other)
        {
            if (other == null) return false;
            PlayerController pc = other.GetComponentInParent<PlayerController>();
            return pc != null && pc.IsLocal;
        }
    }
}
