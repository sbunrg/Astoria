using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;


namespace SecureSimulator
{
    /// <summary>
    /// this class contains structures relating to the 
    /// </summary>
   [Serializable]
   public class Destination
    {
       public List<UInt32>[][] BucketTable;
       public List<UInt32>[] Best;
       public List<UInt32>[] BestNew;
       public byte[] BestRelation;
       public bool[] SecP;

       public UInt32[] ChosenParent;
       public List<UInt32>[] ChosenPath;
       public Int64[] U;

       public byte[] L;

       public int totalCount;
      
       public UInt32 destination;

        public const int _CUSTOMERCOLUMN = 0;
        public const int _PEERCOLUMN = 1;
        public const int _PROVIDERCOLUMN = 2;
        public const int _NOREL = 3;


        public Destination(MiniDestination miniDestination)
        {
            this.BucketTable = miniDestination.BucketTable;
            this.Best = miniDestination.Best;
            this.BestNew = miniDestination.BestNew;
            this.BestRelation = miniDestination.BestRelation;
            this.L = miniDestination.L;
            this.destination = miniDestination.destination;

         
            //rehydrate the parts of the object not passed in text.
            this.ChosenParent = new UInt32[this.Best.Length];
            this.ChosenPath = new List<UInt32>[this.Best.Length];

            this.SecP = new bool[this.Best.Length];
            this.U = new Int64[this.Best.Length];
            for (int i = 0; i < SecP.Length; i++)
                SecP[i] = false;
            //init the destination path to itself to kick things off
            //this.SecP[this.destination] = true; //*bug*
            this.ChosenPath[this.destination] = new List<UInt32>();
            this.ChosenPath[this.destination].Add(this.destination);
            this.ChosenParent[this.destination] = this.destination;
        }

        public Destination(UInt32 destination, List<UInt32>[][] BucketTable, List<UInt32>[] Best, List<UInt32>[] BestNew,
            List<UInt32>[] ChosenPath, UInt32[] ChosenParent,byte[] L,  byte[] BestRelation)
        {
            this.destination = destination;
            this.BucketTable = BucketTable;
            this.Best = Best;
            this.BestNew = BestNew;
            this.ChosenPath = ChosenPath;
            this.ChosenParent = ChosenParent;
            this.L = L;

            this.BestRelation = BestRelation;

            SecP = new bool[Constants._numASNs];
            for (int i = 0; i < SecP.Length; i++)
                SecP[i] = false;

            U = new Int64[Constants._numASNs];
            
        }

        /// <summary>
        /// constructor for when we just want to use utility functions from within this class. 
        /// pretty much just for BFS.
        /// </summary>
        public Destination()
        {

        }


        public double averageBestSize()
        {
            double averageBestSize = 0;
            double numberOfNodes = 0;
             //iterating over the buckets
            for (int row = 0; row < BucketTable.GetLength(0); row++)
            {
                for (int col = 0; col < BucketTable[0].GetLength(0); col++)
                {
                    if (BucketTable[row][col] != null)
                    {
                        foreach (UInt32 ASN in BucketTable[row][col])
                        {
                            if (Best[ASN] == null)
                                continue;
                            numberOfNodes++;
                            averageBestSize += Best[ASN].Count;
                        }
                    }
                }
            }

            return averageBestSize / numberOfNodes;
        }
        /// <summary>
        /// performs the UpdatePaths function from pseudo code note that since
        /// the destination maintains its BucketTable, Best, SecP and ChosenPath
        /// they do not get passed as parameters
        /// </summary>
        /// <param name="S"></param>
        /// <param name="minRow"> row in the BFS to start at.</param>
        public void UpdatePaths(bool[] S)
        {
            int minRow = 0; ;//start at first row

            /** need to deal with the destination when we 
             * are setting up the secure paths 
             * **/
            if (minRow < 1)
            {
                //iterating over columns in row 0
                for (int i = 0; i < BucketTable[0].GetLength(0); i++)
                {
                    foreach (int n in BucketTable[0][0]) //for all destinations (all destinations live in customer column
                    {
                        if (S[n])
                            SecP[n] = true; //they have secure paths
                        else
                            SecP[n] = false;
                    }
                }
                minRow++;//do not reconsider first row.
            }

            //iterating over the buckets
            for (int row = minRow; row < BucketTable.GetLength(0); row++)
            {
                for (int col = 0; col < BucketTable[0].GetLength(0); col++)
                {
                    if (BucketTable[row][ col] == null)
                        continue;
                    foreach (UInt32 i in BucketTable[row][ col])
                    {
                        /* grab the set of nodes offering us equally "good" paths */
                        List<UInt32> tieBreakSet = new List<UInt32>();

                        //if we are secure, see if we have secure parents; secure paths get priority here
                        if (S[i])
                        {
                            foreach (UInt32 parent in Best[i])
                            {
                                if (SecP[parent])
                                    tieBreakSet.Add(parent);
                            }
                            if (tieBreakSet.Count == 0)
                                tieBreakSet = Best[i];
                        }
                        else
                            tieBreakSet = Best[i];

                      UInt32 newParent = ModifiedBfs.tieBreak(tieBreakSet,i);

                        updatePath(i, ChosenPath[newParent], col, ref ChosenPath);
                        ChosenParent[i] = newParent; //update our parent as well
                        SecP[i] = SecP[newParent]& S[i];//AND it with our security to keep non-secure nodes from announcing available secure paths
                    }
                }
            }
        }

        /// <summary>
        /// returns current utility as determined by looking at the ChosenPath structure
        /// </summary>
        /// <returns>vector with 1 element per ASN with that ASNs utility.</returns>
        public void ComputeU(UInt16[] W)
        {
            //4 temporary utility types (customer sub tree size, 
            //customer weighted subtree, peer weighted subtree and provider weighted subtree
            const Int32 CustomerTreeSize = 0;
            const Int32 CustomerWeightedTreeSize = 1;
            const Int32 PeerWeightedTreeSize = 2;
            const Int32 ProviderWeightedTreeSize = 3;

            Int32[][] tempU = new Int32[4][];
            for (int i = 0; i < tempU.Length; i++)
                tempU[i] = new Int32[Constants._numASNs];

            //reinitialize U
            for (int i = 0; i < U.Length; i++)
                U[i] = 0;

            for (int i = 0; i < ChosenPath.Length; i++)
            {
               
                if (ChosenPath[i] != null)
                {
                    /** first element of the path is non-transformed ASN of the
                     * node at the end of the path, skip it **/
                    for (int j = 1; j < ChosenPath[i].Count; j++)
                    {
                        UInt32 ASN;
                        Int32 col;
                        unjoin(ChosenPath[i][j], out ASN, out col);
                        if (ASN != destination)
                        {
                            if (col == _PROVIDERCOLUMN)
                            {
                                tempU[CustomerTreeSize][ASN]++;
                                tempU[CustomerWeightedTreeSize][ASN] += W[i];/*W[i] because i is the node routing through us 
                                                                        (whose path we are considering)
                                                                        how much are they worth to us?*/
                            }
                            else if (col == _PEERCOLUMN)
                                tempU[PeerWeightedTreeSize][ASN] += W[i];
                            else if (col == _CUSTOMERCOLUMN)
                                tempU[ProviderWeightedTreeSize][ASN] += W[i];

                        }
                    }
                }
            }

            for (int i = 0; i < U.Length; i++)
                U[i] = SimulatorLibrary.utilityComputation(tempU[CustomerTreeSize][i],
                                    tempU[CustomerWeightedTreeSize][i],
                                    tempU[PeerWeightedTreeSize][i],
                                    tempU[ProviderWeightedTreeSize][i],
                                    BestRelation[i],W[destination]);
        }

        /// <summary>
        /// given a numerically marked ASN return to us the column and ASN
        /// </summary>
        /// <param name="joined"></param>
        /// <param name="ASN"></param>
        /// <param name="col"></param>
        public void unjoin(UInt32 joined, out UInt32 ASN, out int col)
        {
            ASN = (UInt32)(((uint)joined) >> 3);
            col = (int)(joined & 7); //and with 7 to get lower 3 bits
        }

        /// <summary>
        /// function to numerically mark this ASN with having the and edge with relationship type specified by the column.
        /// </summary>
        /// <param name="ASN"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        private UInt32 join(UInt32 ASN, int col)
        {
            return (UInt32)((ASN << 3) + col);
        }


        /// <summary>
        /// Returns the chosen parents from the paths (1 row per ASN)
        /// </summary>
        /// <param name="ChosenPath"></param>
        private UInt32[] ChosenParentFromPath(List<UInt32>[] ChosenPath)
        {
            UInt32[] ChosenParent = new UInt32[Constants._numASNs];
            for (int i = 0; i < ChosenParent.Length; i++)
                ChosenParent[i] = 0;

            //foreach row; set the parent to the first element in the path vector.
            for (int i = 0; i < ChosenPath.Length; i++)
            {
                int tmp;
                unjoin(ChosenPath[i][0], out ChosenParent[i], out tmp);
            }

            return ChosenParent;
        }

        /// <summary>
        /// takes in an ASN, it's parent's path and the relationship it has to its parent. This is a utility function
        /// for the BFS class when it needs to create the ChosenPath structure.
        /// </summary>
        /// <param name="currASN"></param>
        /// <param name="parentPath"></param>
        /// <param name="relationshipType"></param>
        public void updatePath(UInt32 currASN, List<UInt32> parentPath, int relationshipType, ref List<UInt32>[] CPToUpdate)
        {

            CPToUpdate[currASN] = new List<UInt32>();
            //add itself 
            CPToUpdate[currASN].Add(currASN);
            //add its parent with its relationship to itself
            if (parentPath!= null && parentPath[0] != currASN) // check that it isn't its own parent (ie. a destination)
            {
                CPToUpdate[currASN].Add(join(parentPath[0], relationshipType));
                //add the rest of the parent's path.
                CPToUpdate[currASN].AddRange(parentPath.GetRange(1, parentPath.Count - 1));
            }
        }

        /// <summary>
        /// debugging function to give us a string representation of the path for
        /// a node n.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public string GetPath(UInt32 n)
        {
            string toreturn = "";

            if (ChosenPath[n] == null)
				return "no_path";

            toreturn = Convert.ToString(ChosenPath[n][0]);//first element is this node with no annotations.
            for (int i = 1; i < ChosenPath[n].Count; i++)
            {
                UInt32 ASN;
                int col;
                unjoin(ChosenPath[n][i], out ASN, out col);

                switch (col)
                {
                    case _CUSTOMERCOLUMN:
                        toreturn = toreturn + " <- ";
                        break;
                    case _PEERCOLUMN:
                        toreturn = toreturn + " -- ";
                        break;
                    case _PROVIDERCOLUMN:
                        toreturn = toreturn + " -> ";
                        break;
                }

                toreturn = toreturn + ASN;
            }

            return toreturn;
        }

        public string GetPathCommaSep(UInt32 n)
        {
            string toreturn = "";

            if (ChosenPath[n] == null)
                return "no path found for this node, are you sure it exists?";

            toreturn = Convert.ToString(ChosenPath[n][0]);//first element is this node with no annotations.
            for (int i = 1; i < ChosenPath[n].Count; i++)
            {
                UInt32 ASN;
                int col;
                unjoin(ChosenPath[n][i], out ASN, out col);

                switch (col)
                {
                    case _CUSTOMERCOLUMN:
                        toreturn = toreturn + ",";
                        break;
                    case _PEERCOLUMN:
                        toreturn = toreturn + ",";
                        break;
                    case _PROVIDERCOLUMN:
                        toreturn = toreturn + ",";
                        break;
                }

                toreturn = toreturn + ASN;
            }

            return toreturn;
        }

        /// <summary>
        /// debugging function to give us a string representation of the path for
        /// a node n.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public string GetPath(UInt32 n,NetworkGraph g)
        {
            string toreturn = "";

            if (ChosenPath[n] == null)
                return "no path found for this node, are you sure it exists?";

            toreturn = Convert.ToString(ChosenPath[n][0]);//first element is this node with no annotations.
            UInt32 lastASN = ChosenPath[n][0];
            for (int i = 1; i < ChosenPath[n].Count; i++)
            {
                UInt32 ASN;
                int col;
                unjoin(ChosenPath[n][i], out ASN, out col);
                RelationshipType rt = g.GetNode(ASN).GetRelationshipTypeOfNeighbor(g.GetNode(lastASN));
                switch (rt)
                {
                    case RelationshipType.CustomerOf:
                        toreturn = toreturn + " <- ";
                        break;
                    case RelationshipType.PeerOf:
                        toreturn = toreturn + " -- ";
                        break;
                    case RelationshipType.ProviderTo:
                        toreturn = toreturn + " -> ";
                        break;
                }
                lastASN = ASN;
                toreturn = toreturn + ASN;
            }

            return toreturn;
        }

        /// <summary>
        /// debugging function to give us a string representation of the path for
        /// a node n.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public string GetPath(UInt32 n,bool[] S)
        {
            string toreturn = "";

            if (ChosenPath[n] == null)
                return "no path found for this node, are you sure it exists?";
            if(S[ChosenPath[n][0]])
            toreturn = Convert.ToString(ChosenPath[n][0]+"(S)");//first element is this node with no annotations.
            else
                 toreturn = Convert.ToString(ChosenPath[n][0]);
            for (int i = 1; i < ChosenPath[n].Count; i++)
            {
                UInt32 ASN;
                int col;
                unjoin(ChosenPath[n][i], out ASN, out col);

                switch (col)
                {
                    case _CUSTOMERCOLUMN:
                        toreturn = toreturn + " <- ";
                        break;
                    case _PEERCOLUMN:
                        toreturn = toreturn + " -- ";
                        break;
                    case _PROVIDERCOLUMN:
                        toreturn = toreturn + " -> ";
                        break;
                }
                if (S[ASN])
                    toreturn = toreturn + ASN + "(S)";
                else
                    toreturn = toreturn + ASN;
            }

            return toreturn;
        }

        public UInt32[] GetPathList(UInt32 n)
        {
            List<UInt32> path = new List<UInt32>();

            if (ChosenPath[n] == null)
                return path.ToArray();
         
                path.Add(ChosenPath[n][0] );//first element is this node with no annotations.
           
            for (int i = 1; i < ChosenPath[n].Count; i++)
            {
                UInt32 ASN;
                int col;
                unjoin(ChosenPath[n][i], out ASN, out col);

               
                    path.Add(ASN);
            }

            return path.ToArray();
        }

        /// <summary>
        /// debugging function to tell us utility of a node n.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public string GetUtility(int n)
        {
            return Convert.ToString(U[n]);
        }

        /// <summary>
        /// debugging function to tell us level of n in BFS rooted at d
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public string getL(int n)
        {
            return Convert.ToString(L[n]);
        }
        /// <summary>
        /// debugging function to tell us if a node has SecP
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public string GetSecP(int n)
        {
            if (SecP[n])
                return "true";
            return "false";
        }

        /// <summary>
        /// debug function to tell us who is in the best set of n.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public string GetBest(int n)
        {
            if (Best[n] == null)
                return "no elements in the best set of " + n + " is this a valid ASN?";
            string toreturn = "Best set of node " + n + " is: ";
            for (int i = 0; i < Best[n].Count; i++)
            {
                if (i < Best[n].Count - 1)
                    toreturn = toreturn + Best[n][i] + ",";
                else
                    toreturn = toreturn + Best[n][i];//last element do not put a comma.
            }

            return toreturn;

        }
        public string GetBestNew(int n)
        {
            if (BestNew[n] == null)
                return "no elements in the bestNew set of " + n + " is this a valid ASN?";
            string toreturn = "BestNew set of node " + n + " is: ";
            for (int i = 0; i < BestNew[n].Count; i++)
            {
                UInt32 ASnum;
                int rel;

                unjoin(BestNew[n][i], out ASnum, out rel);
                
                if (i < BestNew[n].Count - 1)
                    toreturn = toreturn + ASnum + ",";
                else
                    toreturn = toreturn + ASnum;//last element do not put a comma.
            }

            return toreturn;

        }

        public string pathString(List<UInt32> path)
        {
            string toreturn = "";
            toreturn = toreturn + Convert.ToString((UInt32)(((uint)path[0]) >> 3));
            for (int j = 1; j < path.Count; j++)
            {
                UInt32 asn;
                int col;
                asn = (UInt32)(((uint)path[j]) >> 3);
                col = (int)(path[j] & 7);

                switch (col)
                {
                    case 0:
                        toreturn = toreturn + " <- ";
                        break;
                    case 1:
                        toreturn = toreturn + " -- ";
                        break;
                    case 2:
                        toreturn = toreturn + " -> ";
                        break;
                    case 3:
                        break;
                }

                toreturn = toreturn + asn;
            }

            return toreturn;

        }

        public bool GetAllBestPaths(UInt32 src, UInt32 dst, ref List<List<UInt32>> allPaths) 
        {
            if (dst != destination) return false;
        
            if (Best[src] != null) {
                // BFS queue of paths
                Queue<List<UInt32>> queue = new Queue<List<UInt32>>();
                // Starting from the source
                List<UInt32> pathFirst = new List<UInt32>();
                pathFirst.Add(src);
                queue.Enqueue(pathFirst);
                while (queue.Count > 0) {
                    List<UInt32> path = queue.Dequeue();
                    UInt32 last = path.Last();
                    if (last == dst) {
                        // Saving a path
                        allPaths.Add(path);
                    }
                    else if (path.Count < 20) {
                        // Building more
                        for (int i = 0; i < Best[last].Count; ++i) {
                            List<UInt32> pathNew = new List<UInt32>();
                            pathNew.AddRange(path);
                            pathNew.Add(Best[last][i]);
                            queue.Enqueue(pathNew);
                        }
                    }
                }
            }

            return true;
        }

        public bool GetAllPaths(UInt32 n, UInt32 dst, ref List<List<UInt32>> allPaths, List<UInt32> recvpath, ref TextWriter tw, ref int count)
        {
            if (dst != destination)
            {
                Console.Write("Incorrect destination call\n");
                return false;
            }
            List<UInt32> path = new List<UInt32>(recvpath);
            
            int showStopper = 0;

            //if (count > 50000)
            //{
            //    return true;
            //}
            
            
            if (BestNew[n] != null)
            {
                //foreach (UInt32 asnode in BestNew[n])
                for (int i = 0; i < BestNew[n].Count; i++)
                {
                    //if (n == 3356)
                    //{
                    //    Console.WriteLine(n + ": i -> " + i + " where i goes upto " + BestNew[n].Count);
                    //}
                    UInt32 asnode = BestNew[n][i];
                    int rels;
                    UInt32 lastNum, destNum;
                    unjoin(asnode, out destNum, out rels);
                    unjoin(path[path.Count - 1], out lastNum, out rels);
                    if (destNum == dst)
                    {
                        //UInt32 end = join(dst, Destination._NOREL);
                        if (lastNum != destNum)
                        {
                            int rel, prevRel;
                            UInt32 temp;
                            unjoin(asnode, out temp, out rel);
                            unjoin(path[path.Count - 1], out temp, out prevRel);

                            if ((rel == Destination._PEERCOLUMN && prevRel == Destination._CUSTOMERCOLUMN) || (prevRel == Destination._PEERCOLUMN && rel == Destination._PROVIDERCOLUMN) || (prevRel == Destination._PEERCOLUMN && rel == Destination._PEERCOLUMN) || (prevRel == Destination._CUSTOMERCOLUMN && (rel == Destination._PEERCOLUMN || rel == Destination._PROVIDERCOLUMN)))
                            {
                                //continue;
                                //break;
                                return true;
                            }
                            
                            path.Add(asnode);
                        }
                        if (allPaths.Contains(path))
                        {
                            Console.Write("\n**** Duplicate ****\n");
                        }
                        count = count + 1;
                        allPaths.Add(path);
                        
                        /*
                        if (allPaths.Count % 1500 == 0)
                        {
                            for (int j = 0; j < allPaths.Count; j++)
                            {
                                tw.WriteLine(pathString(allPaths[j]));
                            }
                            allPaths.Clear();
                        }
                        */

                        //if (count % 1000 == 0)
                        //{
                        //    Console.Write(count + " Paths found.\n");
                        //}
                        //Console.Write("Terminated\n");
                        showStopper = 1;

                        /*string toreturn = "";
                        toreturn = toreturn + Convert.ToString((UInt32)(((uint)path[0]) >> 3));
                        for (int j = 1; j < path.Count; j++)
                        {
                            UInt32 asn;
                            int col;
                            asn = (UInt32)(((uint)path[j]) >> 3);
                            col = (int)(path[j] & 7);

                            switch (col)
                            {
                                case 0:
                                    toreturn = toreturn + " <- ";
                                    break;
                                case 1:
                                    toreturn = toreturn + " -- ";
                                    break;
                                case 2:
                                    toreturn = toreturn + " -> ";
                                    break;
                                case 3:
                                    break;
                            }

                            toreturn = toreturn + asn;
                        }

                        Console.Write(toreturn + "\n");
                         * */
                        //break;
                    }
                    else
                    {
                        
                        UInt32 ASN = (UInt32)(((uint)asnode) >> 3);
                        int rel, prevRel;
                        UInt32 temp;
                        unjoin(asnode, out temp, out rel);
                        unjoin(path[path.Count-1], out temp, out prevRel);
                        //Console.Write("Came here for " + n + " : " + ASN +" and i: " + i +"\n");

                        UInt32 encoded0 = (UInt32)((ASN << 3) + Destination._PROVIDERCOLUMN);
                        UInt32 encoded1 = (UInt32)((ASN << 3) + Destination._CUSTOMERCOLUMN);
                        UInt32 encoded2 = (UInt32)((ASN << 3) + Destination._PEERCOLUMN);
                        UInt32 encoded3 = (UInt32)((ASN << 3) + Destination._NOREL);

                        if (!((path.Exists(element => element == encoded0)) || (path.Exists(element => element == encoded1)) || (path.Exists(element => element == encoded2) || (path.Exists(element => element == encoded3)))) && (path.Count < 7))
                        {
                            if (showStopper == 0)
                            {
                                if ((rel == Destination._PEERCOLUMN && prevRel == Destination._CUSTOMERCOLUMN) || (prevRel == Destination._PEERCOLUMN && rel == Destination._PROVIDERCOLUMN) || (prevRel == Destination._PEERCOLUMN && rel == Destination._PEERCOLUMN) || (prevRel == Destination._CUSTOMERCOLUMN && (rel == Destination._PEERCOLUMN || rel == Destination._PROVIDERCOLUMN)))
                                {
                                    //tw.WriteLine("Dropped: " +pathString(path));
                                    continue;
                                }
                                //Console.Write("Added :" + ASN + "\n");
                                List<UInt32> tempath = new List<UInt32>(path);
                                tempath.Add(asnode);
                                GetAllPaths(ASN, dst, ref allPaths, tempath, ref tw, ref count);
                            }
                        }
                    }
                }
            }
            return true;
        }

        public bool pathoflength(UInt32 n, UInt32 dst, ref List<List<UInt32>> allPaths, List<UInt32> recvpath, int length)
        {
            List<UInt32> path = new List<UInt32>(recvpath);

            int showStopper = 0;

            if (BestNew[n] != null)
            {
                for (int i = 0; i < BestNew[n].Count; i++)
                {
                    UInt32 asnode = BestNew[n][i];
                    int rels;
                    UInt32 lastNum, destNum;
                    unjoin(asnode, out destNum, out rels);
                    unjoin(path[path.Count - 1], out lastNum, out rels);

                    if (destNum == dst)
                    {
                        if (lastNum != destNum)
                        {
                            int rel, prevRel;
                            UInt32 temp;
                            unjoin(asnode, out temp, out rel);
                            unjoin(path[path.Count - 1], out temp, out prevRel);

                            if ((rel == Destination._PEERCOLUMN && prevRel == Destination._CUSTOMERCOLUMN) || (prevRel == Destination._PEERCOLUMN && rel == Destination._PROVIDERCOLUMN) || (prevRel == Destination._PEERCOLUMN && rel == Destination._PEERCOLUMN) || (prevRel == Destination._CUSTOMERCOLUMN && (rel == Destination._PEERCOLUMN || rel == Destination._PROVIDERCOLUMN)))
                            {
                                return false;
                            }

                            path.Add(asnode);
                        }
                        allPaths.Add(path);
                        showStopper = 1;
						//Console.Out.WriteLine ("Checking Length: " + length);
						//Console.Out.WriteLine("Shortest path is: " + pathString(path));
                        return true;
                    }
                    else
                    {

                        UInt32 ASN = (UInt32)(((uint)asnode) >> 3);
                        int rel, prevRel;
                        UInt32 temp;
                        unjoin(asnode, out temp, out rel);
                        unjoin(path[path.Count - 1], out temp, out prevRel);
            
                        UInt32 encoded0 = (UInt32)((ASN << 3) + Destination._PROVIDERCOLUMN);
                        UInt32 encoded1 = (UInt32)((ASN << 3) + Destination._CUSTOMERCOLUMN);
                        UInt32 encoded2 = (UInt32)((ASN << 3) + Destination._PEERCOLUMN);
                        UInt32 encoded3 = (UInt32)((ASN << 3) + Destination._NOREL);

						if (!((path.Exists(element => element == encoded0)) || (path.Exists(element => element == encoded1)) || (path.Exists(element => element == encoded2) || (path.Exists(element => element == encoded3)))) && (path.Count < length))
                        {
                            if (showStopper == 0)
                            {
                                if ((rel == Destination._PEERCOLUMN && prevRel == Destination._CUSTOMERCOLUMN) || (prevRel == Destination._PEERCOLUMN && rel == Destination._PROVIDERCOLUMN) || (prevRel == Destination._PEERCOLUMN && rel == Destination._PEERCOLUMN) || (prevRel == Destination._CUSTOMERCOLUMN && (rel == Destination._PEERCOLUMN || rel == Destination._PROVIDERCOLUMN)))
                                {
                                    return false;
                                }
                                List<UInt32> tempath = new List<UInt32>(path);
                                tempath.Add(asnode);
                                return pathoflength(ASN, dst, ref allPaths, tempath, length);
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
            return false;
        }
    }
}
