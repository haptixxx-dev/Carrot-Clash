using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CarrotClash
{
    /// <summary>
    /// Objective-and-combat bot brain (tech-architecture Bot AI FSM):
    /// Idle -> MoveToObjective -> Capture -> FightNearbyEnemy -> Retreat(&lt;30% HP).
    /// Paths toward the nearest contestable capture zone (using a <see cref="NavMeshAgent"/> when one
    /// is present, otherwise steering directly), uses <c>Physics.OverlapSphere</c> to spot enemies
    /// within 8m, and engages with difficulty-scaled aim accuracy + reaction time. Drops to Retreat
    /// when its own health falls below 30%, heading back toward team spawn.
    /// </summary>
    public class CombatBotBrain : IBotBrain
    {
        const float EnemyScanRadius = 8f;
        const float CaptureArriveRadius = 1.5f;
        const float RepathInterval = 0.5f;
        const float EngageRange = 35f;       // won't bother firing past this
        const float RetreatFraction = 0.3f;
        const float AimTurnSpeed = 360f;     // deg/sec the look is allowed to slew

        // Difficulty tuning (set by ctor).
        readonly float aimAccuracy;          // 0..1 — chance the shot is "on target" this burst
        readonly float reactionTime;         // seconds before the bot reacts to a newly seen enemy

        // FSM
        BotState state = BotState.Idle;

        // Targeting / pathing scratch
        static readonly Collider[] scanBuffer = new Collider[32];
        readonly List<CaptureZone> zones = new List<CaptureZone>(4);
        bool zonesDiscovered;

        NavMeshAgent agent;
        bool agentChecked;

        PlayerController currentEnemy;
        Vector3 navTarget;
        float repathTimer;
        float reactionTimer;
        bool reacting;

        // Per-frame outputs
        Vector2 moveInput;
        Vector2 lookInput;
        bool fireInput;
        bool ability1Input;
        bool ability2Input;

        // Aim noise refresh so accuracy "wobble" isn't recomputed every single frame.
        float aimWobbleTimer;
        Vector3 aimErrorOffset;

        public CombatBotBrain(float accuracy, float reaction)
        {
            aimAccuracy = Mathf.Clamp01(accuracy);
            reactionTime = Mathf.Max(0f, reaction);
        }

        public BotState State => state;

        public void Tick(PlayerController self)
        {
            // Reset transient outputs each tick.
            moveInput = Vector2.zero;
            lookInput = Vector2.zero;
            fireInput = false;
            ability1Input = false;
            ability2Input = false;

            if (self == null) return;

            EnsureSetup(self);

            // Retreat overrides everything below the HP threshold.
            if (self.health != null && self.health.HealthFraction < RetreatFraction)
            {
                state = BotState.Retreat;
                TickRetreat(self);
                return;
            }

            // Acquire / refresh nearest enemy.
            PlayerController enemy = FindNearestEnemy(self);
            HandleReaction(enemy);

            if (currentEnemy != null && IsValidEnemy(self, currentEnemy))
            {
                state = BotState.FightNearbyEnemy;
                TickFight(self, currentEnemy);
                return;
            }

            // No enemy in range — work the objective.
            CaptureZone target = FindObjectiveZone(self);
            if (target == null)
            {
                state = BotState.Idle;
                return;
            }

            float distToZone = GameExtensions.FlatDistance(self.transform.position, target.transform.position);
            if (distToZone <= target.captureRadius)
            {
                state = BotState.Capture;
                TickCapture(self, target);
            }
            else
            {
                state = BotState.MoveToObjective;
                MoveToward(self, target.transform.position);
            }
        }

        // ----- Setup -----

        void EnsureSetup(PlayerController self)
        {
            if (!agentChecked)
            {
                agentChecked = true;
                // NavMeshAgent is optional; guard the GetComponent result.
                agent = self.GetComponent<NavMeshAgent>();
                if (agent == null) agent = self.GetComponentInChildren<NavMeshAgent>();
                if (agent != null)
                {
                    // The bot's movement is driven through PlayerMovement; the agent is used only as a
                    // path planner, so it must not move the transform itself.
                    agent.updatePosition = false;
                    agent.updateRotation = false;
                    agent.updateUpAxis = false;
                }
            }

            if (!zonesDiscovered)
            {
                zonesDiscovered = true;
                zones.Clear();
                // One-time discovery (not per-frame): cache every capture zone in the scene.
                CaptureZone[] found = Object.FindObjectsByType<CaptureZone>(FindObjectsSortMode.None);
                zones.AddRange(found);
            }
        }

        // ----- Retreat -----

        void TickRetreat(PlayerController self)
        {
            // Head back toward a fixed team spawn; SpawnManager exposes one for our team/index.
            Vector3 dest = self.transform.position;
            if (SpawnManager.Instance != null)
            {
                var (pos, _) = SpawnManager.Instance.GetInitialSpawn(self.Team, self.PlayerId);
                dest = pos;
            }
            MoveToward(self, dest);

            // While retreating, still keep an eye out and shoot back if a threat is right on us.
            PlayerController enemy = FindNearestEnemy(self);
            if (enemy != null && IsValidEnemy(self, enemy))
            {
                AimAt(self, enemy);
                fireInput = ShouldFire(self, enemy, EngageRange * 0.4f);
            }
        }

        // ----- Fight -----

        void TickFight(PlayerController self, PlayerController enemy)
        {
            AimAt(self, enemy);

            float dist = GameExtensions.FlatDistance(self.transform.position, enemy.transform.position);

            // Strafe a little to be a harder target, while closing if far / backing off if point-blank.
            float forward = dist > EnemyScanRadius * 0.6f ? 0.6f : (dist < 2.5f ? -0.4f : 0f);
            float strafe = Mathf.Sin(Time.time * 2f) * 0.5f;
            moveInput = new Vector2(strafe, forward);

            fireInput = ShouldFire(self, enemy, EngageRange);

            // Higher-skill bots use offensive abilities when an enemy is in front of them.
            if (aimAccuracy >= 0.6f && self.abilities != null)
            {
                if (self.abilities.IsReady((int)AbilitySlot.Active1) && dist <= EngageRange)
                    ability1Input = true;
                else if (self.abilities.IsReady((int)AbilitySlot.Active2) && dist <= EngageRange * 0.6f)
                    ability2Input = true;
            }
        }

        // ----- Capture -----

        void TickCapture(PlayerController self, CaptureZone zone)
        {
            // Sit on/near the centre to keep capturing; small idle drift keeps it from clumping perfectly.
            Vector3 toCentre = (zone.transform.position - self.transform.position).Flat();
            if (toCentre.sqrMagnitude > CaptureArriveRadius * CaptureArriveRadius)
                MoveToward(self, zone.transform.position);
            else
                moveInput = Vector2.zero;
        }

        // ----- Movement helpers -----

        void MoveToward(PlayerController self, Vector3 worldDest)
        {
            Vector3 steer;

            if (agent != null && agent.isOnNavMesh)
            {
                repathTimer -= Time.deltaTime;
                if (repathTimer <= 0f || (navTarget - worldDest).sqrMagnitude > 1f)
                {
                    repathTimer = RepathInterval;
                    navTarget = worldDest;
                    agent.nextPosition = self.transform.position;
                    agent.SetDestination(worldDest);
                }
                // Steer along the agent's desired velocity (planner), but PlayerMovement does the moving.
                steer = agent.desiredVelocity;
                if (steer.sqrMagnitude < 0.01f)
                    steer = (worldDest - self.transform.position);
            }
            else
            {
                // No NavMesh: steer straight at the destination.
                steer = worldDest - self.transform.position;
            }

            steer = steer.Flat();
            if (steer.sqrMagnitude < 0.0001f)
            {
                moveInput = Vector2.zero;
                return;
            }
            steer.Normalize();

            // Convert world steer direction into local move input relative to the bot's facing.
            Vector3 fwd = self.transform.forward.Flat().normalized;
            Vector3 right = self.transform.right.Flat().normalized;
            float forward = Vector3.Dot(steer, fwd);
            float strafe = Vector3.Dot(steer, right);
            moveInput = Vector2.ClampMagnitude(new Vector2(strafe, forward), 1f);

            // Look where we're going when not actively fighting.
            if (state != BotState.FightNearbyEnemy)
                FaceDirection(self, steer);
        }

        void FaceDirection(PlayerController self, Vector3 worldDir)
        {
            worldDir = worldDir.Flat();
            if (worldDir.sqrMagnitude < 0.0001f) return;
            float desiredYaw = Mathf.Atan2(worldDir.x, worldDir.z) * Mathf.Rad2Deg;
            float currentYaw = self.transform.eulerAngles.y;
            float deltaYaw = Mathf.DeltaAngle(currentYaw, desiredYaw);
            float step = Mathf.Clamp(deltaYaw, -AimTurnSpeed * Time.deltaTime, AimTurnSpeed * Time.deltaTime);
            lookInput = new Vector2(step, 0f);
        }

        // ----- Combat helpers -----

        void AimAt(PlayerController self, PlayerController enemy)
        {
            Transform aimPoint = enemy.headTransform != null ? enemy.headTransform : enemy.transform;
            Vector3 target = aimPoint.position + GetAimError(self);
            Vector3 dir = target - (self.headTransform != null ? self.headTransform.position : self.transform.position);

            // Yaw
            float desiredYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            float currentYaw = self.transform.eulerAngles.y;
            float deltaYaw = Mathf.DeltaAngle(currentYaw, desiredYaw);

            // Pitch (negative = up in typical FPS look conventions; camera owns clamping/inversion).
            float flatDist = new Vector3(dir.x, 0f, dir.z).magnitude;
            float desiredPitch = -Mathf.Atan2(dir.y, Mathf.Max(0.01f, flatDist)) * Mathf.Rad2Deg;

            float yawStep = Mathf.Clamp(deltaYaw, -AimTurnSpeed * Time.deltaTime, AimTurnSpeed * Time.deltaTime);
            float pitchStep = Mathf.Clamp(desiredPitch, -AimTurnSpeed * Time.deltaTime, AimTurnSpeed * Time.deltaTime);
            lookInput = new Vector2(yawStep, pitchStep);
        }

        Vector3 GetAimError(PlayerController self)
        {
            aimWobbleTimer -= Time.deltaTime;
            if (aimWobbleTimer <= 0f)
            {
                aimWobbleTimer = 0.25f;
                // Lower accuracy => larger random miss cone. Perfect accuracy => no offset.
                float spread = (1f - aimAccuracy) * 2.5f;
                aimErrorOffset = new Vector3(
                    Random.Range(-spread, spread),
                    Random.Range(-spread * 0.5f, spread * 0.5f),
                    Random.Range(-spread, spread));
            }
            return aimErrorOffset;
        }

        bool ShouldFire(PlayerController self, PlayerController enemy, float maxRange)
        {
            if (!reacting && reactionTimer > 0f) return false;   // still within reaction delay
            if (enemy == null || self.weapon == null) return false;

            float dist = GameExtensions.FlatDistance(self.transform.position, enemy.transform.position);
            if (dist > maxRange) return false;

            // Only commit to firing once roughly aimed at the target.
            Vector3 toEnemy = (enemy.transform.position - self.transform.position).Flat();
            if (toEnemy.sqrMagnitude < 0.0001f) return false;
            float facingDot = Vector3.Dot(self.transform.forward.Flat().normalized, toEnemy.normalized);
            if (facingDot < 0.9f) return false;

            // Accuracy gate: hold fire on "missed" windows so low-skill bots whiff.
            return Random.value <= aimAccuracy || aimAccuracy >= 0.999f;
        }

        // ----- Perception -----

        void HandleReaction(PlayerController seen)
        {
            if (seen == null)
            {
                currentEnemy = null;
                reacting = false;
                reactionTimer = 0f;
                return;
            }

            if (seen != currentEnemy)
            {
                // Newly spotted target: start the reaction delay before we can fire.
                currentEnemy = seen;
                reacting = false;
                reactionTimer = reactionTime;
            }

            if (!reacting)
            {
                reactionTimer -= Time.deltaTime;
                if (reactionTimer <= 0f) reacting = true;
            }
        }

        PlayerController FindNearestEnemy(PlayerController self)
        {
            int mask = LayerMask.GetMask(GameConstants.LayerPlayer);
            int count = Physics.OverlapSphereNonAlloc(
                self.transform.position, EnemyScanRadius, scanBuffer, mask, QueryTriggerInteraction.Collide);

            PlayerController nearest = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var pc = scanBuffer[i].GetComponentInParent<PlayerController>();
                if (!IsValidEnemy(self, pc)) continue;
                float sqr = GameExtensions.FlatSqrDistance(self.transform.position, pc.transform.position);
                if (sqr < bestSqr) { bestSqr = sqr; nearest = pc; }
            }
            return nearest;
        }

        bool IsValidEnemy(PlayerController self, PlayerController other)
        {
            if (other == null || other == self) return false;
            if (other.health == null || other.health.IsDead) return false;
            return self.Team.IsEnemyOf(other.Team);
        }

        // ----- Objective selection -----

        CaptureZone FindObjectiveZone(PlayerController self)
        {
            CaptureZone best = null;
            float bestSqr = float.MaxValue;
            bool bestOwned = true;   // prefer zones we don't already own

            for (int i = 0; i < zones.Count; i++)
            {
                CaptureZone z = zones[i];
                if (z == null || !z.IsUnlocked) continue;

                bool weOwn = z.OwningTeam == self.Team;
                float sqr = GameExtensions.FlatSqrDistance(self.transform.position, z.transform.position);

                // Prioritise any not-yet-owned zone over owned ones; within a tier pick the nearest.
                if (best == null
                    || (bestOwned && !weOwn)
                    || (bestOwned == weOwn && sqr < bestSqr))
                {
                    best = z;
                    bestSqr = sqr;
                    bestOwned = weOwn;
                }
            }
            return best;
        }

        public Vector2 GetMoveInput() => moveInput;
        public Vector2 GetLookInput() => lookInput;
        public bool GetFireInput() => fireInput;
        public bool GetAbility1Input() => ability1Input;
        public bool GetAbility2Input() => ability2Input;
    }
}
