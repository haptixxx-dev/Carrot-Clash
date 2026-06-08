using System;
using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// Hitscan weapon handling: fire timing (full-auto / semi / burst), spread, falloff, headshots,
    /// reload, auto-refilling ammo, ADS, weapon swap (primary &lt;-&gt; pistol), and feel hooks
    /// (camera kick, hit-stop request, hit markers). Server-authoritative damage in networked play;
    /// locally authoritative offline.
    /// </summary>
    [DisallowMultipleComponent]
    public class WeaponController : MonoBehaviour
    {
        [Header("Muzzle / origin")]
        [SerializeField] Transform muzzle;

        PlayerController owner;
        WeaponDataSO primary;
        WeaponDataSO secondary;
        WeaponDataSO active;

        int ammoInMag;
        bool firingHeld;
        bool isAiming;
        bool isReloading;
        bool usingSecondary;

        float nextFireTime;
        float reloadFinishTime;
        float swapReadyTime;
        int burstRoundsRemaining;
        int recoilIndex;

        // ----- Events for HUD / feedback -----
        public event Action<int, int> OnAmmoChanged;       // mag, magSize
        public event Action OnFired;
        public event Action<bool> OnReloadStateChanged;    // reloading?
        public event Action<DamageResult, Vector3> OnHitConfirmed; // result, hit point (local feedback)
        public event Action OnWeaponSwapped;

        public bool IsAiming => isAiming;
        public bool IsReloading => isReloading;
        public WeaponDataSO ActiveWeapon => active;
        public int Ammo => ammoInMag;
        public int MagSize => active != null ? active.magazineSize : 0;

        public void Initialize(PlayerController player, CharacterDataSO data)
        {
            owner = player;
            primary = data.primaryWeapon;
            secondary = data.secondaryWeapon;
            active = primary;
            usingSecondary = false;
            ammoInMag = active != null ? active.magazineSize : 0;
            isReloading = false;
            isAiming = false;
            OnAmmoChanged?.Invoke(ammoInMag, MagSize);
        }

        public void SetFiring(bool held) => firingHeld = held;
        public void SetADS(bool held) => isAiming = held && !isReloading;

        void Update()
        {
            if (owner == null || active == null) return;
            if (owner.health != null && owner.health.IsDead) return;

            if (isReloading && Time.time >= reloadFinishTime)
                FinishReload();

            // Potato LMG move penalty while actively firing.
            if (owner.movement != null)
                owner.movement.SetFiring(firingHeld && !isReloading ? active.moveSpeedWhileFiring : 1f);

            if (!firingHeld || isReloading || Time.time < swapReadyTime) return;
            if (owner.movement != null && owner.movement.IsStaggered) return;

            TryFire();
        }

        void TryFire()
        {
            if (Time.time < nextFireTime) return;

            if (ammoInMag <= 0)
            {
                Reload();
                return;
            }

            if (active.isBurst)
            {
                FireBurst();
            }
            else
            {
                FireOnce();
                nextFireTime = Time.time + active.SecondsBetweenShots;
                if (!active.isFullAuto) firingHeld = false; // semi-auto: one shot per press
            }
        }

        void FireBurst()
        {
            burstRoundsRemaining = Mathf.Min(active.burstCount, ammoInMag);
            StartCoroutine(BurstRoutine());
            nextFireTime = Time.time + active.burstDelay;
            firingHeld = active.isFullAuto ? firingHeld : false;
        }

        System.Collections.IEnumerator BurstRoutine()
        {
            while (burstRoundsRemaining > 0 && ammoInMag > 0)
            {
                FireOnce();
                burstRoundsRemaining--;
                yield return new WaitForSeconds(active.BurstSecondsBetweenRounds);
            }
        }

        void FireOnce()
        {
            if (ammoInMag <= 0) { Reload(); return; }
            ammoInMag--;
            OnAmmoChanged?.Invoke(ammoInMag, MagSize);
            OnFired?.Invoke();

            float momentumDmgMult = owner.momentum != null ? owner.momentum.DamageMultiplier : 1f;
            int pellets = Mathf.Max(1, active.pelletsPerShot);

            for (int i = 0; i < pellets; i++)
                FirePellet(momentumDmgMult, pellets > 1 || !isAiming);

            ApplyRecoil();

            if (!string.IsNullOrEmpty(active.weaponName))
                Audio.AudioManager.Instance?.PlaySfx($"weapon_{active.weaponName}_fire", muzzle != null ? muzzle.position : transform.position);
        }

        void FirePellet(float momentumDmgMult, bool applySpread)
        {
            Ray ray = owner.cam != null ? owner.cam.AimRay : new Ray(transform.position, transform.forward);

            if (applySpread && active.spreadAngle > 0f)
            {
                float a = active.spreadAngle * (isAiming ? 0.25f : 1f);
                Quaternion spread = Quaternion.Euler(
                    UnityEngine.Random.Range(-a, a),
                    UnityEngine.Random.Range(-a, a), 0f);
                ray.direction = spread * ray.direction;
            }

            // Networked builds rewind hitboxes server-side; offline we raycast directly.
            float maxDist = active.effectiveRange + 25f;
            int mask = LayerMask.GetMask(GameConstants.LayerEnvironment, GameConstants.LayerPlayer, GameConstants.LayerHitbox);

            if (!Physics.Raycast(ray, out RaycastHit hit, maxDist, mask, QueryTriggerInteraction.Ignore))
            {
                Audio.AudioManager.Instance?.PlaySfx("impact_air", ray.origin + ray.direction * maxDist);
                return;
            }

            var hitbox = hit.collider.GetComponentInParent<PlayerHitbox>();
            if (hitbox != null && hitbox.Owner != null && hitbox.Owner != owner)
            {
                // friendly fire off: ignore allies
                if (!owner.Team.IsEnemyOf(hitbox.Team))
                    return;

                float dist = hit.distance;
                bool headshot = hitbox.IsHead;
                int dmg = active.ComputeDamage(dist, headshot, momentumDmgMult);

                var info = new DamageInfo(dmg, DamageType.Bullet, owner, hit.point, ray.direction, headshot);
                DamageResult result = hitbox.Owner.ApplyDamage(info);
                OnHitConfirmed?.Invoke(result, hit.point);
            }
            else
            {
                Audio.AudioManager.Instance?.PlaySfx("impact_surface", hit.point);
            }
        }

        void ApplyRecoil()
        {
            if (owner.cam == null) return;
            owner.cam.Shake(active.cameraKick * 0.2f, 0.05f, ShakeType.Directional, Vector3.up);
            if (active.recoilPattern != null && active.recoilPattern.Length > 0)
            {
                recoilIndex = (recoilIndex + 1) % active.recoilPattern.Length;
                // The pattern is consumed by camera pitch/yaw nudge; kept minimal here.
            }
        }

        public void Reload()
        {
            if (isReloading || active == null) return;
            if (ammoInMag >= active.magazineSize) return;
            isReloading = true;
            reloadFinishTime = Time.time + active.reloadTime;
            isAiming = false;
            OnReloadStateChanged?.Invoke(true);
            Audio.AudioManager.Instance?.PlaySfx($"weapon_{active.weaponName}_reload", transform.position);
        }

        void FinishReload()
        {
            isReloading = false;
            // Infinite reserve ammo economy (Weapons doc): mag simply refills.
            ammoInMag = active.magazineSize;
            recoilIndex = 0;
            OnReloadStateChanged?.Invoke(false);
            OnAmmoChanged?.Invoke(ammoInMag, MagSize);
        }

        /// <summary>Swap between primary and the shared pistol. Faster than reloading (doc: 0.2s swap).</summary>
        public void SwapWeapon()
        {
            if (secondary == null) return;
            usingSecondary = !usingSecondary;
            active = usingSecondary ? secondary : primary;
            ammoInMag = active.magazineSize; // each weapon tracks its own; simplified to full on swap-in
            isReloading = false;
            swapReadyTime = Time.time + GameConstants.WeaponSwapTime;
            OnWeaponSwapped?.Invoke();
            OnAmmoChanged?.Invoke(ammoInMag, MagSize);
        }

        public void ResetForRespawn()
        {
            usingSecondary = false;
            active = primary;
            ammoInMag = active != null ? active.magazineSize : 0;
            isReloading = false;
            isAiming = false;
            firingHeld = false;
            recoilIndex = 0;
            OnAmmoChanged?.Invoke(ammoInMag, MagSize);
        }
    }
}
