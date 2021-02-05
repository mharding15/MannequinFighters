using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using UnityEngine.UI;

public class SetupManager : MonoBehaviour
{
    public int gameType; // 0 -> training (dummy), 1 -> human vs. ai, 2 -> ai vs. ai, 3 -> GP traning, 4 -> evalFile1 vs. evalFile2, 5 -> evalFile1 vs. Ai, 6 -> evalFile1 vs. random
    public int populationSize; // should match the number of agents in the "gpAgentsFile" if there is stuff in that file
    public int totalRounds; // # of rounds of GP to do
    public int aggressiveness; // neutral (0), aggro (1), def (2)
    public int fitnessType; // 0 -> even, 1 -> aggressive
    public bool useAiTrainer;
    public string agentFile1, agentFile2, gpAgentsFile, fitnessLogFile, aiTrainerFile, inputFilesFile, evalAiFileName;
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
    private DecisionTree aiTrainer;
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

    // *** evaluation variables *** //

    private DecisionTree[] eval1Trees, eval2Trees;
    private int eval1Pos, eval2Pos, 
                inputFiles1Idx, inputFiles2Idx;
    // these are holding the amount of damage inflicted on the opponent, so higher is better
    private float eval1DamageRoundTotal, eval2DamageRoundTotal,
                    eval1DamageTotal, eval2DamageTotal;
    private string[] inputFiles1, inputFiles2;
    private string evalOutputFileName;

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
                SaveSortedDecisionTrees();
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

        // doing evaluation
        } else {

            System.IO.StreamReader file = new System.IO.StreamReader("Assets/Output/Evaluation/Collections/" + inputFilesFile + ".txt");
            string firstLine = file.ReadLine();

            int numFiles = Int32.Parse(firstLine.Split(' ')[1]);
            inputFiles1 = new string[numFiles];
            if (gameType == 4){
                inputFiles2 = new string[numFiles];
            }

            string line = file.ReadLine();
            int count = 0;
            while(line != null){
                string[] vals = line.Split(' ');
                inputFiles1[count] = vals[0];
                if (vals.Length == 2){
                    inputFiles2[count] = vals[1];
                }
                count++;
                line = file.ReadLine();
            }
            file.Close();

            inputFiles1Idx = 0;
            inputFiles2Idx = 0;

            SetUpEvaluation();
        }
    }

    void InstantiateTexts(){
        GpRoundText = GameObject.FindWithTag("GpRound").GetComponent<Text>();
        GpMatchText = GameObject.FindWithTag("GpMatch").GetComponent<Text>();
        TimeText    = GameObject.FindWithTag("GameTime").GetComponent<Text>();
    }

    void SetVariables(){
        freshTreeCount = (int)Math.Floor(populationSize * freshTreePercentage);
        // if this is using an aiTrainer, then need to load the ai trainer from the file
        if (useAiTrainer){
            System.IO.StreamReader file = new System.IO.StreamReader("Assets/Output/testAgents/" + aiTrainerFile + ".txt");
            string line = file.ReadLine();
            Queue<string> q = new Queue<string>(line.Split(' '));
            aiTrainer = new DecisionTree();
            aiTrainer.Load(q);
            file.Close();
        }
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
        print("1122 the filename is: " + filename);
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
        if (useAiTrainer){
            player2.GetComponent<AiController>().dt = aiTrainer;
        } else {
            player2.GetComponent<AiController>().dt = population[(currentMatch + 1) % populationSize];
        }

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
        if (!useAiTrainer){
            population[(currentMatch + 1) % populationSize].CalculateFitness(healthP2, healthP1, distance, moveFitnessP2, fitnessType);
        }

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


    //////////////////////////////
    // *** Evaluating Trees *** //
    //////////////////////////////

    void SetUpEvaluation(){

        eval1Trees = GetEvalDecisionTrees(inputFiles1[inputFiles1Idx]);

        // evalFile1 vs. evalFile2
        if (gameType == 4){
            eval2Trees = GetEvalDecisionTrees(inputFiles2[inputFiles2Idx]);
            evalOutputFileName = "versusResults/" + inputFiles1[inputFiles1Idx] + "_vs_" + inputFiles2[inputFiles2Idx];
            using (System.IO.StreamWriter resultsFile = new System.IO.StreamWriter(@"Assets/Output/Evaluation/Results/" + evalOutputFileName + ".txt", true)){
                resultsFile.WriteLine(inputFiles1[inputFiles1Idx] + " VS. " + inputFiles2[inputFiles2Idx] + "\n");
            }
        // evalFile1 vs. hardcoded Ai
        } else if (gameType == 5){
            eval2Trees = GetEvalAiTrees(evalAiFileName, eval1Trees.Length);
            evalOutputFileName = "aiResults/" + inputFiles1[inputFiles1Idx] + "_vs_ai";
            using (System.IO.StreamWriter resultsFile = new System.IO.StreamWriter(@"Assets/Output/Evaluation/Results/" + evalOutputFileName + ".txt", true)){
                resultsFile.WriteLine(inputFiles1[inputFiles1Idx] + " VS. AI" + "\n");
            }
        // evalFile1 vs. random trees
        } else if (gameType == 6) {
            eval2Trees = new DecisionTree[eval1Trees.Length];
            for (int i = 0; i < eval2Trees.Length; i++){
                DecisionTree dt = new DecisionTree();
                dt.RandomTree(0);
                eval2Trees[i] = dt;
            }
            evalOutputFileName = "randomResults/" + inputFiles1[inputFiles1Idx] + "_vs_random";
            using (System.IO.StreamWriter resultsFile = new System.IO.StreamWriter(@"Assets/Output/Evaluation/Results/" + evalOutputFileName + ".txt", true)){
                resultsFile.WriteLine(inputFiles1[inputFiles1Idx] + " VS. Random" + "\n");
            }
        } else {
            print("*** The input for game type was not 0 - 6, so not sure what you're trying to do ***");
        }
        eval1Pos = 0;
        eval2Pos = 0;
        eval1DamageRoundTotal = 0f;
        eval2DamageRoundTotal = 0f;
        eval1DamageTotal = 0f;
        eval2DamageTotal = 0f;

        CreateEvalMatch();
    }

    DecisionTree[] GetEvalDecisionTrees(string filename){
        System.IO.StreamReader evalFile = new System.IO.StreamReader("Assets/Output/Evaluation/Agents/" + filename + ".txt");
        string firstLine = evalFile.ReadLine();
        int evalTreeSize = Int32.Parse(firstLine.Split(' ')[1]);
        DecisionTree[] trees = new DecisionTree[evalTreeSize];

        int count = 0;
        string line = evalFile.ReadLine();
        while(line != null){
            if (count > evalTreeSize){
                print("*** the evaluation file's size does not match it's labelled size ***");
            }
            Queue<string> q = new Queue<string>(line.Split(' '));
            DecisionTree dt = new DecisionTree();
            dt.Load(q);
            trees[count++] = dt;
            line = evalFile.ReadLine();
        }
        evalFile.Close();

        return trees;
    }

    DecisionTree[] GetEvalAiTrees(string filename, int numTrees){
        System.IO.StreamReader evalFile = new System.IO.StreamReader("Assets/Output/Evaluation/Agents/" + filename + ".txt");
        string firstLine = evalFile.ReadLine();
        DecisionTree[] trees = new DecisionTree[numTrees];
        string line = evalFile.ReadLine();

        Queue<string> q = new Queue<string>(line.Split(' '));
        DecisionTree dt = new DecisionTree();
        dt.Load(q);

        for (int i = 0; i < numTrees; i++){
            trees[i] = dt;
        }
        evalFile.Close();

        return trees;
    }

    // creates the next match by just taking the next index and the one below it (counts by 2). Also the array is already randomized by the tournament selection.
    void CreateEvalMatch(){

        if (eval1Pos == 0){
            using (System.IO.StreamWriter resultsFile = new System.IO.StreamWriter(@"Assets/Output/Evaluation/Results/" + evalOutputFileName + ".txt", true)){
                resultsFile.WriteLine("Starting Round: " + eval2Pos);
            }
            GpRoundText.text = eval2Pos.ToString() + " of " + (eval2Trees.Length - 1).ToString();
        }

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
        GpMatchText.text = eval1Pos.ToString() + " of " + (eval1Trees.Length - 1).ToString();

        // instantiate the new players
        player1 = InstantiatePlayer(true);
        player2 = InstantiatePlayer(false);

        // assign the newly created players a decision tree from the population of decision trees
        player1.GetComponent<AiController>().dt = eval1Trees[eval1Pos];
        player2.GetComponent<AiController>().dt = eval2Trees[eval2Pos];

        // initialize the players so they can start fighting
        player1.GetComponent<PlayerController>().Initialize(2, true);
        player2.GetComponent<PlayerController>().Initialize(2, false);

        resetTime();
        StartCoroutine(EvalWait());
    }

    // the method is called when the match is over so that the fitness can be calculated for both agents and the next match can be started
    void EvalMatchOver(){
    
        float damageP1 = 100f - player2.GetComponent<PlayerController>().GetMyHealth();
        float damageP2 = 100f - player1.GetComponent<PlayerController>().GetMyHealth();

        eval1DamageRoundTotal += damageP1;
        eval2DamageRoundTotal += damageP2;
        eval1DamageTotal += damageP1;
        eval2DamageTotal += damageP2;

        using (System.IO.StreamWriter resultsFile = new System.IO.StreamWriter(@"Assets/Output/Evaluation/Results/" + evalOutputFileName + ".txt", true)){
            resultsFile.WriteLine(damageP1 + " " + damageP2);
        }

        eval1Pos++;
        // the round is over
        if (eval1Pos == eval1Trees.Length){
            eval2Pos++;

            float eval1RoundAvgDamage = eval1DamageRoundTotal / eval1Trees.Length;
            float eval2RoundAvgDamage = eval2DamageRoundTotal / eval1Trees.Length;
            using (System.IO.StreamWriter resultsFile = new System.IO.StreamWriter(@"Assets/Output/Evaluation/Results/" + evalOutputFileName + ".txt", true)){
                resultsFile.WriteLine("Round over. Player1 Avg Damage: " + eval1RoundAvgDamage + ", Player2 Avg Damage: " + eval2RoundAvgDamage + "\n");
            }

            // check if it's all over
            if (eval2Pos < eval2Trees.Length){
                
                eval1Pos = 0;
                eval1DamageRoundTotal = 0f;
                eval2DamageRoundTotal = 0f;

                CreateEvalMatch();

            // the current file(s) is done
            } else {
                print("*** Evaluation File " + inputFiles1Idx + " Finished ***");

                float eval1AvgDamage = eval1DamageTotal / (eval1Trees.Length * eval2Trees.Length);
                float eval2AvgDamage = eval2DamageTotal / (eval1Trees.Length * eval2Trees.Length);
                using (System.IO.StreamWriter resultsFile = new System.IO.StreamWriter(@"Assets/Output/Evaluation/Results/" + evalOutputFileName + ".txt", true)){
                    resultsFile.WriteLine("Evaluation over. Player1 Overall Avg Damage Done: " + eval1AvgDamage + ", Player2 Avg Damage Done: " + eval2AvgDamage);
                }

                inputFiles1Idx++;
                inputFiles2Idx++;

                // go on to the next file(s)
                if (inputFiles2Idx < inputFiles1.Length){
                    SetUpEvaluation();
                // it's ALL over
                } else {
                    print("*** All Evaluation Files Finished ***");
                    UnityEditor.EditorApplication.isPlaying = false;
                }
            }
        } else {
            CreateEvalMatch();
        }
    }

    IEnumerator EvalWait(){      
        // yield on a new YieldInstruction that waits for 10 seconds.
        yield return new WaitForSeconds(matchTime);
        EvalMatchOver();
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