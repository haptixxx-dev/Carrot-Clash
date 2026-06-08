using UnityEngine;
#if NETCODE_PRESENT
using Unity.Netcode;

namespace CarrotClash.Net
{
    /// <summary>
    /// Network mirror for the spine <see cref="GameModeManager"/>.
    ///
    /// SERVER-AUTHORITATIVE: the host runs the real GameModeManager (state machine, timer, scoring,
    /// win condition). This component samples that authoritative state each server tick and replicates
    /// team scores, match timer and match state to clients via NetworkVariables. Clients read these and
    /// raise the matching local GameEvents so HUD / scoreboard update exactly as on the host. Clients
    /// never advance the timer or mutate scores.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameModeNetworkManager : NetworkBehaviour
    {
        [Header("Spine reference (host only)")]
        [SerializeField] GameModeManager gameMode;

        /// <summary>Authoritative Team.A score. Server writes; everyone reads.</summary>
        readonly NetworkVariable<int> netScoreA = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        /// <summary>Authoritative Team.B score. Server writes; everyone reads.</summary>
        readonly NetworkVariable<int> netScoreB = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        /// <summary>Authoritative remaining match time (seconds). Server writes; everyone reads.</summary>
        readonly NetworkVariable<float> netMatchTimer = new NetworkVariable<float>(
            GameConstants.MatchDuration, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        /// <summary>Authoritative match state (cast of <see cref="MatchState"/>). Server writes; everyone reads.</summary>
        readonly NetworkVariable<int> netState = new NetworkVariable<int>(
            (int)MatchState.Lobby, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        [Tooltip("Seconds between timer replications (the timer is interpolated locally between pushes).")]
        [SerializeField] float timerPushInterval = 0.25f;

        float timerPushAccumulator;

        void Awake()
        {
            if (gameMode == null) gameMode = GameModeManager.Instance;
        }

        public override void OnNetworkSpawn()
        {
            netScoreA.OnValueChanged += OnScoreAChanged;
            netScoreB.OnValueChanged += OnScoreBChanged;
            netMatchTimer.OnValueChanged += OnTimerChanged;
            netState.OnValueChanged += OnStateChanged;

            if (IsServer)
            {
                if (gameMode == null) gameMode = GameModeManager.Instance;
                if (gameMode != null)
                {
                    netScoreA.Value = gameMode.GetScore(Team.A);
                    netScoreB.Value = gameMode.GetScore(Team.B);
                    netMatchTimer.Value = gameMode.RemainingTime;
                    netState.Value = (int)gameMode.State;
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            netScoreA.OnValueChanged -= OnScoreAChanged;
            netScoreB.OnValueChanged -= OnScoreBChanged;
            netMatchTimer.OnValueChanged -= OnTimerChanged;
            netState.OnValueChanged -= OnStateChanged;
        }

        public override void OnDestroy()
        {
            netScoreA.OnValueChanged -= OnScoreAChanged;
            netScoreB.OnValueChanged -= OnScoreBChanged;
            netMatchTimer.OnValueChanged -= OnTimerChanged;
            netState.OnValueChanged -= OnStateChanged;
            base.OnDestroy();
        }

        void Update()
        {
            // Server pushes the authoritative state. Clients interpolate the timer locally between pushes.
            if (IsServer)
            {
                if (gameMode == null) { gameMode = GameModeManager.Instance; if (gameMode == null) return; }

                int sa = gameMode.GetScore(Team.A);
                int sb = gameMode.GetScore(Team.B);
                if (netScoreA.Value != sa) netScoreA.Value = sa;
                if (netScoreB.Value != sb) netScoreB.Value = sb;

                int st = (int)gameMode.State;
                if (netState.Value != st) netState.Value = st;

                timerPushAccumulator += Time.deltaTime;
                if (timerPushAccumulator >= timerPushInterval)
                {
                    timerPushAccumulator = 0f;
                    float remaining = gameMode.RemainingTime;
                    if (!Mathf.Approximately(netMatchTimer.Value, remaining))
                        netMatchTimer.Value = remaining;
                }
            }
            else
            {
                // Client-side smooth countdown between server timer pushes (presentation only).
                // Advance the local accumulator so the displayed value actually ticks down between
                // the 0.25s server pushes; OnTimerChanged re-anchors it to 0 on each fresh push.
                timerPushAccumulator += Time.deltaTime;
                if (netMatchTimer.Value > 0f)
                    GameEvents.RaiseMatchTimerTick(Mathf.Max(0f, netMatchTimer.Value - timerPushAccumulator));
            }
        }

        void OnScoreAChanged(int previous, int current)
        {
            if (IsServer) return;          // server already raised its own events
            GameEvents.RaiseScoreChanged(Team.A.Id(), current);
        }

        void OnScoreBChanged(int previous, int current)
        {
            if (IsServer) return;
            GameEvents.RaiseScoreChanged(Team.B.Id(), current);
        }

        void OnTimerChanged(float previous, float current)
        {
            if (IsServer) return;
            timerPushAccumulator = 0f;     // re-anchor the client interpolation to the fresh value
            GameEvents.RaiseMatchTimerTick(Mathf.Max(0f, current));
        }

        void OnStateChanged(int previous, int current)
        {
            if (IsServer) return;
            GameEvents.RaiseMatchStateChanged((MatchState)previous, (MatchState)current);

            // Surface the match-end winner to clients when entering the end state.
            if ((MatchState)current == MatchState.MatchEnd)
            {
                int a = netScoreA.Value, b = netScoreB.Value;
                Team winner = a == b ? Team.None : (a > b ? Team.A : Team.B);
                GameEvents.RaiseMatchEnded(winner);
            }
        }
    }
}
#endif
