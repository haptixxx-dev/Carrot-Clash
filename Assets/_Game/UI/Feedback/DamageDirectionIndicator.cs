using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CarrotClash.UI
{
    /// <summary>
    /// Apex/COD-style directional damage indicator (Game Feel "Receiving Damage"). Listens to the
    /// local player's <see cref="HealthController.OnDamaged"/> and shows a short arc on the HUD
    /// pointing toward the damage source, then fades it out.
    ///
    /// Direction is derived from <see cref="DamageInfo.HitDirection"/> (world, from instigator toward
    /// victim) projected into the player's local frame, so the arc rotates around the crosshair to the
    /// angle the damage came from. A small pool of arc images lets several near-simultaneous hits show
    /// at once without per-hit allocation.
    /// </summary>
    [DisallowMultipleComponent]
    public class DamageDirectionIndicator : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Prefab/template arc Image, pivoted at the screen centre so rotating its RectTransform sweeps it around the crosshair.")]
        [SerializeField] RectTransform arcTemplate;
        [Tooltip("Parent that the arcs rotate around — typically the HUD centre.")]
        [SerializeField] RectTransform center;

        [Header("Style")]
        [SerializeField] Color arcColor = new Color(1f, 0.2f, 0.2f, 1f);
        [SerializeField] float fadeTime = 1.2f;
        [SerializeField] int poolSize = 6;

        PlayerController localPlayer;

        readonly List<Arc> pool = new List<Arc>();

        class Arc
        {
            public RectTransform rect;
            public Image image;
            public float age;      // seconds since spawned
            public bool active;
        }

        void Awake()
        {
            if (center == null) center = transform as RectTransform;
            BuildPool();
        }

        void BuildPool()
        {
            if (arcTemplate == null || center == null) return;
            arcTemplate.gameObject.SetActive(false);
            for (int i = 0; i < poolSize; i++)
            {
                RectTransform rt = Instantiate(arcTemplate, center);
                rt.gameObject.SetActive(false);
                var arc = new Arc
                {
                    rect = rt,
                    image = rt.GetComponent<Image>(),
                    active = false
                };
                pool.Add(arc);
            }
        }

        void OnEnable()
        {
            GameEvents.OnPlayerSpawned += HandlePlayerSpawned;
            if (localPlayer == null && GameModeManager.Instance != null)
                TryBindFromExistingPlayers();
        }

        void OnDisable()
        {
            GameEvents.OnPlayerSpawned -= HandlePlayerSpawned;
            Unbind();
        }

        void TryBindFromExistingPlayers()
        {
            var players = GameModeManager.Instance.Players;
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i] != null && players[i].IsLocal)
                {
                    Bind(players[i]);
                    return;
                }
            }
        }

        void HandlePlayerSpawned(PlayerController pc)
        {
            if (pc == null || !pc.IsLocal || pc == localPlayer) return;
            Bind(pc);
        }

        void Bind(PlayerController pc)
        {
            Unbind();
            localPlayer = pc;
            if (localPlayer.health != null)
                localPlayer.health.OnDamaged += HandleDamaged;
        }

        void Unbind()
        {
            if (localPlayer != null && localPlayer.health != null)
                localPlayer.health.OnDamaged -= HandleDamaged;
            localPlayer = null;
        }

        void HandleDamaged(DamageInfo info, DamageResult result)
        {
            if (localPlayer == null) return;

            // HitDirection points from instigator toward the victim, so the source is the opposite way.
            Vector3 toSource = -info.HitDirection;
            // Environmental / non-directional damage carries no usable direction — skip the arc.
            if (toSource.Flat().sqrMagnitude < 0.0001f) return;

            float angle = SignedYawAngle(localPlayer.transform.forward, toSource);
            SpawnArc(angle);
        }

        /// <summary>
        /// Yaw angle (degrees) of <paramref name="worldDir"/> relative to <paramref name="forward"/>,
        /// flattened to the ground plane. 0 = straight ahead, positive = source is to the player's right.
        /// </summary>
        static float SignedYawAngle(Vector3 forward, Vector3 worldDir)
        {
            Vector3 f = forward.Flat();
            Vector3 d = worldDir.Flat();
            if (f.sqrMagnitude < 0.0001f || d.sqrMagnitude < 0.0001f) return 0f;
            return Vector3.SignedAngle(f, d, Vector3.up);
        }

        void SpawnArc(float angle)
        {
            Arc arc = GetFreeArc();
            if (arc == null) return;

            arc.active = true;
            arc.age = 0f;
            arc.rect.gameObject.SetActive(true);
            // UI z-rotation is counter-clockwise; a source on the player's right (positive yaw) should
            // place the arc on the right, hence the negation.
            arc.rect.localRotation = Quaternion.Euler(0f, 0f, -angle);
            if (arc.image != null)
            {
                Color c = arcColor;
                c.a = 1f;
                arc.image.color = c;
            }
        }

        Arc GetFreeArc()
        {
            for (int i = 0; i < pool.Count; i++)
                if (!pool[i].active) return pool[i];
            // All busy: reuse the oldest so the newest hit always shows.
            Arc oldest = null;
            for (int i = 0; i < pool.Count; i++)
                if (oldest == null || pool[i].age > oldest.age) oldest = pool[i];
            return oldest;
        }

        void Update()
        {
            for (int i = 0; i < pool.Count; i++)
            {
                Arc arc = pool[i];
                if (!arc.active) continue;

                arc.age += Time.unscaledDeltaTime;
                float k = fadeTime > 0f ? 1f - (arc.age / fadeTime) : 0f;
                if (k <= 0f)
                {
                    arc.active = false;
                    arc.rect.gameObject.SetActive(false);
                    continue;
                }
                if (arc.image != null)
                {
                    Color c = arc.image.color;
                    c.a = k;
                    arc.image.color = c;
                }
            }
        }
    }
}
