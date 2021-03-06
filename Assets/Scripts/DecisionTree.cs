﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DecisionTree
{
      public float fitness {set; get;}
      public Node head {set; get;}

      private bool debug = false;
      private int numFeatures = 5, numActions = 16;
      private int[] aggroActions = {0, 3, 6, 7, 8, 9, 10, 11, 12};
      private int[] defActions = {1, 4, 13, 14};

      public DecisionTree(){
         fitness = 0f;
      }

      ////////////////////////////
      // *** Creating Trees *** //
      ////////////////////////////

      public DecisionTree Copy(){
         DecisionTree copy = new DecisionTree();
         copy.head = CopyHelp(head);
         copy.fitness = fitness;
         return copy;
      }

      private Node CopyHelp(Node curr){

         Node newNode = new Node();

         // at a leaf
         if (curr.action1 != -1){
            newNode.action1 = curr.action1;
            newNode.action1Prob = curr.action1Prob;
            newNode.action2 = curr.action2;
            return newNode;
         }

         newNode.featureIdx = curr.featureIdx;
         newNode.threshold = curr.threshold;

         newNode.left = CopyHelp(curr.left);
         newNode.right = CopyHelp(curr.right);

         return newNode;
      }

      // Create a random decision tree. Type says if the tree should be neutral (0), aggro (1), or defensive (2)
      public void RandomTree(int type){
         head = CreateRandomTree(0f, type, 0.1f);
      }

      // this method does NOT care if there are multiple nodes with the same feature in a path to the root
      Node CreateRandomTree(float actionProb, int type, float actionProbIncrementAmount){
         string methodName = "*** Create Random Tree ***";
         DebugMsg(methodName, "");

         Node newNode = new Node();

         // there might be a more efficient way to do this
         float rand = UnityEngine.Random.Range(0f, 1f);

         // create a feature
         if (actionProb < rand){
            newNode.featureIdx = UnityEngine.Random.Range(0, numFeatures);
            newNode.threshold = UnityEngine.Random.Range(0f, 30f);

            // increase the probability of getting an action in the next iteration, unless at the .8 threshold
            float newActionProb = actionProb;
            if (actionProb < 0.8f){
               newActionProb += actionProbIncrementAmount;
            }
            newNode.left = CreateRandomTree(newActionProb, type, actionProbIncrementAmount);
            newNode.right = CreateRandomTree(newActionProb, type, actionProbIncrementAmount);

         // create an action
         } else {
            // 50% chance that just any action will be chosen, 50% that Aggro or Def action will be chosen
            float actionRand = UnityEngine.Random.Range(0f, 1f);
            // neutral action (just pick any action)
            if (type == 0 || actionRand < .5f){
               newNode.action1 = UnityEngine.Random.Range(0, numActions);
               newNode.action2 = UnityEngine.Random.Range(0, numActions);
            // aggro action
            } else if (type == 1){
               int randAggroAction1 = UnityEngine.Random.Range(0, aggroActions.Length);
               int randAggroAction2 = UnityEngine.Random.Range(0, aggroActions.Length);
               newNode.action1 = aggroActions[randAggroAction1];
               newNode.action2 = aggroActions[randAggroAction2];
            // defensive action
            } else if (type == 2){
               int randDefAction1 = UnityEngine.Random.Range(0, defActions.Length);
               int randDefAction2 = UnityEngine.Random.Range(0, defActions.Length);
               newNode.action1 = defActions[randDefAction1];
               newNode.action2 = defActions[randDefAction2];
            }
            newNode.action1Prob = UnityEngine.Random.Range(0.25f, 0.75f);
         }

         return newNode;
      }


      ////////////////////////////////
      // *** Genetic Operations *** //
      ////////////////////////////////

      // this function will go through the tree and randomly mutate the nodes (will certainly have to tweak these probs while doing the GP)
      public void Mutate(int aggressionType){
         int treeCount = NodeCount();
         Queue<int> q = GetNodeQueue(treeCount);
         int randomIndex = UnityEngine.Random.Range(0, q.Count);
         Node randomInternalNode = RandomInternalNode(q, randomIndex);

         // passing params: new action prob, aggressiveness, actionProbIncrementAmount
         randomInternalNode.left = CreateRandomTree(0f, aggressionType, 0.2f);
         randomInternalNode.right = CreateRandomTree(0f, aggressionType, 0.2f);
      }

      // Perform crossover wih the other tree
      public void Crossover(DecisionTree otherTree) {
         Debug.Log("In Crossover....");

         int treeOneCount = NodeCount();
         int treeTwoCount = otherTree.NodeCount();

         // if either of the two trees is just the head, then don't try to crossover
         if (treeOneCount == 0 || treeTwoCount == 0){
            Debug.Log("One of the trees had a 0 internal nodes, so no crossover can take place.");
            return;
         }

         Queue<int> q1 = GetNodeQueue(treeOneCount);
         Queue<int> q2 = GetNodeQueue(treeTwoCount);

         // Random.Range is exclusive on the upper limit
         int randomIndexOne = UnityEngine.Random.Range(0, q1.Count);
         int randomIndexTwo = UnityEngine.Random.Range(0, q2.Count);

         Debug.Log("Random index 1: " + randomIndexOne);
         Debug.Log("Random index 2: " + randomIndexTwo);

         Node randomInternalNodeOne = RandomInternalNode(q1, randomIndexOne);
         Node randomInternalNodeTwo = otherTree.RandomInternalNode(q2, randomIndexTwo);

         Debug.Log("The RandomInternalNodeOne is: " + randomInternalNodeOne.featureIdx + "," + randomInternalNodeOne.threshold);
         Debug.Log("The RandomInternalNodeTwo is: " + randomInternalNodeTwo.featureIdx + "," + randomInternalNodeTwo.threshold);

         // randomly swap the children of the given internal nodes
         float randSwap = UnityEngine.Random.Range(0f, 1f);
         if (randSwap < .25f){
            Debug.Log("Swap 1");
            Node temp = randomInternalNodeOne.left;
            randomInternalNodeOne.left = randomInternalNodeTwo.left;
            randomInternalNodeTwo.left = temp;
         } else if (randSwap < .5f){
            Debug.Log("Swap 2");
            Node temp = randomInternalNodeOne.left;
            randomInternalNodeOne.left = randomInternalNodeTwo.right;
            randomInternalNodeTwo.right = temp;
         } else if (randSwap < .75f){
            Debug.Log("Swap 3");
            Node temp = randomInternalNodeOne.right;
            randomInternalNodeOne.right = randomInternalNodeTwo.left;
            randomInternalNodeTwo.left = temp;
         } else {
            Debug.Log("Swap 4");
            Node temp = randomInternalNodeOne.right;
            randomInternalNodeOne.right = randomInternalNodeTwo.right;
            randomInternalNodeTwo.right = temp;
         }
      }

      // return a random permutation of all the numbers in [0, limit)
      public Queue<int> GetNodeQueue(int limit){
         Queue<int> q = new Queue<int>();
         for (int i = 0; i < limit; i++){
            q.Enqueue(i);
         }
         return q;
      }

      Node RandomInternalNode(Queue<int> q, int x){
         return RandomInternalNodeHelper(head, q, x);
      }

      Node RandomInternalNodeHelper(Node curr, Queue<int> q, int x){
         // if this is a leaf node
         if (curr.action1 != -1){
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
         // we are at a leaf, so this should not be counted
         if (curr.action1 != -1){
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

         if (curr.action1 != -1){
            if (curr == head){
               line += curr.action1 + ":" + curr.action1Prob + ":" + curr.action2;
            } else {
               line += " " + curr.action1 + ":" + curr.action1Prob + ":" + curr.action2;
            }
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
            string[] vals = curr.Split(':');
            newNode.action1 = System.Int32.Parse(vals[0]);
            newNode.action1Prob = float.Parse(vals[1]);
            newNode.action2 = System.Int32.Parse(vals[2]);
         }

         return newNode;
      }


      //////////////////////////////
      // *** Making Decisions *** //
      //////////////////////////////

      // when a character is deciding what to do, look at the features to figure it out.
      public string Decide(float[] features){
         return DecisionHelper(features, head);
      }

      private string DecisionHelper(float[] features, Node curr){
         // base case, this is a leaf node and an action is to be performed
         if (curr.action1 != -1){
            return curr.action1 + ":" + curr.action1Prob + ":" + curr.action2;
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
         public int action1 {set; get;}
         public int action2 {set; get;}
         public float action1Prob {set; get;}
         public float threshold {set; get;}
         public List<int> usedFeautures;
         public Node left, right;

         public Node(){
            action1 = -1;
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

      public void CalculateFitness(float myhealth, float opponentHealth, float distance, float moveFitness, int type){
         // float healthFactor = myhealth - opponentHealth;
         // // as long as the distance factor is guaranteed to be less than 1, then it will only make any difference if the health is tied.
         // float distanceFactor = (float)distance * .01f;
         // // this is really just a guess, but I'm gonna say that once the tree has a height of more than 50 it will really start to cause issues (I'm sure this is something I will have to play with a lot)
         // float treeDepthFactor = Mathf.Log10((float)GetHeight()) * Mathf.Log10(Mathf.Pow((float)GetHeight(), 3f)) - 5f;
         // if (treeDepthFactor < 0){
         //    treeDepthFactor = 0;
         // }

         // Debug.Log("Calculating fitness, healthFactor: " + healthFactor + ", distanceFactor: " + distanceFactor + ", treeDepthFactor: " + treeDepthFactor + ", moveFitness factor: " + moveFitness);

         // //fitness = healthFactor - distanceFactor - treeDepthFactor + moveFitness;
         // // gonna try this for now and see how it works out, I might have to add some stuff later to jump start things, but I'm thinking I might not have to with a large population         
         // float totalFitness = healthFactor - treeDepthFactor;

         // 1/11/21, trying this out to see if it helps. I would like aggressive ones to be rewarded more than ones that do nothing
         float damageDone = 100f - opponentHealth, damageTaken = 100f - myhealth;

         float totalFitness = 0f;
         // fitness type 0 is the even fitness type, 1 is aggro
         if (type == 0){
             totalFitness = damageDone - damageTaken;
         } else if (type == 1){
            totalFitness = damageDone - (damageTaken * .5f);
         }

         // 1/18/21, adding a depth factor because with the new mutation they can get pretty large.
         int treeHeight = GetHeight();
         float treeDepthFactor = 0f;
         if (treeHeight > 100){
            Debug.Log("!!! The tree height was greater than 100.");
            totalFitness -= 25f;
         } else if (treeHeight > 50){
            Debug.Log("!!! The tree height was greater than 50.");
         }

         fitness += totalFitness;
      }

      // the height of the tree will help when finding the fitness for the tree
      public int GetHeight(){
         return HeightHelper(head);
      }

      int HeightHelper(Node node){
         // base case, either this node is null (don't think this is actually possible, but what the hell) or is a leaf
         if (node == null){
            return 0;
         } else if (node.action1 != -1){
            return 1;
         }

         int leftHeight = HeightHelper(node.left);
         int rightHeight = HeightHelper(node.right);

         return System.Math.Max(leftHeight, rightHeight) + 1;
      }

      bool coinFlip(){
         float randNum = UnityEngine.Random.Range(0f, 1f);
         return randNum < .5f;
      }
}
