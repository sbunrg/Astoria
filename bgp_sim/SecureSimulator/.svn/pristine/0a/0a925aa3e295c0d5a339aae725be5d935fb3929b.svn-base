using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace SecureSimulator
{
    /// <summary>
    /// This class contains static "utility" functions for executing BFS and BFS-like algorithms
    /// on a graph object.
    /// </summary>
     public static class ModifiedBfs
    {
         //default to  hash.
         public static bool Hash=true;
         public static bool OnlyNonStubs = false;


             /// <summary>
        /// static constructor
        /// </summary>
        static ModifiedBfs()
        {
            if (File.Exists("paramsFile.txt"))
            {
                StreamReader input = new StreamReader("paramsFile.txt");
               
                while (!input.EndOfStream)
                {
                    string line = input.ReadLine().ToLower();
                    //hashing
                    if (line.IndexOf("sethash") == 0)
                    {
                        bool inputSetHash;
                        if (SimulatorLibrary.readParameter(line, out inputSetHash))
                            Hash = inputSetHash;
                   
                
                    }//only nonstubs
                    else if (line.IndexOf("setonlynotstubs") == 0 || line.IndexOf("setonlynonstubs") == 0)
                    {
                        bool inputNonStubs;
                        if (SimulatorLibrary.readParameter(line, out inputNonStubs))
                           OnlyNonStubs = inputNonStubs;
                    }

                }
                input.Close();
                Console.WriteLine("*******************");
                Console.WriteLine("Done reading parameters for BFS.");
                Console.WriteLine("Hash is " + ModifiedBfs.Hash);
         
                Console.WriteLine("onlynonstubs is: " + ModifiedBfs.OnlyNonStubs);
                Console.WriteLine("*******************");
            }
        }


         /// <summary>
        /// Extension method on the NetworkGraph class that performs a [modified] BFS on the
        /// specified graph from the specified source ndoe.  The allowedRelationships specifies
        /// which edges may be traversed during the BFS.
        /// This algorithm breaks ties by picking the node with the lower node number.
        /// This algorithm allows you to execute multiple BFS iterations on a single graph, with the
        /// constraint that any previous BFS trees created in a prior BFS run will not be modified (only
        /// added to).
        /// If limitedDiscovery is true, the BFS will only find new nodes that are 1 edge away from any existing
        /// BFS tree.
        /// If includeSibling is true, then ties are broken by first taking non-siblings over siblings.
        /// 
        /// Modified by PGill Oct. 2010 to take in references to the Best, BucketTable and ChosenPaths sets. The function
        /// was also modified to populate these for our new sims.
        /// </summary>
         public static void ExecuteBfs(NetworkGraph graph, List<UInt32> srcNodeNums, bool limitedDiscovery, bool includeSibling, 
            RelationshipType allowedRelationships,ref List<UInt32>[] Best,ref List<UInt32>[][] BucketTable,ref List<UInt32>[] ChosenPath,
            ref UInt32[] ChosenParent, ref byte[] L, ref byte[] BestRelation)
        {
           
            Destination utility = new Destination();
            // Initialize some stuff...
            Queue<AsNode> nodeQueue = new Queue<AsNode>(graph.NodeCount);
            graph.BfsTreeNodeCount = 0;

            // "Visit" the source nodes
            foreach (UInt32 srcNodeNum in srcNodeNums)
            {
                AsNode currentNode = graph.GetNode(srcNodeNum);
                currentNode.InProcessBfsStatus = NodeInProcessBfsStatus.SeenInCurrentRun;
                currentNode.BfsDepth = 0;
                nodeQueue.Enqueue(currentNode);
                graph.BfsTreeNodeCount++;

                //init the destination's path to itself
                ChosenPath[srcNodeNum] = new List<UInt32>();
                ChosenPath[srcNodeNum].Add(srcNodeNum);
                Best[srcNodeNum] = new List<UInt32>();
                Best[srcNodeNum].Add(srcNodeNum);

            
                //if (allowedRelationships.HasFlag(RelationshipType.CustomerOf))
                if ((allowedRelationships & RelationshipType.CustomerOf) == RelationshipType.CustomerOf || allowedRelationships==RelationshipType.NullRelationship)
                {
                    BucketTable[0][ Destination._CUSTOMERCOLUMN] = new List<UInt32>();
                    BucketTable[0][ Destination._CUSTOMERCOLUMN].Add(srcNodeNum);
                }
            }

            // While there's still nodes to be examined...
            while (nodeQueue.Count > 0)
            {
                // Dequeue a node to examine.  Iterate through all of its neighbors of the specified type (plus
                // existing BFS children) and visit them.
                AsNode currentNode = nodeQueue.Dequeue();
                foreach (AsNode oppositeNode in currentNode.GetNeighborsByType(allowedRelationships | RelationshipType.BfsParentOf).Distinct())
                {
                    // If this is the first time we've see this node, mark it and possibly enqueue it for later examination
                    if (oppositeNode.InProcessBfsStatus == NodeInProcessBfsStatus.UnseenInCurrentRun)
                    {
                        // Case 1: oppositeNode is a newly discovered node, also unseen in any previous BFS runs
                        if (oppositeNode.PriorBfsStatus == NodePriorBfsStatus.NotDiscoveredInPriorBfs)
                        {
                            oppositeNode.InProcessBfsStatus = NodeInProcessBfsStatus.SeenInCurrentRun;
                            oppositeNode.BfsDepth = currentNode.BfsDepth + 1;
                            oppositeNode.BfsParentNode = currentNode;
                            
                            currentNode.AddBfsChild(oppositeNode);
                          
                            graph.BfsTreeDepth = Math.Max(graph.BfsTreeDepth, oppositeNode.BfsDepth);
                            graph.BfsTreeNodeCount++;

                            if (!oppositeNode.isStub() || !OnlyNonStubs)
                            {
                                L[oppositeNode.NodeNum] = (byte)oppositeNode.BfsDepth;
                                /*** add this node to the buckettable and update its chosen path, parent and BFS depth***/

                                //if (allowedRelationships.HasFlag(RelationshipType.CustomerOf)) -- this is .NET 4, downgraded to make comptabilte with .NET 3.5
                                if ((allowedRelationships & RelationshipType.CustomerOf) == RelationshipType.CustomerOf || allowedRelationships == RelationshipType.NullRelationship)
                                {

                                    //init this spot in the bucket table (if needed)
                                    if (BucketTable[oppositeNode.BfsDepth][Destination._CUSTOMERCOLUMN] == null)
                                        BucketTable[oppositeNode.BfsDepth][Destination._CUSTOMERCOLUMN] = new List<UInt32>();

                                    BestRelation[oppositeNode.NodeNum] = Destination._CUSTOMERCOLUMN;
                                    BucketTable[oppositeNode.BfsDepth][Destination._CUSTOMERCOLUMN].Add(oppositeNode.NodeNum);
                                    utility.updatePath(oppositeNode.NodeNum, ChosenPath[currentNode.NodeNum], Destination._CUSTOMERCOLUMN, ref ChosenPath);

                                }
                                //else if (allowedRelationships.HasFlag(RelationshipType.ProviderTo)) -- this is .NET 4, downgraded to make comptabilte with .NET 3.5
                                else if ((allowedRelationships & RelationshipType.ProviderTo) == RelationshipType.ProviderTo)
                                {

                                    //init this spot in the bucket table (if needed)
                                    if (BucketTable[oppositeNode.BfsDepth][Destination._PROVIDERCOLUMN] == null)
                                        BucketTable[oppositeNode.BfsDepth][Destination._PROVIDERCOLUMN] = new List<UInt32>();

                                    BestRelation[oppositeNode.NodeNum] = Destination._PROVIDERCOLUMN;
                                    BucketTable[oppositeNode.BfsDepth][Destination._PROVIDERCOLUMN].Add(oppositeNode.NodeNum);
                                    utility.updatePath(oppositeNode.NodeNum, ChosenPath[currentNode.NodeNum], Destination._PROVIDERCOLUMN, ref ChosenPath);

                                }

                                //else if (allowedRelationships.HasFlag(RelationshipType.PeerOf)) -- this is .NET 4, downgraded to make comptabilte with .NET 3.5
                                else if ((allowedRelationships & RelationshipType.PeerOf) == RelationshipType.PeerOf)
                                {
                                    //init this spot in the bucket table (if needed)
                                    if (BucketTable[oppositeNode.BfsDepth][Destination._PEERCOLUMN] == null)
                                        BucketTable[oppositeNode.BfsDepth][Destination._PEERCOLUMN] = new List<UInt32>();

                                    BestRelation[oppositeNode.NodeNum] = Destination._PEERCOLUMN;
                                    BucketTable[oppositeNode.BfsDepth][Destination._PEERCOLUMN].Add(oppositeNode.NodeNum);
                                    utility.updatePath(oppositeNode.NodeNum, ChosenPath[currentNode.NodeNum], Destination._PEERCOLUMN, ref ChosenPath);
                                }

                                /*** update this node's Best set ***/
                                if (Best[oppositeNode.NodeNum] == null)
                                    Best[oppositeNode.NodeNum] = new List<UInt32>();
                                Best[oppositeNode.NodeNum].Add(currentNode.NodeNum);
                                ChosenParent[oppositeNode.NodeNum] = currentNode.NodeNum;
                            }
                           
                            // If we want to continue discovering past this newly found node, enqueue it
                            if (!limitedDiscovery)
                            {
                                nodeQueue.Enqueue(oppositeNode);
                            }

                        }
                        // Case 2: oppositeNode was found in a prior BFS run, and the path to oppositeNode went through currentNode
                        else if (oppositeNode.BfsParentNode == currentNode)
                        {
                            // Don't need to do any marking of the opposite node because it's already in the BFS tree.
                            // Just enqueue it so we can continue our BFS from that node at some later time.
                            oppositeNode.InProcessBfsStatus = NodeInProcessBfsStatus.SeenInCurrentRun;
                            nodeQueue.Enqueue(oppositeNode);
                            graph.BfsTreeDepth = Math.Max(graph.BfsTreeDepth, oppositeNode.BfsDepth);
                            graph.BfsTreeNodeCount++;

                            // Sanity check... the depth should be the same, right?
                            if (oppositeNode.BfsDepth != currentNode.BfsDepth + 1)
                            {
                                throw new Exception("Unexpected BFS depth during BFS re-run");
                            }
                        }
                        // Case 3: oppositeNode was found in a prior BFS run, and the path to oppositeNode did NOT go through currentNode
                        // No action necessary.  We can't process oppositeNode now because we aren't allow to follow this edge.
                        // Eventually we will hit the already-existing edge that's part of a prior BFS run, and we'll enter Case 2 above.
                    }
                    // We've seen this node before...
                    else
                    {
                        // Did we find an alternate route to the opposite node?
                        // We cannot change the route if this node was found in a prior BFS run.
                        // This is where tie-breaking happens...
                        if ((oppositeNode.InProcessBfsStatus == NodeInProcessBfsStatus.SeenInCurrentRun) &&
                            (oppositeNode.BfsDepth == (currentNode.BfsDepth + 1)) &&
                            (oppositeNode.PriorBfsStatus != NodePriorBfsStatus.DiscoveredInPriorBfs))
                        {
                            // This is an alternate route... break the tie
                            //note that current node is a potential parent of opposite node here.
                            //equivalent to current node being one of the nodes in the tiebreak set

                            //UPDATED CONDITION TO DEAL WITH HASH FLAG
                            if ((Hash && NewRouteWinsTieBreak(currentNode, oppositeNode, includeSibling)) || (!Hash && NewRouteWinsTieBreakOriginal(currentNode,oppositeNode,includeSibling)))
                            {
                                // Tie-break algorithm says we have a new, better route to this node.
                                // We need to switch the route through the current node instead.
                                oppositeNode.BfsParentNode.RemoveBfsChild(oppositeNode);
                                oppositeNode.BfsParentNode = currentNode;
                                currentNode.AddBfsChild(oppositeNode);

                                if (!oppositeNode.isStub()||!OnlyNonStubs)
                                {
                                    /*** update chosen parent***/
                                    ChosenParent[oppositeNode.NodeNum] = currentNode.NodeNum;

                                    /***  update its chosen path ***/

                                    //if (allowedRelationships.HasFlag(RelationshipType.CustomerOf))
                                    if ((allowedRelationships & RelationshipType.CustomerOf) == RelationshipType.CustomerOf || allowedRelationships==RelationshipType.NullRelationship)
                                        utility.updatePath(oppositeNode.NodeNum, ChosenPath[currentNode.NodeNum], Destination._CUSTOMERCOLUMN, ref ChosenPath);
                                    //else if (allowedRelationships.HasFlag(RelationshipType.ProviderTo))
                                    else if ((allowedRelationships & RelationshipType.ProviderTo) == RelationshipType.ProviderTo)
                                        utility.updatePath(oppositeNode.NodeNum, ChosenPath[currentNode.NodeNum], Destination._PROVIDERCOLUMN, ref ChosenPath);
                                    //else if (allowedRelationships.HasFlag(RelationshipType.PeerOf))
                                    else if ((allowedRelationships & RelationshipType.PeerOf) == RelationshipType.PeerOf)
                                        utility.updatePath(oppositeNode.NodeNum, ChosenPath[currentNode.NodeNum], Destination._PEERCOLUMN, ref ChosenPath);
                                }

                            }
                            /*** NEED TO UPDATE BEST SET WHETHER OR NOT THIS WINS THE TIE BREAK!! **/
                            if(!oppositeNode.isStub() || !OnlyNonStubs)
                            Best[oppositeNode.NodeNum].Add(currentNode.NodeNum);
                        }
                    }
                }
                // Update the in-process BFS status
                currentNode.InProcessBfsStatus = NodeInProcessBfsStatus.ProcessedInCurrentRun;
            }

            // Finished the BFS... lock the discovered nodes into the BFS tree
            foreach (AsNode node in graph.GetAllNodes())
            {
                // FYI In limitedDiscovery mode, some nodes may have been left in the SeenInCurrentRun state
                // (instead of ProcessedInCurrentRun).
                if (node.InProcessBfsStatus != NodeInProcessBfsStatus.UnseenInCurrentRun)
                {
                    node.PriorBfsStatus = NodePriorBfsStatus.DiscoveredInPriorBfs;
                }
                node.InProcessBfsStatus = NodeInProcessBfsStatus.UnseenInCurrentRun;
            }
        }

        public static UInt32 tieBreak(List<UInt32> tieBreakSet,UInt32 currNode)
        {
            /** to do: put an associative/transitive non-monotonically decreasing function over
             * the integers here*/
            UInt32 min = UInt32.MaxValue;
            UInt32 minparent = UInt32.MaxValue;
            foreach (UInt32 i in tieBreakSet)
            {
                //debugging
               // Console.WriteLine("min is " + min + " curr hash("+currNode+","+i+") is: " + hash6432shift(currNode, i));
                
                //BREAKING TIES DETERMINISTICALLY FOR DEBUGGING PURPOSES! uncomment next line for proper randomized tiebreaking
                UInt32 currHash;
                if(Hash)
                currHash= hash6432shift(currNode, i);
                else currHash = i;
                if (currHash < min)
                {
                    min = currHash;
                    minparent = i;
                }
            }
            return minparent;
        }

         /// <summary>
         /// hashing function u should be the fixed node making the decision and v should be a potential parent (from tiebreak set).
         /// </summary>
         /// <param name="u"></param>
         /// <param name="v"></param>
         /// <returns></returns>
        public static UInt32 hash6432shift(UInt32 u, UInt32 v)
        {
            UInt64 key = ((UInt64)u << 32) | v;
            key = (~key) + (key << 18); // key = (key << 18) - key - 1;
            key = key ^ (key >> 31);
            key = key * 21; // key = (key + (key << 2)) + (key << 4);
            key = key ^ (key >> 11);
            key = key + (key << 6);
            key = key ^ (key >> 22);
            return (UInt32)key;
        }

        /// <summary>
        /// This function contains the tie-breaking algorithm when two routes are found to the same node (with
        /// the same depth).
        /// </summary>
        public static bool NewRouteWinsTieBreak(AsNode newNode, AsNode oppositeNode, bool includeSibling)
        {
            // If includeSibling is true, then the first tie-break is to prefer non-sibling over sibling
            if (includeSibling)
            {
                // Determine sibling status
                bool otherRouteIsViaSiblings = oppositeNode.IsSiblingOf(oppositeNode.BfsParentNode);
                bool newRouteIsViaSiblings = newNode.IsSiblingOf(oppositeNode);

                // If other route is via siblings and new route isn't, new route wins
                if (otherRouteIsViaSiblings && !newRouteIsViaSiblings)
                {
                    return true;
                }

                // If other route isn't via siblings and new route is, other route wins
                if (!otherRouteIsViaSiblings && newRouteIsViaSiblings)
                {
                    return false;
                }

                // Other cases are a tie... continue tie-break algorithm
            }

            // Lowest node hashed number wins.
            //child , potential parent.
            if (hash6432shift(oppositeNode.NodeNum,newNode.NodeNum) < hash6432shift(oppositeNode.NodeNum,oppositeNode.BfsParentNode.NodeNum))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// This function contains the tie-breaking algorithm when two routes are found to the same node (with
        /// the same depth).
        /// </summary>
        public static bool NewRouteWinsTieBreakOriginal(AsNode newNode, AsNode oppositeNode, bool includeSibling)
        {
            // If includeSibling is true, then the first tie-break is to prefer non-sibling over sibling
            if (includeSibling)
            {
                // Determine sibling status
                bool otherRouteIsViaSiblings = oppositeNode.IsSiblingOf(oppositeNode.BfsParentNode);
                bool newRouteIsViaSiblings = newNode.IsSiblingOf(oppositeNode);

                // If other route is via siblings and new route isn't, new route wins
                if (otherRouteIsViaSiblings && !newRouteIsViaSiblings)
                {
                    return true;
                }

                // If other route isn't via siblings and new route is, other route wins
                if (!otherRouteIsViaSiblings && newRouteIsViaSiblings)
                {
                    return false;
                }

                // Other cases are a tie... continue tie-break algorithm
            }

            // Lowest node number wins
            if (newNode.NodeNum < oppositeNode.BfsParentNode.NodeNum)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Retrieves a path from the BFS root to the specified start node.
        /// The path is returned as a list of AsNode objects.
        /// If there is no path from the BFS root, this function returns null.
        /// </summary>
        public static List<AsNode> GetPathFromBfsRoot(NetworkGraph graph, UInt32 srcNodeNum)
        {
            // Check the input
            AsNode currNode = graph.GetNode(srcNodeNum);
            if ((currNode == null) || (currNode.BfsDepth == Int32.MaxValue))
            {
                return null;
            }
            // Start a list of nodes in the path
            List<AsNode> nodeList = new List<AsNode>();
            nodeList.Insert(0, currNode);
            // Keep adding parents to the list until we get to the root
            while (currNode.BfsParentNode != null)
            {
                currNode = currNode.BfsParentNode;
                nodeList.Insert(0, currNode);
            }
            return nodeList;
        }

     

        /// <summary>
        /// ADDED BY SHARON
        /// Checks if a particular other node (usually the attacker) is on the path
        /// from the source node to the BFS root.  Returns true if it is, false otherwise
        /// If there is no path to the BFS root, this function returns false
        /// </summary>
        public static bool isNodeOnPathFromBfsRoot(NetworkGraph graph, UInt32 srcNodeNum, UInt32 attackerNodeNum)
        {
            AsNode currNode = graph.GetNode(srcNodeNum);
            AsNode attackerNode = graph.GetNode(attackerNodeNum);

            // Check the input
            if ((currNode == null) || (attackerNode == null) || (currNode.BfsDepth == Int32.MaxValue))
            {
                return false;
            }
            
            //walk down path to root, checking for attacker
            while (currNode != null)
            {
                if (currNode.NodeNum == attackerNode.NodeNum)
                {
                    return true;
                }
                currNode = currNode.BfsParentNode;
            }
            return false;
        }

        /// <summary>
        /// ADDED BY SHARON
        /// Looks at the path this node has in the BFS
        /// and returns the relationship with the first hop on this path
        /// /// </summary>
        public static RelationshipType GetPathTypeFromBfsRoot(NetworkGraph graph, UInt32 nodeNum)
        {
            AsNode node = graph.GetNode(nodeNum);
            // first check that the node has a path to the root
            if (node.BfsDepth == Int32.MaxValue)
                return RelationshipType.NullRelationship;
            else  // return the relationship it has with its BFS parent
                return node.BfsParentNode.GetRelationshipTypeOfNeighbor(node); 
        }



        /// <summary>
        /// Examines the BFS subtree rooted at the node specified by nodeNum and returns a list of the
        /// nodes in the tree.
        /// </summary>
        public static List<AsNode> GetNodesInBfsSubtree(NetworkGraph graph, UInt32 nodeNum)
        {
            // Make sure the node exists and is in the BFS tree
            AsNode rootNode = graph.GetNode(nodeNum);
            if ((rootNode == null) ||
                (rootNode.BfsDepth == Int32.MaxValue))
            {
                return null;
            }

            // Running node count & list
            List<AsNode> nodeList = new List<AsNode>();

            // Similar to BFS, but we're only going to follow edges that are BFS children
            Queue<AsNode> nodeQueue = new Queue<AsNode>();
            nodeQueue.Enqueue(rootNode);

            // While there's still nodes in the sub-tree to be examined...
            while (nodeQueue.Count > 0)
            {
                // Dequeue a node and iterate through its BFS children
                AsNode currentNode = nodeQueue.Dequeue();
                foreach (AsNode oppositeNode in currentNode.GetNeighborsByType(RelationshipType.BfsParentOf))
                {
                    nodeQueue.Enqueue(oppositeNode);
                    nodeList.Add(oppositeNode);

                    // Sanity check... current node should be the BFS parent of opposite node
                    if (oppositeNode.BfsParentNode != currentNode)
                    {
                        throw new Exception("Expected current node to be parent of child node");
                    }
                }
            }

            return nodeList;
        }

       
        /// <summary>
        /// Builds the honest tree!
        /// </summary>
        public static NetworkGraph RoutingTreeAlg(NetworkGraph graph, UInt32 rootNodeNum, ref List<UInt32>[] Best, ref List<UInt32>[][] BucketTable,
            ref List<UInt32>[] ChosenPath, ref UInt32[] ChosenParent, ref byte[] L, ref byte[] BestRelation)
        {
            //BFS to create the honest tree
            List<UInt32> nodeList = new List<UInt32>();
            nodeList.Add(rootNodeNum);
            ExecuteBfs(graph, nodeList, false, true, RelationshipType.CustomerOf | RelationshipType.SiblingOf, ref Best,  ref BucketTable, ref ChosenPath, ref ChosenParent,ref L,ref BestRelation); // first stage BFS, customers and siblings
            ExecuteBfs(graph, nodeList, true, false, RelationshipType.PeerOf, ref Best, ref BucketTable, ref ChosenPath, ref ChosenParent, ref L, ref BestRelation);  // second stage BFS, peers
            ExecuteBfs(graph, nodeList, false, true, RelationshipType.ProviderTo | RelationshipType.SiblingOf, ref Best, ref BucketTable, ref ChosenPath, ref ChosenParent, ref L,ref BestRelation); // third stage BFS, providers and siblings

         /*   OutputLog.LogMessageNoNewline(
                 LogLevelType.LogLevelInfo,
                 "\n Routing tree algorithm on destination {0} complete. \n",  rootNodeNum);*/

            return graph;
        }

        /// <summary>
        /// Builds the SHORTEST PATH tree!
        /// </summary>
        public static NetworkGraph ShortestRoutingTreeAlg(NetworkGraph graph, UInt32 rootNodeNum, ref List<UInt32>[] Best, ref List<UInt32>[][] BucketTable,
            ref List<UInt32>[] ChosenPath, ref UInt32[] ChosenParent, ref byte[] L, ref byte[] BestRelation)
        {
            //BFS to create the honest tree
            List<UInt32> nodeList = new List<UInt32>();
            nodeList.Add(rootNodeNum);
            ExecuteBfs(graph, nodeList, false, true, RelationshipType.NullRelationship, ref Best, ref BucketTable, ref ChosenPath, ref ChosenParent, ref L, ref BestRelation); // first stage BFS, customers and siblings
           
            /*   OutputLog.LogMessageNoNewline(
                    LogLevelType.LogLevelInfo,
                    "\n Routing tree algorithm on destination {0} complete. \n",  rootNodeNum);*/

            return graph;
        }

     
    }
}
