This is the folder that holds the files for the population 80, Control (so no islands and aggressiveness is 0), with matches between population memebers only. 

Below are the settings in the SetupManager: 

// setting some variables for the training
    private int tournamentSize      = 3,
                freshTreeCount      = 0,
                matchTime           = 20,
                displayTime         = 0;
    private float mutateProb        = 0.25f, 
                crossoverProb       = 0.1f,
                freshTreePercentage = 0.1f,
                countTimer          = 0f;

And here are the settings for the DecisionTree:
// settings for the decision trees
   	private int numFeatures = 5, numActions = 16;
  	private float thresholdDelta = 1.5f; // this is the value that the threshold will either increase or decrease during mutation
    private float _probAction2Action     = .025f, // these probs are cumulative meaning that the difference between them is the actual prob.
                  _probAction2Feature    = .05f,
                  _probFeature2Action    = .025f,
                  _probFeature2Feature   = .05f,
                  _probDecreaseThreshold = .1f,
                  _probIncreaseThreshold = .1f;


Public fields:

	GameType														3
	PopulationSize													80			
	TotalRounds														200	
	aggressiveness                                              	0
	AgentFile1 (shouldn't matter, but might as well record it)		-
	AgentFile2 (shouldn't matter, but might as well record it)		-
	GpAgentsFile													popEightyControlAggFitPopMatches
	FitnessLogFile (not even using this anymore I don't think)		testFitnessLog
	Character 														Player_Prefab
