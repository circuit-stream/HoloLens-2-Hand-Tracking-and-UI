using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;

using SpatialAwarenessHandler = Microsoft.MixedReality.Toolkit.SpatialAwareness.IMixedRealitySpatialAwarenessObservationHandler<Microsoft.MixedReality.Toolkit.SpatialAwareness.SpatialAwarenessMeshObject>;

public class AddToSpatialMeshObserver : MonoBehaviour, SpatialAwarenessHandler
{
    [SerializeField]
    private SpawnOnPointerEvent spawnOnPointerEvent;

    /// <summary>
    /// Collection that tracks the IDs and count of updates for each active spatial awareness mesh.
    /// </summary>
    private Dictionary<int, uint> meshUpdateData = new Dictionary<int, uint>();

    /// <summary>
    /// Value indicating whether or not this script has registered for spatial awareness events.
    /// </summary>
    private bool isRegistered = false;


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

#if UNITY_EDITOR
        // Add the spatial mesh to the observer
        StartCoroutine(TimedAddPointerHandler());
#endif
    }

    private IEnumerator TimedAddPointerHandler()
    {
        Debug.Log("started");

        yield return new WaitForSeconds(0.5f);

        var meshGameObject = GameObject.Find("Spatial Object Mesh Observer");        

        Debug.Log(meshGameObject);

        // Add a pointer handler to the observer gameobject
        if (meshGameObject != null)
        {
            PointerHandler pointerHandler = meshGameObject.AddComponent<PointerHandler>();
            Debug.Log($"added pointer handler to {pointerHandler.gameObject}");
            pointerHandler.OnPointerClicked.AddListener(spawnOnPointerEvent.Spawn);

        }
    }

    private void AddPointerHandlerToSpatialMesh()
    {
        var meshGameObject = GameObject.Find("Spatial Object Mesh Observer");

        //spawnOnPointerEvent = GetComponent<SpawnOnPointerEvent>();

        Debug.Log(meshGameObject);

        // Add a pointer handler to the observer gameobject
        if (meshGameObject != null)
        {
            if (!meshGameObject.GetComponent<PointerHandler>())
            {
                PointerHandler pointerHandler = meshGameObject.AddComponent<PointerHandler>();
                Debug.Log($"added pointer handler to {pointerHandler.gameObject}");
                pointerHandler.OnPointerClicked.AddListener(spawnOnPointerEvent.Spawn);
            }
        }
    }

    /// <summary>
    /// Registers for the spatial awareness system events.
    /// </summary>
    private void RegisterEventHandlers()
    {
        if (!isRegistered && (CoreServices.SpatialAwarenessSystem != null))
        {
            CoreServices.SpatialAwarenessSystem.RegisterHandler<SpatialAwarenessHandler>(this);
            isRegistered = true;
        }
    }

    /// <summary>
    /// Unregisters from the spatial awareness system events.
    /// </summary>
    private void UnregisterEventHandlers()
    {
        if (isRegistered && (CoreServices.SpatialAwarenessSystem != null))
        {
            CoreServices.SpatialAwarenessSystem.UnregisterHandler<SpatialAwarenessHandler>(this);
            isRegistered = false;
        }
    }

    public void OnObservationAdded(MixedRealitySpatialAwarenessEventData<SpatialAwarenessMeshObject> eventData)
    {
        AddPointerHandlerToSpatialMesh();

        // A new mesh has been added.
        Debug.Log($"Tracking mesh {eventData.Id}");
        meshUpdateData.Add(eventData.Id, 0);
    }

    public void OnObservationUpdated(MixedRealitySpatialAwarenessEventData<SpatialAwarenessMeshObject> eventData)
    {
        //AddPointerHandlerToSpatialMesh();

        // A mesh has been updated. Find it and increment the update count.
        if (meshUpdateData.TryGetValue(eventData.Id, out uint updateCount))
        {
            // Set the new update count.
            meshUpdateData[eventData.Id] = ++updateCount;

            Debug.Log($"Mesh {eventData.Id} has been updated {updateCount} times.");
        }
    }

    public void OnObservationRemoved(MixedRealitySpatialAwarenessEventData<SpatialAwarenessMeshObject> eventData)
    {
        // A mesh has been removed. We no longer need to track the count of updates.
        if (meshUpdateData.ContainsKey(eventData.Id))
        {
            Debug.Log($"No longer tracking mesh {eventData.Id}.");
            meshUpdateData.Remove(eventData.Id);
        }
    }
}
