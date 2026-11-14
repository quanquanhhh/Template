// **********************************************
// 
// Author : xuejie.guo
// Date : 2023/06/27/14:42
// Ver : 1.0.0
// Description : SoundModule.cs
// ChangeLog :
// **********************************************

using System.Collections.Generic;
using DG.Tweening;
using MythMerge.LauncherEngine;
using MythMerge.Core;
using UnityEngine;

namespace MythMerge
{
    public class SoundModule : Singleton<SoundModule>, IModule
    {
        protected AudioSource bgAudioSource;
        protected string bgAudioAddress;
        protected static GameObject soundContainer;
        protected float musicMaxVolume = 1.0f;
        public bool SoundEnabled = true;
        public string ButtonClickAddress = string.Empty;
        public string ButtonCloseAddress = string.Empty;
        public string SwitchAddress = string.Empty;
        protected Dictionary<string, AudioClip> soundAssetsDict;
        protected List<AudioSource> sfxAudioSourceList = new List<AudioSource>();

        protected Dictionary<string,AudioSource> bgAudioList = new Dictionary<string, AudioSource>();
        protected string lastAudio = "";
        public class SaveSpot
        {
            public string musicName;
            public float progress;
        }

        protected Dictionary<string, SaveSpot> musicSaveSpotList;

        public void InitializeModule()
        {
            soundContainer = GameObject.Find("SoundContainer");

            if (soundContainer == null)
            {
                soundContainer = new GameObject("SoundContainer");
            }

            Object.DontDestroyOnLoad(soundContainer);
            bgAudioAddress = lastAudio = string.Empty;
            soundAssetsDict = new Dictionary<string, AudioClip>();

            musicSaveSpotList = new Dictionary<string, SaveSpot>();
        }

        public void OnShutDown()
        {
            Clear();
            if (soundContainer != null) Object.Destroy(soundContainer);
            soundContainer = null;
        }

        public void OnStartGameLogic()
        {
            
        }
        
        public static void PlayBgMusic(string address, bool startFromBegin = false)
        {
            
            Instance.PlayBgMusicInternal(address, startFromBegin);
        }
 
        protected void PlayBgMusicInternal(string address, bool startFromBegin = false)
        {
            if (string.IsNullOrEmpty(address)) return;
            if (bgAudioAddress == address && !startFromBegin)
            {
                return;
            }

            if (!soundAssetsDict.ContainsKey(address))
            {
                var audioClip = AssetModule.Instance.LoadAsset<AudioClip>(address);
                if(audioClip != null)
                    soundAssetsDict[address] = audioClip;
            }

            if (soundAssetsDict.TryGetValue(address, out var loadedClip) && loadedClip != null)
            {
                if (!startFromBegin && musicSaveSpotList.ContainsKey(address))
                {
                    PlayBgMusic(soundAssetsDict[address], address, musicSaveSpotList[address].progress);
                }
                else
                {
                    PlayBgMusic(soundAssetsDict[address], address, 0);
                }
            }
            else
            {
                Log.Error("Load Audio Clip Failed:" + address);
            }
        }

        // public void RecoverLastPlayingMusic()
        // {
        //     if (musicSaveSpotList.Count > 0)
        //     {
        //         var sp = musicSaveSpotList[musicSaveSpotList.Count - 1];
        //         musicSaveSpotList.RemoveAt(musicSaveSpotList.Count - 1);
        //
        //         if (!soundAssetsDict.ContainsKey(sp.musicName))
        //         {
        //             Log.Info($"{sp.musicName} Is Not In soundAssetsDict");
        //             return;
        //         }
        //
        //         var clip = soundAssetsDict[sp.musicName];
        //
        //         if (bgAudioSource.isPlaying)
        //         {
        //             if (bgAudioSource.clip.name == clip.name)
        //             {
        //                 return;
        //             }
        //
        //             bgAudioSource.Stop();
        //         }
        //
        //         bgAudioSource.clip = clip;
        //         bgAudioAddress = sp.musicName;
        //         bgAudioSource.loop = true;
        //         bgAudioSource.time = sp.progress;
        //         bgAudioSource.Play();
        //         bgAudioSource.DOKill();
        //         bgAudioSource.DOFade(0, 0.6f).From();
        //
        //         bgAudioSource.DOFade(musicMaxVolume, 0.6f);
        //     }
        //     else
        //     {
        //         if (bgAudioSource != null)
        //         {
        //             if (bgAudioSource.isPlaying)
        //                 bgAudioSource.Stop();
        //
        //             bgAudioSource.time = 0;
        //         }
        //     }
        // }

        // protected void CreateBgSaveSpot()
        // {
        //     if (bgAudioSource)
        //     {
        //         if (bgAudioSource.isPlaying)
        //         {
        //             SaveSpot sp = new SaveSpot();
        //             sp.musicName = bgAudioAddress;
        //             sp.progress = bgAudioSource.time;
        //             musicSaveSpotList[bgAudioAddress] = sp;
        //         }
        //     }
        // }

        protected void PlayBgMusic(AudioClip clip, string clipAddress, float progress = 0)
        {
            if (!bgAudioList.ContainsKey(clipAddress))
            {
                var sound = soundContainer.AddComponent<AudioSource>();
                bgAudioList[clipAddress] = sound;
            }

            if (!string.IsNullOrEmpty(lastAudio) && bgAudioList.ContainsKey(lastAudio) && lastAudio != clipAddress)
            {
                var lastplayer = bgAudioList[lastAudio];
                lastplayer.DOKill();
                lastplayer.DOFade(0, 0.6f).OnKill(() =>
                {
                    lastplayer.volume = 0;
                });
            }

            lastAudio = clipAddress;
            AudioSource nowplayer = bgAudioList[clipAddress];
            nowplayer.clip = clip;
            
            nowplayer.loop = true;
            nowplayer.time = progress;
            nowplayer.Play();
            nowplayer.DOKill();
            nowplayer.DOFade(musicMaxVolume, 0.6f).OnKill(() =>
            {
                nowplayer.volume = musicMaxVolume;
            });
            // if (bgAudioSource == null)
            // {
            //     bgAudioSource = soundContainer.AddComponent<AudioSource>();
            // }
            bgAudioSource = bgAudioList[clipAddress];

            // if (bgAudioSource.isPlaying)
            // {
            //     bgAudioSource.Stop();
            // }

            bgAudioAddress = clipAddress;
            // bgAudioSource.clip = clip;
            // bgAudioSource.loop = true;
            // bgAudioSource.time = progress;
            // bgAudioSource.DOKill();
            // bgAudioSource.DOFade(0, 0.6f).From();
            // bgAudioSource.DOFade(musicMaxVolume, 0.6f);
        }

        // public static string GetPlayingBgMusicName()
        // {
        //     if (Instance.bgAudioSource == null)
        //         return string.Empty;
        //     if (Instance.bgAudioSource.clip == null)
        //         return string.Empty;
        //
        //     return Instance.bgAudioSource.clip.name;
        // }

        public static void PlaySfx(string address, bool loop = false)
        {
            if (!string.IsNullOrEmpty(address))
                Instance.PlaySfxInternal(address, loop);
        }
  
        protected void PlaySfxInternal(string address, bool loop = false)
        {
            if (string.IsNullOrEmpty(address) || !SoundEnabled)
                return;

            if (soundAssetsDict.ContainsKey(address))
            {
                AudioSource source = GetAudioSource();
                source.clip = soundAssetsDict[address];
                source.loop = loop;
                source.Play();
            }
            else
            {
                var audioClip = AssetModule.Instance.LoadAsset<AudioClip>(address);

                if (audioClip == null) return;
                AudioSource source = GetAudioSource();
                source.loop = loop;
                source.clip = audioClip;
                source.Play();

                soundAssetsDict.Add(address, audioClip);
            }
        }

        public static void StopSfx(string address)
        {
            Instance.StopSfxInternal(address);
        }

        public static void PlayButtonClick()
        {
            Instance.PlaySfxInternal(Instance.ButtonClickAddress);
        }

        public static void PlaySwitchFx()
        {
            Instance.PlaySfxInternal(Instance.SwitchAddress);
        }

        public static void PlayButtonClose()
        {
            Instance.PlaySfxInternal(Instance.ButtonCloseAddress);
        }

        public static void PlayPopUpClose()
        {
            Instance.PlaySfxInternal(Instance.ButtonCloseAddress);
        }

        public void StopSfxInternal(string address)
        {
            var audioSource = GetAudioSourceBySfx(address);

            if (audioSource != null)
            {
                audioSource.Stop();
                audioSource.clip = null;
            }
        }

        public AudioSource GetAudioSourceBySfx(string address)
        {
            for (int i = 0; i < sfxAudioSourceList.Count; i++)
            {
                //一般不会有名字包含的情况。
                if (sfxAudioSourceList[i] != null && sfxAudioSourceList[i].clip && sfxAudioSourceList[i].clip.name.Contains(address))
                {
                    return sfxAudioSourceList[i];
                }
            }

            return null;
        }

        private AudioSource GetAudioSource()
        {
            for (int i = 0; i < sfxAudioSourceList.Count; i++)
            {
                if (!sfxAudioSourceList[i].isPlaying)
                {
                    return sfxAudioSourceList[i];
                }
            }

            var source = soundContainer.AddComponent<AudioSource>();
            sfxAudioSourceList.Add(source);
            return source;
        }

        private bool CheckHasAudioIsPlaying(string audioAddress)
        {
            for (int i = 0; i < sfxAudioSourceList.Count; i++)
            {
                if (sfxAudioSourceList[i].isPlaying && sfxAudioSourceList[i].clip.name == audioAddress)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool CheckAudioIsPlaying(string audioAddress)
        {
            return Instance.CheckHasAudioIsPlaying(audioAddress);
        }

        public void Clear()
        {
            // if (bgAudioSource != null && bgAudioSource.isPlaying)
            // {
            //     bgAudioSource.Stop();
            // }

            foreach (var audio in bgAudioList)
            {
                audio.Value.DOKill();
                if (audio.Value.isPlaying)
                {
                    audio.Value.Stop();
                }
                Object.Destroy(audio.Value);
            }
            bgAudioList.Clear();

            musicSaveSpotList.Clear();

            for (int i = sfxAudioSourceList.Count - 1; i >= 0; i--)
            {
                sfxAudioSourceList[i].Stop();
                sfxAudioSourceList[i].clip = null;
                UnityEngine.Object.Destroy(sfxAudioSourceList[i]);
                sfxAudioSourceList.RemoveAt(i);
            }

            sfxAudioSourceList.Clear();

            // if (bgAudioSource)
            //     UnityEngine.Object.Destroy(bgAudioSource);

            soundAssetsDict.Clear();
            bgAudioSource = null;
            bgAudioAddress = lastAudio = string.Empty;
        }

       

        public void OnMusicStateChanged(bool enable)
        {
            if (!enable)
            {
                musicMaxVolume = 0.00001f;
        
                if (bgAudioSource)
                {
                    bgAudioSource.volume = musicMaxVolume;
                }
            }
            else
            {
                musicMaxVolume = 1.0f;
        
                if (bgAudioSource)
                {
                    bgAudioSource.volume = musicMaxVolume;
                }
            }
        }

        public void OnSoundStateChanged(bool enable)
        {
            SoundEnabled = enable;
            if (!enable)
            {
                foreach (var audioSource in sfxAudioSourceList)
                {
                    audioSource.Stop();
                }
            }
        }

        public void PauseAllSounds()
        {
            foreach (var audioSource in sfxAudioSourceList)
            {
                if(audioSource.clip !=null)
                    audioSource.Pause();
            }
        }
        
        public void UnPauseAllSounds()
        {
            foreach (var audioSource in sfxAudioSourceList)
            {
                if(audioSource.clip !=null)
                    audioSource.UnPause();
            }
        }
        
        public void StopAllSound()
        {
            foreach (var audioSource in sfxAudioSourceList)
            {
                if (audioSource.clip != null)
                {
                    audioSource.Stop();
                    audioSource.clip = null;
                }
            }
        }
        public static void PlaySfxOneShot(string address)
        {
            Instance.PlayOneShotAudio(address);
        }
        protected void PlayOneShotAudio(string address)
        {
            if (!string.IsNullOrEmpty(address) && !CheckAudioIsPlaying(address))
            {
                PlaySfx(address);
            }
            
        }
        public void PauseBGMusic()
        {
            if (bgAudioSource != null && bgAudioSource.clip !=null)
            {
                bgAudioSource.Pause();
            }
        }

        public void UnPauseBGMusic()
        {
            if (bgAudioSource != null && bgAudioSource.clip !=null)
            {
                bgAudioSource.UnPause();
            }
        }
    }
}
