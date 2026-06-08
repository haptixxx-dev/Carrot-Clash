using System;
using UnityEngine;
using UnityEngine.UI;

namespace CarrotClash.UI
{
    /// <summary>
    /// The three zone-ownership icons (A / B / C) shown below the score bar (UI/UX HUD spec).
    /// Each icon is tinted grey (neutral), the local team colour (owned), or the enemy colour
    /// (enemy owned); Zone C shows a lock icon until <see cref="GameEvents.OnZoneCUnlocked"/> fires.
    /// While a zone is being contested its icon flashes. Listens to
    /// <see cref="GameEvents.OnZoneCaptured"/>, <see cref="GameEvents.OnZoneProgressChanged"/> and
    /// <see cref="GameEvents.OnZoneCUnlocked"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class ZoneIndicatorUI : MonoBehaviour
    {
        /// <summary>Per-zone icon references, authored in the prefab.</summary>
        [Serializable]
        public class ZoneIcon
        {
            public ZoneId zone = ZoneId.A;
            public Image iconImage;
            public GameObject lockIcon;   // shown only for a locked Zone C
        }

        [Header("Zone icons (expects A, B, C)")]
        [SerializeField] ZoneIcon[] icons = new ZoneIcon[3];

        [Header("Colours")]
        [SerializeField] Color neutralColor = new Color(0.6f, 0.6f, 0.6f);
        [SerializeField] Color teamAColor = new Color(0.3f, 0.6f, 1f);
        [SerializeField] Color teamBColor = new Color(1f, 0.4f, 0.3f);

        [Header("Contest flash")]
        [SerializeField] float contestFlashHz = 4f;
        [Tooltip("Progress within this margin of either end is treated as settled (not contesting).")]
        [SerializeField] float contestEpsilon = 0.02f;

        Team localTeam = Team.None;
        readonly Team[] owner = { Team.None, Team.None, Team.None };
        readonly float[] progress = { 0f, 0f, 0f };
        readonly bool[] contesting = { false, false, false };
        bool zoneCUnlocked;
        bool subscribed;

        /// <summary>Set the local player's team so zones can be coloured as friendly vs hostile.</summary>
        public void SetLocalTeam(Team team)
        {
            localTeam = team;
            RefreshAll();
        }

        /// <summary>Subscribe to the global zone events.</summary>
        public void Bind()
        {
            if (subscribed) return;
            GameEvents.OnZoneCaptured += HandleZoneCaptured;
            GameEvents.OnZoneProgressChanged += HandleZoneProgress;
            GameEvents.OnZoneCUnlocked += HandleZoneCUnlocked;
            subscribed = true;

            zoneCUnlocked = GameModeManager.Instance != null &&
                            GameConstants.IsZoneCUnlocked(GameModeManager.Instance.ElapsedTime);
            RefreshAll();
        }

        void OnDisable() => Unbind();
        void OnDestroy() => Unbind();

        void Unbind()
        {
            if (!subscribed) return;
            GameEvents.OnZoneCaptured -= HandleZoneCaptured;
            GameEvents.OnZoneProgressChanged -= HandleZoneProgress;
            GameEvents.OnZoneCUnlocked -= HandleZoneCUnlocked;
            subscribed = false;
        }

        void HandleZoneCaptured(ZoneCaptureEvent e)
        {
            int idx = (int)e.Zone;
            if (idx < 0 || idx >= owner.Length) return;
            owner[idx] = e.NewOwner;
            contesting[idx] = false;
            progress[idx] = 1f;
            RefreshIcon(e.Zone);
        }

        void HandleZoneProgress(ZoneId zone, float p)
        {
            int idx = (int)zone;
            if (idx < 0 || idx >= progress.Length) return;
            progress[idx] = Mathf.Clamp01(p);
            // Mid-progress (not fully captured, not fully reset) means a fight for ownership.
            contesting[idx] = progress[idx] > contestEpsilon && progress[idx] < 1f - contestEpsilon;
            RefreshIcon(zone);
        }

        void HandleZoneCUnlocked()
        {
            zoneCUnlocked = true;
            RefreshIcon(ZoneId.C);
        }

        void RefreshAll()
        {
            RefreshIcon(ZoneId.A);
            RefreshIcon(ZoneId.B);
            RefreshIcon(ZoneId.C);
        }

        ZoneIcon Find(ZoneId zone)
        {
            for (int i = 0; i < icons.Length; i++)
                if (icons[i] != null && icons[i].zone == zone) return icons[i];
            return null;
        }

        void RefreshIcon(ZoneId zone)
        {
            ZoneIcon icon = Find(zone);
            if (icon == null) return;

            bool locked = zone == ZoneId.C && !zoneCUnlocked;
            if (icon.lockIcon != null) icon.lockIcon.SetActive(locked);
            if (icon.iconImage != null)
            {
                icon.iconImage.enabled = !locked;
                icon.iconImage.color = OwnerColor(owner[(int)zone]);
            }
        }

        Color OwnerColor(Team team)
        {
            if (team == Team.None) return neutralColor;
            Color teamColor = team == Team.A ? teamAColor : teamBColor;
            return teamColor;
        }

        void Update()
        {
            if (SettingsService.ReduceMotion) return;
            float flash = 0.4f + 0.6f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * Mathf.PI * contestFlashHz));

            for (int i = 0; i < icons.Length; i++)
            {
                ZoneIcon icon = icons[i];
                if (icon == null || icon.iconImage == null) continue;
                int idx = (int)icon.zone;
                bool locked = icon.zone == ZoneId.C && !zoneCUnlocked;
                if (locked) continue;

                Color baseColor = OwnerColor(owner[idx]);
                if (contesting[idx])
                {
                    Color c = baseColor;
                    c.a = flash;
                    icon.iconImage.color = c;
                }
                else
                {
                    icon.iconImage.color = baseColor;
                }
            }
        }
    }
}
