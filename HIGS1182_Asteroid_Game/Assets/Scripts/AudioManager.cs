using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/*
 * External script: adapted from Brackeys, "Introduction to AUDIO in Unity" (2017),
 * https://www.youtube.com/watch?v=6OT43pvUyfY
 *
 * The original keeps a Sound[] array, adds one AudioSource per Sound in Awake, persists with
 * DontDestroyOnLoad and is called with FindObjectOfType<AudioManager>().Play("name").
 *
 * Adaptations:
 *  - Sounds have a category (Music/Effect). Final volume = sound volume x category volume x master volume.
 *  - Master/Music/Effects volume can be changed in code (SetMasterVolume etc.) and is saved via SaveManager.
 *  - Mute key toggles all audio on and off.
 *  - Static, null-safe PlaySound/StopSound replace FindObjectOfType, so scenes still run without an AudioManager.
 *  - Sounds are looked up in a Dictionary built once, instead of Array.Find on every call.
 *  - Effects use PlayOneShot so rapid sounds (lasers) overlap instead of cutting each other off.
 *  - Optional random pitch per effect so repeated sounds feel less robotic.
 *  - Music is not restarted if it is already playing, so it carries on smoothly between scenes.
 *  - SoundNames constants replace string literals at call sites, so typos become compile errors.
 *  - Reset() pre-fills the sound list, so only the clips need assigning in the Inspector.
 */
public class AudioManager : MonoBehaviour
{
    [Tooltip("Every sound in the game. Names must match the constants in SoundNames.")]
    [SerializeField] private Sound[] sounds;
    [Tooltip("Started automatically and kept playing across scenes.")]
    [SerializeField] private string backgroundMusic = SoundNames.Music;

    [Header("Volume")]
    [Tooltip("Defaults; once a volume is changed in code, the saved value is used instead.")]
    [Range(0f, 1f)] [SerializeField] private float masterVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.5f;
    [Range(0f, 1f)] [SerializeField] private float effectsVolume = 0.8f;
    [SerializeField] private KeyCode muteKey = KeyCode.M;

    private const string MasterChannel = "Master";
    private const string MusicChannel = "Music";
    private const string EffectsChannel = "Effects";

    private static AudioManager instance;

    private readonly Dictionary<string, Sound> soundLookup = new Dictionary<string, Sound>();
    private bool isMuted;

    private void Awake()
    {
        // Only the first AudioManager survives, so every scene can hold one for testing.
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        masterVolume = SaveManager.LoadVolume(MasterChannel, masterVolume);
        musicVolume = SaveManager.LoadVolume(MusicChannel, musicVolume);
        effectsVolume = SaveManager.LoadVolume(EffectsChannel, effectsVolume);

        BuildSounds();
        ApplyVolumes();
        Debug.Log($"AudioManager ready with {soundLookup.Count} sound(s).");
    }

    private void Start()
    {
        if (instance == this && !string.IsNullOrEmpty(backgroundMusic))
        {
            Play(backgroundMusic);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(muteKey))
        {
            ToggleMute();
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public static float MasterVolume => instance != null ? instance.masterVolume : 1f;

    public static void PlaySound(string soundName)
    {
        if (instance != null)
        {
            instance.Play(soundName);
        }
    }

    public static void StopSound(string soundName)
    {
        if (instance != null)
        {
            instance.Stop(soundName);
        }
    }

    public static void SetMasterVolume(float volume)
    {
        if (instance != null)
        {
            instance.ChangeVolume(ref instance.masterVolume, MasterChannel, volume);
        }
    }

    public static void SetMusicVolume(float volume)
    {
        if (instance != null)
        {
            instance.ChangeVolume(ref instance.musicVolume, MusicChannel, volume);
        }
    }

    public static void SetEffectsVolume(float volume)
    {
        if (instance != null)
        {
            instance.ChangeVolume(ref instance.effectsVolume, EffectsChannel, volume);
        }
    }

    private void BuildSounds()
    {
        foreach (Sound sound in sounds)
        {
            if (string.IsNullOrEmpty(sound.name) || sound.clip == null)
            {
                Debug.LogWarning($"AudioManager: sound '{sound.name}' needs a name and a clip, so it was skipped.", this);
                continue;
            }

            if (soundLookup.ContainsKey(sound.name))
            {
                Debug.LogWarning($"AudioManager: duplicate sound name '{sound.name}', so the second one was skipped.", this);
                continue;
            }

            sound.source = gameObject.AddComponent<AudioSource>();
            sound.source.clip = sound.clip;
            sound.source.loop = sound.category == SoundCategory.Music;
            sound.source.playOnAwake = false;

            soundLookup.Add(sound.name, sound);
        }
    }

    private void Play(string soundName)
    {
        if (!TryGetSound(soundName, out Sound sound))
        {
            return;
        }

        if (sound.category == SoundCategory.Music)
        {
            if (!sound.source.isPlaying)
            {
                sound.source.Play();
                Debug.Log($"Music started: {soundName}.");
            }

            return;
        }

        sound.source.pitch = 1f + Random.Range(-sound.pitchVariation, sound.pitchVariation);
        sound.source.PlayOneShot(sound.clip);
        Debug.Log($"Sound played: {soundName}.");
    }

    private void Stop(string soundName)
    {
        if (TryGetSound(soundName, out Sound sound))
        {
            sound.source.Stop();
            Debug.Log($"Sound stopped: {soundName}.");
        }
    }

    private bool TryGetSound(string soundName, out Sound sound)
    {
        if (soundLookup.TryGetValue(soundName, out sound))
        {
            return true;
        }

        Debug.LogWarning($"AudioManager: no sound named '{soundName}'.", this);
        return false;
    }

    private void ChangeVolume(ref float channelVolume, string channel, float volume)
    {
        channelVolume = Mathf.Clamp01(volume);
        SaveManager.SaveVolume(channel, channelVolume);
        ApplyVolumes();
        Debug.Log($"{channel} volume set to {channelVolume:P0}.");
    }

    private void ToggleMute()
    {
        isMuted = !isMuted;
        ApplyVolumes();
        Debug.Log(isMuted ? "Audio muted." : "Audio unmuted.");
    }

    private void ApplyVolumes()
    {
        float master = isMuted ? 0f : masterVolume;

        foreach (Sound sound in soundLookup.Values)
        {
            float categoryVolume = sound.category == SoundCategory.Music ? musicVolume : effectsVolume;
            sound.source.volume = sound.volume * categoryVolume * master;
        }
    }

    // Lets the Inspector volume sliders be tweaked live in Play mode.
    private void OnValidate()
    {
        if (Application.isPlaying && instance == this)
        {
            ApplyVolumes();
        }
    }

    // Runs when the component is added (or Reset from its menu): fills in every sound the game uses.
    private void Reset()
    {
        sounds = new[]
        {
            new Sound { name = SoundNames.Music, category = SoundCategory.Music, volume = 1f },
            new Sound { name = SoundNames.Laser, volume = 0.6f, pitchVariation = 0.1f },
            new Sound { name = SoundNames.Pickup, volume = 0.8f, pitchVariation = 0.05f },
            new Sound { name = SoundNames.GameOver, volume = 1f },
            new Sound { name = SoundNames.ShieldUp, volume = 0.8f },
            new Sound { name = SoundNames.ShieldDown, volume = 0.8f },
            new Sound { name = SoundNames.Explosion, volume = 0.7f, pitchVariation = 0.15f },
        };
    }
}

public enum SoundCategory
{
    Music,
    Effect
}

[Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;
    public SoundCategory category = SoundCategory.Effect;
    [Range(0f, 1f)] public float volume = 1f;
    [Tooltip("Random pitch change each time it plays. 0 = always the same.")]
    [Range(0f, 0.3f)] public float pitchVariation;

    [HideInInspector] public AudioSource source;
}

public static class SoundNames
{
    public const string Music = "Music";
    public const string Laser = "Laser";
    public const string Pickup = "Pickup";
    public const string GameOver = "GameOver";
    public const string ShieldUp = "ShieldUp";
    public const string ShieldDown = "ShieldDown";
    public const string Explosion = "Explosion";
}
