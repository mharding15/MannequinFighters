This is the folder that holds the files for the population 60, Control (so no islands and aggressiveness is 0), Aggressive Fitness (only half of damage taken counts), with matches between population memebers only. 

Below are the settings in the SetupManager: 

// setting some variables for the training
    private int tournamentSize      = 3,
                freshTreeCount      = 0,
                matchTime           = 20,
                displayTime         = 0;
    private float mutateProb        = 0.2f, 
                crossoverProb       = 0.1f,
                freshTreePercentage = 0.1f,
                countTimer          = 0f;

And here are the settings for the DecisionTree:
// settings for the decision trees
   private int numFeatures = 5, numActions = 16;
      private int[] aggroActions = {0, 3, 6, 7, 8, 9, 10, 11, 12};
      private int[] defActions = {1, 4, 13, 14};

Public fields:

	GameType														                          3
	PopulationSize													                      80			
	TotalRounds														                        200	
	aggressiveness                                              	0
  Fitness Type                                                  1
	AgentFile1 (shouldn't matter, but might as well record it)		-
	AgentFile2 (shouldn't matter, but might as well record it)		-
	GpAgentsFile													                        popSixtyControlAggFitPopMatches
	FitnessLogFile (not even using this anymore I don't think)		testFitnessLog
	Character 														                        Player_Prefab
