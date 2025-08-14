using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLocator : MonoBehaviour
{
    public static Transform Instance;
    void Awake() { Instance = transform; }
    void OnDestroy() { if (Instance == transform) Instance = null; }
}
