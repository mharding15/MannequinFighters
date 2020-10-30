using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

public class SetupManager : MonoBehaviour
{
    public int gameType; // 0 -> training (dummy), 1 -> human vs. ai, 2 -> ai vs. ai, 3 -> GP traning
    public int populationSize; // should match the number of agents in the "gpAgentsFile" if there is stuff in that file
    public int totalRounds; // # of rounds of GP to do
    public string agentFile1, agentFile2, gpAgentsFile;
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

    private DecisionTree[] population;
    private float[] features;
    private GameObject player1, player2;
    private int round, currentMatch;

    /* features are:
        x-dist
        y-dist
        fireball-dist (and + or - can maybe indicate direction, towards or away. Hmmm, maybe not. I could always just have it only care about fireballs that are  towards you)
        my health
        enemy's health 

        for now I will go with these 5, but might add later:
            time
            enemy crouching
            enemy blocking (not sure about this, or enemy attacking in some way) */

    // Start is called before the first frame update
    void Start(){
        // dummy training
        if (gameType == 0){
            // create both players
            GameObject p1 = CreatePlayer(1, true);
            GameObject p2 = CreatePlayer(0, false);
            // initialize both players
            p1.GetComponent<PlayerController>().Initialize(1, true);
            p2.GetComponent<PlayerController>().Initialize(0, false);
        // human vs. cpu
        } else if (gameType == 1){
            // create both players
            GameObject p1 = CreatePlayer(2, true);
            GameObject p2 = CreatePlayer(0, false);
            // initialize both players
            p1.GetComponent<PlayerController>().Initialize(2, true);
            p2.GetComponent<PlayerController>().Initialize(0, false);
        // cpu vs. cpu single match
        } else if (gameType == 2){
            // create both players
            GameObject p1 = CreatePlayer(2, true);
            GameObject p2 = CreatePlayer(2, false);
            // initialize both players
            p1.GetComponent<PlayerController>().Initialize(2, true);
            p2.GetComponent<PlayerController>().Initialize(2, false);
        // GP Training
        } else {
            // // 1. Load all decision trees from the file into the array
            //    // a. check if there is data to use
            //        // i. if there is
            //            // save the current data in the "previousData" file
            //        // ii. if there is not
            //            // create x random decision trees and write them to the file
            // // 2. for i in n rounds do
            //   // a. load the data from the file into the players and add players to the array
            //    // b. create a random purmutation of x (the population)
            //    // c. have each player play its corresponding random opponent (can just be the one after it in the array)
            //    // d. for each above match calculate the fitness of each player
            //    // e. store the highest fitness player's strategy (erase the file and write this line at the top)
            //    // f. user tournament selection to fill out the rest of the next rounds population

            population = new DecisionTree[populationSize];
            // 1)
            System.IO.StreamReader file = new System.IO.StreamReader("Assets/Output/GeneticAgents/" + gpAgentsFile + ".txt");

            string firstLine = file.ReadLine();
            round = 0;
            if (firstLine == null){
                print("The file is empty, need to create random agents.");
                for (int i = 0; i < populationSize; i++){
                    DecisionTree dt = new DecisionTree();
                    dt.RandomTree(5, 16);
                    population[i] = dt;
                }
                file.Close();
                // testing saving the trees to a file
                SaveDecisionTrees();
            } else {
                round = Int32.Parse(firstLine.Split(' ')[2]);
                print("The file is not empty, need to load the agents from the file.");
                int count = 0;
                string line = file.ReadLine();
                while(line != null){
                    if (count > populationSize){
                        print("*** the population size is too big for this file ***");
                    }
                    Queue<string> q = new Queue<string>(line.Split(' '));
                    DecisionTree dt = new DecisionTree();
                    dt.Load(q);
                    population[count++] = dt;
                    line = file.ReadLine();
                }
            }
            
            // 2) Create the first match
            currentMatch = 1;
            CreateGPMatch();
        }
    }

    GameObject CreatePlayer(int playerType, bool isPlayer1){
        print("In initializePlayer() and playerType is: " + playerType + ", isPlayer1 is: " + isPlayer1);
        GameObject player = null;
        // instantiate the player
        if (isPlayer1){
            player = Instantiate(character, new Vector2(-5f, 0f), Quaternion.identity);
            player.transform.GetChild(0).transform.GetComponent<SpriteRenderer>().color = new Color(.5f,0,0,1);
        } else {
            player = Instantiate(character, new Vector2(5f, 0f), Quaternion.identity);
        }

        // if the player is an AI, create its decision tree
        if (playerType == 2){
            print("In SetupManager.Start() and gameType was: " + gameType);
            player.GetComponent<AiController>().dt = GetDecisionTree(isPlayer1);
        }

        return player;
    }

    DecisionTree GetDecisionTree(bool isPlayer1){
        string filename = agentFile1;
        if (!isPlayer1){
            filename = agentFile2;
        } 
        // get decision tree 1 from the file and create it
        System.IO.StreamReader file = new System.IO.StreamReader("Assets/Output/TestAgents/" + filename + ".txt");
        string line = file.ReadLine();
        Queue<string> q = new Queue<string>(line.Split(' '));
        DecisionTree dt = new DecisionTree();
        dt.Load(q);
        print("*** The serialized tree is: " + dt.SerializeTree());

        file.Close();

        return dt;
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    //////////////////////////
    // *** GP Functions *** //
    //////////////////////////

    // creates the next match by just taking the next index and the one below it (counts by 2). Also the array is already randomized by the tournament selection.
    void CreateGPMatch(){
        print("In CreateGPMatch......");
        if (player1 != null){
            Destroy(player1);
        }
        if (player2 != null){
            Destroy(player2);
        }

        player1 = CreatePlayer(2, true);
        player2 = CreatePlayer(2, false);

        print("Created both players...");

        player1.GetComponent<AiController>().dt = population[currentMatch - 1];
        player2.GetComponent<AiController>().dt = population[currentMatch];

        player1.GetComponent<PlayerController>().Initialize(2, true);
        player2.GetComponent<PlayerController>().Initialize(2, true);

        StartCoroutine(Wait());
    }

    // the method is called when the match is over so that the fitness can be calculated for both agents and the next match can be started
    void MatchOver(){
        print("In match over....current match is: " + currentMatch);
    
        float healthP1 = player1.GetComponent<PlayerController>().GetMyHealth();
        float healthP2 = player2.GetComponent<PlayerController>().GetMyHealth();
        float distance = player1.GetComponent<PlayerController>().PlayerDistance();
        // TO DO: calculate the fitness for these two decision trees (maybe there should be a variable that stores the fitness in the DT, and maybe it could calculate it's own fitness too)
        population[currentMatch - 1].CalculateFitness(healthP1, healthP2, distance);
        population[currentMatch].CalculateFitness(healthP2, healthP1, distance);

        // print the results of the match
        print("Match #: " + currentMatch + "  over. P1 health: " + healthP1 + ", P2 health: " + healthP2 + ", distance: " + distance);
        print("  P1 Fitness: " + population[currentMatch - 1].fitness + ", P2 Fitness: " + population[currentMatch].fitness);

        // the round is over
        if ((currentMatch + 1) == populationSize){
            RoundOver();
        } else {
            currentMatch += 2;
            CreateGPMatch();
        }
    }

    void RoundOver(){

        print("In RoundOver()......");
        if (player1 != null){
            Destroy(player1);
        }
        if (player2 != null){
            Destroy(player2);
        }
        // DecisionTree[] temp = new DecisionTree[populationSize];
        // int tournamentSize = 2;
        // float absolutMaxFitness = float.MinValue;
        // DecisionTree absoluteFittest = null;
        // // do tournament selection to fill in the next population
        // for (int count = 1; count < populationSize; count++){
        //     List<int> indices = new List<int>();
        //     // get the random indices to compare
        //     while(indices.Count < tournamentSize){
        //         int r = UnityEngine.Random.Range(0, populationSize);
        //         while (indices.Contains(r)){
        //             r = UnityEngine.Random.Range(0, populationSize);
        //         }
        //     }
        //     // of those indices grab the one with the highest fitness
        //     float maxFitness = population[indices[0]].fitness;
        //     DecisionTree fittest = population[indices[0]];
        //     for (int i = 1; i < tournamentSize; i++){
        //         float fitness = population[indices[i]].fitness;
        //         if (fitness > maxFitness){
        //             maxFitness = fitness;
        //             fittest = population[indices[i]];
        //         }
        //         if (fitness > absolutMaxFitness){
        //             absolutMaxFitness = fitness;
        //             absoluteFittest = population[indices[i]];
        //         }
        //     }
        //     temp[count] = fittest;
        // }
        // temp[0] = absoluteFittest;
        
        // for (int i = 1; i < populationSize; i++){
        //     // with some probability mutate

        //     // with some probability do crossover
        //     if (i%2 == 0){
        //         temp[i].Crossover(temp[i - 1]);
        //     }
        // }

        // round++;
        // if (round == totalRounds){
        //     Finish();
        // }

        // currentMatch = 1; 
        // CreateGPMatch();
    }

    IEnumerator Wait(){       
        //yield on a new YieldInstruction that waits for 10 seconds.
        yield return new WaitForSeconds(10);
        print("The 10 seconds are up, calling MatchOver........");
        print("Would be calling match over, but not going to now....");
        MatchOver();
    }


    //////////////////////////////////
    // *** Saving/Loading Trees *** //
    //////////////////////////////////

    void SaveDecisionTrees(){
        // for each decision tree create a string representation of that tree

        // this will hold the trees and the top line which will have some info
        string[] serializedTrees = new string[populationSize + 1];

        // save some data at the top of the file (maybe I should save the date is was saved)
        serializedTrees[0] = "population size: " + populationSize + " round: " + round + " saved: " + GetTimestamp(DateTime.Now);
        for (int i = 1; i < populationSize + 1; i++){
            serializedTrees[i] = population[i - 1].SerializeTree();
        } 

        // first copy the old agents file (will overwrite the copy if it already exists)
        File.Copy("Assets/Output/GeneticAgents/" + gpAgentsFile + ".txt", "Assets/Output/GeneticAgents/Backups" + gpAgentsFile + "-cp.txt", true);

        // now can delete the old file
        File.Delete("Assets/Output/GeneticAgents/" + gpAgentsFile + ".txt");

        // and then recreate the file (this will also close the file, so no need to close it explicitly). Not sure if this '@' is necessary...
        System.IO.File.WriteAllLines(@"Assets/Output/GeneticAgents/" + gpAgentsFile + ".txt", serializedTrees);
    }

    String GetTimestamp(DateTime value){
        return value.ToString("yyyy-MM-dd HH:mm:ss.ffff");
    }

    void Finish(){
        print("### GP is Finished ###");
    }
}