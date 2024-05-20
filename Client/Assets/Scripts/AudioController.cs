using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioController : MonoBehaviour
{
    [System.Serializable]
    public struct BgmType
    {
        public string name;
        public AudioClip audio;
    }
    public BgmType[] BGMList;

    private AudioSource BGM;
    private string sceneName = "";

    void Start()
    {
        BGM = gameObject.AddComponent<AudioSource>();
        BGM.clip = BGMList[0].audio;
        BGM.Play();
        BGM.loop = true;
        SceneManager.sceneLoaded += LoadedsceneEvent;
    }
    private void LoadedsceneEvent(Scene scene, LoadSceneMode mode)
    {
        Debug.Log(scene.name + "으로 변경되었습니다.");

        sceneName = scene.name;

        if (sceneName == "Title")
        {
            BGM.clip = BGMList[0].audio;
            BGM.Play();
            return;
        }

        if (sceneName == "Lobby" || sceneName == "Loading")
        {
            if (BGM.clip == BGMList[1].audio)
                return;
            BGM.clip = BGMList[1].audio;
            BGM.Play();
            return;
        }

        if (sceneName == "Main")
        {
            BGM.Stop();
            BGM.clip = null;
        }

    }
}
