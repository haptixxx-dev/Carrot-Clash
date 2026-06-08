using System.IO;
using UnityEditor;
using UnityEngine;

namespace CarrotClash.EditorTools
{
    /// <summary>
    /// One-shot authoring tool that materialises every gameplay data asset (weapons, characters,
    /// abilities, momentum config) with the exact numbers from the design docs. Re-runnable: existing
    /// assets at the target paths are loaded and overwritten in place so references stay intact.
    ///
    /// Source of truth: docs/weapons.md, docs/characters.md, docs/momentum-system.md.
    /// Ability names match <see cref="AbilityFactory"/> exactly (the factory keys on abilityName).
    /// </summary>
    public static class DataAssetGenerator
    {
        // ----- Asset folders (created if missing) -----
        const string WeaponDir = "Assets/_Game/Weapons/Data";
        const string CharacterDir = "Assets/_Game/Characters/Data";
        const string AbilityDir = "Assets/_Game/Characters/Abilities/Data";
        const string MomentumDir = "Assets/_Game/Gameplay/Momentum/Data";

        [MenuItem("Carrot Clash/Generate Default Data Assets")]
        public static void GenerateAll()
        {
            EnsureFolder(WeaponDir);
            EnsureFolder(CharacterDir);
            EnsureFolder(AbilityDir);
            EnsureFolder(MomentumDir);

            AssetDatabase.StartAssetEditing();
            try
            {
                // ----- Weapons (docs/weapons.md) -----
                WeaponDataSO nub = MakeNub();
                WeaponDataSO pepper = MakePepperBlaster();
                WeaponDataSO stem = MakeStem();
                WeaponDataSO root = MakeRootCannon();
                WeaponDataSO pip = MakePip();

                // ----- Abilities (docs/characters.md; names per AbilityFactory) -----
                // Carrot
                AbilityDataSO sprintDash = MakeSprintDash();
                AbilityDataSO radarPulse = MakeRadarPulse();
                AbilityDataSO silentSteps = MakeSilentSteps();
                AbilityDataSO backstab = MakeBackstabMomentum();
                // Jalapeño
                AbilityDataSO spiceBurst = MakeSpiceBurst();
                AbilityDataSO heatTrail = MakeHeatTrail();
                AbilityDataSO burnStreak = MakeBurnStreak();
                AbilityDataSO extendedStreak = MakeExtendedStreak();
                // Broccoli
                AbilityDataSO leafShield = MakeLeafShield();
                AbilityDataSO sporeCloud = MakeSporeCloud();
                AbilityDataSO regenAura = MakeRegenAura();
                AbilityDataSO sharedHarvest = MakeSharedHarvest();
                // Potato
                AbilityDataSO starchArmor = MakeStarchArmor();
                AbilityDataSO earthenSlam = MakeEarthenSlam();
                AbilityDataSO thickSkin = MakeThickSkin();
                AbilityDataSO stubbornRoot = MakeStubbornRoot();

                // ----- Characters (docs/characters.md) -----
                MakeCarrot(nub, pip, sprintDash, radarPulse, silentSteps, backstab);
                MakeJalapeno(pepper, pip, spiceBurst, heatTrail, burnStreak, extendedStreak);
                MakeBroccoli(stem, pip, leafShield, sporeCloud, regenAura, sharedHarvest);
                MakePotato(root, pip, starchArmor, earthenSlam, thickSkin, stubbornRoot);

                // ----- Momentum config (docs/momentum-system.md) -----
                MakeMomentumConfig();
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DataAssetGenerator] Generated 5 weapons, 16 abilities, 4 characters, 1 momentum config.");
        }

        // =====================================================================
        // Weapons (docs/weapons.md)
        // =====================================================================

        static WeaponDataSO MakeNub()
        {
            WeaponDataSO w = LoadOrCreate<WeaponDataSO>(WeaponDir, "Weapon_TheNub");
            w.weaponName = "The Nub";
            w.damageBody = 18;
            w.headshotMultiplier = 1.5f;
            w.pelletsPerShot = 1;
            w.spreadAngle = 1.5f;
            w.fireRateRPM = 750f;
            w.isFullAuto = true;
            w.isBurst = false;
            w.burstCount = 3;
            w.burstFireRateRPM = 1200f;
            w.burstDelay = 0.85f;
            w.magazineSize = 30;
            w.reloadTime = 1.8f;
            w.effectiveRange = 25f;
            w.steepFalloff = false;
            w.moveSpeedWhileFiring = 1f;
            w.recoilPattern = SmgRecoil();
            w.recoilRecoveryTime = 0.2f;
            w.cameraKick = 0.18f;
            Save(w);
            return w;
        }

        static WeaponDataSO MakePepperBlaster()
        {
            WeaponDataSO w = LoadOrCreate<WeaponDataSO>(WeaponDir, "Weapon_PepperBlaster");
            w.weaponName = "Pepper Blaster";
            w.damageBody = 12;            // per pellet
            w.headshotMultiplier = 1.25f;
            w.pelletsPerShot = 8;
            w.spreadAngle = 6f;
            w.fireRateRPM = 100f;         // semi-auto
            w.isFullAuto = false;
            w.isBurst = false;
            w.magazineSize = 6;
            w.reloadTime = 2.6f;
            w.effectiveRange = 12f;
            w.steepFalloff = true;        // 50% at +3m
            w.moveSpeedWhileFiring = 1f;
            w.recoilPattern = ShotgunRecoil();
            w.recoilRecoveryTime = 0.3f;
            w.cameraKick = 0.45f;
            Save(w);
            return w;
        }

        static WeaponDataSO MakeStem()
        {
            WeaponDataSO w = LoadOrCreate<WeaponDataSO>(WeaponDir, "Weapon_TheStem");
            w.weaponName = "The Stem";
            w.damageBody = 22;
            w.headshotMultiplier = 1.5f;
            w.pelletsPerShot = 1;
            w.spreadAngle = 1f;
            w.fireRateRPM = 1200f;        // within burst (kept consistent with burstFireRateRPM)
            w.isFullAuto = false;
            w.isBurst = true;
            w.burstCount = 3;
            w.burstFireRateRPM = 1200f;
            w.burstDelay = 0.85f;
            w.magazineSize = 24;          // 8 bursts
            w.reloadTime = 2.2f;
            w.effectiveRange = 45f;
            w.steepFalloff = false;
            w.moveSpeedWhileFiring = 1f;
            w.recoilPattern = BurstRecoil();
            w.recoilRecoveryTime = 0.2f;
            w.cameraKick = 0.25f;
            Save(w);
            return w;
        }

        static WeaponDataSO MakeRootCannon()
        {
            WeaponDataSO w = LoadOrCreate<WeaponDataSO>(WeaponDir, "Weapon_RootCannon");
            w.weaponName = "Root Cannon";
            w.damageBody = 20;
            w.headshotMultiplier = 1.3f;
            w.pelletsPerShot = 1;
            w.spreadAngle = 2.5f;
            w.fireRateRPM = 400f;
            w.isFullAuto = true;
            w.isBurst = false;
            w.magazineSize = 60;
            w.reloadTime = 3.5f;
            w.effectiveRange = 35f;
            w.steepFalloff = false;
            w.moveSpeedWhileFiring = 0.85f;   // -15% move speed while firing
            w.recoilPattern = LmgRecoil();
            w.recoilRecoveryTime = 0.25f;
            w.cameraKick = 0.3f;
            Save(w);
            return w;
        }

        static WeaponDataSO MakePip()
        {
            WeaponDataSO w = LoadOrCreate<WeaponDataSO>(WeaponDir, "Weapon_ThePip");
            w.weaponName = "The Pip";
            w.damageBody = 28;
            w.headshotMultiplier = 2.0f;
            w.pelletsPerShot = 1;
            w.spreadAngle = 1f;
            w.fireRateRPM = 300f;         // semi-auto
            w.isFullAuto = false;
            w.isBurst = false;
            w.magazineSize = 12;
            w.reloadTime = 1.5f;
            w.effectiveRange = 20f;
            w.steepFalloff = false;
            w.moveSpeedWhileFiring = 1f;
            w.recoilPattern = PistolRecoil();
            w.recoilRecoveryTime = 0.15f;
            w.cameraKick = 0.35f;
            Save(w);
            return w;
        }

        // ----- Placeholder learnable recoil patterns (Apex-style, deterministic) -----
        static Vector2[] SmgRecoil() => new[]
        {
            new Vector2(0f, 0.5f), new Vector2(0.1f, 0.6f), new Vector2(-0.1f, 0.7f),
            new Vector2(0.15f, 0.65f), new Vector2(-0.2f, 0.6f), new Vector2(0.2f, 0.55f),
        };

        static Vector2[] ShotgunRecoil() => new[]
        {
            new Vector2(0f, 1.4f), new Vector2(0.2f, 1.3f),
        };

        static Vector2[] BurstRecoil() => new[]
        {
            new Vector2(0f, 0.8f), new Vector2(0.1f, 0.9f), new Vector2(-0.1f, 1.0f),
        };

        static Vector2[] LmgRecoil() => new[]
        {
            new Vector2(0f, 0.7f), new Vector2(0.2f, 0.8f), new Vector2(-0.25f, 0.85f),
            new Vector2(0.3f, 0.8f), new Vector2(-0.3f, 0.75f),
        };

        static Vector2[] PistolRecoil() => new[]
        {
            new Vector2(0f, 0.9f), new Vector2(0.05f, 0.85f),
        };

        // =====================================================================
        // Abilities (docs/characters.md) — names MUST match AbilityFactory.Map
        // =====================================================================

        // ----- Carrot -----
        static AbilityDataSO MakeSprintDash()
        {
            AbilityDataSO a = LoadOrCreate<AbilityDataSO>(AbilityDir, "Ability_SprintDash");
            a.abilityName = "Sprint Dash";
            a.description = "Instant directional dash; usable mid-air; preserves momentum.";
            a.kind = AbilityDataSO.AbilityKind.Active;
            a.cooldown = 6f;
            a.duration = 0.08f;     // ~0.08s dash duration
            a.range = 8f;           // dash distance (m)
            a.radius = 0f;
            a.magnitude = 8f;       // dash force / distance
            a.magnitudeSecondary = 0f;
            a.sfxActivate = "ability_carrot_dash";
            Save(a);
            return a;
        }

        static AbilityDataSO MakeRadarPulse()
        {
            AbilityDataSO a = LoadOrCreate<AbilityDataSO>(AbilityDir, "Ability_RadarPulse");
            a.abilityName = "Radar Pulse";
            a.description = "Reveals all enemies within 15m through walls for 2.5s.";
            a.kind = AbilityDataSO.AbilityKind.Active;
            a.cooldown = 18f;
            a.duration = 2.5f;      // reveal duration
            a.range = 15f;          // reveal radius
            a.radius = 15f;
            a.magnitude = 0f;
            a.sfxActivate = "ability_carrot_radar";
            Save(a);
            return a;
        }

        static AbilityDataSO MakeSilentSteps()
        {
            AbilityDataSO a = LoadOrCreate<AbilityDataSO>(AbilityDir, "Passive_SilentSteps");
            a.abilityName = "Silent Steps";
            a.description = "Footstep SFX reduced to 0 when moving below 50% sprint speed.";
            a.kind = AbilityDataSO.AbilityKind.Passive;
            a.cooldown = 0f;
            a.duration = 0f;
            a.range = 0f;
            a.radius = 0f;
            a.magnitude = GameConstants.SilentStepsSpeedFraction; // 0.5
            Save(a);
            return a;
        }

        static AbilityDataSO MakeBackstabMomentum()
        {
            AbilityDataSO a = LoadOrCreate<AbilityDataSO>(AbilityDir, "MomentumPassive_Backstab");
            a.abilityName = "Backstab Momentum";
            a.description = "Kills from a 180 degree rear arc grant +2 tiers instead of +1.";
            a.kind = AbilityDataSO.AbilityKind.MomentumPassive;
            a.cooldown = 0f;
            a.duration = 0f;
            a.range = 0f;
            a.radius = 0f;
            a.magnitude = 2f;       // tier gain on backstab
            a.magnitudeSecondary = GameConstants.CarrotBackstabDot; // -0.1 rear-arc dot
            Save(a);
            return a;
        }

        // ----- Jalapeño -----
        static AbilityDataSO MakeSpiceBurst()
        {
            AbilityDataSO a = LoadOrCreate<AbilityDataSO>(AbilityDir, "Ability_SpiceBurst");
            a.abilityName = "Spice Burst";
            a.description = "AOE spice grenade; slows enemies 35% for 3s and deals 15 damage.";
            a.kind = AbilityDataSO.AbilityKind.Active;
            a.cooldown = 12f;
            a.duration = GameConstants.SlowDuration;   // 3s slow
            a.range = 20f;          // throw range
            a.radius = 4f;          // AOE radius
            a.magnitude = GameConstants.SlowAmount;     // 0.35 slow
            a.magnitudeSecondary = 15f;                 // direct impact damage
            a.sfxActivate = "ability_jalapeno_spice";
            a.sfxImpact = "ability_jalapeno_spice_impact";
            Save(a);
            return a;
        }

        static AbilityDataSO MakeHeatTrail()
        {
            AbilityDataSO a = LoadOrCreate<AbilityDataSO>(AbilityDir, "Ability_HeatTrail");
            a.abilityName = "Heat Trail";
            a.description = "Drops fire behind you while moving; 20 DPS to enemies in the trail.";
            a.kind = AbilityDataSO.AbilityKind.Active;
            a.cooldown = 20f;
            a.duration = 3f;        // trail duration
            a.range = 15f;          // trail line length (m)
            a.radius = 0.5f;        // half-width (1m wide)
            a.magnitude = 20f;      // DPS
            a.sfxActivate = "ability_jalapeno_heattrail";
            a.sfxLoop = "ability_jalapeno_heattrail_loop";
            Save(a);
            return a;
        }

        static AbilityDataSO MakeBurnStreak()
        {
            AbilityDataSO a = LoadOrCreate<AbilityDataSO>(AbilityDir, "Passive_BurnStreak");
            a.abilityName = "Burn Streak";
            a.description = "Each kill ignites the victim; nearby enemies take 10 DPS for 2s.";
            a.kind = AbilityDataSO.AbilityKind.Passive;
            a.cooldown = 0f;
            a.duration = 2f;        // burn duration
            a.range = 0f;
            a.radius = 5f;          // aura radius
            a.magnitude = 10f;      // DPS
            a.sfxImpact = "ability_jalapeno_burn";
            Save(a);
            return a;
        }

        static AbilityDataSO MakeExtendedStreak()
        {
            AbilityDataSO a = LoadOrCreate<AbilityDataSO>(AbilityDir, "MomentumPassive_ExtendedStreak");
            a.abilityName = "Extended Streak";
            a.description = "Each kill adds +5s to the momentum decay timer.";
            a.kind = AbilityDataSO.AbilityKind.MomentumPassive;
            a.cooldown = 0f;
            a.duration = 0f;
            a.range = 0f;
            a.radius = 0f;
            a.magnitude = GameConstants.JalapenoDecayExtension; // +5s
            Save(a);
            return a;
        }

        // ----- Broccoli -----
        static AbilityDataSO MakeLeafShield()
        {
            AbilityDataSO a = LoadOrCreate<AbilityDataSO>(AbilityDir, "Ability_LeafShield");
            a.abilityName = "Leaf Shield";
            a.description = "Deploys a 1x2m destructible cover object with 80 HP that blocks bullets.";
            a.kind = AbilityDataSO.AbilityKind.Active;
            a.cooldown = 15f;
            a.duration = 0f;        // persists until destroyed
            a.range = 3f;           // placement range
            a.radius = 0f;
            a.magnitude = 80f;      // shield HP
            a.sfxActivate = "ability_broccoli_shield";
            Save(a);
            return a;
        }

        static AbilityDataSO MakeSporeCloud()
        {
            AbilityDataSO a = LoadOrCreate<AbilityDataSO>(AbilityDir, "Ability_SporeCloud");
            a.abilityName = "Spore Cloud";
            a.description = "Throwable vision-blocking fog cloud at impact.";
            a.kind = AbilityDataSO.AbilityKind.Active;
            a.cooldown = 18f;
            a.duration = 4f;        // cloud duration
            a.range = 20f;          // throw range
            a.radius = 6f;          // cloud radius
            a.magnitude = 0f;
            a.sfxActivate = "ability_broccoli_spore";
            a.sfxLoop = "ability_broccoli_spore_loop";
            Save(a);
            return a;
        }

        static AbilityDataSO MakeRegenAura()
        {
            AbilityDataSO a = LoadOrCreate<AbilityDataSO>(AbilityDir, "Passive_RegenAura");
            a.abilityName = "Regen Aura";
            a.description = "Allies (not Broccoli) within 8m regenerate 2 HP/s continuously.";
            a.kind = AbilityDataSO.AbilityKind.Passive;
            a.cooldown = 0f;
            a.duration = GameConstants.RegenAuraTick;       // 1s tick cadence
            a.range = 0f;
            a.radius = GameConstants.RegenAuraRadius;        // 8m
            a.magnitude = GameConstants.RegenAuraHeal;       // 2 HP/tick
            Save(a);
            return a;
        }

        static AbilityDataSO MakeSharedHarvest()
        {
            AbilityDataSO a = LoadOrCreate<AbilityDataSO>(AbilityDir, "MomentumPassive_SharedHarvest");
            a.abilityName = "Shared Harvest";
            a.description = "Nearby ally kills (within 10m) give Broccoli 10% of the kill charge.";
            a.kind = AbilityDataSO.AbilityKind.MomentumPassive;
            a.cooldown = 0f;
            a.duration = 0f;
            a.range = 0f;
            a.radius = GameConstants.BroccoliHarvestRadius;  // 10m
            a.magnitude = GameConstants.BroccoliHarvestShare; // 0.1 charge
            Save(a);
            return a;
        }

        // ----- Potato -----
        static AbilityDataSO MakeStarchArmor()
        {
            AbilityDataSO a = LoadOrCreate<AbilityDataSO>(AbilityDir, "Ability_StarchArmor");
            a.abilityName = "Starch Armor";
            a.description = "+40 temporary HP for 4s; absorbs before base HP.";
            a.kind = AbilityDataSO.AbilityKind.Active;
            a.cooldown = 20f;
            a.duration = GameConstants.StarchArmorDuration;  // 4s
            a.range = 0f;           // self
            a.radius = 0f;
            a.magnitude = GameConstants.StarchArmorAmount;   // 40 temp HP
            a.sfxActivate = "ability_potato_starch";
            Save(a);
            return a;
        }

        static AbilityDataSO MakeEarthenSlam()
        {
            AbilityDataSO a = LoadOrCreate<AbilityDataSO>(AbilityDir, "Ability_EarthenSlam");
            a.abilityName = "Earthen Slam";
            a.description = "Jump slam; knocks enemies back 6m and staggers them for 0.5s.";
            a.kind = AbilityDataSO.AbilityKind.Active;
            a.cooldown = 16f;
            a.duration = GameConstants.StaggerDuration;      // 0.5s stagger
            a.range = 0f;
            a.radius = 4f;          // AoE radius
            a.magnitude = GameConstants.KnockbackDistance;   // 6m knockback
            a.sfxActivate = "ability_potato_slam";
            a.sfxImpact = "ability_potato_slam_impact";
            Save(a);
            return a;
        }

        static AbilityDataSO MakeThickSkin()
        {
            AbilityDataSO a = LoadOrCreate<AbilityDataSO>(AbilityDir, "Passive_ThickSkin");
            a.abilityName = "Thick Skin";
            a.description = "-8% incoming damage while stationary for over 1 second.";
            a.kind = AbilityDataSO.AbilityKind.Passive;
            a.cooldown = 0f;
            a.duration = 1f;        // stationary threshold seconds
            a.range = 0f;
            a.radius = 0f;
            a.magnitude = 0.08f;    // damage reduction fraction
            Save(a);
            return a;
        }

        static AbilityDataSO MakeStubbornRoot()
        {
            AbilityDataSO a = LoadOrCreate<AbilityDataSO>(AbilityDir, "MomentumPassive_StubbornRoot");
            a.abilityName = "Stubborn Root";
            a.description = "On death, only 25% of momentum transfers to the killer instead of 50%.";
            a.kind = AbilityDataSO.AbilityKind.MomentumPassive;
            a.cooldown = 0f;
            a.duration = 0f;
            a.range = 0f;
            a.radius = 0f;
            a.magnitude = GameConstants.MomentumTransferPotato; // 0.25 transfer
            Save(a);
            return a;
        }

        // =====================================================================
        // Characters (docs/characters.md)
        // =====================================================================

        static void MakeCarrot(WeaponDataSO primary, WeaponDataSO secondary,
            AbilityDataSO a1, AbilityDataSO a2, AbilityDataSO passive, AbilityDataSO momentumPassive)
        {
            CharacterDataSO c = LoadOrCreate<CharacterDataSO>(CharacterDir, "Character_Carrot");
            c.classId = ClassId.Carrot;
            c.characterName = "Carrot";
            c.tagline = "Fast, quiet, and always behind you.";
            c.difficulty = 3;
            c.baseHP = 90;
            c.baseMoveSpeed = 7.5f;
            c.sprintMultiplier = 1.4f;  // 7.5 * 1.4 = 10.5 m/s sprint
            c.jumpHeight = 1.4f;
            c.primaryWeapon = primary;
            c.secondaryWeapon = secondary;
            c.active1 = a1;
            c.active2 = a2;
            c.passive = passive;
            c.momentumPassive = momentumPassive;
            c.primaryColor = new Color(1f, 0.5f, 0.1f);     // carrot orange
            c.accentColor = new Color(0.2f, 0.7f, 0.2f);    // leafy green
            Save(c);
        }

        static void MakeJalapeno(WeaponDataSO primary, WeaponDataSO secondary,
            AbilityDataSO a1, AbilityDataSO a2, AbilityDataSO passive, AbilityDataSO momentumPassive)
        {
            CharacterDataSO c = LoadOrCreate<CharacterDataSO>(CharacterDir, "Character_Jalapeno");
            c.classId = ClassId.Jalapeno;
            c.characterName = "Jalapeño";
            c.tagline = "Gets hotter the longer the fight goes.";
            c.difficulty = 2;
            c.baseHP = 110;
            c.baseMoveSpeed = 6.5f;
            c.sprintMultiplier = 1.4f;  // 6.5 * 1.4 = 9.1 m/s sprint
            c.jumpHeight = 1.2f;
            c.primaryWeapon = primary;
            c.secondaryWeapon = secondary;
            c.active1 = a1;
            c.active2 = a2;
            c.passive = passive;
            c.momentumPassive = momentumPassive;
            c.primaryColor = new Color(0.85f, 0.1f, 0.1f);  // hot red
            c.accentColor = new Color(0.95f, 0.6f, 0.1f);   // ember orange
            Save(c);
        }

        static void MakeBroccoli(WeaponDataSO primary, WeaponDataSO secondary,
            AbilityDataSO a1, AbilityDataSO a2, AbilityDataSO passive, AbilityDataSO momentumPassive)
        {
            CharacterDataSO c = LoadOrCreate<CharacterDataSO>(CharacterDir, "Character_Broccoli");
            c.classId = ClassId.Broccoli;
            c.characterName = "Broccoli";
            c.tagline = "Nobody picks Broccoli. Until they see what Broccoli can do.";
            c.difficulty = 4;
            c.baseHP = 100;
            c.baseMoveSpeed = 6.0f;
            c.sprintMultiplier = 1.4f;  // 6.0 * 1.4 = 8.4 m/s sprint
            c.jumpHeight = 1.2f;
            c.primaryWeapon = primary;
            c.secondaryWeapon = secondary;
            c.active1 = a1;
            c.active2 = a2;
            c.passive = passive;
            c.momentumPassive = momentumPassive;
            c.primaryColor = new Color(0.15f, 0.5f, 0.15f);  // deep green
            c.accentColor = new Color(0.55f, 0.8f, 0.35f);   // floret light green
            Save(c);
        }

        static void MakePotato(WeaponDataSO primary, WeaponDataSO secondary,
            AbilityDataSO a1, AbilityDataSO a2, AbilityDataSO passive, AbilityDataSO momentumPassive)
        {
            CharacterDataSO c = LoadOrCreate<CharacterDataSO>(CharacterDir, "Character_Potato");
            c.classId = ClassId.Potato;
            c.characterName = "Potato";
            c.tagline = "Hard to move. Harder to kill.";
            c.difficulty = 2;
            c.baseHP = 140;
            c.baseMoveSpeed = 5.0f;
            c.sprintMultiplier = 1.4f;  // 5.0 * 1.4 = 7.0 m/s sprint
            c.jumpHeight = 1.0f;
            c.primaryWeapon = primary;
            c.secondaryWeapon = secondary;
            c.active1 = a1;
            c.active2 = a2;
            c.passive = passive;
            c.momentumPassive = momentumPassive;
            c.primaryColor = new Color(0.6f, 0.45f, 0.25f);  // earthy brown
            c.accentColor = new Color(0.85f, 0.75f, 0.5f);   // tan
            Save(c);
        }

        // =====================================================================
        // Momentum config (docs/momentum-system.md tier table)
        // =====================================================================

        static void MakeMomentumConfig()
        {
            MomentumConfigSO cfg = LoadOrCreate<MomentumConfigSO>(MomentumDir, "MomentumConfig");
            cfg.decayIntervalSeconds = GameConstants.MomentumDecayInterval;     // 12s
            cfg.transferOnDeath = GameConstants.MomentumTransferOnDeath;        // 0.5

            // Tier table: speed 0/.1/.2/.3, cd 0/.1/.2/.3, dmg 0/0/.1/.2, killscore 5/5/5/8.
            // Colours per momentum-system UI spec: grey / class-colour stand-in / bright warm / orange-white.
            MomentumTierData[] tiers = new MomentumTierData[4];

            tiers[0] = new MomentumTierData
            {
                moveSpeedBonus = 0f,
                cooldownReduction = 0f,
                damageBonus = 0f,
                teamScoreKillValue = 5,
                vfxColor = new Color(0.6f, 0.6f, 0.6f, 1f),   // Cold: grey
            };
            tiers[1] = new MomentumTierData
            {
                moveSpeedBonus = 0.1f,
                cooldownReduction = 0.1f,
                damageBonus = 0f,
                teamScoreKillValue = 5,
                vfxColor = new Color(1f, 0.65f, 0.2f, 1f),    // Warm: warm glow (class colour at runtime)
            };
            tiers[2] = new MomentumTierData
            {
                moveSpeedBonus = 0.2f,
                cooldownReduction = 0.2f,
                damageBonus = 0.1f,
                teamScoreKillValue = 5,
                vfxColor = new Color(1f, 0.45f, 0.05f, 1f),   // Hot: bright warm
            };
            tiers[3] = new MomentumTierData
            {
                moveSpeedBonus = 0.3f,
                cooldownReduction = 0.3f,
                damageBonus = 0.2f,
                teamScoreKillValue = 8,
                vfxColor = new Color(1f, 0.85f, 0.4f, 1f),    // On Fire: animated orange/white pulse
            };

            cfg.tiers = tiers;
            Save(cfg);
        }

        // =====================================================================
        // Asset IO helpers
        // =====================================================================

        static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath)) return;

            string[] parts = assetPath.Split('/');
            string current = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        /// <summary>Load the asset at dir/name.asset, or create a fresh instance + asset if absent.</summary>
        static T LoadOrCreate<T>(string dir, string name) where T : ScriptableObject
        {
            string path = dir + "/" + name + ".asset";
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            T instance = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(instance, path);
            return instance;
        }

        /// <summary>Mark a (possibly pre-existing) asset dirty so edits are persisted on SaveAssets.</summary>
        static void Save(ScriptableObject so)
        {
            EditorUtility.SetDirty(so);
        }
    }
}
