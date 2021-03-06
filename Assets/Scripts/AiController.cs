﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AiController : MonoBehaviour
{
    public DecisionTree dt;

    private PlayerController pc;
    private float idleTime, idleTimer, actionTime, actionTimer;
    private bool makeDecision;
    private int currentAction;
    private string currentActionString;

    // Start is called before the first frame update
    void Start(){
        pc = GetComponent<PlayerController>(); 
        SetVars();
    }

    void SetVars(){
        idleTimer = 0f;
        idleTime = .15f;
        makeDecision = true;
        currentAction = -1;
        currentActionString = "none";
        actionTime = 1f;
        actionTimer = 0f;
    }

    // Update is called once per frame
    void Update(){
        //print("In AiController Update()...");
        if (pc.GetInitialized() && pc.playerType == 2){
            //print("In AiController and in the if....");
            // don't need to make a decision every frame (might adjust this value though)
            if (idleTimer > idleTime){
                // gather info to make decision based on
                float[] decisionData = getDecisionData();
                // get decision
                string actionString = dt.Decide(decisionData);
                // get the currentAction
                SetActionNum(actionString);
                // do the action
                DoAction();
                idleTimer = 0f;
            } else if (currentAction == 0 || currentAction == 1){
                DoAction();
            }
        }
        idleTimer += Time.deltaTime;
    }

    void SetActionNum(string actionString){
        // if the new action string is not equal to the old one, or the action time is up, need to reset the action
        if (actionString != currentActionString || actionTimer > actionTime){
            float randNum = UnityEngine.Random.Range(0f, 1f);
            string[] vals = actionString.Split(':');
            float action1Prob = float.Parse(vals[1]);
            if (action1Prob < randNum){
                currentAction = System.Int32.Parse(vals[0]);
            } else {
                currentAction = System.Int32.Parse(vals[2]);
            }
            currentActionString = actionString;
            actionTimer = 0f;
        }
        actionTimer += idleTime;
    }

    // 0 - x dist
    // 1 - y dist
    // 2 - fireball dist
    // 3 - my health
    // 4 - opponent's health
    float[] getDecisionData(){
        float x_dist = pc.PlayerDistance(), 
              y_dist = pc.PlayerYDistance(),
              fb_dist = GetFireballDistance(),  
              myhealth = pc.GetMyHealth(), 
              enemyHealth = pc.GetEnemyHealth();

        return new float[]{x_dist, y_dist, fb_dist, myhealth, enemyHealth};
    }

    // this method returns the distance to the closest fireball that is coming towards to player
    float GetFireballDistance(){
        float minDist = 30f; // basically infinite distance
        GameObject[] fireballs = GameObject.FindGameObjectsWithTag("Fireball");
        // print("### The number of fireballs is: " + fireballs.Length);
        foreach (GameObject go in fireballs){
            float vel = go.GetComponent<Rigidbody2D>().velocity.x;
            float dist = transform.position.x - go.transform.position.x;
            if (vel * dist >= 0){
                if (Mathf.Abs(dist) < minDist){
                    minDist = Mathf.Abs(dist);
                }
            }
        }
        return minDist;
    }

    //          move_forward = 0,
    //          move_backwards = 1,
    //          jump_up = 2,
    //          jump_forward = 3,
    //          jump_back = 4,
    //          crouch = 5, 
    //          crouch_kick = 6,
    //          crouch_punch = 7,
    //          kick = 8,
    //          punch = 9,
    //          projectile = 10,
    //          antiair = 11,
    //          grab = 12,
    //          stand_block = 13,
    //          crouch_block = 14,
    //          idle = 15;
    void DoAction(){

        // don't want the player to be able to do anything if they have just been hit or are currntly grappled
        if (pc.GetIsHit() || pc.GetIsGrappled() || pc.GetIsDown()){
            return;
        } 

        switch(currentAction){
            case 0:
                pc.WalkForward();
                break;
            case 1:
                pc.WalkBack();
                break;
            case 2:
                pc.Jump();
                break;
            case 3:
                pc.JumpForward();
                break;
            case 4:
                pc.JumpBack();
                break;
            case 5:
                pc.Crouch();
                break;
            case 6:
                pc.CrouchKick();
                break;
            case 7:
                pc.CrouchPunch();
                break;
            case 8:
                pc.Kick();
                break;
            case 9:
                pc.Punch();
                break;
            case 10:
                pc.Projectile();
                break;
            case 11:
                pc.AntiAir();
                break;
            case 12:
                pc.Grab();
                break;
            case 13:
                pc.Block();
                break;
            case 14:
                pc.CrouchBlock();
                break;
            case 15:
                pc.Idle();
                break;
        }
    }

}
