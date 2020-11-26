using System.Collections.Generic;
using UnityEngine;
public class Creature : MonoBehaviour 
{
  #region VARIABLES
  [Space (10)] [Header("ARTIFICIAL INTELLIGENCE")]
	public bool UseAI=false;
	const string PathHelp=
	"Use gameobjects as waypoints to define a path for this creature by \n"+
	"taking into account the priority between autonomous AI and its path.";
	const string WaypointHelp=
	"Place your waypoint gameobject in a reacheable position.\n"+
	"Don't put a waypoint in air if the creature are not able to fly";
	const string PriorityPathHelp=
	"Using a priority of 100% will disable all autonomous AI for this waypoint\n"+
	"Obstacle avoid AI and custom targets search still enabled";
	const string TargetHelp=
	"Use gameobjects to assign a custom enemy/friend for this creature\n"+
	"Can be any kind of gameobject e.g : player, other creature.\n"+
	"The creature will include friend/enemy goals in its search. \n"+
	"Enemy: triggered if the target is in range. \n"+
	"Friend: triggered when the target moves away.";
	const string MaxRangeHelp=
	"If MaxRange is zero, range is infinite. \n"+
	"Creature will start his attack/tracking once in range.";
	//Path editor
	[Space (10)]
  [Tooltip(PathHelp)] public List<_PathEditor> PathEditor;
	[HideInInspector] public int nextPath=0;
	[HideInInspector] public enum PathType { Walk, Run };
  [HideInInspector] public enum TargetAction { None, Sleep, Eat, Drink };
	[System.Serializable] public struct _PathEditor
	{
		[Tooltip(WaypointHelp)] public GameObject _Waypoint;
		public PathType _PathType;
    public TargetAction _TargetAction;
		[Tooltip(PriorityPathHelp)] [Range(1, 100)] public int _Priority;
   
    public _PathEditor(GameObject Waypoint, PathType PathType, TargetAction TargetAction, int Priority)
    {_Waypoint=Waypoint; _PathType=PathType; _TargetAction=TargetAction; _Priority=Priority; }
	}

	//Target editor
	[Space (10)]
  [Tooltip(TargetHelp)]  public List< _TargetEditor> TargetEditor;
	[HideInInspector] public enum TargetType { Enemy, Friend };
	[System.Serializable] public struct _TargetEditor
	{
		public GameObject _GameObject;
		public TargetType _TargetType;
		[Tooltip(MaxRangeHelp)]
		public int MaxRange;
	}

  [Space (10)] [Header("CREATURE SETTINGS")]
	public Skin bodyTexture;
	public Eyes eyesTexture;
  [Space (5)]
	[Range(0.0f, 100.0f)] public float Health=100f;
	[Range(0.0f, 100.0f)] public float Water=100f;
	[Range(0.0f, 100.0f)] public float Food=100f;
	[Range(0.0f, 100.0f)] public float Stamina=100f;
  [Space (5)]
	[Range(1.0f, 10.0f)] public float DamageMultiplier=1.0f;
	[Range(1.0f, 10.0f)] public float ArmorMultiplier=1.0f;
	[Range(0.0f, 2.0f)] public float AnimSpeed=1.0f;
  public bool Herbivorous, CanAttack, CanHeadAttack, CanTailAttack, CanWalk, CanJump, CanFly, CanSwim, LowAltitude, CanInvertBody;
  public float BaseMass=1, Ang_T=0.025f, Crouch_Max=0, Yaw_Max=0, Pitch_Max=0;
  
  [Space (20)] [Header("COMPONENTS AND TEXTURES")]
  public Rigidbody body;
  public LODGroup lod;
  public Animator anm;
  public AudioSource[] source;
  public SkinnedMeshRenderer[] rend;
  public Texture[] skin, eyes;
  public enum Skin {SkinA, SkinB, SkinC};
  public enum Eyes {Type0, Type1, Type2, Type3, Type4, Type5, Type6, Type7, Type8, Type9, Type10, Type11, Type12, Type13, Type14, Type15};
  [Space (20)] [Header("TRANSFORMS AND SOUNDS")]
  public Transform Head;

  [HideInInspector] public Manager main=null;
  [HideInInspector] public AnimatorStateInfo OnAnm;
  [HideInInspector] public bool IsActive, IsVisible, IsDead, IsOnGround, IsOnWater, IsInWater, IsConstrained, IsOnLevitation;
  [HideInInspector] public bool OnAttack, OnJump, OnCrouch, OnReset, OnInvert, OnHeadMove, OnAutoLook, OnTailAttack;
  [HideInInspector] public int rndX, rndY, rndMove, rndIdle, loop;
  [HideInInspector] public string behavior, specie;
  [HideInInspector] public GameObject objTGT=null, objCOL=null;
  [HideInInspector] public Vector3 HeadPos, posCOL=Vector3.zero, posTGT=Vector3.zero, lookTGT=Vector3.zero, boxscale=Vector3.zero, normal=Vector3.zero;
  [HideInInspector] public Quaternion angTGT=Quaternion.identity, normAng=Quaternion.identity;
  [HideInInspector] public float currframe, lastframe, lastHit;
  [HideInInspector] public float crouch, spineX, spineY, headX, headY, pitch, roll, reverse;
  [HideInInspector] public float posY, waterY, withersSize, size, speed;
  [HideInInspector] public float behaviorCount, distTGT, delta, actionDist, angleAdd, avoidDelta, avoidAdd;
	const int enemyMaxRange=50, waterMaxRange=200, foodMaxRange=200, friendMaxRange=200, preyMaxRange=200;

  //Input actions
  private const int 
  MoveX=0, MoveY=1, Attack=2, Interact=3, Sleep=4, MoveZ=5, Run=6,
  CamX=7, CamY=8, CamZ=9, Focus=10, Target=11, Map=12, YesNo=13, Menu=14;
  //IK TYPES
  public enum IkType { None, Convex, Quad, Flying, SmBiped, LgBiped }
	// IK goal position
	Vector3 FR_HIT, FL_HIT, BR_HIT, BL_HIT;
  // Terrain normals
  Vector3 FR_Norm=Vector3.up, FL_Norm=Vector3.up, BR_Norm=Vector3.up, BL_Norm=Vector3.up;
	//Back Legs
	float BR1, BR2, BR3, BR_Add; //Right
	float BL1, BL2, BL3, BL_Add; //Left
  float alt1, alt2, a1, a2, b1, b2, c1, c2;
	//Front Legs
	float FR1, FR2, FR3, FR_Add; //Right
	float FL1, FL2, FL3, FL_Add; //Left
  float alt3, alt4, a3, a4, b3, b4, c3, c4;
  #endregion
  #region CREATURE INITIALIZATION
	void Start()
	{
		main=Camera.main.transform.GetComponent<Manager>(); //Get manager compononent
		SetScale(transform.localScale.x);//Start scale 
		SetMaterials(bodyTexture.GetHashCode(), eyesTexture.GetHashCode());//Start materials
		loop=Random.Range(0, 100);//Randomise start action
		specie=transform.GetChild(0).name;//Creature specie name
	}
#endregion
  #region CREATURE SETUP FUNCTIONS
  //AI on/off
  public void SetAI(bool UseAI) { this.UseAI=UseAI; if(!this.UseAI) { posTGT=Vector3.zero; objTGT=null; objCOL=null; behaviorCount=0; } }
  //Change materials
  #if UNITY_EDITOR
  void OnDrawGizmosSelected()
	{
		foreach (SkinnedMeshRenderer o in rend)
		{
			if(o.sharedMaterials[0].mainTexture!=skin[bodyTexture.GetHashCode()]) o.sharedMaterials[0].mainTexture=skin[bodyTexture.GetHashCode()];
      if(o.sharedMaterials[1].mainTexture!=eyes[eyesTexture.GetHashCode()]) o.sharedMaterials[1].mainTexture=eyes[eyesTexture.GetHashCode()];
		}
	}
	#endif
  
	public void SetMaterials(int bodyindex, int eyesindex)
	{
		bodyTexture= (Skin) bodyindex; eyesTexture= (Eyes) eyesindex;
		foreach (SkinnedMeshRenderer o in rend)
		{
			o.materials[0].mainTexture = skin[bodyindex];
			o.materials[1].mainTexture = eyes[eyesindex];
		}
	}

  //Creature size
	public void SetScale(float resize)
	{
    size=resize;
		transform.localScale=new Vector3(resize, resize, resize); //creature size
    body.mass=BaseMass*size; //creature mass based on size
		withersSize = (transform.GetChild(0).GetChild(0).position-transform.position).magnitude; //At the withers altitude
		boxscale = rend[0].bounds.extents; //bounding box scale
    source[0].maxDistance=Mathf.Lerp(50f, 300f, size);
    source[1].maxDistance=Mathf.Lerp(50f, 150f, size);
	}
  #endregion FUNCTION
  #region CREATURE STATUS UPDATE
  public void StatusUpdate()
	{
		// Check if this creature are visible or near the camera, if not, and game are not in realtime mode, turn off all activity
    IsVisible=false;
    foreach (SkinnedMeshRenderer o in rend) { if(o.isVisible) IsVisible=true; }
    if(!main.RealtimeGame)
    {
      float dist=(main.transform.position-transform.position).magnitude;
      if(!IsVisible && dist>100f) { IsActive=false; anm.cullingMode=AnimatorCullingMode.CullCompletely; return; }
      else { IsActive=true; anm.cullingMode=AnimatorCullingMode.AlwaysAnimate; }
    } else { IsActive=true; anm.cullingMode=AnimatorCullingMode.AlwaysAnimate; }


    anm.speed=AnimSpeed;
    if(anm.GetNextAnimatorClipInfo(0).Length!=0) OnAnm=anm.GetNextAnimatorStateInfo(0);
    else if(anm.GetCurrentAnimatorClipInfo(0).Length!=0) OnAnm=anm.GetCurrentAnimatorStateInfo(0);

		if(currframe==15f | anm.GetAnimatorTransitionInfo(0).normalizedTime>0.5) { currframe=0.0f; lastframe=-1; }
		else currframe = Mathf.Round((OnAnm.normalizedTime % 1.0f) * 15f);

		//Manage health bar
		if(Health>0.0f)
		{
			if(loop>100)	
			{
        if(CanSwim)
        { 
          if(anm.GetInteger("Move")!=0) Food=Mathf.Clamp(Food-0.01f, 0.0f, 100f);
          if(IsInWater | IsOnWater) { Stamina=Mathf.Clamp(Stamina+1.0f, 0.0f, 100f); Water=Mathf.Clamp(Water+1.0f, 0.0f, 100f); }  
          else if(CanWalk) { Stamina=Mathf.Clamp(Stamina-0.01f, 0.0f, 100f);  Water=Mathf.Clamp(Water-0.01f, 0.0f, 100f); }
          else { Stamina=Mathf.Clamp(Stamina-1.0f, 0.0f, 100f); Water=Mathf.Clamp(Water-1.0f, 0.0f, 100f); Health=Mathf.Clamp(Health-1.0f, 0.0f, 100f); }
        }
        else
        { 
          if(anm.GetInteger("Move")!=0) { Stamina=Mathf.Clamp(Stamina-0.01f, 0.0f, 100f); Water=Mathf.Clamp(Water-0.01f, 0.0f, 100f); Food=Mathf.Clamp(Herbivorous?Food-0.1f:Food-0.01f, 0.0f, 100f); }
          if(IsInWater) { Stamina=Mathf.Clamp(Stamina-1.0f, 0.0f, 100f); Health=Mathf.Clamp(Health-1.0f, 0.0f, 100f); } 
        }

        if(Food==0.0f | Stamina==0.0f | Water==0.0f) Health=Mathf.Clamp(Health-0.1f, 0.0f, 100f); else Health=Mathf.Clamp(Health+0.1f, 0.0f, 100f);
				loop=0;
			}
      else loop++;
		}
		else
		{
			Water=0.0f; Food=0.0f; Stamina=0.0f; behavior="Dead";
      if(main.TimeAfterDead==0) return;
			if(behaviorCount>0) behaviorCount=0;
			else if(behaviorCount==-main.TimeAfterDead)
			{
				//Delete from list and destroy gameobject
				if(main.selected>=main.creaturesList.IndexOf(transform.gameObject)) { if(main.selected>0) main.selected--; }
				main.creaturesList.Remove(transform.gameObject); Destroy(transform.gameObject);
			}
			else behaviorCount--;
		}
	}
#endregion
  #region COLLISIONS AND DAMAGES
  //Spawn blood particle
  void SpawnBlood(Vector3 position)
	{
		ParticleSystem particle=Instantiate(main.blood, position, Quaternion.Euler(-90, 0, 0))as ParticleSystem; //spawn particle
		particle.transform.localScale=new Vector3(boxscale.z/10, boxscale.z/10, boxscale.z/10); //particle size
		Destroy(particle.gameObject, 1.0f); //destroy particle
	}

  //Collisions
  void OnCollisionExit() { objCOL=null; }
  public void ManageCollision(Collision col, float pitch_max, float crouch_max, AudioSource[] source, AudioClip pain, AudioClip Hit_jaw, AudioClip Hit_head, AudioClip Hit_tail)
	{
		//Collided with a Creature
	  if(col.transform.root.tag.Equals("Creature"))
		{
			Creature other=col.gameObject.GetComponent<Creature>(); objCOL=other.gameObject;

      //Is Player?
		  if(!UseAI && OnAttack)
		  {
			  objTGT=other.gameObject; other.objTGT=transform.gameObject;
			  behaviorCount=500; other.behaviorCount=500;
			  if(other.specie==specie) { behavior="Contest"; other.behavior="Contest"; }
			  else if(other.CanAttack) { behavior="Battle"; other.behavior="Battle"; }
			  else { behavior="Battle"; other.behavior="ToFlee"; }
		  }
			//Eat ?
      if(IsDead && lastHit==0 && other.IsConstrained)
      { 
        SpawnBlood(col.GetContact(0).point);
        body.AddForce(-other.transform.forward, ForceMode.Acceleration);
        lastHit=25; return;
      }
      //Attack ?
			else if(lastHit==0 && other.OnAttack)
			{
				float baseDamages=Mathf.Clamp((other.BaseMass*other.DamageMultiplier) / (BaseMass*ArmorMultiplier), 10,100);

				if(col.collider.gameObject.name.StartsWith("jaw")) //bite damage
				{
          SpawnBlood(col.GetContact(0).point);
          if(!IsInWater) body.AddForce(-col.GetContact(0).normal*other.body.mass/4, ForceMode.Acceleration);
          lastHit=50; if(IsDead) return;
          source[0].pitch=Random.Range(1.0f, 1.5f); source[0].PlayOneShot(pain, 1.0f);
					source[1].PlayOneShot(Hit_jaw, Random.Range(0.1f, 0.4f));
					Health=Mathf.Clamp(Health-baseDamages, 0.0f, 100f);
				}
				else if(col.collider.gameObject.name.Equals("head")) //head damage
				{
          SpawnBlood(col.GetContact(0).point); 
					if(!IsInWater) body.AddForce(col.GetContact(0).normal*other.body.mass/4, ForceMode.Acceleration);
          lastHit=50; if(IsDead) return;
          source[0].pitch=Random.Range(1.0f, 1.5f); source[0].PlayOneShot(pain, 1.0f);
					source[1].PlayOneShot(Hit_head, Random.Range(0.1f, 0.4f));
					if(!Herbivorous) Health=Mathf.Clamp(Health-baseDamages, 0.0f, 100f);
					else Health=Mathf.Clamp(Health-baseDamages/10, 0.0f, 100f);
				}
				else  if(!col.collider.gameObject.name.Equals("root")) //tail damage
				{
					SpawnBlood(col.GetContact(0).point);
          if(!IsInWater) body.AddForce(col.GetContact(0).normal*other.body.mass/4, ForceMode.Acceleration);
          lastHit=50; if(IsDead) return;
          source[0].pitch=Random.Range(1.0f, 1.5f); source[0].PlayOneShot(pain, 1.0f);
          source[1].PlayOneShot(Hit_tail, Random.Range(0.1f, 0.4f));
					if(!Herbivorous) Health=Mathf.Clamp(Health-baseDamages, 0.0f, 100f);
					else Health-= Health=Mathf.Clamp(Health-baseDamages/10, 0.0f, 100f);
				 }
			}

      //Not the current target creature, avoid and look at
      if(objTGT!=objCOL) { lookTGT=other.Head.position; posCOL=col.GetContact(0).point; }
		}
		//Collided with world, avoid
		else if(col.gameObject!=objTGT)
    { 
       objCOL=col.gameObject;
       posCOL=col.GetContact(0).point;
    }
	}
  #endregion
  #region ENVIRONEMENTAL CHECKING
  public void GetGroundPos(IkType ikType, Transform RLeg1=null, Transform RLeg2=null, Transform RLeg3=null, Transform LLeg1=null, Transform LLeg2=null, Transform LLeg3=null,
                     Transform RArm1=null, Transform RArm2=null, Transform RArm3=null, Transform LArm1=null, Transform LArm2=null, Transform LArm3=null, float FeetOffset=0.0f)
  {
	  posY=-transform.position.y;
    #region Use Raycast
    if(main.UseRaycast)
    {
      if(ikType==IkType.None | IsDead | IsInWater | !IsOnGround)
      {
          if(Physics.Raycast(transform.position+Vector3.up*withersSize, -Vector3.up, out RaycastHit hit, withersSize*1.5f,1<<0))
          { posY=hit.point.y; normal=hit.normal; IsOnGround=true; } else IsOnGround=false;
      }
      else if(ikType>=IkType.SmBiped) // Biped
      {
        if(Physics.Raycast((transform.position+transform.forward*2)+Vector3.up, -Vector3.up, out RaycastHit hit, withersSize*2.0f,1<<0))
        { posY=hit.point.y; normal=hit.normal; }
			  if(Physics.Raycast(RLeg3.position+Vector3.up*withersSize, -Vector3.up, out RaycastHit BR, withersSize*2.0f,1<<0))
        { IsOnGround=true; BR_HIT=BR.point; BR_Norm=BR.normal; } else BR_HIT.y=-transform.position.y;
          if(Physics.Raycast(LLeg3.position+Vector3.up*withersSize, -Vector3.up, out RaycastHit BL, withersSize*2.0f,1<<0))
        { IsOnGround=true;  BL_HIT=BL.point; BL_Norm=BL.normal; } else BL_HIT.y=-transform.position.y;

        if(posY>BL_HIT.y && posY>BR_HIT.y) posY=Mathf.Max(BL_HIT.y,BR_HIT.y); else posY=Mathf.Min(BL_HIT.y,BR_HIT.y);
        normal=(BL_Norm+BR_Norm+normal)/3;
      }
      else if(ikType==IkType.Flying) // Flying
      {
        IsOnGround=false;
        if(Physics.Raycast(transform.position+Vector3.up*withersSize, -Vector3.up, out RaycastHit hit, withersSize*4.0f,1<<0))
        {
          normal=hit.normal; IsOnGround=true;
        if(Physics.Raycast(RArm3.position+Vector3.up*withersSize, -Vector3.up, out RaycastHit FR, withersSize*4.0f,1<<0))
          { FR_HIT=FR.point; FR_Norm=FR.normal; } else { FR_Norm=hit.normal; FR_HIT.y=-transform.position.y; }
        if(Physics.Raycast(LArm3.position+Vector3.up*withersSize, -Vector3.up, out RaycastHit FL, withersSize*4.0f,1<<0))
          { FL_HIT=FL.point; FL_Norm=FL.normal; } else { FL_Norm=hit.normal; FL_HIT.y=-transform.position.y;}
        if(Physics.Raycast(RLeg3.position+Vector3.up*withersSize, -Vector3.up, out RaycastHit BR, withersSize*4.0f,1<<0))
          { BR_HIT=BR.point; BR_Norm=BR.normal; } else { BR_Norm=hit.normal; BR_HIT.y=-transform.position.y; }
        if(Physics.Raycast(LLeg3.position+Vector3.up*withersSize, -Vector3.up, out RaycastHit BL, withersSize*4.0f,1<<0))
          { BL_HIT=BL.point; BL_Norm=BL.normal;  }else { BL_Norm=hit.normal; BL_HIT.y=-transform.position.y; }
          posY=hit.point.y;
        }
          
      }
      else //Quadruped
      {
        IsOnGround=false;
        if(Physics.Raycast(RArm3.position+Vector3.up*withersSize, -Vector3.up, out RaycastHit FR, withersSize*2.0f,1<<0))
          { FR_HIT=FR.point; FR_Norm=FR.normal; IsOnGround=true; } else FR_HIT.y=-transform.position.y;
        if(Physics.Raycast(LArm3.position+Vector3.up*withersSize, -Vector3.up, out RaycastHit FL, withersSize*2.0f,1<<0))
          { FL_HIT=FL.point; FL_Norm=FL.normal; IsOnGround=true; } else FL_HIT.y=-transform.position.y;
        if(Physics.Raycast(RLeg3.position+Vector3.up*withersSize, -Vector3.up, out RaycastHit BR, withersSize*2.0f,1<<0))
          { BR_HIT=BR.point; BR_Norm=BR.normal; IsOnGround=true; } else BR_HIT.y=-transform.position.y;
        if(Physics.Raycast(LLeg3.position+Vector3.up*withersSize, -Vector3.up, out RaycastHit BL, withersSize*2.0f,1<<0))
          { BL_HIT=BL.point; BL_Norm=BL.normal; IsOnGround=true; } else BL_HIT.y=-transform.position.y;

        if(ikType==IkType.Convex)
        {
          if(IsConstrained) posY=Mathf.Min(BR_HIT.y,BL_HIT.y,FR_HIT.y,FL_HIT.y);
          else posY=(BR_HIT.y+BL_HIT.y+FR_HIT.y+FL_HIT.y)/4;
        }
        else
        {
          if(IsConstrained | !main.UseIK) posY=Mathf.Min(BR_HIT.y,BL_HIT.y,FR_HIT.y,FL_HIT.y);
          else posY=(BR_HIT.y+BL_HIT.y+FR_HIT.y+FL_HIT.y-size)/4;
        }

        normal=Vector3.Cross(FR_HIT-BL_HIT,BR_HIT-FL_HIT).normalized;
      }
		}
    #endregion
    #region Terrain Only
    else
    {
      if(ikType==IkType.None | IsDead | IsInWater | !IsOnGround)
      {
        float x = ((transform.position.x -main.T.transform.position.x) / main.T.terrainData.size.x ) * main.tres;
		    float y = ((transform.position.z -main.T.transform.position.z) / main.T.terrainData.size.z ) * main.tres;
		    normal=main.T.terrainData.GetInterpolatedNormal(x/main.tres, y/main.tres);
        posY=main.T.SampleHeight(transform.position)+main.T.GetPosition().y;
      }
      else if(ikType>=IkType.SmBiped) // Biped
      {
		    BR_HIT=new Vector3(RLeg3.position.x, main.T.SampleHeight(RLeg3.position)+main.tpos.y, RLeg3.position.z);
        float x = ((RLeg3.position.x - main.tpos.x) / main.tdata.size.x ) * main.tres, y = ((RLeg3.position.z - main.tpos.z) / main.tdata.size.z ) * main.tres;
		    BR_Norm=main.tdata.GetInterpolatedNormal(x/main.tres, y/main.tres);
        BL_HIT=new Vector3(LLeg3.position.x, main.T.SampleHeight(LLeg3.position)+main.tpos.y, LLeg3.position.z);
		    x = ((LLeg3.position.x - main.tpos.x) / main.tdata.size.x ) * main.tres; y = ((LLeg3.position.z - main.tpos.z) / main.tdata.size.z ) * main.tres;
		    BL_Norm=main.tdata.GetInterpolatedNormal(x/main.tres, y/main.tres);

        if(posY>BL_HIT.y && posY>BR_HIT.y) posY=Mathf.Max(BL_HIT.y,BR_HIT.y);  else posY=Mathf.Min(BL_HIT.y,BR_HIT.y);
        normal=(BL_Norm+BR_Norm+normal)/3;
      }
      else if(ikType==IkType.Flying) // Flying
      {
        float x = ((transform.position.x -main.T.transform.position.x) / main.T.terrainData.size.x ) * main.tres;
		    float y = ((transform.position.z -main.T.transform.position.z) / main.T.terrainData.size.z ) * main.tres;
        normal=main.T.terrainData.GetInterpolatedNormal(x/main.tres, y/main.tres);
        posY=main.T.SampleHeight(transform.position)+main.T.GetPosition().y;

		    BR_HIT=new Vector3(RLeg3.position.x, main.T.SampleHeight(RLeg3.position)+main.tpos.y, RLeg3.position.z);
        x = ((RLeg3.position.x - main.tpos.x) / main.tdata.size.x ) * main.tres;  y = ((RLeg3.position.z - main.tpos.z) / main.tdata.size.z ) * main.tres;
		    BR_Norm=main.tdata.GetInterpolatedNormal(x/main.tres, y/main.tres);
        BL_HIT=new Vector3(LLeg3.position.x, main.T.SampleHeight(LLeg3.position)+main.tpos.y, LLeg3.position.z);
		    x = ((LLeg3.position.x - main.tpos.x) / main.tdata.size.x ) * main.tres; y = ((LLeg3.position.z - main.tpos.z) / main.tdata.size.z ) * main.tres;
		    BL_Norm=main.tdata.GetInterpolatedNormal(x/main.tres, y/main.tres);
        FR_HIT=new Vector3(RArm3.position.x, main.T.SampleHeight(RArm3.position)+main.tpos.y, RArm3.position.z);
		    x = ((RArm3.position.x - main.tpos.x) / main.tdata.size.x ) * main.tres;  y = ((RArm3.position.z - main.tpos.z) / main.tdata.size.z ) * main.tres;
		    FR_Norm=main.tdata.GetInterpolatedNormal(x/main.tres, y/main.tres);
        FL_HIT=new Vector3(LArm3.position.x, main.T.SampleHeight(LArm3.position)+main.tpos.y, LArm3.position.z);
		    x = ((LArm3.position.x - main.tpos.x) / main.tdata.size.x ) * main.tres;  y = ((LArm3.position.z - main.tpos.z) / main.tdata.size.z ) * main.tres;
		    FL_Norm=main.tdata.GetInterpolatedNormal(x/main.tres, y/main.tres);
      }
      else //Quadruped
      {
		    BR_HIT=new Vector3(RLeg3.position.x, main.T.SampleHeight(RLeg3.position)+main.tpos.y, RLeg3.position.z);
        float x = ((RLeg3.position.x - main.tpos.x) / main.tdata.size.x ) * main.tres, y = ((RLeg3.position.z - main.tpos.z) / main.tdata.size.z ) * main.tres;
		    BR_Norm=main.tdata.GetInterpolatedNormal(x/main.tres, y/main.tres);
        BL_HIT=new Vector3(LLeg3.position.x, main.T.SampleHeight(LLeg3.position)+main.tpos.y, LLeg3.position.z);
		    x = ((LLeg3.position.x - main.tpos.x) / main.tdata.size.x ) * main.tres; y = ((LLeg3.position.z - main.tpos.z) / main.tdata.size.z ) * main.tres;
		    BL_Norm=main.tdata.GetInterpolatedNormal(x/main.tres, y/main.tres);
        FR_HIT=new Vector3(RArm3.position.x, main.T.SampleHeight(RArm3.position)+main.tpos.y, RArm3.position.z);
		    x = ((RArm3.position.x - main.tpos.x) / main.tdata.size.x ) * main.tres; y = ((RArm3.position.z - main.tpos.z) / main.tdata.size.z ) * main.tres;
		    FR_Norm=main.tdata.GetInterpolatedNormal(x/main.tres, y/main.tres);
        FL_HIT=new Vector3(LArm3.position.x, main.T.SampleHeight(LArm3.position)+main.tpos.y, LArm3.position.z);
		    x = ((LArm3.position.x - main.tpos.x) / main.tdata.size.x ) * main.tres; y = ((LArm3.position.z - main.tpos.z) / main.tdata.size.z ) * main.tres;
		    FL_Norm=main.tdata.GetInterpolatedNormal(x/main.tres, y/main.tres);

        if(ikType==IkType.Convex)
        {
          if(IsConstrained) posY=Mathf.Min(BR_HIT.y,BL_HIT.y,FR_HIT.y,FL_HIT.y);
          else posY=(BR_HIT.y+BL_HIT.y+FR_HIT.y+FL_HIT.y)/4;
        }
        else
        {
          if(IsConstrained | !main.UseIK) posY=Mathf.Min(BR_HIT.y,BL_HIT.y,FR_HIT.y,FL_HIT.y);
          else posY=(BR_HIT.y+BL_HIT.y+FR_HIT.y+FL_HIT.y-size)/4;
        }
        normal=Vector3.Cross(FR_HIT-BL_HIT,BR_HIT-FL_HIT).normalized;
      }
	}
    #endregion
    #region Set status
    //Set status
    if((transform.position.y-size)<=posY) IsOnGround=true; else IsOnGround=false; //On ground?
    waterY=main.WaterAlt-crouch; //Check for water altitude
    if((transform.position.y)<waterY && body.worldCenterOfMass.y>waterY) IsOnWater=true; else IsOnWater=false; //On water ?
    if(body.worldCenterOfMass.y<waterY) IsInWater=true; else IsInWater=false; // In water ?

    //Setup Rigidbody
    if(IsDead)
    {
      body.maxDepenetrationVelocity=0.25f;
      body.constraints=RigidbodyConstraints.None;
    }
    else if(IsConstrained)
    {
      body.maxDepenetrationVelocity=0.0f; crouch=0.0f;
      body.constraints=RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
    }
    else
    {
      body.maxDepenetrationVelocity=5.0f;
      if(lastHit==0) body.constraints=RigidbodyConstraints.FreezeRotationZ;
      else body.constraints=RigidbodyConstraints.None;
    }
    
    //Setup Y position and rotation
    if(IsOnGround && !IsInWater) //On Ground outside water
    {
      Quaternion n=Quaternion.LookRotation(Vector3.Cross(transform.right, normal), normal);
      if(!CanFly)
      {
        float rx = Mathf.DeltaAngle(n.eulerAngles.x, 0.0f), rz = Mathf.DeltaAngle(n.eulerAngles.z, 0.0f);
        float pitch=Mathf.Clamp(rx, -45f, 45f), roll=Mathf.Clamp(rz, -10f, 10f);
        normAng=Quaternion.Euler(-pitch, anm.GetFloat("Turn"), -roll);
      }
      else normAng=Quaternion.Euler(n.eulerAngles.x, anm.GetFloat("Turn"), n.eulerAngles.z); posY-=crouch;
    }
    else if(IsInWater | IsOnWater) //On Water or In water
    { normAng=Quaternion.Euler(0, anm.GetFloat("Turn"), 0); posY=waterY-body.centerOfMass.y; }
    else //In Air
    { normAng=Quaternion.Euler(0, anm.GetFloat("Turn"), 0); posY=-transform.position.y; }

    #endregion
  }
  #endregion
  #region PHYSICAL FORCES
  public void ApplyGravity(float multiplier=1.0f)
  {
    body.AddForce((Vector3.up*size)*(body.velocity.y>0?-20*body.drag:-50*body.drag)*multiplier, ForceMode.Acceleration);
  }
  public void ApplyYPos()
  {
    if(IsOnGround && (Mathf.Abs(normal.x)>main.MaxSlope | Mathf.Abs(normal.z)>main.MaxSlope))
    { body.AddForce(new Vector3(normal.x, -normal.y, normal.z)*64, ForceMode.Acceleration); behaviorCount=0; }
    body.AddForce(Vector3.up*Mathf.Clamp(posY-transform.position.y,-size,size), ForceMode.VelocityChange);
  }
  public void Move(Vector3 dir, float force=0, bool jump=false)
  {
    if(CanAttack && anm.GetBool("Attack").Equals(true))
    { force*=1.5f; transform.rotation=Quaternion.Lerp(transform.rotation, normAng, Ang_T*2);
    } else transform.rotation=Quaternion.Lerp(transform.rotation, normAng, Ang_T);

    if(dir!=Vector3.zero)
    {
      if(!CanSwim && !IsOnGround)
      {
        if(IsInWater | IsOnWater) force/=8;
        else if(!CanFly && !OnJump) force/=8;
        else force/=(4/body.drag);
      }
      else force/=(4/body.drag);

      body.AddForce(dir*force*speed, jump?ForceMode.VelocityChange:ForceMode.Acceleration);
    }
  }
  #endregion
  #region LERP SKELETON ROTATION
  public void RotateBone(IkType ikType, float maxX, float maxY=0, bool CanMoveHead=true, float t=0.5f)
  {
    //Freeze all
    if(AnimSpeed==0.0f) return;

    //Slowdown on turning
    if(!OnAttack && !OnJump)
    { speed=size*anm.speed*(1.0f-Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y,anm.GetFloat("Turn")))/135f); }
  
    //Lerp feet position
    if(main.UseIK) { main.message=2; ikType=IkType.None; main.UseIK=false;  }

    //Take damages animation
    if(lastHit!=0) { if(!IsDead&&CanWalk) crouch=Mathf.Lerp(crouch, (Crouch_Max*size)/2, 1.0f); lastHit--; }

    //Reset skeleton rotations
    if(OnReset)
    {
      pitch = Mathf.Lerp(pitch, 0.0f, t/10f);
      roll = Mathf.Lerp(roll, 0.0f, t/10f);
      headX= Mathf.LerpAngle(headX, 0.0f, t/10f);
      headY= Mathf.LerpAngle(headY, 0.0f, t/10f);
      crouch=Mathf.Lerp(crouch, 0.0f, t/10f);
      spineX= Mathf.LerpAngle(spineX, 0.0f, t/10f);
      spineY= Mathf.LerpAngle(spineY, 0.0f, t/10f);
      return;
    }

    //Smooth avoiding angle
    if(avoidDelta!=0)
    { 
      if(Mathf.Abs(avoidAdd)>90) avoidDelta=0;
      avoidAdd=Mathf.MoveTowardsAngle(avoidAdd,avoidDelta>0.0f?135f:-135f, t);
    }
    else avoidAdd=Mathf.MoveTowardsAngle(avoidAdd, 0.0f, t);
    
    //Setup Look target position
    if(objTGT)
		{
			if(behavior.EndsWith("Hunt") | behavior.Equals("Battle") |  behavior.EndsWith("Contest") ) lookTGT=objTGT.transform.position;
      else if(Herbivorous && behavior.Equals("Food")) lookTGT=posTGT;
			else if(loop==0) lookTGT=Vector3.zero;
		} else if(loop==0) lookTGT=Vector3.zero;

    //Lerp all skeleton parts
    if(CanMoveHead)
    {
      if(!OnTailAttack && !anm.GetInteger("Move").Equals(0))
      {
        spineX= Mathf.MoveTowardsAngle(spineX, (Mathf.DeltaAngle(anm.GetFloat("Turn"), transform.eulerAngles.y)/360f)*maxX, t);
        spineY= Mathf.LerpAngle(spineY, 0.0f, t/10f);
      }
      else
      {
        spineX= Mathf.MoveTowardsAngle(spineX, 0.0f, t/10f);
        spineY= Mathf.LerpAngle(spineY, 0.0f, t/10f);
      }

      if((!CanFly && !CanSwim && anm.GetInteger("Move")!=2) | !IsOnGround) roll=Mathf.Lerp(roll, 0.0f, Ang_T);
		  crouch=Mathf.Lerp(crouch, 0.0f, t/10f);

      if(OnHeadMove) return;

      if(lookTGT!=Vector3.zero && (lookTGT-transform.position).magnitude>boxscale.z)
      {
        Quaternion dir;
			  if(objTGT && objTGT.tag.Equals("Creature")) dir= Quaternion.LookRotation(objTGT.GetComponent<Rigidbody>().worldCenterOfMass-HeadPos);
			  else dir= Quaternion.LookRotation(lookTGT-HeadPos);

        headX = Mathf.MoveTowardsAngle(headX, (Mathf.DeltaAngle(dir.eulerAngles.y, transform.eulerAngles.y)/(180f-Yaw_Max))*Yaw_Max, t);
        headY = Mathf.MoveTowardsAngle(headY, (Mathf.DeltaAngle(dir.eulerAngles.x, transform.eulerAngles.x)/(90f-Pitch_Max))*Pitch_Max, t);
      }
      else
      {
        if(Mathf.RoundToInt(anm.GetFloat("Turn"))==Mathf.RoundToInt(transform.eulerAngles.y))
        {
          if(loop==0 && Mathf.RoundToInt(headX*100)==Mathf.RoundToInt(rndX*100) && Mathf.RoundToInt(headY*100)==Mathf.RoundToInt(rndY*100))
          {
            rndX=Random.Range((int)-Yaw_Max/2, (int)Yaw_Max/2);
            rndY=Random.Range((int)-Pitch_Max/2,(int)Pitch_Max/2);
          }
          headX= Mathf.LerpAngle(headX, rndX, t/10f);
          headY= Mathf.LerpAngle(headY, rndY, t/10f);
        } 
        else
        {
          headX= Mathf.LerpAngle(headX, spineX, t/10f);
          headY= Mathf.LerpAngle(headY, 0.0f, t/10f);
        }
      }
    }
    else
    {
      spineX = Mathf.LerpAngle(spineX, (Mathf.DeltaAngle(anm.GetFloat("Turn"), transform.eulerAngles.y)/360f)*maxX, Ang_T);
      if(IsOnGround && !IsInWater) { spineY= Mathf.LerpAngle(spineY, 0.0f, t/10f); roll=Mathf.LerpAngle(roll, 0.0f, t/10f); pitch=Mathf.Lerp(pitch, 0.0f, t/10f); }
      else if(CanFly) 
      { 
        if(anm.GetInteger("Move")>=2 && anm.GetInteger("Move")<3)
        spineY = Mathf.LerpAngle(spineY, (Mathf.DeltaAngle(anm.GetFloat("Pitch")*90f, pitch)/180f)*maxY, Ang_T);
        roll=Mathf.LerpAngle(roll, -spineX, t/10f);
      }
      else { spineY = Mathf.LerpAngle(spineY, (Mathf.DeltaAngle(anm.GetFloat("Pitch")*90f, pitch)/180f)*maxY, Ang_T); roll=Mathf.LerpAngle(roll, -spineX, t/10f); }
      headX= Mathf.LerpAngle(headX, spineX, t);
      headY= Mathf.LerpAngle(headY, spineY, t);
    }

  }
  #endregion
  #region PLAYER INPUTS
  public void GetUserInputs(int idle1=0, int idle2=0, int idle3=0, int idle4=0, int eat=0, int drink=0, int sleep=0, int rise=0)
	{
		if(behavior=="Repose" && anm.GetInteger("Move")!=0) behavior="Player";
		else if(behaviorCount<=0) { objTGT=null; behavior="Player"; behaviorCount=0; } else behaviorCount--;

		// Current camera manager target ?
		if(transform.gameObject==main.creaturesList[main.selected].gameObject && main.CameraMode!=0)
		{
			//Run key
			bool run=Input.GetKey(KeyCode.LeftShift)?true:false;

			//Attack key
			if(CanAttack)
      { 
        if(Input.GetKey(KeyCode.Mouse0)) { behaviorCount=500; behavior="Hunt"; anm.SetBool ("Attack", true); }
        else anm.SetBool ("Attack", false);
      }

			//Crouch key
			if(main.UseIK && Input.GetKey(KeyCode.LeftControl)) { crouch=Crouch_Max*size; OnCrouch=true; }
			else OnCrouch=false;

			//Fly/swim up/down key
			if(CanFly | CanSwim)
			{
				if(Input.GetKey(KeyCode.Mouse1))
				{
					anm.SetFloat("Turn", transform.eulerAngles.y+Input.GetAxis("Mouse X")*22.5f);//Mouse turn
					if(Input.GetAxis("Mouse Y")!=0 && anm.GetInteger("Move")==3) //Pitch with mouse if is moving
					anm.SetFloat("Pitch",Input.GetAxis("Mouse Y"));
					else if(Input.GetKey(KeyCode.LeftControl)) anm.SetFloat("Pitch", 1.0f);
					else if(Input.GetKey(KeyCode.Space)) anm.SetFloat("Pitch",-1.0f);
				}
				else
				{
					if(Input.GetKey(KeyCode.LeftControl)) anm.SetFloat("Pitch", 1.0f);
					else if(Input.GetKey(KeyCode.Space)) anm.SetFloat("Pitch", -1.0f);
					else anm.SetFloat("Pitch", 0);
				}
			}

			//Jump
			if(CanJump && Input.GetKey(KeyCode.Space) && !OnJump) anm.SetInteger ("Move", 3);
			//Move
			else if(Input.GetAxis("Horizontal")!=0 | Input.GetAxis("Vertical")!=0)
			{
        //Flying/swim
				if(CanSwim | (CanFly&&!IsOnGround))
				{
					if(Input.GetKey(KeyCode.Mouse1))
					{
						if(Input.GetAxis("Vertical")<0) anm.SetInteger ("Move", -1); //Backward
						else if(Input.GetAxis("Vertical")>0) anm.SetInteger ("Move", 3); //Forward
						else if(Input.GetAxis("Horizontal")>0) anm.SetInteger ("Move", -10); //Strafe-
						else if(Input.GetAxis("Horizontal")<0) anm.SetInteger ("Move", 10); //Strafe+
						else anm.SetInteger ("Move", 0);
					}
					else
					{
						if(run) anm.SetInteger ("Move", CanSwim?2:1); else  anm.SetInteger ("Move", CanSwim?1:2); 
            float ang=main.transform.eulerAngles.y+Mathf.Atan2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"))*Mathf.Rad2Deg;
            anm.SetFloat("Turn", ang); //Turn
					}
				}
        //Terrestrial
				else
				{
					if(Input.GetKey(KeyCode.Mouse1))
					{
						if(Input.GetAxis("Vertical")>0 && !run) anm.SetInteger ("Move", 1); //Forward
						else if(Input.GetAxis("Vertical")>0) anm.SetInteger ("Move", 2); //Run
						else if(Input.GetAxis("Vertical")<0) anm.SetInteger ("Move", -1);	//Backward
						else if(Input.GetAxis("Horizontal")>0) anm.SetInteger ("Move", -10); //Strafe-
						else if(Input.GetAxis("Horizontal")<0) anm.SetInteger ("Move", 10); //Strafe+
						anm.SetFloat("Turn", transform.eulerAngles.y+Input.GetAxis("Mouse X")*22.5f);//Mouse turn
					}
					else
					{
            float ang=main.transform.eulerAngles.y+Mathf.Atan2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"))*Mathf.Rad2Deg;
            anm.SetInteger ("Move", run?2:1); anm.SetFloat("Turn", ang); //Turn
					}
				}
			}
			//Stop
			else
			{
        //Flying/Swim
				if((CanSwim | CanFly) && !IsOnGround)
				{
					if(CanSwim && anm.GetFloat("Pitch")!=0 && !Input.GetKey(KeyCode.Mouse1)) anm.SetInteger ("Move", run?2:1);
					else anm.SetInteger ("Move", 0);
				}
         //Terrestrial
				else
				{
					if(Input.GetKey(KeyCode.Mouse1))
					{
						if(Input.GetAxis("Mouse X")>0) anm.SetInteger ("Move", 10); //Strafe- 
						else if(Input.GetAxis("Mouse X")<0) anm.SetInteger ("Move", -10); //Strafe+
						else anm.SetInteger ("Move", 0);
            anm.SetFloat("Turn", transform.eulerAngles.y+Input.GetAxis("Mouse X")*22.5f);//Mouse turn
					}
					else anm.SetInteger ("Move", 0); //Stop
				}
			}

      //Invert body (Ammonite & Cameroceras)
      if(CanInvertBody && Input.GetKeyDown(KeyCode.R)) { if(OnInvert) OnInvert=false; else OnInvert=true; }

			//Idles key
			if(Input.GetKey(KeyCode.E))
			{
				int idles=0; if(idle1>0) idles++; if(idle2>0) idles++; if(idle3>0) idles++; if(idle4>0) idles++; //idles to play
				rndIdle = Random.Range(1, idles+1);
		
        switch(rndIdle)
        {
				  case 1: anm.SetInteger ("Idle", idle1); break;
          case 2: anm.SetInteger ("Idle", idle2); break;
          case 3: anm.SetInteger ("Idle", idle3); break;
          case 4: anm.SetInteger ("Idle", idle4); break;
        }
			}
			else if(Input.GetKey(KeyCode.F)) //Eat / Drink
			{
				if(posTGT==Vector3.zero) FindPlayerFood(); //looking for food
				//Drink
				if(IsOnWater)
				{
					anm.SetInteger ("Idle", drink);
					if(Water<100) { behavior="Water"; Water=Mathf.Clamp(Water+0.05f, 0.0f, 100f); }
					if(Input.GetKeyUp(KeyCode.F)) posTGT=Vector3.zero;
					else posTGT=transform.position;
				}
				//Eat
				else if(posTGT!=Vector3.zero)
				{
					anm.SetInteger ("Idle", eat); behavior="Food";
					if(Food<100) Food=Mathf.Clamp(Food+0.05f, 0.0f, 100f);
					if(Water<25) Water+=0.05f;
					if(Input.GetKeyUp(KeyCode.F)) posTGT=Vector3.zero;
				}
				//nothing found
				else main.message=1;
      }
			//Sleep/Sit
			else if(Input.GetKey(KeyCode.Q))
			{ 
				anm.SetInteger("Idle", sleep);
				if(anm.GetInteger("Move")!=0) anm.SetInteger ("Idle", 0);
			}
			//Rise
			else if(rise!=0 && Input.GetKey(KeyCode.Space)) anm.SetInteger ("Idle", rise);
			else { anm.SetInteger ("Idle", 0); posTGT=Vector3.zero; }

      
      //Head move
      if(Input.GetKey(KeyCode.Mouse2))
      {
        OnHeadMove=true;
        headX=Mathf.Lerp(headX, Mathf.Clamp(headX-Input.GetAxis("Mouse X"), -Yaw_Max, Yaw_Max), 0.5f);
        headY=Mathf.Lerp(headY, Mathf.Clamp(headY+Input.GetAxis("Mouse Y"), -Pitch_Max, Pitch_Max),  0.5f);
      } else OnHeadMove=false;
      

      //Angle gap
      delta=Mathf.DeltaAngle(main.transform.eulerAngles.y, anm.GetFloat("Turn"));

			if(OnAnm.IsName(specie+"|Sleep"))
			{ behavior="Repose"; Stamina=Mathf.Clamp(Stamina+0.05f, 0.0f, 100f); }

		}
		// Not current camera target, reset parameters
		else
		{
			//anm.SetFloat("Turn", transform.eulerAngles.y);
      anm.SetInteger ("Move", 0); anm.SetInteger ("Idle", 0); //Stop
			if(CanAttack) anm.SetBool ("Attack", false);
			if(CanFly | CanSwim) anm.SetFloat ("Pitch", 0.0f);
		}
	}
  
  bool FindPlayerFood()
  {
		//Find carnivorous food (looking for a dead creature in range)
		if(!Herbivorous)
		{
			foreach (GameObject o in main.creaturesList.ToArray())
			{
				if((o.transform.position-Head.position).magnitude>boxscale.z) continue; //not in range
				Creature other= o.GetComponent<Creature>(); //Get other creature script
				if(other.IsDead) { objTGT=other.gameObject; posTGT = other.body.worldCenterOfMass; return true; } // meat found
			}
		}
		else
		{
			//Find herbivorous food (looking for trees/details on terrain in range )
			if(main.T)
			{
				//Large creature, look for trees
				if(withersSize>8) 
				{
          if(Physics.CheckSphere(Head.position, withersSize, main.treeLayer)) { posTGT = Head.position; return true; }
          else return false;
				}
				//Look for grass detail
				else
				{
					int layer=0;
					float x = ((transform.position.x - main.T.transform.position.x) / main.tdata.size.z * main.tres);
          float y = ((transform.position.z - main.T.transform.position.z) / main.tdata.size.x * main.tres);

					for(layer=0; layer<main.tdata.detailPrototypes.Length; layer++)
					{
						if(main.tdata.GetDetailLayer( (int) x,  (int) y, 1, 1, layer) [ 0, 0]>0)
						{
							posTGT.x=(main.tdata.size.x/main.tres)*x+main.T.transform.position.x;
							posTGT.z=(main.tdata.size.z/main.tres)*y+main.T.transform.position.z;
							posTGT.y = main.T.SampleHeight( new Vector3(posTGT.x, 0, posTGT.z)); 
							objTGT=null; return true; 
						}
					}
				}
			}
		}

		objTGT=null; posTGT=Vector3.zero; return false; //nothing found...
}
  public void AICore(int idle1=0, int idle2=0, int idle3=0, int idle4=0, int eat=0, int drink=0, int sleep=0)
	{
	  main.message=2; UseAI=false; return;
	}
  #endregion

}


