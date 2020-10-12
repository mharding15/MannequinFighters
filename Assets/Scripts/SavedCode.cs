// this is the one that only allows for each feature to be used at most once per route
// public Node CreateRandomTree(List<int> remainingFeatures, int totalFeatures, int numActions)
// {
// 	Node newNode = new Node();

// 	Debug.Log("*** In CreateRandomTree ***");

// 	printRemainingFeatures(remainingFeatures);

// 	// if all the features have been used, simply choose a random action
// 	if (remainingFeatures.Count == 0){
// 		newNode.action = Random.Range(0, numActions);
// 		Debug.Log("111 ::: In CreateRandomTree ::: Count was 0, and action chosen is: " + newNode.action);
// 		return newNode;
// 	}

// 	// if this is not the head, then there will be a chance that it will become a leaf node
// 	int featureIdx = Random.Range(0, remainingFeatures.Count + 1);
// 	if (remainingFeatures.Count == totalFeatures){
// 		featureIdx = Random.Range(0, remainingFeatures.Count);
// 	}

// 	// if the index chosen is out of range of the remainingFeatures, it means this node will prematurely become a leaf
// 	if (featureIdx == remainingFeatures.Count){
// 		newNode.action = Random.Range(0, numActions);
// 		Debug.Log("222 ::: In CreateRandomTree ::: Count was NOT 0, but action chosen is: " + newNode.action);
// 		return newNode;
// 	}

// 	// need to make a deep copy of this list (minus the one that was chosen)
// 	List<int> featuresAfterRemoving = new List<int>();
// 	foreach (int i in remainingFeatures){
// 		if (i != featureIdx){
// 			featuresAfterRemoving.Add(i);
// 		}
// 	}
// 	float threshold = Random.Range(0f, 30f);

// 	newNode.featureIdx = featureIdx;
// 	newNode.threshold = threshold;

// 	Debug.Log("333 ::: In CreateRandomTree ::: feature chosen is: " + featureIdx);
// 	Debug.Log("333 ::: In CreateRandomTree ::: threshold chosen is: " + threshold);

// 	newNode.left = CreateRandomTree(featuresAfterRemoving, totalFeatures, numActions);
// 	newNode.right = CreateRandomTree(featuresAfterRemoving, totalFeatures, numActions);

// 	return newNode;
// }