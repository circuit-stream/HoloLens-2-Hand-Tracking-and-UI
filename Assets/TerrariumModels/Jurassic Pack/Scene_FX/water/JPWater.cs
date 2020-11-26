using UnityEngine;
using System.Collections.Generic;

public enum WaterQuality { High = 2, Medium = 1, Low = 0, }
[ExecuteInEditMode]
public class JPWater : MonoBehaviour
{
  Camera cam=null;
	public Material WaterMaterial;
	[SerializeField] WaterQuality WaterQuality = WaterQuality.High;
	[SerializeField] bool EdgeBlend = true;
	[SerializeField] bool GerstnerDisplace = true;
	[SerializeField] bool DisablePixelLights = true;
	[SerializeField] int ReflectionSize = 256;
	[SerializeField] float ClipPlaneOffset = 0.07f;
	[SerializeField] LayerMask ReflectLayers = -1;
	public Light DirectionalLight;

	Dictionary<Camera, Camera> m_ReflectionCameras = new Dictionary<Camera, Camera>(); // Camera -> Camera table
	RenderTexture m_ReflectionTexture;
	int m_OldReflectionTextureSize;
	static bool s_InsideWater;

	[Header("UNDERWATER EFFECT")]
	[SerializeField] bool UnderwaterEffect=true;
	public AudioSource Underwater;
	public Texture[] LightCookie;
	public Color32 defaultFogColor;
	public float defaultFogDensity;
  Vector3 defaultLightDir=Vector3.zero;
  
	[SerializeField]  float UnderwaterDensity = 0.0f;
	float screenWaterY;
	[Header("WATER PARTICLES FX")]
	[SerializeField] bool ParticlesEffect=true;
	public ParticleSystem ripples;
	public ParticleSystem splash;
	public AudioClip Largesplash;
	float count=0;
  FlareLayer sunflare=null;

  void Start()
  {
    cam=Camera.main;
    defaultLightDir=DirectionalLight.transform.forward;
    sunflare=cam.GetComponent<FlareLayer>();
  }

	void Update()
	{
    if(!GetComponent<MeshRenderer>().isVisible | !WaterMaterial | !cam | s_InsideWater) return;

		if(WaterQuality > WaterQuality.Medium) WaterMaterial.shader.maximumLOD = 501;
		else if(WaterQuality > WaterQuality.Low) WaterMaterial.shader.maximumLOD = 301;
		else WaterMaterial.shader.maximumLOD = 201;
			
		if(DirectionalLight) WaterMaterial.SetVector("_DirectionalLightDir", DirectionalLight.transform.forward);
	
		if(!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth) | !EdgeBlend)
		{
			Shader.EnableKeyword("WATER_EDGEBLEND_OFF");
			Shader.DisableKeyword("WATER_EDGEBLEND_ON");
		}
		else
		{
			Shader.EnableKeyword("WATER_EDGEBLEND_ON");
			Shader.DisableKeyword("WATER_EDGEBLEND_OFF");
			// just to make sure (some peeps might forget to add a water tile to the patches)
			cam.depthTextureMode |= DepthTextureMode.Depth;
		}
			
		if(GerstnerDisplace)
		{
			Shader.EnableKeyword("WATER_VERTEX_DISPLACEMENT_ON");
			Shader.DisableKeyword("WATER_VERTEX_DISPLACEMENT_OFF");
		}
		else
		{
			Shader.EnableKeyword("WATER_VERTEX_DISPLACEMENT_OFF");
			Shader.DisableKeyword("WATER_VERTEX_DISPLACEMENT_ON");
		}

		// Safeguard from recursive water reflections.
		s_InsideWater = true;

		CreateWaterObjects(cam, out Camera reflectionCamera);
		
		// find out the reflection plane: position and normal in world space
		Vector3 pos = transform.position;
		Vector3 normal = transform.up;
		
		// Optionally disable pixel lights for reflection
		int oldPixelLightCount = QualitySettings.pixelLightCount;
		if(DisablePixelLights) QualitySettings.pixelLightCount = 0;
		
		if(!UpdateCameraModes(cam, reflectionCamera)) return;
		
		// Reflect camera around reflection plane
		float d = -Vector3.Dot(normal, pos) - ClipPlaneOffset;
   
		Vector4 reflectionPlane = new Vector4(normal.x, normal.y, normal.z, d);
		Matrix4x4 reflection = Matrix4x4.zero;
		CalculateReflectionMatrix(ref reflection, reflectionPlane);
		Vector3 newpos = reflection.MultiplyPoint(cam.transform.position);
		reflectionCamera.worldToCameraMatrix = cam.worldToCameraMatrix * reflection;
		
		// Setup oblique projection matrix so that near plane is our reflection
		// plane. This way we clip everything below/above it for free.
		Vector4 clipPlane = CameraSpacePlane(reflectionCamera, pos, normal, 1.0f);
		reflectionCamera.projectionMatrix = cam.CalculateObliqueMatrix(clipPlane);
		reflectionCamera.cullingMask = ~(1 << 4) & ReflectLayers.value; // never render water layer
		reflectionCamera.targetTexture = m_ReflectionTexture;
		GL.invertCulling = true;
		
		Vector3 euler = cam.transform.eulerAngles;
		reflectionCamera.transform.eulerAngles = new Vector3(-euler.x, euler.y, euler.z);
    reflectionCamera.transform.position = newpos;

 
    reflectionCamera.Render();

		GL.invertCulling = false;
		GetComponent<Renderer>().sharedMaterial.SetTexture("_ReflectionTex", m_ReflectionTexture);
		
		// Restore pixel light count
		if(DisablePixelLights) QualitySettings.pixelLightCount = oldPixelLightCount;
		s_InsideWater = false;
	}

	// Cleanup all the objects we possibly have created
	void OnDisable()
	{
		if(m_ReflectionTexture)
		{
			DestroyImmediate(m_ReflectionTexture);
			m_ReflectionTexture = null;
		}
		
		foreach (var kvp in m_ReflectionCameras)
		{
			DestroyImmediate((kvp.Value).gameObject);
		}
		m_ReflectionCameras.Clear();
	}

	bool UpdateCameraModes(Camera src, Camera dest)
	{
		if(dest == null) return false;

		// set water camera to clear the same way as current camera
		dest.clearFlags = src.clearFlags;
		dest.backgroundColor = src.backgroundColor;
		

		// update other values to match current camera.
		// even ifwe are supplying custom camera&projection matrices, 
		// some of values are used elsewhere (e.g. skybox uses far plane)
		dest.farClipPlane = src.farClipPlane;
		dest.nearClipPlane = src.nearClipPlane;
		dest.orthographic = src.orthographic;
		dest.fieldOfView = src.fieldOfView;
		dest.aspect = src.aspect;
		dest.orthographicSize = src.orthographicSize;
    return true;
	}
	
	// On-demand create any objects we need for water
	void CreateWaterObjects(Camera currentCamera, out Camera reflectionCamera)
	{
		  reflectionCamera = null;
			// Reflection render texture
			if(!m_ReflectionTexture | m_OldReflectionTextureSize != ReflectionSize)
			{
				if(m_ReflectionTexture) DestroyImmediate(m_ReflectionTexture);
        m_ReflectionTexture=new RenderTexture(ReflectionSize, ReflectionSize, 16)
        {
          name="__WaterReflection"+GetInstanceID(),
          isPowerOfTwo=true,
          hideFlags=HideFlags.DontSave
        };
        m_OldReflectionTextureSize = ReflectionSize;
			}
      
			// Camera for reflection
			m_ReflectionCameras.TryGetValue(currentCamera, out reflectionCamera);
			if(!reflectionCamera) // catch both not-in-dictionary and in-dictionary-but-deleted-GO
			{
				GameObject go = new GameObject("Water Refl Camera id" + GetInstanceID() + " for " + currentCamera.GetInstanceID(), typeof(Camera), typeof(Skybox));
				reflectionCamera = go.GetComponent<Camera>();
				reflectionCamera.enabled = false;
				reflectionCamera.transform.position = transform.position;
				reflectionCamera.transform.rotation = transform.rotation;
				reflectionCamera.gameObject.AddComponent<FlareLayer>();
				go.hideFlags = HideFlags.HideAndDontSave;
				m_ReflectionCameras[currentCamera] = reflectionCamera;
			}
      
	}

	// Given position/normal of the plane, calculates plane in camera space.
	Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
	{
		Vector3 offsetPos = pos + normal * ClipPlaneOffset;
		Matrix4x4 m = cam.worldToCameraMatrix;
		Vector3 cpos = m.MultiplyPoint(offsetPos);
		Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;
		return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
	}
	
	// Calculates reflection matrix around the given plane
	static void CalculateReflectionMatrix(ref Matrix4x4 reflectionMat, Vector4 plane)
	{
		reflectionMat.m00 = (1F - 2F * plane[0] * plane[0]);
		reflectionMat.m01 = (- 2F * plane[0] * plane[1]);
		reflectionMat.m02 = (- 2F * plane[0] * plane[2]);
		reflectionMat.m03 = (- 2F * plane[3] * plane[0]);
		
		reflectionMat.m10 = (- 2F * plane[1] * plane[0]);
		reflectionMat.m11 = (1F - 2F * plane[1] * plane[1]);
		reflectionMat.m12 = (- 2F * plane[1] * plane[2]);
		reflectionMat.m13 = (- 2F * plane[3] * plane[1]);
		
		reflectionMat.m20 = (- 2F * plane[2] * plane[0]);
		reflectionMat.m21 = (- 2F * plane[2] * plane[1]);
		reflectionMat.m22 = (1F - 2F * plane[2] * plane[2]);
		reflectionMat.m23 = (- 2F * plane[3] * plane[2]);
		
		reflectionMat.m30 = 0F;
		reflectionMat.m31 = 0F;
		reflectionMat.m32 = 0F;
		reflectionMat.m33 = 1F;
	}


//*************************************************************************************************************************************************
//UNDERWATER EFFECT 
	void OnGUI ()
	{
		if(!Application.isPlaying | !UnderwaterEffect | !cam) return;

    //Get water altitude based on screen
    float d_l = cam.ScreenToWorldPoint(new Vector3(0, 0, cam.nearClipPlane)).y;
		float u_l = cam.ScreenToWorldPoint(new Vector3(0, Screen.height, cam.nearClipPlane)).y;
		float d_r = cam.ScreenToWorldPoint(new Vector3(Screen.width, 0, cam.nearClipPlane)).y;
		float u_r = cam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, cam.nearClipPlane)).y;
		screenWaterY = Mathf.Clamp( (Mathf.Min(d_l, d_r)-transform.position.y) / (Mathf.Min(d_l, d_r) - Mathf.Min(u_l, u_r)) , -16.0f, 16.0f);
    //Color based on altitude
    Color32 col=Color32.Lerp(WaterMaterial.GetColor("_ReflectionColor"), WaterMaterial.GetColor("_BaseColor"), screenWaterY/16f);
    //Set fog color & density
    RenderSettings.fogColor = Color32.Lerp(defaultFogColor, col, Mathf.Clamp01(screenWaterY));
    RenderSettings.fogDensity = Mathf.Lerp(defaultFogDensity, UnderwaterDensity, Mathf.Clamp01(screenWaterY));
    //Set camera background color
    cam.backgroundColor = RenderSettings.fogColor;
    //Set water material reflection alpha
    Color refl=WaterMaterial.GetColor("_ReflectionColor");
		WaterMaterial.SetColor("_ReflectionColor", new Color (refl.r,refl.g,refl.b,Mathf.Clamp(screenWaterY/16f, 0.5f, 1.0f)));
		if(screenWaterY>0.5f)
		{ 
      if(!Underwater.isPlaying)
      {
        Underwater.Play(); // play underwater sound
        sunflare.enabled=false; //Disable sun flare
        cam.clearFlags=CameraClearFlags.SolidColor; // Set CameraClearFlags
			  DirectionalLight.transform.forward=-Vector3.up; // Set light direction
      }
      if(LightCookie.Length>0)
      DirectionalLight.cookie=LightCookie[Mathf.FloorToInt((Time.fixedTime*16)%LightCookie.Length)]; //Animate light cookie
		}
    else if(Underwater.isPlaying)
    {
      Underwater.Stop(); //Stop underwater sound 
      sunflare.enabled=true; //Enable sun flare
      cam.clearFlags=CameraClearFlags.Skybox;	// Reset CameraClearFlags
      DirectionalLight.transform.forward=defaultLightDir; // Reset light direction
      DirectionalLight.cookie=null; // Remove light cookie
    }
	}

//*************************************************************************************************************************************************
//PARTICLES EFFECT 

	void OnTriggerStay(Collider col) { if(!ParticlesEffect) return; WaterParticleFX(col, ripples); }
	void OnTriggerExit(Collider col) { if(!ParticlesEffect) return; WaterParticleFX(col, splash); }
	void OnTriggerEnter(Collider col) { if(ParticlesEffect) WaterParticleFX(col, splash); }

	//Spawn water particle FX
	void WaterParticleFX(Collider col, ParticleSystem particleFx)
	{
		count+=Time.fixedDeltaTime;
		ParticleSystem particle=null; Creature creatureScript=null;

		//Has a Rigidbody component ?
		if(col.transform.root.GetComponent<Rigidbody>())
		{
			//Is a JP Creature?
			if(col.transform.root.tag == "Creature")
			{
				creatureScript=col.transform.root.GetComponent<Creature>(); //Get creature script
				creatureScript.waterY=transform.position.y; //Set creature current water layer altitude
				if(!creatureScript.IsVisible) return; //Check if creature is visible
				if(particleFx==ripples && count<creatureScript.loop%10) return; //prevent particle overflow
				SkinnedMeshRenderer rend =  creatureScript.rend[0];

				//Check if the creature bounds are in contact with the water surface
				if(rend.bounds.Contains(new Vector3(col.transform.position.x, transform.position.y, col.transform.position.z)))
				{
					//Check if the creature are in motion
					if(!creatureScript.anm.GetInteger("Move").Equals(0) |
						(creatureScript.CanFly && creatureScript.IsOnLevitation) | creatureScript.OnJump | creatureScript.OnAttack)
					{
						if(particleFx==splash && (!creatureScript.IsOnGround | creatureScript.OnJump) )
						{
							col.transform.root.GetComponents<AudioSource>()[1].pitch=Random.Range(0.5f, 0.75f); 
							col.transform.root.GetComponents<AudioSource>()[1].PlayOneShot(Largesplash, Random.Range(0.5f, 0.75f));
						} else particleFx=ripples;
					} else return;

					//The spawn position
					Vector2 pos=new Vector2(rend.bounds.center.x, rend.bounds.center.z);

					//Spawn the particle prefab
					particle=Instantiate(particleFx, new Vector3(pos.x, transform.position.y+0.01f, pos.y), Quaternion.Euler(-90, 0, 0)) as ParticleSystem;
					//Set particle size relative to creature size x
					float size=rend.bounds.size.magnitude/10;
					//particle.transform.localScale=new Vector3(size,size, size);
					particle.transform.localScale=new Vector3(size,size, size);

					//Destroy particle after 3 sec
					Destroy(particle.gameObject, 3.0f);
					count=0;
				}
			}
		}
	}

}

