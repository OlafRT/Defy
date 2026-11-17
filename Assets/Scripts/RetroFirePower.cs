using UnityEngine;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace RetroPlatformer
{
    [RequireComponent(typeof(RetroPlayer), typeof(RetroMarioAnimation))]
    [DisallowMultipleComponent, DefaultExecutionOrder(200)]
    public sealed class RetroFirePower : MonoBehaviour
    {
        [Header("Fire Mario palettes (same order as normal poses)")]
        public Sprite[] normal, fire, green, dark;
        public Sprite throwPose;
        public Sprite[] ballFrames, burstFrames;
        [Header("Fireballs")]
        public int maxFireballs = 2;
        public float fireballSpeed = 15f, fireballGravity = 28f, reboundSpeed = 7f;
        public float throwPoseDuration = .12f, cooldown = .15f, flashDuration = .65f, flashFPS = 16f;
        public bool shiftAlsoShoots = true;
        RetroPlayer player;
        bool wasFire;
        float flashUntil, throwingUntil, nextShot;
        readonly List<RetroFireball> shots = new List<RetroFireball>();
        void Awake() { player = GetComponent<RetroPlayer>(); }
        void Update()
        {
            if (player.IsFire && !wasFire) { flashUntil = Time.time + flashDuration; throwingUntil = 0; }
            wasFire = player.IsFire;
            if (!player.IsFire || player.IsDead || !player.ControlEnabled || player.IsCrouching || Time.time < flashUntil) return;
            bool press = false;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null) press = k.xKey.wasPressedThisFrame || (shiftAlsoShoots && (k.leftShiftKey.wasPressedThisFrame || k.rightShiftKey.wasPressedThisFrame));
            if (Gamepad.current != null) press |= Gamepad.current.buttonWest.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            press = Input.GetKeyDown(KeyCode.X) || (shiftAlsoShoots && (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)));
#endif
            if (press) TryShoot();
        }
        public void TryShoot()
        {
            if (!player.IsFire || player.IsDead || !player.ControlEnabled || player.IsCrouching || Time.time < flashUntil || Time.time < nextShot) return;
            shots.RemoveAll(s => !s || s.Finished);
            if (shots.Count >= Mathf.Max(0, maxFireballs) || ballFrames == null || ballFrames.Length == 0 || !ballFrames[0]) return;
            int dir = player.Facing;
            // Spawn on the inside of Mario's front edge; the first cast tests the entire exit path.
            var go = new GameObject("Fireball"); go.transform.position = transform.position + new Vector3(dir * .23f, 1.25f, 0);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = ballFrames[0]; sr.sortingOrder = 5;
            if (player.artwork) sr.sharedMaterial = player.artwork.sharedMaterial;
            var shot = go.AddComponent<RetroFireball>();
            shot.Initialize(player, sr, ballFrames, burstFrames, dir, fireballSpeed, fireballGravity, reboundSpeed);
            shots.Add(shot); nextShot = Time.time + cooldown; throwingUntil = Time.time + throwPoseDuration;
            RetroAudio.Play(RetroAudio.Cue.Fireball);
        }
        void LateUpdate()
        {
            if (!player.IsFire || player.IsDead || !player.artwork) return;
            var sr = player.artwork;
            int idx = System.Array.IndexOf(normal ?? System.Array.Empty<Sprite>(), sr.sprite);
            Sprite pose = null;
            if (Time.time < flashUntil)
            {
                int i = Mathf.FloorToInt((flashDuration - (flashUntil - Time.time)) * Mathf.Max(1, flashFPS)) % 4;
                var palette = i == 0 ? normal : i == 1 ? green : i == 2 ? dark : fire;
                pose = Frame(palette, idx >= 0 ? idx : 0);
            }
            else if (Time.time < throwingUntil && throwPose && !player.IsCrouching)
            { pose = throwPose; sr.flipX = player.Facing < 0; }
            else pose = Frame(fire, idx >= 0 ? idx : 0);
            if (!pose) return;
            sr.sprite = pose;
            if (sr.transform.parent == transform)
            {
                var a = GetComponent<RetroMarioAnimation>();
                sr.transform.localPosition = new Vector3(a.visualOffset.x, a.visualOffset.y - pose.bounds.min.y * sr.transform.localScale.y, sr.transform.localPosition.z);
            }
        }
        static Sprite Frame(Sprite[] arr, int index) => arr != null && index >= 0 && index < arr.Length ? arr[index] : null;
    }
}
