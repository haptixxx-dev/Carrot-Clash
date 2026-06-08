using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CarrotClash.UI
{
    /// <summary>
    /// Bottom-right ammo readout. Listens to <see cref="WeaponController.OnAmmoChanged"/> and
    /// <see cref="WeaponController.OnReloadStateChanged"/>. Shows "mag / size"; the count turns
    /// orange below 30% of the magazine and red with a flashing "RELOAD" prompt at 0. While
    /// reloading, a small spinning icon is shown (UI/UX HUD spec).
    /// </summary>
    [DisallowMultipleComponent]
    public class AmmoUI : MonoBehaviour
    {
        [Header("Labels")]
        [SerializeField] TMP_Text ammoLabel;        // "28 / 30"
        [SerializeField] TMP_Text reloadLabel;      // "RELOAD" (toggled)

        [Header("Reload spinner")]
        [SerializeField] RectTransform reloadSpinner;   // optional; rotated while reloading
        [SerializeField] float spinnerSpeed = 360f;     // degrees / second

        [Header("Colours")]
        [SerializeField] Color normalColor = Color.white;
        [SerializeField] Color lowColor = new Color(1f, 0.55f, 0.1f);   // orange < 30%
        [SerializeField] Color emptyColor = new Color(0.95f, 0.2f, 0.2f);

        [Tooltip("Fraction of mag at/under which the count turns orange.")]
        [SerializeField] float lowFraction = 0.30f;
        [SerializeField] float reloadFlashHz = 4f;

        WeaponController weapon;
        int mag;
        int size = 1;
        bool reloading;

        /// <summary>Wire to the player's weapon controller and seed from its current ammo.</summary>
        public void Bind(WeaponController source)
        {
            Unbind();
            weapon = source;
            if (weapon == null) return;

            weapon.OnAmmoChanged += HandleAmmoChanged;
            weapon.OnReloadStateChanged += HandleReloadStateChanged;

            HandleAmmoChanged(weapon.Ammo, weapon.MagSize);
            HandleReloadStateChanged(weapon.IsReloading);
        }

        public void Unbind()
        {
            if (weapon == null) return;
            weapon.OnAmmoChanged -= HandleAmmoChanged;
            weapon.OnReloadStateChanged -= HandleReloadStateChanged;
            weapon = null;
        }

        void OnDisable() => Unbind();
        void OnDestroy() => Unbind();

        void HandleAmmoChanged(int magCount, int magSize)
        {
            mag = magCount;
            size = Mathf.Max(1, magSize);
            UpdateLabel();
        }

        void HandleReloadStateChanged(bool isReloading)
        {
            reloading = isReloading;
            if (reloadSpinner != null) reloadSpinner.gameObject.SetActive(reloading);
            if (reloadLabel != null && reloading && reloadLabel.gameObject.activeSelf)
                reloadLabel.text = "RELOADING";
            UpdateLabel();
        }

        void UpdateLabel()
        {
            if (ammoLabel != null)
            {
                ammoLabel.text = $"{mag} / {size}";

                float frac = size > 0 ? (float)mag / size : 0f;
                if (mag <= 0) ammoLabel.color = emptyColor;
                else if (frac <= lowFraction) ammoLabel.color = lowColor;
                else ammoLabel.color = normalColor;
            }

            if (reloadLabel != null)
            {
                bool showReload = mag <= 0 || reloading;
                reloadLabel.gameObject.SetActive(showReload);
                if (showReload && !reloading) reloadLabel.text = "RELOAD";
            }
        }

        void Update()
        {
            if (reloading && reloadSpinner != null)
                reloadSpinner.Rotate(0f, 0f, -spinnerSpeed * Time.deltaTime);

            // Flash the empty "RELOAD" prompt (not while actively reloading).
            if (reloadLabel != null && mag <= 0 && !reloading && !SettingsService.ReduceMotion)
            {
                float a = 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * Mathf.PI * reloadFlashHz));
                Color c = reloadLabel.color;
                c.a = a;
                reloadLabel.color = c;
            }
            else if (reloadLabel != null && reloadLabel.gameObject.activeSelf)
            {
                Color c = reloadLabel.color;
                c.a = 1f;
                reloadLabel.color = c;
            }
        }
    }
}
