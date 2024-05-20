using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnbreakableObject : MonoBehaviour
{
    private void Awake()
    {
        var obj = FindObjectsOfType<UnbreakableObject>();
        if (obj.Length == 2)
        {
            DontDestroyOnLoad(gameObject);
        }
        else if(obj.Length == 1 && obj[0].gameObject.name == "DontDestroyAudioOnLoad") {
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

}
