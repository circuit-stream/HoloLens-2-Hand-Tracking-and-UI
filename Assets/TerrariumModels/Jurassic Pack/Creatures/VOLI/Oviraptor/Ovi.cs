using UnityEngine;

public class Ovi : Creature
{
	public Transform Spine0,Spine1,Spine2,Spine3,Spine4,Spine5,Neck0,Neck1,Neck2,Neck3,Tail0,Tail1,Tail2,Tail3,Tail4,Tail5,Tail6,Tail7,Tail8,Tail9,Tail10,Tail11,
  Arm1,Arm2,Left_Hips,Right_Hips,Left_Leg,Right_Leg,Left_Foot0,Right_Foot0;
  public AudioClip Waterflush,Hit_jaw,Hit_head,Hit_tail,Smallstep,Smallsplash,Idlecarn,Swallow,Bite,Ovi1,Ovi2,Ovi3,Ovi4,Ovi5,Ovi6;
  Vector3 dir=Vector3.zero;

	//*************************************************************************************************************************************************
	//Play sound
	void OnCollisionStay(Collision col)
	{
		int rndPainsnd=Random.Range(0, 4); AudioClip painSnd=null;
		switch (rndPainsnd) { case 0: painSnd=Ovi1; break; case 1: painSnd=Ovi2; break; case 2: painSnd=Ovi3; break; case 3: painSnd=Ovi5; break; }
		ManageCollision(col, Pitch_Max, Crouch_Max, source, painSnd, Hit_jaw, Hit_head, Hit_tail);
	}

	void PlaySound(string name, int time)
	{
		if(time==currframe && lastframe!=currframe)
		{
			switch (name)
			{
			case "Step": source[1].pitch=Random.Range(0.75f, 1.25f); 
				if(IsInWater) source[1].PlayOneShot(Waterflush, Random.Range(0.25f, 0.5f));
				else if(IsOnWater) source[1].PlayOneShot(Smallsplash, Random.Range(0.25f, 0.5f));
				else if(IsOnGround) source[1].PlayOneShot(Smallstep, Random.Range(0.25f, 0.5f));
				lastframe=currframe; break;
			case "Bite": source[1].pitch=Random.Range(1.0f, 1.25f); source[1].PlayOneShot(Bite, 0.5f);
				lastframe=currframe; break;
			case "Die": source[1].pitch=Random.Range(0.8f, 1.0f); source[1].PlayOneShot(IsOnWater|IsInWater?Smallsplash:Smallstep, 1.0f);
				lastframe=currframe; IsDead=true; break;
			case "Food": source[0].pitch=Random.Range(3.0f, 3.5f); source[0].PlayOneShot(Swallow, 0.025f);
				lastframe=currframe; break;
			case "Repose": source[0].pitch=Random.Range(3.0f, 3.5f); source[0].PlayOneShot(Idlecarn, 0.25f);
				lastframe=currframe; break;
			case "Call": source[0].pitch=Random.Range(0.9F, 1.1F); source[0].PlayOneShot(Ovi4, 1.0f);
				lastframe=currframe; break;
			case "AtkA": int rnd1 = Random.Range (0, 2); source[0].pitch=Random.Range(1.0f, 1.25f);
				if(rnd1==0)source[0].PlayOneShot(Ovi2, 1.0f);
				else source[0].PlayOneShot(Ovi3, 1.0f);
				lastframe=currframe; break;
			case "AtkB": int rnd2 = Random.Range (0, 2); source[0].pitch=Random.Range(1.0f, 1.25f);
				if(rnd2==0)source[0].PlayOneShot(Ovi1, 1.0f);
				else source[0].PlayOneShot(Ovi6, 1.0f);
				lastframe=currframe; break;
			case "Growl": int rnd3 = Random.Range (0, 2); source[0].pitch=Random.Range(1.0f, 1.25f);
				if(rnd3==0)source[0].PlayOneShot(Ovi5, 1.0f);
				else source[0].PlayOneShot(Ovi6, 1.0f);
				lastframe=currframe; break;
			}
		}
	}

	//*************************************************************************************************************************************************
	// Add forces to the Rigidbody
	void FixedUpdate()
	{
		StatusUpdate(); if(!IsActive | AnimSpeed==0.0f) { body.Sleep(); return; }
		OnReset=false; OnAttack=false; IsConstrained=false;

		if(UseAI && Health!=0) { AICore(1, 2, 3, 4, 5, 6, 7); }// CPU
		else if(Health!=0) { GetUserInputs(1, 2, 3, 4, 5, 6, 7); }// Human
		else { anm.SetBool("Attack", false); anm.SetInteger ("Move", 0); anm.SetInteger ("Idle", -1); }//Dead

    //Set Y position
    if(IsOnGround | IsInWater | IsOnWater)
    {
      if(!IsOnGround) { body.drag=1; body.angularDrag=1; } else { body.drag=4; body.angularDrag=4; }
      ApplyYPos(); anm.SetBool("OnGround", true);
      dir=new Vector3(transform.forward.x, 0, transform.forward.z);
    }
    else { ApplyGravity(); anm.SetBool("OnGround", false); }

		//Stopped
		if(OnAnm.IsName("Ovi|IdleA") | OnAnm.IsName("Ovi|Die"))
		{
      Move(Vector3.zero);
			if(OnAnm.IsName("Ovi|Die")) { OnReset=true; if(!IsDead) { PlaySound("AtkB", 2); PlaySound("Die", 12); } }
		}

		//Jump
		else if(OnAnm.IsName("Ovi|IdleJumpStart") | OnAnm.IsName("Ovi|RunJumpStart") | OnAnm.IsName("Ovi|JumpIdle") |
			      OnAnm.IsName("Ovi|IdleJumpEnd") | OnAnm.IsName("Ovi|RunJumpEnd") | OnAnm.IsName("Ovi|JumpAtk"))
		{
			if(OnAnm.IsName("Ovi|IdleJumpStart") | OnAnm.IsName("Ovi|RunJumpStart"))
			{
				if(OnAnm.normalizedTime > 0.4) Move(Vector3.up, 3, true); else OnJump=true;
        if(anm.GetInteger("Move").Equals(2)) Move(dir, 160);
        else if(anm.GetInteger("Move").Equals(1)) Move(dir,32);
        PlaySound("Step", 1); PlaySound("Step", 2);
			}
			else if(OnAnm.IsName("Ovi|IdleJumpEnd") | OnAnm.IsName("Ovi|RunJumpEnd"))
			{ 
        if(OnAnm.IsName("Ovi|RunJumpEnd")) Move(dir, 160);
        body.velocity = new Vector3(body.velocity.x, 0.0f, body.velocity.z); OnJump=false;
				PlaySound("Step", 3); PlaySound("Step", 4); 
			}
      else if(OnAnm.IsName("Ovi|JumpAtk"))
      {
        if(anm.GetInteger("Move").Equals(1)|anm.GetInteger("Move").Equals(2)) Move(Vector3.Lerp(dir, Vector3.zero,0.5f), 160);
        OnAttack=true; PlaySound("AtkB", 1); PlaySound("Bite", 9);
        body.velocity = new Vector3(body.velocity.x, body.velocity.y>0?body.velocity.y:0, body.velocity.z);
      }
      else if(!anm.GetInteger("Move").Equals(0)) Move(Vector3.Lerp(dir, Vector3.zero, 0.5f), 160);
		}

		//Forward
		else if(OnAnm.IsName("Ovi|Walk") | OnAnm.IsName("Ovi|WalkGrowl"))
		{
			Move(transform.forward, 32);
			if(OnAnm.IsName("Ovi|Walk")){ PlaySound("Step", 6); PlaySound("Step", 14);}
			else if(OnAnm.IsName("Ovi|WalkGrowl")) { PlaySound("Growl", 2); PlaySound("Step", 6); PlaySound("Step", 14); }
		}

		//Running
		else if(OnAnm.IsName("Ovi|Run") | OnAnm.IsName("Ovi|RunGrowl") | OnAnm.IsName("Ovi|RunAtk1") |
		       (OnAnm.IsName("Ovi|RunAtk2") && OnAnm.normalizedTime < 0.9) |
		       (OnAnm.IsName("Ovi|IdleAtk3") && OnAnm.normalizedTime > 0.5 && OnAnm.normalizedTime < 0.9))
		{
      roll=Mathf.Clamp(Mathf.Lerp(roll, spineX*15.0f, 0.1f), -30f, 30f);
			Move(transform.forward, 160);
			if(OnAnm.IsName("Ovi|Run")){ PlaySound("Step", 4); PlaySound("Step", 12); }
			else if(OnAnm.IsName("Ovi|RunGrowl")) { PlaySound("AtkB", 2); PlaySound("Step", 4); PlaySound("Step", 12); }
			else if( OnAnm.IsName("Ovi|RunAtk1")) { OnAttack=true; PlaySound("AtkB", 2); PlaySound("Step", 4); PlaySound("Step", 12); }
			else if( OnAnm.IsName("Ovi|RunAtk2")| OnAnm.IsName("Ovi|IdleAtk3"))
			{ OnAttack=true; PlaySound("AtkA", 2); PlaySound("Step", 4); PlaySound("Bite", 9); PlaySound("Step", 12); }
		}
		
		//Backward
		else if(OnAnm.IsName("Ovi|Walk-") | OnAnm.IsName("Ovi|WalkGrowl-"))
		{
			if(OnAnm.normalizedTime > 0.25 && OnAnm.normalizedTime < 0.45 | 
			 OnAnm.normalizedTime > 0.75 && OnAnm.normalizedTime < 0.9) Move(-transform.forward, 32);
			if(OnAnm.IsName("Ovi|WalkGrowl-")) { PlaySound("Growl", 1); PlaySound("Step", 6); PlaySound("Step", 13); }
			else { PlaySound("Step", 6); PlaySound("Step", 13); }
		}

		//Strafe/Turn right
		else if(OnAnm.IsName("Ovi|Strafe-"))
		{
			Move(transform.right, 16);
			PlaySound("Step", 6); PlaySound("Step", 14);
		}
		
		//Strafe/Turn left
		else if(OnAnm.IsName("Ovi|Strafe+"))
		{
			Move(-transform.right, 16);
			PlaySound("Step", 6); PlaySound("Step", 14);
		}

		//Various
		else if(OnAnm.IsName("Ovi|IdleAtk3")) { OnAttack=true; Move(Vector3.zero); PlaySound("AtkB", 1); }
		else if(OnAnm.IsName("Ovi|GroundAtk")) { OnAttack=true; PlaySound("AtkB", 2); PlaySound("Bite", 4); }
		else if(OnAnm.IsName("Ovi|IdleAtk1") | OnAnm.IsName("Ovi|IdleAtk2"))
		{ OnAttack=true; Move(Vector3.zero); PlaySound("AtkB", 2); PlaySound("Bite", 9); }
		else if(OnAnm.IsName("Ovi|ToSleep")) { OnReset=true; IsConstrained=true; }
		else if(OnAnm.IsName("Ovi|Sleep")) { OnReset=true; IsConstrained=true; PlaySound("Repose", 1); }
		else if(OnAnm.IsName("Ovi|EatA")) { OnReset=true; IsConstrained=true; PlaySound("Food", 1); }
		else if(OnAnm.IsName("Ovi|EatB")) { OnReset=true; IsConstrained=true; PlaySound("Bite", 3); }
		else if(OnAnm.IsName("Ovi|EatC")) OnReset=true;
		else if(OnAnm.IsName("Ovi|IdleB")) { PlaySound("AtkB", 1); PlaySound("Bite", 7); PlaySound("Bite", 9); PlaySound("Bite", 11); }
		else if(OnAnm.IsName("Ovi|IdleC")) PlaySound("Growl", 1);
		else if(OnAnm.IsName("Ovi|IdleD")) { PlaySound("Call", 1); PlaySound("Call", 4); PlaySound("Call", 8); }
		else if(OnAnm.IsName("Ovi|IdleE")) { OnReset=true; PlaySound("Bite", 4); PlaySound("Bite", 7); PlaySound("Bite", 9); }
		else if(OnAnm.IsName("Ovi|Die-")) { OnReset=true; PlaySound("AtkA", 1);  IsDead=false; }

    RotateBone(IkType.SmBiped, 60f);
	}
  //*************************************************************************************************************************************************
	// Bone rotation
	void LateUpdate()
	{
		if(!IsActive) return; HeadPos=Head.GetChild(0).GetChild(0).position;
		float headZ =-headY*headX/Yaw_Max;
		Spine0.rotation*=Quaternion.Euler(-headY, headZ, headX);
		Spine1.rotation*=Quaternion.Euler(-headY, headZ, headX);
		Spine2.rotation*=Quaternion.Euler(-headY, headZ, headX);
		Spine3.rotation*=Quaternion.Euler(-headY, headZ, headX);
		Spine4.rotation*=Quaternion.Euler(-headY, headZ, headX);
		Spine5.rotation*=Quaternion.Euler(-headY, headZ, headX);
		Arm1.rotation*=Quaternion.Euler(headY*8, 0, 0);
		Arm2.rotation*=Quaternion.Euler(0, headY*8, 0);
		Neck0.rotation*=Quaternion.Euler(-headY, headZ, headX);
		Neck1.rotation*=Quaternion.Euler(-headY, headZ, headX);
		Neck2.rotation*=Quaternion.Euler(-headY, headZ, headX);
		Neck3.rotation*=Quaternion.Euler(-headY, headZ, headX);
		Head.rotation*=Quaternion.Euler(-headY, headZ, headX);
		Tail0.rotation*=Quaternion.Euler(0, 0, -spineX);
		Tail1.rotation*=Quaternion.Euler(0, 0, -spineX);
		Tail2.rotation*=Quaternion.Euler(0, 0, -spineX);
		Tail3.rotation*=Quaternion.Euler(0, 0, -spineX);
		Tail4.rotation*=Quaternion.Euler(0, 0, -spineX);
		Tail5.rotation*=Quaternion.Euler(0, 0, -spineX);
		Tail6.rotation*=Quaternion.Euler(0, 0, -spineX);
		Tail7.rotation*=Quaternion.Euler(0, 0, -spineX);
		Tail8.rotation*=Quaternion.Euler(0, 0, -spineX);
    Tail9.rotation*=Quaternion.Euler(0, 0, -spineX);
		Tail10.rotation*=Quaternion.Euler(0, 0, -spineX);
		Tail11.rotation*=Quaternion.Euler(0, 0, -spineX);
		Right_Hips.rotation*= Quaternion.Euler(-roll, 0, 0);
		Left_Hips.rotation*= Quaternion.Euler(0, roll, 0);
    if(!IsDead) Head.GetChild(0).transform.rotation*=Quaternion.Euler(lastHit, 0, 0);
		//Check for ground layer
		GetGroundPos(IkType.SmBiped, Right_Hips, Right_Leg, Right_Foot0, Left_Hips, Left_Leg, Left_Foot0);
	}
}