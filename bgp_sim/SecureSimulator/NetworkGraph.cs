using System;
using System.Linq;
using System.Collections.Generic;

namespace SecureSimulator
{
    /// <summary>
    /// Class representing a single network graph.  Contains all nodes in the graph.  Nodes in turn contain information
    /// about connections and traversals.
    /// </summary>
    [Serializable]
    public class NetworkGraph
    {
        /// <summary>
        /// List of nodes/AS's in the graph
        /// </summary>
        protected Dictionary<UInt32, AsNode> AsNodes;

        /// <summary>
        /// Property to retrieve the number of nodes in the network graph
        /// </summary>
        public int NodeCount
        {
            get { return AsNodes.Count; }
        }
        /// <summary>
        /// Number of edges in the network graph.  In this context, an edge is a single path
        /// from a source node to a destination node (it is not bidirectional).
        /// </summary>
        public int EdgeCount { get; private set; }
        /// <summary>
        /// Depth of the BFS tree
        /// </summary>
        public int BfsTreeDepth { get; set; }
        /// <summary>
        /// Number of nodes in the BFS tree
        /// </summary>
        public int BfsTreeNodeCount { get; set; }
        /// <summary>
        /// Random number generator, used to pick random nodes
        /// </summary>
        private static readonly Random Random = new Random();

        /// <summary>
        /// Constructor initializes the node list
        /// </summary>
        public NetworkGraph()
        {
            AsNodes = new Dictionary<UInt32, AsNode>();
            EdgeCount = 0;
            BfsTreeDepth = 0;
        }

        public void RemoveNode(UInt32 ASN)
        {
            if (AsNodes.ContainsKey(ASN))
            {
                AsNode toremove = AsNodes[ASN];
                var neighbors = toremove.GetAllNeighbors().ToArray();
                for (int i = 0; i < neighbors.Length; i++)
                {
                    RemoveEdge(neighbors[i].NodeNum, ASN);
                    RemoveEdge(ASN, neighbors[i].NodeNum);
                    if (neighbors[i].GetAllNeighbors().Count() == 0)
                        AsNodes.Remove(neighbors[i].NodeNum);
                }
                AsNodes.Remove(ASN);
              
            }

            
        }


        /// <summary>
        /// Copy constructor creates a copy of a given network graph
        /// </summary>
        public NetworkGraph(NetworkGraph otherGraph)
        {
            AsNodes = new Dictionary<UInt32, AsNode>();
            EdgeCount = 0;
            BfsTreeDepth = 0;

            foreach (AsNode node in otherGraph.GetAllNodes())
            {
                foreach (AsNode neighbor in node.GetAllNeighbors())
                {
                    AddEdge(node.NodeNum, neighbor.NodeNum, node.GetRelationshipTypeOfNeighbor(neighbor));
                }
            }
        }

        /// <summary>
        /// Adds an edge to the graph, where an edge is defined as a relationship between 2 AS's.
        /// This only adds the edge in one direction, from the src node to the dest node.
        /// This does not check for duplicates.
        /// </summary>
        public void AddEdge(UInt32 srcNodeNum, UInt32 destNodeNum, RelationshipType relationshipType)
        {
            // Retrieve the src and dest nodes
            AsNode srcNode = GetOrCreateNode(srcNodeNum);
            AsNode destNode = GetOrCreateNode(destNodeNum);

            // Add the edge to the source node
            srcNode.AddNeighbor(destNode, relationshipType);
            EdgeCount++;
        }

        /// <summary>
        /// Removes an edge from the graph.  This only removes the edge in one direction,
        /// from the src node to the dest node.
        /// </summary>
        public void RemoveEdge(UInt32 srcNodeNum, UInt32 destNodeNum)
        {
            // Retrieve the src and dest nodes
            AsNode srcNode = GetOrCreateNode(srcNodeNum);
            AsNode destNode = GetOrCreateNode(destNodeNum);

            // Add the edge to the source node
            srcNode.RemoveNeighbor(destNode);
            EdgeCount--;
        }

        /// <summary>
        /// Reinitializes the BFS state of the graph.  Returns all nodes back to their "undiscovered" state.
        /// </summary>
        public void ReinitializeBfsState()
        {
            foreach (AsNode node in GetAllNodes())
            {
                node.ReinitializeBfsState();
            }
            BfsTreeDepth = 0;
        }

        /// <summary>
        /// Get a list of all nodes in the graph.
        /// </summary>
        public IEnumerable<AsNode> GetAllNodes()
        {
            foreach (KeyValuePair<UInt32, AsNode> node in AsNodes)
            {
                yield return node.Value;
            }
        }

        /// <summary>
        /// Retrieves a node by node number.  Returns null if the node doesn't exist.
        /// </summary>
        public AsNode GetNode(UInt32 nodeNum)
        {
            AsNode node;
            if (AsNodes.TryGetValue(nodeNum, out node))
            {
                return node;
            }
            return null;
        }

        /// <summary>
        /// Inspects all nodes for duplicate edges.
        /// FYI this is a relatively expensive operation (iterates all edges of all nodes).
        /// </summary>
        public bool HasDuplicateEdges()
        {
            foreach (AsNode node in GetAllNodes())
            {
                if (node.HasDuplicateNeighbors())
                {
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// tells us how many  stubs are in the graph
        /// </summary>
        /// <returns></returns>
        public int countStubs()
        {
            List<UInt32> Stubs = new List<UInt32>();//list of ASNs that are single homed stubs

            foreach (KeyValuePair<UInt32, AsNode> ASN in AsNodes)
            {
                AsNode curr = ASN.Value;
                if (curr.GetNeighborTypeCount(RelationshipType.ProviderTo) == 0)
                    Stubs.Add(ASN.Key);

            }

            return Stubs.Count;
        }



        /// <summary>
        ///get the list of stubs are in the graph
        /// </summary>
        /// <returns></returns>
        public List<UInt32> getStubs()
        {
            List<UInt32> Stubs = new List<UInt32>();//list of ASNs that are stubs

            foreach (KeyValuePair<UInt32, AsNode> ASN in AsNodes)
            {
                AsNode curr = ASN.Value;
                if (curr.GetNeighborTypeCount(RelationshipType.ProviderTo) == 0)
                    Stubs.Add(ASN.Key);

            }

            return Stubs;
        }



        /// <summary>
        ///get the list of non stubs  in the graph
        /// </summary>
        /// <returns></returns>
        public List<UInt32> getNonStubs()
        {
            List<UInt32> NonStubs = new List<UInt32>();//list of ASNs that are single homed stubs

            foreach (KeyValuePair<UInt32, AsNode> ASN in AsNodes)
            {
                AsNode curr = ASN.Value;
                if (curr.GetNeighborTypeCount(RelationshipType.ProviderTo) > 0)
                    NonStubs.Add(ASN.Key);

            }

            return NonStubs;
        }
       

        /// <summary>
        /// Retrieves a node by node number and creates the node if it doesn't exist
        /// </summary>
        protected AsNode GetOrCreateNode(UInt32 nodeNum)
        {
            AsNode node;
            if (!AsNodes.TryGetValue(nodeNum, out node))
            {
                node = new AsNode(nodeNum);
                AsNodes[nodeNum] = node;
            }
            return node;
        }

        /// <summary>
        /// Returns a random node from the list of all nodes in the graph
        /// </summary>
        public AsNode GetRandomNode()
        {
            Int32 index = Random.Next(NodeCount);
            foreach (KeyValuePair<UInt32, AsNode> pair in AsNodes)
            {
                if (index-- == 0)
                {
                    return pair.Value;
                }
            }
            throw new Exception("Unexpected condition in Random function");
        }

        

        /// <summary>
        ///  Returns a random node from the list of nodes in the graph that meet a given constraint
        /// </summary>
        public AsNode GetRandomNodeWithConstraint(Func<AsNode, bool> condition)
        {
            // Create a list containing the nodes that meet the condition
            List<AsNode> nodeList = AsNodes.Select(a => a.Value).Where(condition).ToList();

            // Pick a random element
            Int32 index = Random.Next(nodeList.Count);
            return nodeList[index];
        }

        /// <summary>
        ///  Returns a random node from a list of Tier 1 nodes
        /// </summary>
        public AsNode GetRandomTier1Node(UInt32[] tier1NodeNums)
        {
            // Pick a random element
            UInt32 index = (UInt32)Random.Next(tier1NodeNums.Length);
            return GetNode(tier1NodeNums[index]);
        }

        public static Int32[] Tier1NodeNums()
        {
            Int32[] nodeNums = { 174, 209, 701, 1239, 1299, 2828, 2914, 3549, 3356, 3561, 4323, 6453, 6939, 7018 };
            return nodeNums;
        }

    }
}
