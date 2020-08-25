using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
	public float speed = 5, jumpForce = 8, dpforward = 4;
	public bool isPlayer1;
	public GameObject fireball;
	public int playerType { get; set; } // 0 = human, 1 = dummy, 2 = AI

	private Animator animator;
	private Rigidbody2D rb2d;
	private Transform enemy;
	private float horizontal, vertical; //idleTime, idleTimer;
	private bool isIdle, isWalking, isStandBlocking, isStandKicking, isStandPunching, isAntiAir, isProjectile, isStandHit, 
					isCrouching, isCrouchBlocking, isCrouchKicking, isCrouchPunching, isCrouchHit,
					isJumping, isJumpKicking, isJumpPunching,
					isGrappling, isGettingGrappled, isGrappleWhiff,
					isFalling, isDown,
					isInAir, leftSide;
	private bool falling, first, idleKickSwitch,
				 crouching, standing,
				 kicking, punching, projectiling, antiAiring, blocking, hit, grappled,
				 initialized;

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
				DOWN = 19,
				GRAPPLE_WHIFF = 20;

	private float health; // start with 100 health
	private float FIREBALL_DAMAGE = 10,
				FIREBALL_CHIP = 3,
				KICK_DAMAGE = 20,
				PUNCH_DAMAGE = 15,
				ANTIAIR_DAMAGE = 25,
				ANTIAIR_CHIP = 10,
				GRAPPLE_DAMAGE = 30;

	private Text playerHealthText;

    // Start is called before the first frame update
    void Start()
    {
  //   	if (playerType == 0){
		// 	print("!!! I am the human and the I am in the START() method....");
		// } else if (playerType == 1){
		// 	print("@@@ I am the dummy and the I am in the START() method....");
		// } else {
		// 	print("### I am Neither the Human or the Dummy and I am in the start method");
		// }
  //       animator = GetComponent<Animator>();
  //       rb2d = GetComponent<Rigidbody2D>();
  //       GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
  //       foreach (GameObject go in players){
  //       	if (go.transform != this.transform){
  //       		if (playerType == 0){
  //       			print("!!! I am the human and the enemy is getting set....");
  //       		} else if (playerType == 1){
  //       			print("@@@ I am the dummy and the enemy is getting set...");
  //       		}
  //       		enemy = go.transform;
  //       	}
  //       }
  //       SetBools();
  //       SetStats();
  //       DirectionCheck();

    }

    public void Initialize(int type, bool player1){
    	isPlayer1 = player1;
    	playerType = type;
        animator = GetComponent<Animator>();
        rb2d = GetComponent<Rigidbody2D>();
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject go in players){
        	if (go.transform != this.transform){
        		if (playerType == 0){
        			//print("!!! I am the human and the enemy is getting set....");
        		} else if (playerType == 1){
        			//print("@@@ I am the dummy and the enemy is getting set...");
        		}
        		enemy = go.transform;
        	}
        }
        SetBools();
        FindHealthText();
        SetStats();
        DirectionCheck();
        initialized = true;
    }

    void FindHealthText(){
    	if (isPlayer1){
    		playerHealthText = GameObject.FindWithTag("P1Health").GetComponent<Text>();;
    	} else {
    		playerHealthText = GameObject.FindWithTag("P2Health").GetComponent<Text>();;
    	}
    }

    void SetBools()
    {
    	isCrouching = false;
    	isInAir = true;
    	isIdle = true;
    	falling = false;
    	crouching = false;
    }

    void SetStats()
    {
    	health = 100;
    	SetHealthText();
    }

    // Update is called once per frame
    void Update()
	{
    	if (initialized){
    		GameOverCheck();
    		DirectionCheck();
    	}	
    }

    void GameOverCheck()
    {
    	if (health <= 0){
    		if (isPlayer1){
    			print("*** This is the Player1...and I lost.");
    		} else {
    			print("*** This is the Player2...and I lost.");
    		}
    	}
    }

    void DirectionCheck()
    {
    	if (transform.position.x < enemy.position.x){
    		transform.localScale = new Vector2(-1f, 1f);
    		leftSide = true;
    	} else {
    		transform.localScale = new Vector2(1f, 1f);
    		leftSide = false;
    	}
    }

    public float PlayerDistance()
    {
    	float dist = transform.position.x - enemy.position.x;
    	return Mathf.Abs(dist);
    }

    public float PlayerYDistance()
    {
    	float dist = transform.position.y - enemy.position.y;
    	return Mathf.Abs(dist);
    }

    // *** Triggered Methods *** //

    void OnCollisionEnter2D(Collision2D col)
    {
    	if (col.collider.tag == "Ground"){
    		isInAir = false;
    		if (falling){
    			falling = false;
    			SetAnimBools(DOWN);
    		} else {
    			Idle();
    		}
    	} 
    }

    void OnCollisionExit2D(Collision2D col)
    {
    	if (col.collider.tag == "Ground"){
    		isInAir = true;
    	}
    }

    void OnTriggerEnter2D(Collider2D col)
    {
    	print("!@#$ Should be recognizing that there was a trigger at least...");
    	// set the appropriate animation
    	if (isInAir){
    		if (col.tag != "Player"){
    			print("### I'm in the air =====");
				float xvel = -5f;
				if (transform.position.x > enemy.position.x){
					xvel *= -1f;
				}
				rb2d.velocity = new Vector2(xvel, rb2d.velocity.y);
				falling = true;
				SetAnimBools(FALLING);
    		}
			// first = false;
		} else if (!blocking && col.tag != "Player"){
			hit = true;
			if (!crouching) {
    			print("$$$ Setting the hit bool...");
    			SetAnimBools(STAND_HIT);
    		} else {
    			print("$$$ Setting the CROUCH hit bool...");
    			SetAnimBools(CROUCH_HIT);
    		}
		}

		// do the damage from the attack
		if (col.tag == "Fireball"){
    		//print("@@@ Got hit with a fireball...");
    		if (!blocking){
    			health -= FIREBALL_DAMAGE;
    		} else {
    			health -= FIREBALL_CHIP;
    		}
    		Destroy(col.gameObject);
    	} else if (col.tag == "Kick"){
    		if (!blocking){
    			health -= KICK_DAMAGE;
    		}
    	} else if (col.tag == "Punch"){
    		if (!blocking){
    			health -= PUNCH_DAMAGE;
    		}
    	} else if (col.tag == "AntiAir"){
    		if (!blocking){
    			health -= ANTIAIR_DAMAGE;
    		} else {
    			health -= ANTIAIR_CHIP;
    		}
    	}

    	SetHealthText();
    }

    void ShootFireball()
    {
    	float fireBallSpace = 0.8f, fireBallVelocity = 5f, fireBallDirection = -1f;
    	if (!leftSide){
    		fireBallSpace *= -1f;
    		fireBallVelocity *= -1f;
    		fireBallDirection = 1f;
    	}
		GameObject fb = Instantiate(fireball, transform.position + new Vector3(fireBallSpace, 0f, 0f), transform.rotation);
		fb.transform.localScale = new Vector2(fireBallDirection, 1f);
    	Rigidbody2D fb_rb = fb.GetComponent<Rigidbody2D>();
    	fb_rb.velocity = new Vector3(fireBallVelocity, 0f, 0f);

    	// always destroy the fireball after 10 seconds no matter what (so I don't have a bunch of fireballs just hanging around)
    	Destroy(fb, 10);
    }

    public void AntiAirMotion(int start)
    {
    	if (start == 0){
    		if (leftSide){
    			rb2d.velocity = new Vector2(dpforward, jumpForce);
    		} else {
    			rb2d.velocity = new Vector2(-1f * dpforward, jumpForce);
    		}
    	} else {
    		rb2d.velocity = new Vector2(0f, rb2d.velocity.y);
    	}
    }

    public void Grapple()
    {
		print("### The other dude should be getting grappled");
		enemy.gameObject.GetComponent<PlayerController>().GetGrappled();
    }

    public void GetGrappled(){
    	health -= GRAPPLE_DAMAGE;
    	SetAnimBools(GET_GRAPPLED);
    }

    // *** Getters for player bools *** //

    public bool GetIsInAir(){ return isInAir; }

    public bool GetCrouching(){ return crouching; }

    public bool GetLeftSide(){ return leftSide; }

    public bool GetInitialized(){ return initialized; }

    public float GetMyHealth(){ return health; }

    public float GetEnemyHealth() { return enemy.gameObject.GetComponent<PlayerController>().GetMyHealth(); }

    public void SetStateToIdle(){
    	print("!!! Setting animation to IDLE...");
    	if (isInAir){
    		SetAnimBools(JUMP);
    	} else if (crouching && (kicking || punching)) {
    		SetAnimBools(CROUCH);
    	} else {
    		SetAnimBools(IDLE);
    	}
    }

    public void SetState(int state) 
    { 
    	int action = state % 10;
    	int stature = (state / 10) % 10;

    	SetStaturesToFalse();
    	SetActionsToFalse();

    	switch(stature){
    		case 1: 
    			crouching = true;
    			break;
    		case 2:
    			standing = true; 
    			break;
    	}

    	// 0 is reserved for NO ACTION
    	switch(action){
    		case 1:
    			print("$$$ Setting kicking to true");
    			kicking = true;
    			break;
    		case 2:
    			print("$$$ Setting punching to true");
    			punching = true;
    			break;
    		case 3:
    			projectiling = true;
    			break;
    		case 4:
    			antiAiring = true;
    			break;
    		case 5:
    			blocking = true;
    			break;
    		case 6:
    			hit = true;
    			break;
    		case 7:
    			grappled = true;
    			break;
    	}
    }

    void SetStaturesToFalse()
    {
    	crouching = false;
    	standing = false;
    }

    void SetActionsToFalse()
    {
    	kicking = false;
    	punching = false;
    	projectiling = false;
    	antiAiring = false;
    	blocking = false;
    	hit = false;
    	grappled = false;
    }

    //public bool GetIsBlocking(){ return isBlocking; }

    // *** UI Elements *** //

    void SetHealthText(){
    	playerHealthText.text = health.ToString();
    }

    // *** Actions *** //

    public void Idle(){
    	if (!kicking && !punching){
    		if (!isInAir){
    			SetAnimBools(IDLE);
	    	} else {
	    		SetAnimBools(JUMP);
	    	}
    	}
    }

    public void WalkForward(){
    	if (!isInAir && !grappled){
    		if (leftSide){
    			transform.Translate(new Vector2(speed * Time.deltaTime, 0f));
    		} else {
    			transform.Translate(new Vector2(-1f * speed * Time.deltaTime, 0f));
    		}
    		SetAnimBools(WALK);
    	}
    }

    public void WalkBack(){
    	if (!isInAir && !grappled){
    		if (leftSide){
    			transform.Translate(new Vector2(-1f * speed * Time.deltaTime, 0f));
    		} else {
    			transform.Translate(new Vector2(speed * Time.deltaTime, 0f));
    		}
    		SetAnimBools(WALK);
    	}
    }

    public void Jump(){
    	if (!isInAir && !grappled){
    		rb2d.velocity = new Vector2(0f, jumpForce);
    		SetAnimBools(JUMP);
    	}
    }

    public void JumpForward(){
    	if (!isInAir && !grappled){
    		float xvel = -1f * speed;
    		if (leftSide){
    			xvel = speed;
    		} 
    		rb2d.velocity = new Vector2(xvel, jumpForce);
    		SetAnimBools(JUMP);
    	}
    }

    public void JumpBack(){
    	if (!isInAir && !grappled){
    		float xvel = speed;
    		if (leftSide){
    			xvel = -1f * speed;
    		}
    		rb2d.velocity = new Vector2(xvel, jumpForce);
    		SetAnimBools(JUMP);
    	}
    }

    public void Crouch(){
    	if (!kicking && !punching){
    		SetAnimBools(CROUCH);
    	}
    }

    public void Kick(){
    	if (isInAir){
    		SetAnimBools(JUMP_KICK);
    	} else{
    		SetAnimBools(STAND_KICK);
    	}
    }

    public void CrouchKick(){
    	SetAnimBools(CROUCH_KICK);
    }

    public void Punch(){
    	if (isInAir){
    		SetAnimBools(JUMP_PUNCH);
    	} else {
    		SetAnimBools(STAND_PUNCH);
    	}
    }

    public void CrouchPunch(){
    	SetAnimBools(CROUCH_PUNCH);
    }

    public void Block(){
    	SetAnimBools(STAND_BLOCK);
    }

    public void CrouchBlock(){
    	SetAnimBools(CROUCH_BLOCK);
    }

    public void Projectile(){
    	SetAnimBools(PROJECTILE);
    }

    public void AntiAir(){
    	SetAnimBools(ANTI_AIR);
    }

    public void Grab(){
    	if (PlayerDistance() < 2f && PlayerYDistance() < 1f){
    		SetAnimBools(GRAPPLE);
    	} else {
    		SetAnimBools(GRAPPLE_WHIFF);
    	}
    	
    }

    // *** ANIMATIONS *** //

    // this method can be called from outside of this script to set the animation for the character
    public void SetAnimBools(int state)
    {
        SetAllToFalse();

        switch(state){
            case 0:
                isIdle = true;
                break;
            case 1:
                isWalking = true;
                break;
            case 2:
                isStandBlocking = true;
                break;
            case 3:
                isStandKicking = true;
                break;
            case 4:
                isStandPunching = true;
                break;
            case 5:
                isAntiAir = true;
                break;
            case 6:
            	isProjectile = true;
            	break;
           	case 7:
           		isStandHit = true;
           		break;
           	case 8:
           		isCrouching = true;
           		break;
           	case 9:
           		isCrouchBlocking = true;
           		break;
           	case 10:
           		isCrouchKicking = true;
           		break;
           	case 11:
           		isCrouchPunching = true;
           		break;
           	case 12:
           		isCrouchHit = true;
           		break;
           	case 13:
           		isJumping = true;
           		break;
           	case 14:
           		isJumpKicking = true;
           		break;
           	case 15:
           		isJumpPunching = true;
           		break;
           	case 16:
           		isGrappling = true;
           		break;
           	case 17:
           		isGettingGrappled = true;
           		break;
           	case 18:
           		isFalling = true;
           		break;
           	case 19:
           		isDown = true;
           		break;
           	case 20:
           		isGrappleWhiff = true;
           		break;
        }

        animator.SetBool("isIdle", isIdle);
        animator.SetBool("isWalking", isWalking);
        animator.SetBool("isStandBlocking", isStandBlocking);
        animator.SetBool("isStandKicking", isStandKicking);
        animator.SetBool("isStandPunching", isStandPunching);
        animator.SetBool("isAntiAir", isAntiAir);
        animator.SetBool("isProjectile", isProjectile);
        animator.SetBool("isStandHit", isStandHit);
        animator.SetBool("isCrouching", isCrouching);
        animator.SetBool("isCrouchBlocking", isCrouchBlocking);
        animator.SetBool("isCrouchKicking", isCrouchKicking);
        animator.SetBool("isCrouchPunching", isCrouchPunching);
        animator.SetBool("isCrouchHit", isCrouchHit);
        animator.SetBool("isJumping", isJumping);
        animator.SetBool("isJumpKicking", isJumpKicking);
        animator.SetBool("isJumpPunching", isJumpPunching);
        animator.SetBool("isGrappling", isGrappling);
        animator.SetBool("isGettingGrappled", isGettingGrappled);
        animator.SetBool("isFalling", isFalling);
        animator.SetBool("isDown", isDown);
        animator.SetBool("isGrappleWhiff", isGrappleWhiff);
    }

    void SetAllToFalse()
    {
    	isIdle = false;
        isWalking = false;
        isStandBlocking = false;
        isStandKicking = false;
        isStandPunching = false;
        isAntiAir = false;
        isProjectile = false;
        isStandHit = false;
        isCrouching = false;
        isCrouchBlocking = false;
        isCrouchKicking = false;
        isCrouchPunching = false;
        isCrouchHit = false;
        isJumping = false;
        isJumpKicking = false;
        isJumpPunching = false;
        isGrappling = false;
        isGettingGrappled = false;
        isFalling = false;
        isDown = false;
        isGrappleWhiff = false;
    }
}
