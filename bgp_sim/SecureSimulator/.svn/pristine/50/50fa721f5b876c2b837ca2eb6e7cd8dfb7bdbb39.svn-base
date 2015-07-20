using System;
using System.Collections.Generic;

namespace SecureSimulator
{
    /// <summary>
    /// Enumerates the 4 different relationships possible between AS's.
    /// Defined as flags so they can be passed as OR'd flags to BFS algorithm as a way to specify
    /// which edges can be traversed.
    /// </summary>
    [Flags]
    public enum RelationshipType
    {
        NullRelationship = 0,
        CustomerOf = 1,
        PeerOf = 2,
        ProviderTo = 4,
        SiblingOf = 8,
        BfsParentOf = 16
    }

    /// <summary>
    /// Node status through the stages of an in-process BFS.
    /// </summary>
    public enum NodeInProcessBfsStatus
    {
        UnseenInCurrentRun,
        SeenInCurrentRun,
        ProcessedInCurrentRun
    }

    /// <summary>
    /// Node status after a completed BFS.
    /// </summary>
    public enum NodePriorBfsStatus
    {
        NotDiscoveredInPriorBfs,
        DiscoveredInPriorBfs
    }

    /// <summary>
    /// Represents a single node or AS in the network.
    /// Maintains lists of neighboring nodes, segregated by relationship type.
    /// </summary>
    [Serializable]
    public class AsNode
    {
        /// <summary>
        /// Index into the neighbors array for each type of neighbor
        /// </summary>
        enum NeighborTypeIndex
        {
            CustomerOfIdx = 0,
            PeerOfIdx = 1,
            ProviderToIdx = 2,
            SiblingOfIdx = 3,
            BfsParentOfIdx = 4
        }
        /// <summary>
        /// Number of neighbor relationship types.
        /// 4 normal types plus the BFS Parent Of relationship, which is a subset of the other 4 relationships.
        /// </summary>
        public const int NumNeighborTypes = 5;


        /// <summary>
        /// Unique node/AS number
        /// </summary>
        public UInt32 NodeNum { get; private set; }
        /// <summary>
        /// List of neighbors connected to this node, grouped by neighbor type.  This is an
        /// array of lists, where each entry in the array is a list of neighbors of a particular
        /// type.  The index into the array for a particular type of neighbor is defined by the
        /// NeighborTypeIndex enumeration.
        /// </summary>
        protected List<AsNode>[] Neighbors;
        /// <summary>
        /// BFS status of this node for an in-process BFS
        /// </summary>
        public NodeInProcessBfsStatus InProcessBfsStatus { get; set; }
        /// <summary>
        /// Status of this node after a complete BFS
        /// </summary>
        public NodePriorBfsStatus PriorBfsStatus { get; set; }
        /// <summary>
        /// A link to the parent of this node as determined by the BFS
        /// </summary>
        public AsNode BfsParentNode { get; set; }
        /// <summary>
        /// Depth of this node in the BFS
        /// </summary>
        public Int32 BfsDepth { get; set; }
       

        /// <summary>
        /// Constructor initializes data members
        /// </summary>
        public AsNode(UInt32 nodeNum)
        {
            NodeNum = nodeNum;
            Neighbors = new List<AsNode>[NumNeighborTypes];
            for (int i = 0; i < NumNeighborTypes; i++ )
            {
                Neighbors[i] = new List<AsNode>();
            }
            ReinitializeBfsState();
        }

        /// <summary>
        /// Reinitializes the BFS state of the node
        /// </summary>
        public void ReinitializeBfsState()
        {
            InProcessBfsStatus = NodeInProcessBfsStatus.UnseenInCurrentRun;
            PriorBfsStatus = NodePriorBfsStatus.NotDiscoveredInPriorBfs;
            BfsParentNode = null;
            BfsDepth = Int32.MaxValue;
            Neighbors[(int) NeighborTypeIndex.BfsParentOfIdx].Clear();
        }

        /// <summary>
        /// Add a neighboring node to this node with a specific relationship type.
        /// This does not check for duplicates.
        /// </summary>
        public void AddNeighbor(AsNode neighbor, RelationshipType relationshipType)
        {
            Neighbors[RelationshipTypeToIndex(relationshipType)].Add(neighbor);
        }

        /// <summary>
        /// Removes a neighbor from this node.  FYI this doesn't remove this node
        /// from the neighbor's neighbor list.
        /// </summary>
        public void RemoveNeighbor(AsNode neighbor)
        {
            // Iterate through each nodeList
            foreach (List<AsNode> nodeList in Neighbors)
            {
                // Try to remove the node from the neighbor list
                if (nodeList.Remove(neighbor))
                {
                    // We successfully removed it.  Remove the node from our ParentOf list too, if possible.
                    // FYI this is not a good idea if there are duplicate edges b/w these two nodes, but
                    // we are assuming there aren't duplicate edges.
                    Neighbors[(int)NeighborTypeIndex.BfsParentOfIdx].Remove(neighbor);
                    return;
                }
            }
            throw new Exception("Neighbor not found");
        }


        public bool isStub()
        {
            if (Neighbors[RelationshipTypeToIndex(RelationshipType.ProviderTo)].Count == 0)
                return true;
            return false;
        }

        /// <summary>
        /// Removes a particular BFS child from the BfsParentOf list
        /// Parent is towards root, child towards leaves
        /// </summary>
        public void RemoveBfsChild(AsNode child)
        {
            // Try to remove the node from the list
            if (!Neighbors[(int)NeighborTypeIndex.BfsParentOfIdx].Remove(child))
            {
                throw new Exception("Child not found");
            }
        }

        /// <summary>
        /// Adds a particular BFS child to the BfsParentOf list
        /// </summary>
        public void AddBfsChild(AsNode child)
        {
            Neighbors[(int) NeighborTypeIndex.BfsParentOfIdx].Add(child);
        }

        /// <summary>
        /// Returns the number of neighbors of a given type
        /// </summary>
        public Int32 GetNeighborTypeCount(RelationshipType relationshipType)
        {
            return Neighbors[RelationshipTypeToIndex(relationshipType)].Count;
        }

        /// <summary>
        /// Get a list of neighbors of this node with the given relationship type.
        /// relationshipType could be a bitwise OR of multiple relationship types.
        /// </summary>
        public IEnumerable<AsNode> GetNeighborsByType(RelationshipType relationshipType)
        {
            //if you give it null relationship return all neighbors.
            if (relationshipType == RelationshipType.NullRelationship || (relationshipType==(RelationshipType.NullRelationship|RelationshipType.BfsParentOf)))
            {
                foreach (var n in GetAllNeighbors())
                    yield return n;
            }
            else
            {
                // Iterate through each neighbor type
                for (int neighborIndex = 0; neighborIndex < NumNeighborTypes; neighborIndex++)
                {
                    // If the callre requested this neighbor type...
                    RelationshipType currRelType = IndexToRelationshipType(neighborIndex);
                    if ((relationshipType & currRelType) == currRelType)
                    {
                        // Iterate through all neighbors of this type and return them
                        foreach (AsNode node in Neighbors[neighborIndex])
                        {
                            yield return node;
                        }
                    }
                }
            }
            
        }

        /// <summary>
        /// Retrieves a list of all neighbors of this node (excluding BFS children)
        /// </summary>
        public IEnumerable<AsNode> GetAllNeighbors()
        {
            // Iterate through each neighbor type (except BFS children)
            for (int neighborIndex = 0; neighborIndex < NumNeighborTypes; neighborIndex++)
            {
                if (neighborIndex != (int)NeighborTypeIndex.BfsParentOfIdx)
                {
                    foreach (AsNode node in Neighbors[neighborIndex])
                    {
                        yield return node;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the relationship type of the given neighbor node.  FYI this is
        /// an O(n) operation on the number of neighbors.
        /// </summary>
        public RelationshipType GetRelationshipTypeOfNeighbor(AsNode neighbor)
        {
            // Iterate through each neighbor type and inspect its list for the given neighbor
            for (int i = 0; i < NumNeighborTypes; i++)
            {
                if (Neighbors[i].Contains(neighbor))
                {
                    return IndexToRelationshipType(i);
                }
            }
            //throw new Exception("Neighbor not found");
            return RelationshipType.NullRelationship;
        }

        


        /// <summary>
        /// Checks for duplicate neighbors (two edges to the same neighbor).
        /// </summary>
        public bool HasDuplicateNeighbors()
        {
            HashSet<AsNode> hash = new HashSet<AsNode>();
            foreach (AsNode neighbor in GetAllNeighbors())
            {
                if (hash.Contains(neighbor))
                {
                    return true;
                }
                hash.Add(neighbor);
            }
            return false;
        }

        /// <summary>
        /// Returns true if the other node is a sibling of this node.
        /// </summary>
        public bool IsSiblingOf(AsNode otherNode)
        {
            if (Neighbors[(int) NeighborTypeIndex.SiblingOfIdx].Contains(otherNode))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns the opposite relationship type of the input parameter.
        /// e.g. CustomerOf becomes ProviderTo
        /// </summary>
        public static RelationshipType GetOppositeRelationshipType(RelationshipType relationshipType)
        {
            switch (relationshipType)
            {
                case RelationshipType.CustomerOf:
                    return RelationshipType.ProviderTo;
                case RelationshipType.PeerOf:
                    return RelationshipType.PeerOf;
                case RelationshipType.ProviderTo:
                    return RelationshipType.CustomerOf;
                case RelationshipType.SiblingOf:
                    return RelationshipType.SiblingOf;
                default:
                    throw new Exception("Unknown relationship type.");
            }
        }

        /// <summary>
        /// Converts a neighbor type index into a relationship type.
        /// Currently uses bit shifting to keep this quick (which requires that the values are fixed and
        /// won't change in the future)
        /// </summary>
        private static RelationshipType IndexToRelationshipType(int neighborTypeIndex)
        {
            return (RelationshipType)(1 << neighborTypeIndex);  // take 1, move it (left) nei.. times
        }

        /// <summary>
        /// Converts a relationship type into an index, which can be used to find the list of neighbors of the
        /// given type within the Neighbors array.
        /// </summary>
        private static int RelationshipTypeToIndex(RelationshipType relationshipType)
        {
            switch (relationshipType)
            {
                case RelationshipType.CustomerOf:
                    return (int)NeighborTypeIndex.CustomerOfIdx;
                case RelationshipType.ProviderTo:
                    return (int)NeighborTypeIndex.ProviderToIdx;
                case RelationshipType.PeerOf:
                    return (int)NeighborTypeIndex.PeerOfIdx;
                case RelationshipType.SiblingOf:
                    return (int)NeighborTypeIndex.SiblingOfIdx;
                case RelationshipType.BfsParentOf:
                    return (int)NeighborTypeIndex.BfsParentOfIdx;
                default:
                    throw new Exception("Unexpected relationship type");
            }
        }
    }
}
