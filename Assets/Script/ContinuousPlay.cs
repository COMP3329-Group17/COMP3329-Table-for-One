using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContinuousPlay : MonoBehaviour
{
    private static ContinuousPlay instance;
    
    void Awake()
    {
        // Singleton pattern to avoid duplicates
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}