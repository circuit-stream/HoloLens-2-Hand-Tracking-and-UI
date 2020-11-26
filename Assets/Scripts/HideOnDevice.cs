using UnityEngine;

/// <summary>
/// Hide the game object on device.
/// </summary>
public class HideOnDevice : MonoBehaviour
{
    void Start()
    {
        if (!Application.isEditor)
        {
            gameObject.SetActive(false);
        }
    }
}