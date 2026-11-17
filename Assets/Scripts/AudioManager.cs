using System.Collections;
using UnityEngine;

namespace RetroPlatformer
{
    [DisallowMultipleComponent]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, sourceNamespace: "RetroPlatformer", sourceClassName: "RetroAudio")]
    public class AudioManager : MonoBehaviour
    {
        public enum Cue { JumpSmall, JumpSuper, Bump, BreakBlock, Coin, Stomp, Kick, PowerupAppears, Powerup, Pipe, ExtraLife, Fireball }
        public static AudioManager Instance { get; private set; }
        [Header("Level Music")]
        public AudioSource musicSource;
        public bool startMusicIfStopped = true;
        public AudioClip optionalHurryMusic;
        [Min(0)] public float warningAt = 100;
        [Range(1, 2)] public float hurryPitchMultiplier = 1.25f;
        [Header("Audio Sources and Volume")]
        public AudioSource effectsSource;
        public AudioSource jingleSource;
        [Range(0,1)] public float effectsVolume = 0.8f;
        [Range(0,1)] public float jingleVolume = 0.9f;
        [Header("Gameplay Effects")]
        public AudioClip jumpSmall;
        public AudioClip jumpSuper;
        public AudioClip bump;
        public AudioClip breakBlock;
        public AudioClip coin;
        public AudioClip stomp;
        public AudioClip kick;
        public AudioClip powerupAppears;
        public AudioClip powerup;
        public AudioClip pipe;
        public AudioClip extraLife;
        public AudioClip fireball;
        [Header("Music Interruptions")]
        public AudioClip warning;
        public AudioClip marioDie;
        public AudioClip gameOver;
        public AudioClip flagpole;
        public AudioClip stageClear;
        [Min(0)] public float afterJingleDelay = 0.15f;
        bool warned, terminal, warningChangedMusic;
        Coroutine sequence;
        AudioClip originalMusic;
        float originalPitch = 1;
        bool originalLoop;

        void Awake()
        {
            Instance = this;
            if (!effectsSource || effectsSource == musicSource) effectsSource = NewChannel("Sound Effects");
            if (!jingleSource || jingleSource == effectsSource || jingleSource == musicSource) jingleSource = NewChannel("Jingles");
            if (musicSource) { originalMusic = musicSource.clip; originalPitch = musicSource.pitch; originalLoop = musicSource.loop; }
            effectsSource.volume = effectsVolume; jingleSource.volume = jingleVolume;
        }
        AudioSource NewChannel(string channelName)
        {
            var child = new GameObject(channelName); child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>(); source.playOnAwake = false; source.spatialBlend = 0; return source;
        }
        void Start()
        {
            if (startMusicIfStopped && musicSource && musicSource.clip && !musicSource.isPlaying && !terminal && !warned) musicSource.Play();
        }
        void OnDestroy()
        {
            if (warningChangedMusic && musicSource)
            { musicSource.Stop(); musicSource.clip = originalMusic; musicSource.pitch = originalPitch; musicSource.loop = originalLoop; }
            if (Instance == this) Instance = null;
        }
        AudioClip GetClip(Cue cue)
        {
            switch (cue)
            {
                case Cue.JumpSmall: return jumpSmall; case Cue.JumpSuper: return jumpSuper;
                case Cue.Bump: return bump; case Cue.BreakBlock: return breakBlock; case Cue.Coin: return coin;
                case Cue.Stomp: return stomp; case Cue.Kick: return kick; case Cue.PowerupAppears: return powerupAppears;
                case Cue.Powerup: return powerup; case Cue.Pipe: return pipe; case Cue.ExtraLife: return extraLife;
                case Cue.Fireball: return fireball; default: return null;
            }
        }
        public static void Play(Cue cue) { if (Instance) Instance.PlayEffect(cue); }
        public void PlayEffect(Cue cue)
        {
            if (terminal || !effectsSource) return;
            var clip = GetClip(cue); if (!clip) return;
            effectsSource.volume = effectsVolume; effectsSource.PlayOneShot(clip);
        }
        // Inspector UnityEvent hooks for future mechanics.
        public void PlayPipe() => PlayEffect(Cue.Pipe);
        public void PlayKick() => PlayEffect(Cue.Kick);
        public void PlayFireball() => PlayEffect(Cue.Fireball);
        public static void CheckTime(float timeLeft)
        {
            if (Instance && !Instance.warned && !Instance.terminal && timeLeft > 0 && timeLeft <= Instance.warningAt)
                Instance.BeginWarning();
        }
        void BeginWarning()
        {
            warned = true; if (musicSource) musicSource.Pause();
            sequence = StartCoroutine(WarningSequence());
        }
        IEnumerator WarningSequence()
        {
            if (warning) { PlayJingle(warning); yield return new WaitForSecondsRealtime(warning.length); }
            if (terminal || !musicSource) yield break;
            // Restart the optional authentic fast track, or speed up the current music as a fallback.
            warningChangedMusic = true;
            musicSource.Stop(); musicSource.loop = true;
            if (optionalHurryMusic) { musicSource.clip = optionalHurryMusic; musicSource.pitch = originalPitch; }
            else musicSource.pitch = originalPitch * hurryPitchMultiplier;
            if (musicSource.clip) musicSource.Play();
        }
        void PlayJingle(AudioClip clip)
        {
            if (!jingleSource || !clip) return;
            jingleSource.Stop(); jingleSource.loop = false; jingleSource.pitch = 1;
            jingleSource.volume = jingleVolume; jingleSource.clip = clip; jingleSource.Play();
        }
        static float Length(AudioClip clip) => clip ? clip.length : 0;
        void BeginTerminal()
        {
            terminal = true;
            if (sequence != null) StopCoroutine(sequence);
            if (musicSource) musicSource.Stop();
            if (effectsSource) effectsSource.Stop();
            if (jingleSource) jingleSource.Stop();
        }
        public static float Death(bool finalLife)
        {
            if (!Instance) return 1.6f;
            var a = Instance; a.BeginTerminal();
            a.sequence = a.StartCoroutine(a.DeathSequence(finalLife));
            return Mathf.Max(1.6f, Length(a.marioDie) + (finalLife ? Length(a.gameOver) : 0) + a.afterJingleDelay);
        }
        IEnumerator DeathSequence(bool finalLife)
        {
            if (marioDie) { PlayJingle(marioDie); yield return new WaitForSecondsRealtime(marioDie.length); }
            if (finalLife && gameOver) PlayJingle(gameOver);
        }
        // Flagpole and stage music are separate cues so the animation controls their timing.
        public static void BeginPole()
        {
            if (!Instance) return;
            var a = Instance; a.BeginTerminal();
            if (a.flagpole) a.PlayJingle(a.flagpole);
        }
        public static float BeginCastleMusic()
        {
            if (!Instance) return 0f;
            var a = Instance;
            if (a.stageClear) a.PlayJingle(a.stageClear);
            return Length(a.stageClear) + a.afterJingleDelay;
        }
        public static float Clear()
        {
            if (!Instance) return 1.5f;
            var a = Instance; a.BeginTerminal();
            a.sequence = a.StartCoroutine(a.ClearSequence());
            return Mathf.Max(1.5f, Length(a.flagpole) + Length(a.stageClear) + a.afterJingleDelay);
        }
        IEnumerator ClearSequence()
        {
            if (flagpole) { PlayJingle(flagpole); yield return new WaitForSecondsRealtime(flagpole.length); }
            if (stageClear) PlayJingle(stageClear);
        }
    }
}
