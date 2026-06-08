# Carrot Clash - Frozen API Contract

This file is the single source of truth for the public surface of the gameplay spine. Every
leaf file (abilities, UI, bots, network, audio, progression, feedback) MUST compile against
exactly these signatures. Do NOT invent members not listed here. Namespace: `CarrotClash`
(audio in `CarrotClash.Audio`, network in `CarrotClash.Net`, editor in `CarrotClash.EditorTools`).

Unity **6000.4.10f1**, URP 17.4, InputSystem 1.19. C# style: explicit, no `var` for fields,
4-space indent, doc-comments on public types. No `FindObjectsOfType` per-frame.

---

## Enums (CarrotClash)
- `Team { None=-1, A=0, B=1 }`
- `ClassId { Carrot, Jalapeno, Broccoli, Potato }`
- `MomentumTier { Cold=0, Warm=1, Hot=2, OnFire=3 }`
- `ZoneId { A, B, C }`
- `MatchState { Lobby, ClassSelect, Countdown, MatchActive, SuddenDeath, MatchEnd, PostMatch }`
- `AbilitySlot { Active1=0, Active2=1 }`
- `DamageType { Bullet, Fire, Ability, Explosion, Fall }`
- `ShakeType { Random, Directional, Radial, Downward }`
- `SurfaceType { Stone, Wood, Metal, Dirt }`
- `BotDifficulty { Dummy, Easy, Medium, Hard }`
- `BotState { Idle, MoveToObjective, Capture, FightNearbyEnemy, Retreat }`
- `MatchResult { Win, Loss, Draw }`
- `SettingsService`: `ColorblindMode { None, Protanopia, Deuteranopia, Tritanopia }`, `TextSize { Normal, Large }`

## GameConstants (static, partial list of what's used downstream)
MatchDuration=480, SuddenDeathDuration=60, CountdownDuration=5, ClassSelectDuration=30,
PostMatchDuration=15, ScoreCap=500, ZoneCUnlockTime=300, TeamCount=2, MaxPlayersPerTeam=4,
MaxPlayers=8, RespawnTime=4, SpawnInvulnerability=2, ScoreKill=5, ScoreKillOnFire=8,
ScoreAssist=2, AssistWindow=5, SlowAmount=0.35, SlowDuration=3, KnockbackDistance=6,
KnockbackDuration=0.25, StaggerDuration=0.5, StarchArmorAmount=40, StarchArmorDuration=4,
RegenAuraTick=1, RegenAuraHeal=2, RegenAuraRadius=8, MomentumDecayInterval=12,
BroccoliHarvestShare=0.1, BroccoliHarvestRadius=10, MaxTier=3, DefaultFov=90, Gravity=-22,
CarrotBackstabDot=-0.1, SilentStepsSpeedFraction=0.5, FireTickInterval=0.1, AudioMaxDistance=25.
Layer name consts: `LayerPlayer`, `LayerEnvironment`, `LayerAbility`, `LayerHitbox` (strings).
Scene consts: `SceneBoot`, `SceneMainMenu`, `SceneGameplay`, `SceneGameplayUI`, `SceneTutorial`.
Helpers: `bool IsZoneCUnlocked(float elapsed)`, `Team Opponent(Team)`.

## GameExtensions (static)
- `Vector3 Flat(this Vector3)` - drops Y
- `float FlatDistance(Vector3 a, Vector3 b)`, `float FlatSqrDistance(...)`
- `bool IsInRearArc(Vector3 victimForward, Vector3 victimToKiller, float dotThreshold)`
- `float Remap(this float, inMin, inMax, outMin, outMax)`
- `int Id(this Team)`, `bool IsEnemyOf(this Team a, Team b)`

## DamageInfo (readonly struct)
ctor `(int amount, DamageType type, PlayerController instigator, Vector3 hitPoint, Vector3 hitDirection, bool isHeadshot=false)`.
Static `DamageInfo.Environmental(int amount, DamageType type)`.
Fields: `Amount, Type, Instigator, HitPoint, HitDirection, IsHeadshot`.

## DamageResult (readonly struct)
ctor `(int damageApplied, bool hitTempHp, bool wasLethal, bool wasHeadshot)`. Static `.None`.
Fields: `DamageApplied, HitTempHp, WasLethal, WasHeadshot`.

## KillEvent (readonly struct)
ctor `(PlayerController killer, PlayerController victim, DamageType killingBlowType, bool isBackstab, MomentumTier victimTier, bool isHeadshot)`.
Fields: `Killer, Victim, KillingBlowType, IsBackstab, VictimTier, IsHeadshot`.

## ZoneCaptureEvent (readonly struct)
ctor `(ZoneId zone, Team newOwner, Team previousOwner, bool wasContested)`.
Fields: `Zone, NewOwner, PreviousOwner, WasContested`.

## GameEvents (static hub) - subscribe with `+=`, ALWAYS unsubscribe in OnDisable/OnDestroy
Events:
- `Action<KillEvent> OnKill`
- `Action<PlayerController, DamageInfo, DamageResult> OnDamageDealt` (victim, info, result)
- `Action<PlayerController> OnPlayerSpawned`
- `Action<PlayerController> OnPlayerDied`
- `Action<PlayerController, MomentumTier, MomentumTier> OnTierChanged` (player, old, new)
- `Action<PlayerController, int> OnAssist` (player, amount)
- `Action<ZoneCaptureEvent> OnZoneCaptured`
- `Action<ZoneId, float> OnZoneProgressChanged`
- `Action OnZoneCUnlocked`
- `Action<MatchState, MatchState> OnMatchStateChanged` (old, new)
- `Action<int, int> OnScoreChanged` (teamId, newTotal)
- `Action<float> OnMatchTimerTick` (remainingSeconds)
- `Action<Team> OnMatchEnded` (winner; None=draw)
Raisers: `RaiseKill(in KillEvent)`, `RaiseDamageDealt(pc, in DamageInfo, in DamageResult)`,
`RaisePlayerSpawned(pc)`, `RaisePlayerDied(pc)`, `RaiseTierChanged(pc,old,new)`, `RaiseAssist(pc,int)`,
`RaiseZoneCaptured(in ZoneCaptureEvent)`, `RaiseZoneProgress(ZoneId,float)`, `RaiseZoneCUnlocked()`,
`RaiseMatchStateChanged(old,new)`, `RaiseScoreChanged(int,int)`, `RaiseMatchTimerTick(float)`, `RaiseMatchEnded(Team)`.
`Clear()` detaches all.

## PlayerController (MonoBehaviour) - the orchestrator
Public fields: `PlayerMovement movement; PlayerCamera cam; WeaponController weapon;
AbilityController abilities; HealthController health; MomentumController momentum;
Transform abilityOrigin1; Transform abilityOrigin2; Transform headTransform;`
Props: `CharacterDataSO Data`, `ClassId ClassId`, `Team Team`, `int PlayerId`, `bool IsLocal`,
`bool IsBot`, `Color TeamColor`.
Methods:
- `void Initialize(CharacterDataSO data, MomentumConfigSO momentumCfg, Team team, int id, bool local, bool bot=false)`
- input routing: `InputMove(Vector2)`, `InputLook(Vector2)`, `InputSprint(bool)`, `InputCrouch(bool)`,
  `InputJump()`, `InputFire(bool)`, `InputAds(bool)`, `InputReload()`, `InputAbility(int slot)`, `InputSwapWeapon()`
- `void Respawn(Vector3 pos, Quaternion rot)`
- `DamageResult ApplyDamage(in DamageInfo)`
Note: `cam` is null on remote/bot players - null-check.

## HealthController (MonoBehaviour)
Props: `int CurrentHP`, `int MaxHP`, `int TempHP`, `bool IsDead`, `bool IsFullHealth`,
`bool Invulnerable {get;set;}`, `float HealthFraction`.
Events: `Action<int,int> OnHealthChanged` (cur,max); `Action<int> OnTempHpChanged`;
`Action<DamageInfo,DamageResult> OnDamaged`; `Action<PlayerController> OnDeath` (killer, may be null).
Methods: `Initialize(PlayerController, int maxHP)`, `DamageResult TakeDamage(in DamageInfo)`,
`Heal(int)`, `AddTemporaryHP(int amount, float duration)`, `Revive()`,
`CollectAssisters(List<PlayerController> buffer, PlayerController excludeKiller)`.

## MomentumController (MonoBehaviour)
Props: `int Tier`, `MomentumTier TierEnum`, `float Charge`, `bool IsOnFire`, `MomentumConfigSO Config`,
`float SpeedMultiplier`, `float CooldownMultiplier`, `float DamageMultiplier`.
Events: `Action<MomentumTier,MomentumTier> OnTierChanged` (old,new); `Action<float> OnDecayProgress` (0..1).
Methods: `Initialize(PlayerController, MomentumConfigSO)`, `SetDecayPaused(bool)`,
`RegisterKill(bool isBackstab=false)`, `RegisterAssist()`, `AddCharge(float)`, `ResetDecayTimer()`,
`float HandleDeath()` (returns transferred charge), `AbsorbTransfer(float)`, `ForceTier(int)`.

## MomentumConfigSO (ScriptableObject)
Fields: `float decayIntervalSeconds`, `float transferOnDeath`, `MomentumTierData[] tiers` (len 4).
Methods: `SpeedMultiplier(int tier)`, `CooldownMultiplier(int)`, `DamageMultiplier(int)`,
`int KillScoreValue(int victimTier)`, `Color TierColor(int)`.
`struct MomentumTierData { float moveSpeedBonus, cooldownReduction, damageBonus; int teamScoreKillValue; Color vfxColor; }`

## PlayerMovement (MonoBehaviour, requires CharacterController)
Props: `bool IsSprinting`, `bool IsCrouching`, `bool IsGrounded`, `bool IsStaggered`,
`float CurrentSpeed`, `float HorizontalSpeed`, `float SprintSpeed`, `float IncomingDamageMultiplier`.
Methods: `Initialize(PlayerController, CharacterDataSO)`, `SetMoveInput(Vector2)`, `SetSprint(bool)`,
`SetCrouch(bool)`, `Jump()`, `SetFiring(float speedMultiplier)`, `Dash(Vector3 dir, float distance)`,
`ApplySlow(float amount, float duration)`, `ApplyKnockback(Vector3 force, float stagger)`,
`ResetForRespawn(Vector3, Quaternion)`.

## PlayerCamera (MonoBehaviour) - local player only
Props: `Camera Camera`, `Transform CameraRig`, `bool ShakeEnabled {get;set;}`, `Ray AimRay`.
Methods: `Initialize(PlayerController)`, `SetLookInput(Vector2)`, `OnJump()`, `OnLand()`,
`FovSurge(float)`, `Shake(float magnitude, float duration, ShakeType type=Random, Vector3 direction=default)`,
`DeathTilt()`, `ResetForRespawn()`.

## WeaponController (MonoBehaviour)
Props: `bool IsAiming`, `bool IsReloading`, `WeaponDataSO ActiveWeapon`, `int Ammo`, `int MagSize`.
Events: `Action<int,int> OnAmmoChanged` (mag,size); `Action OnFired`; `Action<bool> OnReloadStateChanged`;
`Action<DamageResult,Vector3> OnHitConfirmed` (result, hitPoint); `Action OnWeaponSwapped`.
Methods: `Initialize(PlayerController, CharacterDataSO)`, `SetFiring(bool)`, `SetADS(bool)`,
`Reload()`, `SwapWeapon()`, `ResetForRespawn()`.

## WeaponDataSO (ScriptableObject)
Fields incl: `string weaponName; int damageBody; float headshotMultiplier; int pelletsPerShot;
float spreadAngle; float fireRateRPM; bool isFullAuto; bool isBurst; int burstCount;
float burstFireRateRPM; float burstDelay; int magazineSize; float reloadTime; float effectiveRange;
bool steepFalloff; float moveSpeedWhileFiring; Vector2[] recoilPattern; float recoilRecoveryTime;
float cameraKick;`
Methods: `float SecondsBetweenShots`, `float BurstSecondsBetweenRounds`, `float FalloffMultiplier(float dist)`,
`int ComputeDamage(float distance, bool headshot, float momentumDamageMultiplier)`.

## CharacterDataSO (ScriptableObject)
Fields: `ClassId classId; string characterName; string tagline; int difficulty; int baseHP;
float baseMoveSpeed; float sprintMultiplier; float jumpHeight; WeaponDataSO primaryWeapon;
WeaponDataSO secondaryWeapon; AbilityDataSO active1, active2, passive, momentumPassive;
Color primaryColor; Color accentColor; GameObject playerModelPrefab;` Prop: `float SprintSpeed`.

## AbilityDataSO (ScriptableObject)
`enum AbilityKind { Active, Passive, MomentumPassive }`. Fields: `string abilityName; string description;
Sprite icon; AbilityKind kind; float cooldown; float duration; float range; float radius;
float magnitude; float magnitudeSecondary; GameObject effectPrefab; GameObject activationVfx;
string sfxActivate, sfxImpact, sfxLoop;`

## AbilityBase (abstract MonoBehaviour) - base for ALL 16 ability behaviours
Props: `AbilityDataSO Data`, `float Cooldown`. Protected: `PlayerController Owner`, `Ray AimRay`,
`static int PlacementMask`, `void PlayActivationFeedback()`, `static GameObject SpawnTimed(prefab,pos,rot,lifetime)`.
Overridables:
- `virtual void Bind(PlayerController owner, AbilityDataSO config)` (calls OnBind)
- `protected virtual void OnBind()` - passives subscribe to GameEvents here
- `virtual void Unbind()` - unsubscribe here
- `virtual bool Activate(PlayerController activator)` - return true if it fired (actives only)
- `virtual void Cancel()`

## AbilityController (MonoBehaviour)
Events: `Action<int,float> OnCooldownChanged` (slot, fraction 0..1); `Action<int> OnAbilityActivated`.
Methods: `Initialize(PlayerController, CharacterDataSO)`, `AbilityBase GetActive(int slot)`,
`bool IsReady(int slot)`, `float CooldownRemaining(int)`, `float CooldownFraction(int)`,
`bool ActivateAbility(int slot)`, `ResetCooldowns()`.

## AbilityFactory (static)
`AbilityBase Attach(GameObject playerObject, AbilityDataSO config)` - maps abilityName → component type.
The 16 concrete classes MUST be named EXACTLY (these are referenced in AbilityFactory.Map):
Carrot: `Ability_SprintDash`, `Ability_RadarPulse`, `Passive_SilentSteps`, `MomentumPassive_Backstab`
Jalapeño: `Ability_SpiceBurst`, `Ability_HeatTrail`, `Passive_BurnStreak`, `MomentumPassive_ExtendedStreak`
Broccoli: `Ability_LeafShield`, `Ability_SporeCloud`, `Passive_RegenAura`, `MomentumPassive_SharedHarvest`
Potato: `Ability_StarchArmor`, `Ability_EarthenSlam`, `Passive_ThickSkin`, `MomentumPassive_StubbornRoot`
All extend `AbilityBase`, live in namespace `CarrotClash`, in `Assets/_Game/Characters/Abilities/Impl/`.
NOTE: Some passives (SilentSteps, ThickSkin) are read directly by PlayerMovement/AudioManager via
`PlayerController.ClassId` checks - their behaviour component can be a thin marker that exists for the
factory + future tuning. Implement them as real components but they may have minimal Activate logic.

## EffectSystem (static)
- `ApplySlow(PlayerController target, float amount, float duration)`
- `ApplyFire(PlayerController target, float dps, float duration, PlayerController instigator)`
- `ApplyKnockback(PlayerController target, Vector3 sourcePosition, float distance, float stagger)`
- `ApplyTemporaryHP(PlayerController target, int amount, float duration)`

## CaptureZone (MonoBehaviour)
Fields: `ZoneId zoneId; float captureRadius; float captureSeconds; int scoreTickRate; float lockUntilMatchTime;`
Props: `Team OwningTeam`, `float Progress`, `Team ProgressTeam`, `bool IsUnlocked`, `bool IsLocked`.
Methods: `int CountTeam(Team)`.

## GameModeManager (MonoBehaviour singleton)
`static GameModeManager Instance`. `MatchStats Stats`.
Props: `MatchState State`, `float ElapsedTime`, `float RemainingTime`, `int ScoreCap`,
`IReadOnlyList<PlayerController> Players`, `float StateTimeRemaining`.
Methods: `int GetScore(Team)`, `RegisterPlayer(pc)`, `UnregisterPlayer(pc)`, `RegisterZone(z)`,
`BeginMatchFlow()`, `AddScore(Team, int)`, `AddKillScore(Team killerTeam, MomentumTier victimTier)`,
`EndMatch(Team winner)`.

## MatchStats (plain class)
`class PlayerStats { int PlayerId; Team Team; ClassId Class; string DisplayName; bool IsLocal, IsBot;
int Kills, Assists, Deaths, DamageDealt; float ObjectiveTime; int ZoneCaptures; float LongestOnFireStreak;
int ScoreContribution; }`
Methods: `IReadOnlyDictionary<int,PlayerStats> All`, `PlayerStats GetOrCreate(pc)`, `Subscribe()`,
`Unsubscribe()`, `AddObjectiveTime(pc, float dt)`, `PlayerStats ResolveMvp()`, `PlayerStats ResolveHotStreak()`, `Clear()`.

## SpawnManager (MonoBehaviour singleton)
`static SpawnManager Instance`. `(Vector3, Quaternion) GetInitialSpawn(Team team, int index)`.
`class SpawnPoint : MonoBehaviour { Team team; }`. Auto-respawns on GameEvents.OnPlayerDied.

## SettingsService (static, PlayerPrefs-backed)
`event Action OnSettingsChanged`. Props (get/set): FieldOfView, TargetFrameRate(int), VSync(bool),
QualityLevel(int), MasterVolume/MusicVolume/SfxVolume/VoiceVolume (0..1), SpatialAudio(bool),
Subtitles(bool), MouseSensitivity, MouseSensitivityY, AdsSensMultiplier, AimAssist(bool),
ReduceMotion(bool), HighContrastHud(bool), Colorblind(ColorblindMode), TextScale(TextSize).
`ApplyVideoSettings()`.

## CarrotClash.Audio.AudioManager (MonoBehaviour singleton)
`static AudioManager Instance`. Methods: `PlaySfx(string key, Vector3 pos, float volumeScale=1)`,
`PlayUi(string key, float volumeScale=1)`, `StartLoop(string key, Vector3 pos, bool spatial=true)`,
`StopLoop(string key)`, `PlayMusic(AudioClip base, AudioClip intensity, AudioClip tier3)`,
`SetMusicParameter(string param, float value)`, `StopMusic()`. Resolve via `CarrotClash.Audio.AudioManager.Instance?`.

## CarrotClash.Audio.AudioLibrary (ScriptableObject)
`AudioClip Resolve(string key)` (random variant, null if missing).

---

## SFX key conventions (use these strings)
Weapon: `weapon_{weaponName}_fire`, `weapon_{weaponName}_reload`, `impact_surface`, `impact_air`.
Abilities resolve via AbilityDataSO.sfxActivate/sfxImpact/sfxLoop (designer-set keys).
Momentum: `momentum_tier1`, `momentum_tier2`, `momentum_tier3`, `momentum_decay_warn`, `momentum_lost`, `momentum_absorb`.
Objectives: `zone_capture_complete`, `zone_c_unlock`. Timer: `warn_1min`, `warn_30s`. Match: `match_win`, `match_draw`.
UI: `ui_button_hover`, `ui_button_confirm`, `ui_match_found`, `ui_xp_tick`, `ui_challenge_complete`.
Footsteps: `footstep_{surface}` where surface ∈ {stone,wood,metal,dirt}.

## Networking note
`CarrotClash.Net` asmdef is guarded by `NETCODE_PRESENT` (defined only when NGO package installed).
ALL files in `Assets/_Game/Network/` MUST be wrapped in `#if NETCODE_PRESENT ... #endif` so the
project compiles without the package. Mirror components hold a reference to the local gameplay
component and push/pull NetworkVariables; gameplay logic stays in the spine.
