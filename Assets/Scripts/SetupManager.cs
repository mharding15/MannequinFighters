using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using UnityEngine.UI;

public class SetupManager : MonoBehaviour
{
    public int gameType; // 0 -> training (dummy), 1 -> human vs. ai, 2 -> ai vs. ai, 3 -> GP traning
    public int populationSize; // should match the number of agents in the "gpAgentsFile" if there is stuff in that file
    public int totalRounds; // # of rounds of GP to do
    public int aggressiveness; // neutral (0), aggro (1), def (2)
    public int fitnessType; // 0 -> even, 1 -> aggressive
    public string agentFile1, agentFile2, gpAgentsFile, fitnessLogFile;
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
    private Text GpRoundText, GpMatchText, TimeText;

    // setting some variables for the training
    private int tournamentSize      = 3,
                freshTreeCount      = 0,
                matchTime           = 20,
                displayTime         = 0;
    private float mutateProb        = 0.2f, 
                crossoverProb       = 0.1f,
                freshTreePercentage = 0.1f,
                countTimer          = 0f;

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

        // get the text objects for the ui
        InstantiateTexts();
        SetVariables();

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
        } else if (gameType == 3) {

            population = new DecisionTree[populationSize];
            // 1)
            System.IO.StreamReader file = new System.IO.StreamReader("Assets/Output/GeneticAgents/" + gpAgentsFile + "/" + gpAgentsFile + ".txt");

            string firstLine = file.ReadLine();
            round = 0;
            if (firstLine == null){
                print("The file is empty, need to create random agents.");
                for (int i = 0; i < populationSize; i++){
                    DecisionTree dt = new DecisionTree();
                    dt.RandomTree(aggressiveness);
                    population[i] = dt;
                }
                file.Close();
                SaveDecisionTrees();
            } else {
                round = Int32.Parse(firstLine.Split(' ')[4]);
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

                // after loading the trees from a file need to shuffle them so that when merging two sets of trees they will not still be next to each other
                ShuffleTrees();
                file.Close();
            }

            // 2) Create the first match
            GpRoundText.text = round.ToString() + " of " + (totalRounds - 1).ToString();
            currentMatch = 0;
            CreateGPMatch();
        }
    }

    void InstantiateTexts(){
        GpRoundText = GameObject.FindWithTag("GpRound").GetComponent<Text>();
        GpMatchText = GameObject.FindWithTag("GpMatch").GetComponent<Text>();
        TimeText    = GameObject.FindWithTag("GameTime").GetComponent<Text>();
    }

    void SetVariables(){
        freshTreeCount = (int)Math.Floor(populationSize * freshTreePercentage);
    }

    void resetTime(){
        displayTime = matchTime;
        TimeText.text = displayTime.ToString();
    }

    GameObject CreatePlayer(int playerType, bool isPlayer1){

        GameObject player = InstantiatePlayer(isPlayer1);

        // if the player is an AI and we're not doing GP training, create its decision tree
        if (playerType == 2){
            player.GetComponent<AiController>().dt = GetDecisionTree(isPlayer1);
        }

        return player;
    }

    GameObject InstantiatePlayer(bool isPlayer1){
        GameObject player = null;
        // instantiate the player
        if (isPlayer1){
            player = Instantiate(character, new Vector2(-5f, 0f), Quaternion.identity);
            player.transform.GetChild(0).transform.GetComponent<SpriteRenderer>().color = new Color(.5f,0,0,1);
        } else {
            player = Instantiate(character, new Vector2(5f, 0f), Quaternion.identity);
        }

        return player;
    }

    // this method gets the decision from from either player 1's file or player 2's file. Not used during GP training.
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

        file.Close();

        return dt;
    }

    // Update is called once per frame
    void Update() {
        if (gameType > 1){
            countTimer += Time.deltaTime;
            if (countTimer >= 1f){
                displayTime -= 1;
                TimeText.text = displayTime.ToString();
                countTimer = 0f;
            }
        }
    }


    //////////////////////////
    // *** GP Functions *** //
    //////////////////////////

    // creates the next match by just taking the next index and the one below it (counts by 2). Also the array is already randomized by the tournament selection.
    void CreateGPMatch(){
        print("In CreateGPMatch......");
        // destroy the previous players so that new ones can be created
        if (player1 != null){
            Destroy(player1);
        }
        if (player2 != null){
            Destroy(player2);
        }

        // destroy all fireballs from the previous match
        GameObject[] fireballs = GameObject.FindGameObjectsWithTag("Fireball");
        foreach (GameObject go in fireballs){
            Destroy(go);
        }

        // update the current match in the UI
        GpMatchText.text = currentMatch.ToString() + " of " + (populationSize - 1).ToString();

        // instantiate the new players
        player1 = InstantiatePlayer(true);
        player2 = InstantiatePlayer(false);

        // assign the newly created players a decision tree from the population of decision trees
        player1.GetComponent<AiController>().dt = population[currentMatch];
        player2.GetComponent<AiController>().dt = population[(currentMatch + 1) % populationSize];

        // initialize the players so they can start fighting
        player1.GetComponent<PlayerController>().Initialize(2, true);
        player2.GetComponent<PlayerController>().Initialize(2, false);

        resetTime();
        StartCoroutine(Wait());
    }

    // the method is called when the match is over so that the fitness can be calculated for both agents and the next match can be started
    void MatchOver(){
        print("In match over....current match is: " + currentMatch);
    
        float healthP1      = player1.GetComponent<PlayerController>().GetMyHealth();
        float healthP2      = player2.GetComponent<PlayerController>().GetMyHealth();
        float distance      = player1.GetComponent<PlayerController>().PlayerDistance();
        float moveFitnessP1 = player1.GetComponent<PlayerController>().moveFitness;
        float moveFitnessP2 = player2.GetComponent<PlayerController>().moveFitness;

        // TO DO: calculate the fitness for these two decision trees (maybe there should be a variable that stores the fitness in the DT, and maybe it could calculate it's own fitness too)
        population[currentMatch].CalculateFitness(healthP1, healthP2, distance, moveFitnessP1, fitnessType);
        population[(currentMatch + 1) % populationSize].CalculateFitness(healthP2, healthP1, distance, moveFitnessP2, fitnessType);

        // print the results of the match
        print("Match #: " + currentMatch + "  over. P1 health: " + healthP1 + ", P2 health: " + healthP2 + ", distance: " + distance);
        print("  P1 Fitness: " + population[currentMatch].fitness + ", P2 Fitness: " + population[(currentMatch + 1) % populationSize].fitness);

        // the round is over
        if (currentMatch == populationSize - 1){
            RoundOver();
        } else {
            currentMatch++;
            CreateGPMatch();
        }
    }

    // this function is called when the round is over so that the next generation's population can be found
    void RoundOver(){

        // create an array that will temporarily store the trees for the next generation
        DecisionTree[] temp = new DecisionTree[populationSize];

        int absoluteFittestIndex = findFittest();
        print("Back in RoundOver(), the fittes index found is: " + absoluteFittestIndex);

        // do tournament selection to fill in the next population
        for (int count = 1; count < populationSize - freshTreeCount; count++){
            print("-- In tournament, count is: " + count);
            List<int> indices = new List<int>();
            // get the random indices to compare
            while(indices.Count < tournamentSize){
                int r = UnityEngine.Random.Range(0, populationSize);
                // can't have two indices that are the same
                while (indices.Contains(r)){
                    r = UnityEngine.Random.Range(0, populationSize);
                }
                indices.Add(r);
            }

            // of those indices grab the one with the highest fitness
            float maxFitness = population[indices[0]].fitness;
            int fittestIndex = indices[0];
            print("---- Finding the fittest...");
            for (int i = 1; i < tournamentSize; i++){
                print("------ i = " + i + " and fittest index is: " + fittestIndex + " with fitness: " + maxFitness);
                float fitness = population[indices[i]].fitness;
                print("------ Current index is: " + indices[i] + ", and current fitness is: " + fitness);
                if (fitness > maxFitness){
                    print("-------- And it was larger than the max.");
                    maxFitness = fitness;
                    fittestIndex = indices[i];
                }
            }
            // TESTING
            print("==== Fittest index found is: " + fittestIndex + ", and it's fitness is: " + maxFitness);
            temp[count] = population[fittestIndex].Copy();
        }

        // using elitism so the best tree will move on without being mutated or crossed over
        temp[0] = population[absoluteFittestIndex].Copy();
        
        // mutate and crossover
        for (int i = 1; i < populationSize - freshTreeCount; i++){
            // with some probability mutate
            if (UnityEngine.Random.Range(0f, 1f) < mutateProb){
                using (System.IO.StreamWriter logFile = new System.IO.StreamWriter(@"Assets/Output/GeneticAgents/" + gpAgentsFile + "/log.txt", true)){
                    logFile.WriteLine(CreateLogMsg("Mutating tree: " + i));
                }
                print("Mutating tree: " + i);
                temp[i].Mutate(aggressiveness);
            }
            // with some probability do crossover
            if (i%2 == 0){
                print("Crossing over " + (i - 1) + " and " + i);
                if (UnityEngine.Random.Range(0f, 1f) < crossoverProb){
                    print("Crossing over " + (i - 1) + " and " + i);
                    try {
                        temp[i].Crossover(temp[i - 1]);
                        // the 'using' statement automatically flushes and closes the file, so no need to do it explicitly
                        using (System.IO.StreamWriter logFile = new System.IO.StreamWriter(@"Assets/Output/GeneticAgents/" + gpAgentsFile + "/log.txt", true)){
                            logFile.WriteLine(CreateLogMsg("Successfully crossed over tree " + (i - 1) + " and tree " + i));
                        }
                    } catch (NullReferenceException e){
                        print("### Caught a NullReferenceException while attempting crossover");
                        // the 'using' statement automatically flushes and closes the file, so no need to do it explicitly
                        using (System.IO.StreamWriter errorFile = new System.IO.StreamWriter(@"Assets/Output/GeneticAgents/" + gpAgentsFile + "/errors.txt", true)){
                            errorFile.WriteLine(CreateLogMsg("Error crossing over trees " + (i - 1) + " and tree " + i));
                        }
                    }
                }
            }
        }

        // adding some fresh trees into the population to keep it from getting stale. Don't forget that before starting the next round, need to shuffle the population now.
        for (int i = populationSize - freshTreeCount; i < populationSize; i++){
            DecisionTree dt = new DecisionTree();
            dt.RandomTree(aggressiveness);
            temp[i] = dt;
        }

        population = temp;

        round++;
        GpRoundText.text = round.ToString() + " of " + (totalRounds - 1).ToString();
        if (round >= totalRounds){
            Finish();
        } else {
            // this will store the trees from the round, every round
            SaveDecisionTrees();
            // save sorted trees every 2 rounds (or if it is divisible by 5, so I can use 25 for analysis if I want)
            if (round % 2 == 0 || round % 5 == 0){
                SaveSortedDecisionTrees();
            }
            currentMatch = 0; 
            // need to reset the fitnesses now that the round is over
            for (int i = 0; i < populationSize; i++){
                population[i].fitness = 0f;
            }
            ShuffleTrees();
            CreateGPMatch();
        }
    }

    IEnumerator Wait(){      
        // yield on a new YieldInstruction that waits for 10 seconds.
        yield return new WaitForSeconds(matchTime);
        print("The " + matchTime + " seconds are up, calling MatchOver........");
        MatchOver();
    }


    //////////////////////////////////
    // *** Saving/Loading Trees *** //
    //////////////////////////////////

    void SaveDecisionTrees(){
        // this will hold the trees and the top line which will have some info
        string[] serializedTrees = new string[populationSize + 1];

        // save some data at the top of the file (maybe I should save the date is was saved)
        serializedTrees[0] = "population size: " + populationSize + " round: " + round + " saved: " + GetTimestamp(DateTime.Now) + " memory used (bytes): " + System.GC.GetTotalMemory(false) + " avg tree size: " + GetAvgTreeSize() + " avg fitness: " + GetAvgFitness();
        // for each tree, save it in the file (unsorted)
        for (int i = 1; i < populationSize + 1; i++){
            serializedTrees[i] = population[i - 1].SerializeTree();
        } 

        // first copy the old agents file (will overwrite the copy if it already exists). Added that it should go in it's own folder.
        File.Copy("Assets/Output/GeneticAgents/" + gpAgentsFile + "/" + gpAgentsFile + ".txt", "Assets/Output/GeneticAgents/Backups/" + gpAgentsFile + "-cp.txt", true);

        // now can delete the old file
        File.Delete("Assets/Output/GeneticAgents/" + gpAgentsFile + "/" + gpAgentsFile + ".txt");

        // and then recreate the file (this will also close the file, so no need to close it explicitly). Not sure if this '@' is necessary...
        System.IO.File.WriteAllLines(@"Assets/Output/GeneticAgents/" + gpAgentsFile + "/" + gpAgentsFile + ".txt", serializedTrees);
    }

    void SaveSortedDecisionTrees(){
        // this will hold the trees and the top line which will have some info
        string[] serializedTrees = new string[populationSize + 1];

        // save some data at the top of the file (maybe I should save the date is was saved)
        serializedTrees[0] = "population size: " + populationSize + " round: " + round + " saved: " + GetTimestamp(DateTime.Now) + " memory used (bytes): " + System.GC.GetTotalMemory(false) + " avg tree size: " + GetAvgTreeSize() + " avg fitness: " + GetAvgFitness();

        // now save a version where the trees are sorted by fitness
        DecisionTree[] sortedTrees = SortTreesByFitness();
        for (int i = 1; i < populationSize + 1; i++){
            serializedTrees[i] = sortedTrees[i - 1].SerializeTree();
        } 

        System.IO.File.WriteAllLines(@"Assets/Output/GeneticAgents/" + gpAgentsFile + "/" + gpAgentsFile + "-round-" + round + ".txt", serializedTrees);
    }

    // this function saves the average fitness and the top five fitness among the trees
    void SaveFitnesses(){
        // store the fitnesses
        string path = @"Assets/Output/GeneticAgents/" + fitnessLogFile + ".txt";
        StreamWriter sw;
        if (File.Exists(path)){
            sw = File.AppendText(path);
        } else {
            sw = new StreamWriter(path);
        }
        
        sw.WriteLine("population size: " + populationSize + " round: " + round + " saved: " + GetTimestamp(DateTime.Now));
        sw.WriteLine("Avg Fitness: " + AvgFitness());

        float[] sortedFitnesses = SortFitnesses();
        int numFitnessesToStore = 5;
        if (populationSize < 5){
            numFitnessesToStore = populationSize;
        }
        string fitLine = "";
        for (int i = 0; i < numFitnessesToStore; i++){
            fitLine += sortedFitnesses[i] + " ";
        }
        sw.WriteLine(fitLine);
        sw.WriteLine("=======================");
        sw.Close();
    }

    String GetTimestamp(DateTime value){
        return value.ToString("yyyy-MM-dd HH:mm:ss.ffff");
    }

    void Finish(){
        print("### GP is Finished ###");
        SaveDecisionTrees();
    }


    ///////////////////////////////
    // *** Utility Functions *** //
    ///////////////////////////////

    float AvgFitness(){
        float total = 0f;
        for (int i = 0; i < populationSize; i++){
            total += population[i].fitness;
        }
        return total/(float)populationSize;
    }

    float[] SortFitnesses(){
        float[] sortedFitness = new float[populationSize];
        for (int i = 0; i < populationSize; i++){
            sortedFitness[i] = population[i].fitness;
        }

        float temp;
        for (int i = 0; i < populationSize; i++){
            int maxIdx = i;
            for (int j = i + 1; j < populationSize; j++){
                if (sortedFitness[j] > sortedFitness[maxIdx]){
                    maxIdx = j;
                }
            }
            temp = sortedFitness[i];
            sortedFitness[i] = sortedFitness[maxIdx];
            sortedFitness[maxIdx] = temp; 
        }

        return sortedFitness;
    }

    DecisionTree[] SortTreesByFitness(){
        DecisionTree[] sortedTrees = new DecisionTree[populationSize];
        for (int i = 0; i < populationSize; i++){
            sortedTrees[i] = population[i].Copy();
        }

        DecisionTree temp;
        for (int i = 0; i < populationSize; i++){
            int maxIdx = i;
            for (int j = i + 1; j < populationSize; j++){
                if (sortedTrees[j].fitness > sortedTrees[maxIdx].fitness){
                    maxIdx = j;
                }
            }
            temp = sortedTrees[i];
            sortedTrees[i] = sortedTrees[maxIdx];
            sortedTrees[maxIdx] = temp; 
        }

        return sortedTrees;
    }

    void printFitnesses(){
        print("++Printing the population's fitnesses:");
        for (int i = 0; i < populationSize; i++){
            print("++++Index: " + i + ", fitness: " + population[i].fitness);
        }
    }

    int findFittest(){
        // TESTING
        print("++ Finding the fittest tree...");
        int maxIndex = 0;
        float maxFitness = population[0].fitness;
        for (int i = 1; i < populationSize; i++){
            float fitness = population[i].fitness;
            // TESTING
            print("++++ Current fittest index: " + maxIndex + ", with fitness of: " + maxFitness);
            print("==== Current index is: " + i + ", and current fitness is: " + fitness);
            if (fitness > maxFitness){
                maxFitness = fitness;
                maxIndex = i;
            }
        }
        return maxIndex;
    }

    // need to shuffle the trees after reading them from the file because when I combine the aggressive and defensive ones they will need to be randomized
    void ShuffleTrees() {
        List<int> indices = new List<int>();
        for (int i = 0; i < populationSize; i++){
            indices.Add(i);
        }
        DecisionTree[] temp = new DecisionTree[populationSize];
        for (int i = 0; i < populationSize; i++){
            int randIdx = UnityEngine.Random.Range(0, indices.Count);
            temp[i] = population[indices[randIdx]].Copy();
            indices.RemoveAt(randIdx);
        }

        population = temp;
    }

    // prints the serialized trees as they exist in the current population
    void PrintPopulation() {
        print("--Printing the population: ");
        for (int i = 0; i < populationSize; i++){
            print("----Tree " + i + ": " + population[i].SerializeTree());
        }
        print("--------------------------------------------------------");
    }

    string CreateLogMsg(string msg){
        return GetTimestamp(DateTime.Now) + " - Round: " + round + " :: " + msg;
    }

    float GetAvgTreeSize(){
        int totalSize = 0;
        for (int i = 0; i < populationSize; i++){
            totalSize += population[i].NodeCount();
        }
        return totalSize / populationSize;
    }

    float GetAvgFitness(){
        float totalFitness = 0f;
        for (int i = 0; i < populationSize; i++){
            totalFitness += population[i].fitness;
        }
        return totalFitness / populationSize;
    }

}