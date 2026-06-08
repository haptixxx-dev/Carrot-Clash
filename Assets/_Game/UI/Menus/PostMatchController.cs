using System.Collections;
using System.Collections.Generic;
using System.Text;
using CarrotClash.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CarrotClash.UI
{
    /// <summary>
    /// Post-match screen (UI/UX doc "5. Post-Match"). Listens for <see cref="GameEvents.OnMatchEnded"/>,
    /// shows the winner banner, MVP (<see cref="MatchStats.ResolveMvp"/>) and Hot Streak
    /// (<see cref="MatchStats.ResolveHotStreak"/>) awards, a scoreboard from <c>Stats.All</c>, an
    /// animated XP count-up (computed via <see cref="XPManager.ComputeMatchXp"/> and granted through
    /// <see cref="ProgressionService.AwardMatch"/>), and battle-pass progress. Rematch / Play Again
    /// reload gameplay; Menu returns to the main menu — all via <see cref="SceneFlow"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class PostMatchController : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] GameObject root;          // shown when the match ends

        [Header("Winner Banner")]
        [SerializeField] TMP_Text winnerBannerLabel;
        [SerializeField] TMP_Text finalScoreLabel;

        [Header("Awards")]
        [SerializeField] TMP_Text mvpLabel;
        [SerializeField] TMP_Text hotStreakLabel;

        [Header("Scoreboard")]
        [SerializeField] TMP_Text scoreboardLabel;

        [Header("XP Breakdown (animated count-up)")]
        [SerializeField] TMP_Text xpBreakdownLabel;
        [SerializeField] TMP_Text xpTotalLabel;
        [SerializeField] TMP_Text levelLabel;
        [SerializeField] Slider xpBar;
        [Tooltip("Seconds the XP total counts up across (UI/UX doc: ~3s dopamine beat).")]
        [SerializeField] float xpCountUpDuration = 3f;

        [Header("Battle Pass")]
        [SerializeField] TMP_Text battlePassLabel;
        [SerializeField] Slider battlePassBar;

        [Header("Buttons")]
        [SerializeField] Button rematchButton;
        [SerializeField] TMP_Text rematchVoteLabel;
        [SerializeField] Button playAgainButton;
        [SerializeField] Button menuButton;

        readonly StringBuilder builder = new StringBuilder(320);
        readonly List<PlayerStats> rows = new List<PlayerStats>(GameConstants.MaxPlayers);
        Coroutine xpRoutine;
        int rematchVotes;

        void OnEnable()
        {
            GameEvents.OnMatchEnded += HandleMatchEnded;

            if (rematchButton != null) rematchButton.onClick.AddListener(OnRematch);
            if (playAgainButton != null) playAgainButton.onClick.AddListener(OnPlayAgain);
            if (menuButton != null) menuButton.onClick.AddListener(OnMenu);

            if (root != null) root.SetActive(false);
        }

        void OnDisable()
        {
            GameEvents.OnMatchEnded -= HandleMatchEnded;

            if (rematchButton != null) rematchButton.onClick.RemoveListener(OnRematch);
            if (playAgainButton != null) playAgainButton.onClick.RemoveListener(OnPlayAgain);
            if (menuButton != null) menuButton.onClick.RemoveListener(OnMenu);

            if (xpRoutine != null) { StopCoroutine(xpRoutine); xpRoutine = null; }
        }

        void HandleMatchEnded(Team winner)
        {
            if (root != null) root.SetActive(true);
            rematchVotes = 0;
            UpdateRematchVoteLabel();

            ShowBanner(winner);
            ShowAwards();
            ShowScoreboard();
            ShowXpAndBattlePass(winner);
        }

        // ----- Banner -----
        void ShowBanner(Team winner)
        {
            if (winnerBannerLabel != null)
            {
                winnerBannerLabel.text = winner switch
                {
                    Team.A => "TEAM A WINS",
                    Team.B => "TEAM B WINS",
                    _ => "DRAW"
                };
            }

            if (finalScoreLabel != null)
            {
                GameModeManager gm = GameModeManager.Instance;
                int a = gm != null ? gm.GetScore(Team.A) : 0;
                int b = gm != null ? gm.GetScore(Team.B) : 0;
                finalScoreLabel.text = a + "  vs  " + b;
            }

            CarrotClash.Audio.AudioManager.Instance?.PlayUi(winner == Team.None ? "match_draw" : "match_win");
        }

        // ----- Awards -----
        void ShowAwards()
        {
            MatchStats stats = GameModeManager.Instance?.Stats;
            if (mvpLabel != null)
            {
                PlayerStats mvp = stats?.ResolveMvp();
                mvpLabel.text = mvp != null
                    ? "MVP   " + mvp.DisplayName + "  (" + mvp.Class + ")   " + mvp.Kills + " kills   " + mvp.ScoreContribution + " pts"
                    : "MVP   —";
            }
            if (hotStreakLabel != null)
            {
                PlayerStats hot = stats?.ResolveHotStreak();
                hotStreakLabel.text = hot != null && hot.LongestOnFireStreak > 0f
                    ? "HOT STREAK   " + hot.DisplayName + "  (" + hot.Class + ")   " + Mathf.RoundToInt(hot.LongestOnFireStreak) + "s on fire"
                    : "HOT STREAK   —";
            }
        }

        // ----- Scoreboard -----
        void ShowScoreboard()
        {
            if (scoreboardLabel == null) return;
            MatchStats stats = GameModeManager.Instance?.Stats;
            builder.Clear();
            builder.Append("Player        Class      K   A   D   Obj    Score\n");

            if (stats != null)
            {
                // Stable order: Team A then Team B, highest score contribution first within team.
                rows.Clear();
                foreach (PlayerStats s in stats.All.Values) rows.Add(s);
                rows.Sort((x, y) =>
                {
                    if (x.Team != y.Team) return x.Team.CompareTo(y.Team);
                    return y.ScoreContribution.CompareTo(x.ScoreContribution);
                });

                Team lastTeam = Team.None;
                for (int i = 0; i < rows.Count; i++)
                {
                    PlayerStats s = rows[i];
                    if (lastTeam != Team.None && s.Team != lastTeam)
                        builder.Append("──────────────────────────────────────\n");
                    lastTeam = s.Team;

                    builder.Append(Pad(s.DisplayName, 12));
                    builder.Append(Pad(s.Class.ToString(), 10));
                    builder.Append(Pad(s.Kills.ToString(), 4));
                    builder.Append(Pad(s.Assists.ToString(), 4));
                    builder.Append(Pad(s.Deaths.ToString(), 4));
                    builder.Append(Pad(Mathf.RoundToInt(s.ObjectiveTime) + "s", 6));
                    builder.Append(s.ScoreContribution).Append('\n');
                }
            }

            scoreboardLabel.text = builder.ToString().TrimEnd('\n');
        }

        // ----- XP + Battle Pass -----
        void ShowXpAndBattlePass(Team winner)
        {
            MatchStats stats = GameModeManager.Instance?.Stats;
            PlayerStats local = FindLocalStats(stats);
            PlayerStats mvp = stats?.ResolveMvp();

            bool win = winner != Team.None && local != null && local.Team == winner;
            bool isMvp = local != null && mvp != null && mvp.PlayerId == local.PlayerId;

            // Itemised breakdown for the readout (mirrors XPManager's award table).
            int total = XPManager.ComputeMatchXp(local, win, isMvp);
            if (xpBreakdownLabel != null)
                xpBreakdownLabel.text = BuildBreakdown(local, win, isMvp, total);

            // Snapshot the pre-award level/XP so the count-up animates from "before".
            int startLevel = ProgressionService.Level;
            int startXpIntoLevel = ProgressionService.CurrentXp;

            // Award (advances both the level track and the battle pass track, persists).
            int awarded = ProgressionService.AwardMatch(local, win, isMvp);

            int endLevel = ProgressionService.Level;
            int endXpIntoLevel = ProgressionService.CurrentXp;

            if (xpRoutine != null) StopCoroutine(xpRoutine);
            xpRoutine = StartCoroutine(AnimateXpCountUp(startLevel, startXpIntoLevel, endLevel, endXpIntoLevel, awarded));

            RefreshBattlePass();
        }

        string BuildBreakdown(PlayerStats stats, bool win, bool isMvp, int total)
        {
            builder.Clear();
            int participation = win ? XPManager.XpWin : XPManager.XpLoss;
            builder.Append(win ? "Win bonus" : "Participation").Append(":\t+").Append(participation).Append('\n');

            if (stats != null)
            {
                if (stats.Kills > 0)
                    builder.Append(stats.Kills).Append(" Kills:\t+").Append(stats.Kills * XPManager.XpPerKill).Append('\n');
                if (stats.Assists > 0)
                    builder.Append(stats.Assists).Append(" Assists:\t+").Append(stats.Assists * XPManager.XpPerAssist).Append('\n');
                if (stats.ZoneCaptures > 0)
                    builder.Append(stats.ZoneCaptures).Append(" Captures:\t+").Append(stats.ZoneCaptures * XPManager.XpPerCapture).Append('\n');

                int objBlocks = Mathf.FloorToInt(stats.ObjectiveTime / 30f);
                if (objBlocks > 0)
                    builder.Append("Obj time:\t+").Append(objBlocks * XPManager.XpObjectivePer30s).Append('\n');

                if (stats.LongestOnFireStreak >= XPManager.HotStreakSeconds)
                    builder.Append("Hot Streak:\t+").Append(XPManager.XpHotStreak).Append('\n');
            }

            if (isMvp) builder.Append("MVP:\t+").Append(XPManager.XpMvp).Append('\n');

            builder.Append("─────────────\n");
            builder.Append("Total:\t+").Append(total);
            return builder.ToString();
        }

        IEnumerator AnimateXpCountUp(int startLevel, int startXpIntoLevel, int endLevel, int endXpIntoLevel, int earned)
        {
            float t = 0f;
            float dur = Mathf.Max(0.01f, xpCountUpDuration);
            int lastTick = -1;

            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float f = Mathf.Clamp01(t / dur);
                ApplyInterpolatedXp(startLevel, startXpIntoLevel, endLevel, endXpIntoLevel, f);

                // Periodic tick SFX as the numbers climb (UI/UX doc dopamine beat).
                int tick = Mathf.FloorToInt(f * 10f);
                if (tick != lastTick && earned > 0)
                {
                    lastTick = tick;
                    CarrotClash.Audio.AudioManager.Instance?.PlayUi("ui_xp_tick");
                }
                yield return null;
            }

            ApplyXpVisual(endLevel, endXpIntoLevel);
            xpRoutine = null;
        }

        // Interpolate the bar smoothly across one or more levels. We work in "XP accumulated since the
        // start level" so a single scalar lerps cleanly even when one or more level-ups are crossed,
        // then walk the curve to derive the level + into-level to display. No reliance on the persisted
        // profile during the animation (it was already granted).
        void ApplyInterpolatedXp(int startLevel, int startXpIntoLevel, int endLevel, int endXpIntoLevel, float f)
        {
            float startOffset = startXpIntoLevel;
            float endOffset = CumulativeXpBetween(startLevel, endLevel) + endXpIntoLevel;
            float shown = Mathf.Lerp(startOffset, endOffset, Mathf.Clamp01(f));

            int level = startLevel;
            float remaining = shown;
            int need = XPManager.XpForLevel(level);
            while (level < endLevel && need > 0 && remaining >= need)
            {
                remaining -= need;
                level++;
                need = XPManager.XpForLevel(level);
            }

            ApplyXpVisual(level, Mathf.RoundToInt(remaining));
        }

        // Total XP required to climb from <paramref name="fromLevel"/> up to (not into) <paramref name="toLevel"/>.
        static int CumulativeXpBetween(int fromLevel, int toLevel)
        {
            int total = 0;
            for (int l = fromLevel; l < toLevel; l++) total += XPManager.XpForLevel(l);
            return total;
        }

        void ApplyXpVisual(int level, int xpIntoLevel)
        {
            int need = XPManager.XpForLevel(level);
            if (levelLabel != null) levelLabel.text = "Level " + level;
            if (xpTotalLabel != null) xpTotalLabel.text = need > 0 ? xpIntoLevel + " / " + need : "MAX";
            if (xpBar != null)
            {
                xpBar.minValue = 0f;
                xpBar.maxValue = 1f;
                xpBar.value = need > 0 ? Mathf.Clamp01((float)xpIntoLevel / need) : 1f;
            }
        }

        void RefreshBattlePass()
        {
            int tier = ProgressionService.BattlePassTier;
            if (battlePassLabel != null)
                battlePassLabel.text = "Battle Pass: Tier " + tier
                    + "   " + ProgressionService.BattlePassXp + "/" + ProgressionService.BattlePassXpToNext;
            if (battlePassBar != null)
            {
                battlePassBar.minValue = 0f;
                battlePassBar.maxValue = 1f;
                battlePassBar.value = ProgressionService.BattlePassFraction;
            }
        }

        static PlayerStats FindLocalStats(MatchStats stats)
        {
            if (stats == null) return null;
            foreach (PlayerStats s in stats.All.Values)
                if (s.IsLocal) return s;
            return null;
        }

        // ----- Buttons -----
        void OnRematch()
        {
            // Local vote registration; networked play would aggregate across clients.
            rematchVotes = Mathf.Min(rematchVotes + 1, GameConstants.MaxPlayersPerTeam);
            UpdateRematchVoteLabel();
            CarrotClash.Audio.AudioManager.Instance?.PlayUi("ui_button_confirm");
            if (rematchVotes >= GameConstants.MaxPlayersPerTeam && !SceneFlow.IsLoading)
                SceneFlow.LoadGameplay();
        }

        void OnPlayAgain()
        {
            CarrotClash.Audio.AudioManager.Instance?.PlayUi("ui_button_confirm");
            if (!SceneFlow.IsLoading) SceneFlow.LoadGameplay();
        }

        void OnMenu()
        {
            CarrotClash.Audio.AudioManager.Instance?.PlayUi("ui_button_confirm");
            if (!SceneFlow.IsLoading) SceneFlow.LoadMainMenu();
        }

        void UpdateRematchVoteLabel()
        {
            if (rematchVoteLabel != null)
                rematchVoteLabel.text = "REMATCH  " + rematchVotes + "/" + GameConstants.MaxPlayersPerTeam + " voted";
        }

        static string Pad(string s, int width)
        {
            if (s == null) s = string.Empty;
            if (s.Length >= width) return s.Substring(0, width - 1) + " ";
            return s + new string(' ', width - s.Length);
        }
    }
}
