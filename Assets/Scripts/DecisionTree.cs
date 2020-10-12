using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecisionTree
{
      public float fitness {set; get;}
      
   	private Node head;
   	private bool debug = false;
   	private int numFeatures = 3, numActions = 3;
  	   private float thresholdDelta = 1.5f; // this is the value that the threshold will either increase or decrease during mutation
      private float _probAction2Action     = .025f, // these probs are cumulative meaning that the difference between them is the actual prob.
                    _probAction2Feature    = .05f,
                    _probFeature2Action    = .025f,
                    _probFeature2Feature   = .05f,
                    _probDecreaseThreshold = .75f,
                    _probIncreaseThreshold = .1f;

      ////////////////////////////
   	// *** Creating Trees *** //
      ////////////////////////////

   	public void RandomTree(int numFeatures, int numActions)
   	{
   		Debug.Log("*** In RandomTree ***");

   		List<int> remainingFeatures = new List<int>();
   		for (int i = 0; i < numFeatures; i++){
   			remainingFeatures.Add(i);
   		}

   		printRemainingFeatures(remainingFeatures);

   		head = CreateRandomTree(numFeatures, numActions, 0f);
   	}

   	// this method does NOT care if there are multiple nodes with the same feature in a path to the root
   	Node CreateRandomTree(int numFeatures, int numActions, float actionProb)
   	{
   		string methodName = "*** Create Random Tree ***";
   		DebugMsg(methodName, "");

         Node newNode = new Node();

   		// there might be a more efficient way to do this
   		float rand = Random.Range(0f, 1f);

         // create a feature
   		if (actionProb < rand){
   			newNode.featureIdx = Random.Range(0, numFeatures);
   			newNode.threshold = Random.Range(0f, 30f);

            // increase the probability of getting an action in the next iteration, unless at the .8 threshold
   			float newActionProb = actionProb;
   			if (actionProb < .8f){
   				newActionProb += .1f;
   			}
   			newNode.left = CreateRandomTree(numFeatures, numActions, newActionProb);
   			newNode.right = CreateRandomTree(numFeatures, numActions, newActionProb);

         // create an action
   		} else {
   			newNode.action = Random.Range(0, numActions);
   		}

   		return newNode;
   	}

   	void printRemainingFeatures(List<int> rfeatures)
   	{
   		Debug.Log("*** In printRemainingFeatures ***");
   		string featureStr = "";
   		foreach (int i in rfeatures){
   			featureStr += "" + i;
   		}
   		Debug.Log("444 ::: remaining features: " + featureStr);
   	}


      ////////////////////////////////
   	// *** Genetic Operations *** //
      ////////////////////////////////

      // this function will go through the tree and randomly mutate the nodes (will certainly have to tweak these probs while doing the GP)
   	public void Mutate(){
   		MutateHelper(head);
   	}

   	void MutateHelper(Node curr){
   		string methodName = "*** MutateHelper ***";

   		// we are at a leaf node (i.e. action node)
   		if (curr.action != -1){
   			float randNum = Random.Range(0f, 1f);
   			// change the action to another action
   			if (randNum < _probAction2Action){
   				DebugMsg(methodName, " Changing from action to another action");
   				curr.action = Random.Range(0, numActions);

   			// change the action to a feature
   			} else if (randNum < _probAction2Feature){
   				DebugMsg(methodName, " Changing from action to a feature");

   				curr.action = -1;
   				curr.featureIdx = Random.Range(0, numFeatures);
   				curr.threshold = Random.Range(0f, 30f);

               // need to create actions as children for this new feature
   				Node newLeft = new Node();
   				Node newRight = new Node();

   				newLeft.action = Random.Range(0, numActions);
   				newRight.action = Random.Range(0, numActions);

   				curr.left = newLeft;
   				curr.right = newRight;
   			}

         // we are at an internal node (feature node)
   		} else {
   			float randNum = Random.Range(0f, 1f);
   			// change the feature to an action (and don't try to traverse down anymore)
   			if (randNum < _probFeature2Action){
   				DebugMsg(methodName, " Changing from feature to an action");
   				curr.action = Random.Range(0, numActions);
   				curr.left = null;
   				curr.right = null;
   				return;
   			// change the feature to another feature
   			} else if (randNum < _probFeature2Feature){
   				DebugMsg(methodName, " Changing from feature to another feature");
   				curr.featureIdx = Random.Range(0, numFeatures);
   			// decrease the current threshold
   			} else if (randNum < _probDecreaseThreshold){
   				DebugMsg(methodName, " Decreasing threshold");
   				curr.threshold -= thresholdDelta;
   				if (curr.threshold < 0f){
   					curr.threshold = 0f;
   				}
   			// increase the current threshold
   			} else if (randNum < _probIncreaseThreshold){
   				DebugMsg(methodName, " Decreasing threshold");
   				curr.threshold += thresholdDelta;
   				if (curr.threshold > 30f){
   					curr.threshold = 30f;
   				}
   			}

   			MutateHelper(curr.left);
   			MutateHelper(curr.right);
   		}
   	}

   	// I could prbably make this a lot simpler by putting everything that has to do with getting the random node in the class itself and just call that on the otherTree.
   	public void Crossover(DecisionTree otherTree)
   	{
   		int treeOneCount = NodeCount();
   		int treeTwoCount = otherTree.NodeCount();

   		Queue<int> randPermOne = RandomPerm(treeOneCount);
   		Queue<int> randPermTwo = RandomPerm(treeTwoCount);

   		Node randomInternalNodeOne = RandomInternalNode(randPermOne, Random.Range(0, randPermOne.Count));
   		Node randomInternalNodeTwo = otherTree.RandomInternalNode(randPermTwo, Random.Range(0, randPermTwo.Count));

         // randomly swap the children of the given internal nodes
   		float randSwap = Random.Range(0f, 1f);
   		if (randSwap < .25f){
   			Node temp = randomInternalNodeOne.left;
   			randomInternalNodeOne.left = randomInternalNodeTwo.left;
   			randomInternalNodeTwo.left = temp;
   		} else if (randSwap < .5f){
   			Node temp = randomInternalNodeOne.left;
   			randomInternalNodeOne.left = randomInternalNodeTwo.right;
   			randomInternalNodeTwo.right = temp;
   		} else if (randSwap < .75f){
   			Node temp = randomInternalNodeOne.right;
   			randomInternalNodeOne.right = randomInternalNodeTwo.left;
   			randomInternalNodeTwo.left = temp;
   		} else {
   			Node temp = randomInternalNodeOne.right;
   			randomInternalNodeOne.right = randomInternalNodeTwo.right;
   			randomInternalNodeTwo.right = temp;
   		}
   	}

      // return a random permutation of all the numbers in [0, limit)
   	Queue<int> RandomPerm(int limit){
   		Queue<int> q = new Queue<int>();
   		List<int> list = new List<int>();
   		for (int i = 0; i < limit; i++){
   			list.Add(i);
   		}
   		while (list.Count > 0){
   			int rand = Random.Range(0, list.Count);
   			q.Enqueue(rand);
   			list.Remove(rand);
   		}
   		return q;
   	}

   	Node RandomInternalNode(Queue<int> q, int x){
   		return RandomInternalNodeHelper(head, q, x);
   	}

   	Node RandomInternalNodeHelper(Node curr, Queue<int> q, int x){
   		if (curr.action == -1){
   			return null;
   		}

   		int num = q.Dequeue();
   		if (num == x){
   			return curr;
   		}

   		Node leftResult = RandomInternalNodeHelper(curr.left, q, x);
   		if (leftResult != null){
   			return leftResult;
   		}
   		Node rightResult = RandomInternalNodeHelper(curr.right, q, x);
   		if (rightResult != null){
   			return rightResult;
   		}

   		return null;
   	}

      // return the number of internal nodes in the decision tree
   	public int NodeCount(){
   		return NodeCountHelper(head);
   	}

   	int NodeCountHelper(Node curr){
   		if (curr.action == -1){
   			return 0;
   		}

   		int sumLeft = NodeCountHelper(curr.left);
   		int sumRight = NodeCountHelper(curr.right);

   		return sumLeft + sumRight + 1;
   	}


      ////////////////////////////
   	// *** Saving/Loading *** //
      ////////////////////////////

   	// will need to be able to serialize and deserialize trees to save and load them. Serializes the tree by doing an in order traversal.
   	public string SerializeTree(){
   		return SerializeHelper(head, "");
   	}

   	private string SerializeHelper(Node curr, string line){
   		string methodName = "*** SerializeHelper ***";
   		DebugMsg(methodName, line);

   		if (curr.action != -1){
   			line += " " + curr.action;
   			return line;
   		}

   		if (curr != head){
   			line += " ";
   		}
   		line += curr.featureIdx;
   		line += "," + curr.threshold;

   		line = SerializeHelper(curr.left, line);
   		line = SerializeHelper(curr.right, line);

   		return line;
   	}

      // load the decision tree by going through a queue of features and actions
   	public void Load(Queue<string> q){
   		head = LoadHelper(q);
   	}

   	private Node LoadHelper(Queue<string> q){
   		Node newNode = new Node();
   		string curr = q.Dequeue();

   		if (curr.Contains(",")){
   			string[] vals = curr.Split(',');
   			newNode.featureIdx = System.Int32.Parse(vals[0]);
   			newNode.threshold = float.Parse(vals[1]);

   			newNode.left = LoadHelper(q);
   			newNode.right = LoadHelper(q);
   		} else {
   			newNode.action = System.Int32.Parse(curr);
   		}

   		return newNode;
   	}


      //////////////////////////////
   	// *** Making Decisions *** //
      //////////////////////////////

   	// when a character is deciding what to do, look at the features to figure it out.
   	public int Decide(float[] features){
   		return DecisionHelper(features, head);
   	}

   	private int DecisionHelper(float[] features, Node curr){
   		// base case, this is a leaf node and an action is to be performed
   		if (curr.action != -1){
   			return curr.action;
   		}

   		// decide which way to go based on the value of the current feature
   		if (features[curr.featureIdx] <= curr.threshold){
   			return DecisionHelper(features, curr.left);
   		} else {
   			return DecisionHelper(features, curr.right);
   		}
   	}

      ////////////////////////
   	// *** Node Class *** //
      ////////////////////////

   	public class Node 
   	{
   		public int featureIdx {set; get;}
   		public int action {set; get;}
   		public float threshold {set; get;}
   		public List<int> usedFeautures;
   		public Node left, right;

   		public Node()
   		{
   			action = -1;
   		}
   	}

      ///////////////////////////////
      // *** Utility Functions *** //
      ///////////////////////////////

   	// the method which will print the debugging statement if it should be printed
   	void DebugMsg(string methodName, string msg)
   	{
   		if (debug){
   			Debug.Log(methodName + " ::: " + msg);
   		}
   	}

      // the height of the tree will help when finding the fitness for the tree
      public int GetHeight(){
         return HeightHelper(head);
      }

      int HeightHelper(Node node){
         // base case, either this node is null (don't think this is actually possible, but what the hell) or is a leaf
         if (node == null){
            return 0;
         } else if (node.action != -1){
            return 1;
         }

         int leftHeight = HeightHelper(node.left);
         int rightHeight = HeightHelper(node.right);

         return System.Math.Max(leftHeight, rightHeight) + 1;
      }
}
