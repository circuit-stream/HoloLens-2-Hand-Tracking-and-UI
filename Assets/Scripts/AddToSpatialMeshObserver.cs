using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;

public class AddToSpatialMeshObserver : MonoBehaviour
{
    private SpawnOnPointerEvent spawnOnPointerEvent;

    void Start()
    {
        // Use CoreServices to quickly get access to the IMixedRealitySpatialAwarenessSystem
        var spatialAwarenessService = CoreServices.SpatialAwarenessSystem;

        // Cast to the IMixedRealityDataProviderAccess to get access to the data providers
        var dataProviderAccess = spatialAwarenessService as IMixedRealityDataProviderAccess;

        var meshObserver = dataProviderAccess.GetDataProvider<IMixedRealitySpatialAwarenessMeshObserver>();

        // Get the first mesh observer available
        var observer = CoreServices.GetSpatialAwarenessSystemDataProvider<IMixedRealitySpatialAwarenessMeshObserver>();

        // Get the SpatialObjectMeshObserver specifically
        var meshObserverName = "SpatialObjectMeshObserver";

        var spatialObjectMeshObserver = dataProviderAccess.GetDataProvider<IMixedRealitySpatialAwarenessMeshObserver>(meshObserverName);

        //if (spatialObjectMeshObserver != null)
        //{
            StartCoroutine(TimedAddPointerHandler());
        //}
    }

    private IEnumerator TimedAddPointerHandler()
    {
        Debug.Log("started");

        yield return new WaitForSeconds(0.2f);

        var meshGameObject = GameObject.Find("Spatial Object Mesh Observer");

        spawnOnPointerEvent = GetComponent<SpawnOnPointerEvent>();

        Debug.Log(meshGameObject);

        // Add a pointer handler to the observer gameobject
        if (meshGameObject != null)
        {
            PointerHandler pointerHandler = meshGameObject.AddComponent<PointerHandler>();
            Debug.Log($"added pointer handler to {pointerHandler.gameObject}");
            pointerHandler.OnPointerClicked.AddListener(spawnOnPointerEvent.Spawn);

        }
    }
}
