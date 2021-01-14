using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;

public class PlamUpDetection : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	private static bool IsAPalmFacingCamera()
	{
		foreach (IMixedRealityController c in CoreServices.InputSystem.DetectedControllers)
		{
			if (c.ControllerHandedness.IsMatch(Handedness.Both))
			{
				MixedRealityPose palmPose;
				var jointedHand = c as IMixedRealityHand;

				if ((jointedHand != null) && jointedHand.TryGetJoint(TrackedHandJoint.Palm, out palmPose))
				{
					if (Vector3.Dot(palmPose.Up, CameraCache.Main.transform.forward) > 0.0f)
					{
						return true;
					}
				}
			}
		}

		return false;
	}
}
