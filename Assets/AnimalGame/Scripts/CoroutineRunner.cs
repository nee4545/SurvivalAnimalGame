using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoroutineRunner : MonoBehaviour
{
    static CoroutineRunner _i;
    public static CoroutineRunner I
    {
        get
        {
            if (_i) return _i;
            var go = new GameObject("~CoroutineRunner");
            DontDestroyOnLoad(go);
            _i = go.AddComponent<CoroutineRunner>();
            return _i;
        }
    }
}
