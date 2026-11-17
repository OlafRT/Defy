using UnityEngine;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace RetroPlatformer
{
    [RequireComponent(typeof(PlayerMovement), typeof(MarioAnimation))]
    [DisallowMultipleComponent, DefaultExecutionOrder(200)]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, sourceNamespace: "RetroPlatformer", sourceClassName: "RetroFirePower")]
    public sealed class FirePower : MonoBehaviour
    {
        [Header("Fire Mario Palettes")]
        public Sprite[] normal;
        public Sprite[] fire;
        public Sprite[] green;
        public Sprite[] dark;
        [Header("Attack Sprites")]
        public Sprite throwPose;
        public Sprite[] ballFrames;
        public Sprite[] burstFrames;
        [Header("Fireballs")]
        public int maxFireballs = 2;
        public float fireballSpeed = 15f;
        public float fireballGravity = 28f;
        public float reboundSpeed = 7f;
        public float throwPoseDuration = .12f;
        public float cooldown = .15f;
        [Header("Transformation")]
        public float flashDuration = .65f;
        public float flashFPS = 16f;
        [Header("Input")]
        public bool shiftAlsoShoots = true;
        PlayerMovement player;
        bool wasFire;
        float flashUntil, throwingUntil, nextShot;
        readonly List<Fireball> shots = new List<Fireball>();
        void Awake() { player = GetComponent<PlayerMovement>(); }
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
            var shot = go.AddComponent<Fireball>();
            shot.Initialize(player, sr, ballFrames, burstFrames, dir, fireballSpeed, fireballGravity, reboundSpeed);
            shots.Add(shot); nextShot = Time.time + cooldown; throwingUntil = Time.time + throwPoseDuration;
            AudioManager.Play(AudioManager.Cue.Fireball);
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
                var a = GetComponent<MarioAnimation>();
                sr.transform.localPosition = new Vector3(a.visualOffset.x, a.visualOffset.y - pose.bounds.min.y * sr.transform.localScale.y, sr.transform.localPosition.z);
            }
        }
        static Sprite Frame(Sprite[] arr, int index) => arr != null && index >= 0 && index < arr.Length ? arr[index] : null;
    }
}
