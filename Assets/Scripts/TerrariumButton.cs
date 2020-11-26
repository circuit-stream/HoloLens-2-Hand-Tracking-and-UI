using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrariumButton : MonoBehaviour
{
    public GameObject modelPrefab;

    private SpawnOnPointerEvent spawnOnPointerEvent;

    void Awake()
    {
        spawnOnPointerEvent = FindObjectOfType<SpawnOnPointerEvent>();
    }

    public void ChangePointerPrefab()
    {
        spawnOnPointerEvent.spawnPrefab = modelPrefab;
    }
}
