using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HumanController : MonoBehaviour
{
	public bool dummyStand, dummyJump, dummyCrouch, dummyKick, dummyPunch, dummyProjectile, dummyBlock, dummyCrouchBlock;

	private bool first;
	private float idleTime, idleTimer;
	private int IDLE = 0,
				WALK = 1,
				STAND_BLOCK = 2,
				STAND_KICK = 3,
				STAND_PUNCH = 4,
				ANTI_AIR = 5,
				PROJECTILE = 6,
				STAND_HIT = 7,
				CROUCH = 8,
				CROUCH_BLOCK = 9,
				CROUCH_KICK = 10,
				CROUCH_PUNCH = 11,
				CROUCH_HIT = 12,
				JUMP = 13,
				JUMP_KICK = 14,
				JUMP_PUNCH = 15,
				GRAPPLE = 16,
				GET_GRAPPLED = 17,
				FALLING = 18,
				DOWN = 19;

	private PlayerController pc;

	void Start()
	{
		pc = GetComponent<PlayerController>();
		SetVars();
	}

	void SetVars(){
		first = true;
		idleTimer = 0f;
		idleTime = 2f;
	}

	// a "dummy" is a training partner who isn't supposed to do anything
	void Update()
	{
    	if (pc.playerType == 0){
	        if (Input.GetKey(KeyCode.I)){
	        	if (Input.GetKey(KeyCode.S)){
	        		pc.CrouchKick();
        		} else {
        			pc.Kick();
        		}
	        } else if (Input.GetKey(KeyCode.O)){
        		if (Input.GetKey(KeyCode.S)){
        			pc.CrouchPunch();
        		} else {
        			pc.Punch();
        		}
	        } else if (Input.GetKey(KeyCode.P)){
	        	if (Input.GetKey(KeyCode.S)){
	        		pc.CrouchBlock();
	        	} else {
	        		pc.Block();
	        	}
	        } else if (Input.GetKey(KeyCode.J)){
	        	if (!Input.GetKey(KeyCode.S)){
	        		pc.Projectile();
	        	}
	        } else if (Input.GetKey(KeyCode.K)){
	        	if (!Input.GetKey(KeyCode.S)){
	        		pc.AntiAir();
	        	}
	        } else if (Input.GetKey(KeyCode.N)){
	        	pc.Grab();
	        } else if (Input.GetKey(KeyCode.B)){
	        	//pc.SetAnimBools(GET_GRAPPLED);
	        	pc.SetAnimBools(DOWN);
	        } else if (Input.GetKey(KeyCode.H)){
	        	if (Input.GetKey(KeyCode.S)){
	        		pc.SetAnimBools(CROUCH_HIT);
	        	} else {
	        		pc.SetAnimBools(STAND_HIT);
	        	}
	        } else if (Input.GetKey(KeyCode.S)){
	        	pc.Crouch();
	        } else if (Input.GetKey(KeyCode.W)){
	        	if (Input.GetKey(KeyCode.A)){
	        		if (pc.GetLeftSide()){
	        			pc.JumpBack();
	        		} else {
	        			pc.JumpForward();
	        		}
	        	} else if (Input.GetKey(KeyCode.D)){
	        		if (pc.GetLeftSide()){
	        			pc.JumpForward();
	        		} else {
	        			pc.JumpBack();
	        		}
	        	} else {
	        		pc.Jump();
	        	}
	        } else if (Input.GetKey(KeyCode.A)){
	        	if (pc.GetLeftSide()){
	        		pc.WalkBack();
	        	} else {
	        		pc.WalkForward();
	        	}
	        }else if (Input.GetKey(KeyCode.D)){
	        	if (pc.GetLeftSide()){
	        		pc.WalkForward();
	        	} else {
	        		pc.WalkBack();
	        	}
	        } else {
	        	// if (idleTimer > idleTime){
	        	// 	if (!pc.GetIsInAir()){
	        	// 		pc.Idle();
		        // 	} else {
		        // 		pc.Jump();
		        // 	}
		        // 	idleTimer = 0f;
	        	// }
	        }
	    } else if (pc.playerType == 1) {
	    	// for the dummy, so I can change it's behavior while playing.
	    	if (idleTimer > idleTime){
	    		print("The idletimer time is up...");
	    		if (dummyStand){
	    			pc.Idle();
	    		} else if (dummyCrouch){
	    			pc.Crouch();
	    		} else if (dummyKick){
	    			pc.Kick();
	    		} else if (dummyPunch){
	    			pc.Punch();
	    		} else if (dummyProjectile){
	    			pc.Projectile();
	    		} else if (dummyBlock){
	    			pc.Block();
	    		} else if (dummyCrouchBlock){
	    			pc.CrouchBlock();
	    		} else if (first) {
	    			pc.Jump();
	    			first = false;
	    		}
	    		idleTimer = 0f;
	    	} else {
	    		print("The idletimer time is NOOOT up...");
	    	}
	    }

	   idleTimer += Time.deltaTime;
	}
}
