using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CarrotClash.UI
{
    /// <summary>
    /// Top-right kill feed (UI/UX doc "Kill Feed" + Game Feel). Listens to <see cref="GameEvents.OnKill"/>
    /// and shows pooled entries of the form "[killer class icon] -> [victim class icon]":
    /// <list type="bullet">
    /// <item>A "+1 tier" transfer badge on the killer icon when the kill was a backstab or the victim
    /// held momentum (tier &gt; 0).</item>
    /// <item>A gold border flash when the local player is the killer.</item>
    /// <item>5-second lifetime then fade; newest on top, max 4 visible (oldest is recycled).</item>
    /// </list>
    /// Entries are pooled (no per-kill allocation) and laid out by their sibling index so the newest
    /// is always at the top.
    /// </summary>
    [DisallowMultipleComponent]
    public class KillFeedUI : MonoBehaviour
    {
        [Header("Layout")]
        [Tooltip("Vertical container (e.g. a VerticalLayoutGroup) the entries are parented under.")]
        [SerializeField] RectTransform entryParent;
        [Tooltip("Template entry. Disabled on Awake and cloned into the pool.")]
        [SerializeField] KillFeedEntry entryTemplate;
        [SerializeField] int maxVisible = 4;

        [Header("Class icons (index by ClassId: Carrot, Jalapeno, Broccoli, Potato)")]
        [SerializeField] Sprite[] classIcons = new Sprite[4];
        [Tooltip("Fallback icon for environment / unknown killers.")]
        [SerializeField] Sprite neutralIcon;

        [Header("Timing / style")]
        [SerializeField] float entryLifetime = 5f;
        [SerializeField] float fadeTime = 0.5f;
        [SerializeField] float goldBorderFlashTime = 0.5f;
        [SerializeField] Color goldBorderColor = new Color(1f, 0.78f, 0.2f, 1f);

        readonly List<KillFeedEntry> pool = new List<KillFeedEntry>();
        readonly List<KillFeedEntry> live = new List<KillFeedEntry>(); // oldest first

        void Awake()
        {
            if (entryParent == null) entryParent = transform as RectTransform;
            if (entryTemplate != null)
            {
                entryTemplate.gameObject.SetActive(false);
                int count = Mathf.Max(1, maxVisible) + 1; // one spare so the recycle is seamless
                for (int i = 0; i < count; i++)
                {
                    KillFeedEntry e = Instantiate(entryTemplate, entryParent);
                    e.gameObject.SetActive(false);
                    pool.Add(e);
                }
            }
        }

        void OnEnable()
        {
            GameEvents.OnKill += HandleKill;
        }

        void OnDisable()
        {
            GameEvents.OnKill -= HandleKill;
        }

        void HandleKill(KillEvent e)
        {
            KillFeedEntry entry = AcquireEntry();
            if (entry == null) return;

            Sprite killerIcon = IconFor(e.Killer);
            Sprite victimIcon = IconFor(e.Victim);

            bool showTierBadge = e.IsBackstab || (int)e.VictimTier > 0;
            bool localKill = e.Killer != null && e.Killer.IsLocal;

            entry.Show(killerIcon, victimIcon, showTierBadge, localKill, goldBorderColor,
                       entryLifetime, fadeTime, goldBorderFlashTime);

            // Newest goes to the top (sibling index 0 in a top-down layout).
            entry.transform.SetSiblingIndex(0);
            live.Add(entry);
        }

        /// <summary>Get a free entry, recycling the oldest live one if we are at the visible cap.</summary>
        KillFeedEntry AcquireEntry()
        {
            // Reap any entries that have finished fading on their own.
            for (int i = live.Count - 1; i >= 0; i--)
            {
                if (live[i] == null || !live[i].IsShowing)
                    live.RemoveAt(i);
            }

            if (live.Count >= maxVisible && live.Count > 0)
            {
                KillFeedEntry oldest = live[0];
                live.RemoveAt(0);
                oldest.HideImmediate();
                return oldest;
            }

            for (int i = 0; i < pool.Count; i++)
                if (!pool[i].IsShowing && !live.Contains(pool[i]))
                    return pool[i];

            // Pool exhausted (shouldn't happen given the spare): recycle the oldest.
            if (live.Count > 0)
            {
                KillFeedEntry oldest = live[0];
                live.RemoveAt(0);
                oldest.HideImmediate();
                return oldest;
            }
            return null;
        }

        Sprite IconFor(PlayerController pc)
        {
            if (pc == null) return neutralIcon;
            int idx = (int)pc.ClassId;
            if (classIcons != null && idx >= 0 && idx < classIcons.Length && classIcons[idx] != null)
                return classIcons[idx];
            return neutralIcon;
        }
    }
}
