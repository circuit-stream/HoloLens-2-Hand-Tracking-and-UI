using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;

/// <summary>
/// Spawns a prefab at the pointer hit location
/// </summary> 

[AddComponentMenu("Scripts/MRTK/Examples/SpawnOnPointerEvent")]
public class SpawnOnPointerEvent : MonoBehaviour
{
    public GameObject spawnPrefab;
    
    public void Spawn(MixedRealityPointerEventData eventData)
    {
        if (spawnPrefab != null)
        {
            var result = eventData.Pointer.Result;
            Instantiate(spawnPrefab, result.Details.Point, Quaternion.LookRotation(result.Details.Normal));
        }
    }
}
