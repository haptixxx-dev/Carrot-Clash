using UnityEngine;

namespace CarrotClash
{
    /// <summary>
    /// Phase-1 dummy target brain. Wanders aimlessly by picking a new random heading every few
    /// seconds and slowly panning its view; never fires and never uses abilities. Used as a moving
    /// target for solo testing and for the momentum tutorial.
    /// </summary>
    public class DummyBrain : IBotBrain
    {
        const float RepathInterval = 3f;
        const float LookSpeed = 12f;

        Vector2 moveInput;
        float lookYaw;
        float repathTimer;

        public DummyBrain()
        {
            PickNewHeading();
        }

        public void Tick(PlayerController self)
        {
            repathTimer -= Time.deltaTime;
            if (repathTimer <= 0f)
                PickNewHeading();

            // Gentle continuous pan so the dummy looks "alive" without ever aiming at anyone.
            lookYaw = LookSpeed * Time.deltaTime;
        }

        void PickNewHeading()
        {
            repathTimer = RepathInterval + Random.value * RepathInterval;
            // Bias toward forward motion; occasionally idle.
            if (Random.value < 0.25f)
                moveInput = Vector2.zero;
            else
                moveInput = new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(0.3f, 1f));
        }

        public Vector2 GetMoveInput() => moveInput;
        public Vector2 GetLookInput() => new Vector2(lookYaw, 0f);
        public bool GetFireInput() => false;
        public bool GetAbility1Input() => false;
        public bool GetAbility2Input() => false;
    }
}
