using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;


public class AudioManager : MonoBehaviour
{
    // Variables
    public static AudioManager Instance;

    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioMixerGroup musicMixer;
    [SerializeField] private AudioMixerGroup sfxMixer;

    [SerializeField] private AudioCue defaultMusic;
    private AudioSource _musicSource;

    private List<AudioSource> _soundEffectSources = new List<AudioSource>();


    // Functions
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        LoadVolume();
        PlayMusic(defaultMusic);
    }

    #region Volume
        private void LoadVolume()
        {
            SetMixerVolume(AudioMixerKeys.MasterVolumeKey, PlayerPrefs.GetFloat(AudioMixerKeys.MasterVolumeKey));
            SetMixerVolume(AudioMixerKeys.MusicVolumeKey, PlayerPrefs.GetFloat(AudioMixerKeys.MusicVolumeKey));
            SetMixerVolume(AudioMixerKeys.SFXVolumeKey, PlayerPrefs.GetFloat(AudioMixerKeys.SFXVolumeKey));
        }

        public void SetMixerVolume(string key, float volume)
        {
            audioMixer.SetFloat(key, Mathf.Log10(volume) * 20);
        }
    #endregion

    #region Plays
        public void PlayMusic(AudioCue music, bool isLooped = true)
        {
            if (_musicSource == null)
            {
                _musicSource = gameObject.AddComponent<AudioSource>();
                _musicSource.outputAudioMixerGroup = musicMixer;
            }
 
            if (_musicSource.isPlaying) { _musicSource.Stop(); }
 
            SetupSource(ref _musicSource, music);

            if (isLooped) { _musicSource.loop = true; } else { _musicSource.loop = false; }
 
            _musicSource.Play();
        }

        public void PlaySound(AudioCue sound)
        {
            AudioSource soundSource = GetAvailableSFXSource();

            SetupSource(ref soundSource, sound);

            soundSource.Play();
        }

        public void PlayRandomSound(List<AudioCue> listOfSFX)
        {
            PlaySound(listOfSFX[Random.Range(0, listOfSFX.Count - 1)]);
        }

        public void StopMusic()
        {
            if (_musicSource != null)
                _musicSource.Stop();
        }
    #endregion

    public void TogglePauseMusic()
    {
        if (_musicSource == null) return;

        if (_musicSource.isPlaying) { _musicSource.Pause(); }
        else { _musicSource.Play(); }
    }

    public void PauseMusic()
    {
        if (_musicSource == null) return;
        if (!_musicSource.isPlaying) return;

        _musicSource.Pause();
    }

    public void UnPauseMusic()
    {
        if (_musicSource == null) return;
        if (_musicSource.isPlaying) return;

        _musicSource.Play();
    }

    private void SetupSource(ref AudioSource source, AudioCue sfx)
    {
        source.clip = sfx.Clip;
        source.volume = sfx.Volume;
        source.pitch = sfx.Pitch;
    }

    private AudioSource GetAvailableSFXSource()
    {
        foreach (AudioSource soundEffectSource in _soundEffectSources)
        {
            if (!soundEffectSource.isPlaying)
            {
                return soundEffectSource;
            }
        }

        AudioSource newAudioSource = gameObject.AddComponent<AudioSource>();
        newAudioSource.outputAudioMixerGroup = sfxMixer;
        _soundEffectSources.Add(newAudioSource);
        return newAudioSource;
    }
}