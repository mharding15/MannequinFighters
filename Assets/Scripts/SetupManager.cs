using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

public class SetupManager : MonoBehaviour
{
	// 0 -> training (dummy), 1 -> human vs. ai, 2 -> ai vs. ai
	public int gameType;
	public string agentName;
	public GameObject character;

	private int move_forward = 0,
				move_backwards = 1,
				jump_up = 2,
				jump_forward = 3,
				jump_back = 4,
				crouch = 5, 
				crouch_kick = 6,
				crouch_punch = 7,
				kick = 8,
				punch = 9,
				projectile = 10,
				antiair = 11,
				grab = 12,
				stand_block = 13,
				crouch_block = 14,
				idle = 15;

	private float[] features;
	/* features are:
		x-dist
		y-dist
		fireball-dist (and + or negative can maybe indicate direction, towards or away. Hmmm, maybe not. I could always just have it only care about fireballs that are come towards you)
		my health
		enemy's health 

		for now I will go with these 5, but might add later:
			time
			enemy crouching
			enemy blocking (not sure about this, or enemy attacking in some way) */

    // Start is called before the first frame update
    void Start()
    {
    	if (gameType == 0){
    		GameObject player1 = Instantiate(character, new Vector2(-5f, 0f), Quaternion.identity);
    		GameObject player2 = Instantiate(character, new Vector2(5f, 0f), Quaternion.identity);

    		// passing 1 (dummy), true (player1)
    		player1.GetComponent<PlayerController>().Initialize(1, true);
    		player1.transform.GetChild(0).transform.GetComponent<SpriteRenderer>().color = new Color(.5f,0,0,1);
    		// passing 0 (human), false (player2)
    		player2.GetComponent<PlayerController>().Initialize(0, false);

        } else if (gameType == 1){
        	// get the decision tree from the file and create it
        	System.IO.StreamReader file = new System.IO.StreamReader("Assets/Output/TestAgents/" + agentName + ".txt");
        	string line = file.ReadLine();
        	Queue<string> q = new Queue<string>(line.Split(' '));
        	DecisionTree dt = new DecisionTree();
        	dt.Load(q);
        	print("*** The serialized tree is: " + dt.SerializeTree());

        	// instantiate the two players
        	GameObject player1 = Instantiate(character, new Vector2(-5f, 0f), Quaternion.identity);
        	GameObject player2 = Instantiate(character, new Vector2(5f, 0f), Quaternion.identity);

        	// initialize player 1 (the AI)
        	player1.GetComponent<AiController>().dt = dt;
        	// passing 2 (AI), true (player1)
        	player1.GetComponent<PlayerController>().Initialize(2, true);

        	// passing 0 (Human), false (player2)
    		player2.GetComponent<PlayerController>().Initialize(0, false);
        }
    }



    // Update is called once per frame
    void Update()
    {
        
    }
}
