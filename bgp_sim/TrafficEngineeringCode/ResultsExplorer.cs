using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SecureSimulator;

namespace TrafficEngineeringCode
{

    public struct resultObject
    {
        public void printParams()
        {
            Console.WriteLine("graph: " + graphFile);
            Console.Write("early adopters: ");
            foreach (var asn in earlyAdopters)
                Console.Write(asn + ", ");
            Console.WriteLine();
            Console.Write("weighted nodes: ");
            foreach (var asn in weightedNodes)
                Console.Write(asn + ", ");
            Console.WriteLine();
            Console.WriteLine("f: " + f);
            Console.WriteLine("u: " + u);
            Console.WriteLine("k: " + k);
            Console.WriteLine("utility: " + utility);
            Console.WriteLine("directory: " + directory);
            Console.WriteLine("num iterations: " + ubefore.Count);

        }
        public string graphFile;
        public List<UInt32> earlyAdopters;
        public List<UInt32> weightedNodes;
        public string f;
        public string u;
        public List<bool[]> state;
        public List<long[]> ubefore;
        public List<long[]> uafter;
        public string k;
        public string precursor;
        public string directory;
        public string utility;
        public NetworkGraph g;
    }

     class ResultsExplorer
    {

         public static string defaultResultsDirectory = "..\\..\\..\\..\\..\\DataFromDryad\\2010Data";
         public static string defaultGraphDirectory = "..\\..\\..\\..\\..\\AugmentedGraphs\\";
         public static string defaultOutputDirectory = "..\\..\\..\\..\\..\\ResultsAnalysis\\FromResultsExplorer\\";
       
         resultObject results;//= new resultObject();
         string baseDirectory ;//= "";
         string graphDirectory;
         char[] space = { ' ' };

         public ResultsExplorer()
         {
             baseDirectory = defaultResultsDirectory;
             graphDirectory = defaultGraphDirectory;
         }

         /// <summary>
         /// tell us the average degree of our neighbors
         /// + average number of stubs they have.
         /// + number of stubs we have. 
         /// </summary>
         /// <param name="commandPieces"></param>
        public void averageStubDegreeOfNeighbors(string[] commandPieces)
        {
            if (commandPieces.Length < 2)
            {
                Console.WriteLine("usage: averagedegree [ASN|big5|top5]");
                return;
            }

            List<UInt32> ASNs = new List<UInt32>();
            if (commandPieces[1] == "big5")
            {
                UInt32[] big5 = { 15169, 8075, 32934, 20940, 22822 };
                ASNs.AddRange(big5);
            }
            else if (commandPieces[1] == "top5")
            {
                UInt32[] top5 = { 1239, 701, 174, 3356, 7018 };
                ASNs.AddRange(top5);
            }
            else
            {
                uint ASN;
                if (uint.TryParse(commandPieces[1], out ASN))
                    ASNs.Add(ASN);     
            }

            var stubs = results.g.getStubs();
            double avgDegree = 0;
            double avgStubDegree = 0;//average across our ASNs.
            foreach (UInt32 ASN in ASNs)
            {
                var neighbors = results.g.GetNode(ASN).GetAllNeighbors();
                avgDegree += neighbors.Count();
                int stubDegree=getStubDegree(ASN,stubs);
                avgStubDegree += stubDegree;
                double numStubs = 0;
                double currentASNAvgDegree = 0;
                double currentASNAvgStubDegree = 0;
                double currentMaxDegree = double.MinValue;
                double currentMaxStubDegree = double.MinValue;
                foreach (var n in neighbors)
                {
                    double currNeighborDegree = n.GetAllNeighbors().Count();
                    double currNeighborStubDegree = getStubDegree(n.NodeNum, stubs);
                    currentASNAvgDegree += currNeighborDegree;
                    currentASNAvgStubDegree +=currNeighborStubDegree;
                    if (currNeighborDegree > currentMaxDegree)
                        currentMaxDegree = currNeighborDegree;
                    if (currNeighborStubDegree > currentMaxStubDegree)
                        currentMaxStubDegree = currNeighborStubDegree;

                }
                Console.WriteLine("ASN: " + ASN + " deg: " + neighbors.Count() + " deg(st) " + stubDegree+" avgNbrDeg: {0:0.00}  avgNbrDeg(st): {1:0.00} ", currentASNAvgDegree / neighbors.Count(), currentASNAvgStubDegree / neighbors.Count());
                Console.WriteLine("maxNbrDeg: " + currentMaxDegree + " maxNbrDeg(st): " + currentMaxStubDegree); 
            }

            Console.WriteLine("average across all ASNs: avgDeg: " + avgDegree / ASNs.Count + " avgDeg(st): " + avgStubDegree / ASNs.Count);
        }

        public int getStubDegree(UInt32 ASN,List<UInt32> stubs)
        {
            var neighbors = results.g.GetNode(ASN).GetAllNeighbors();
         //   var stubs = results.g.getStubs();
            int toreturn = 0;
            foreach (var neighbor in neighbors)
            {
                if (stubs.Contains(neighbor.NodeNum))
                    toreturn++;

            }
            return toreturn;
        }

           /// <summary>
        /// get us the list of stubs 2 hops away from anyone in the target set
        /// that have 2 parents
        /// 
        /// return the 10 stubs in this set whose parents have lowest degree
        /// </summary>
        /// <param name="g"></param>
        public  void findSubGraph()
        { 
        
        
        }

        public void big5FlippersOn(string[] commandPieces)
        {
            bool[] lastState = results.state[results.state.Count - 1];
            int flippersOnNow = 0;

            foreach (var ASN in flippedForBig5)
            {
                if (lastState[ASN])
                    flippersOnNow++;
                
            }
            Console.WriteLine("of " + flippedForBig5.Count+"ASes that flipped for the big 5 " 
                                + flippersOnNow + " are on in this simulation. " + (flippedForBig5.Count-flippersOnNow) + " are not on.");
                
        }

        public  void getPath(string[] commandPieces)
        {
            if (commandPieces.Length < 3)
            {
                Console.WriteLine("usage: getpath [Source ASN] [Dest ASN] [iteration? default will be to print all of them.]");
                return;
            }
            UInt32 SASN,DASN;
            if (!UInt32.TryParse(commandPieces[1], out SASN) || !UInt32.TryParse(commandPieces[2], out DASN))
            {
                Console.WriteLine("malformed ASN");
                return;
            }
            int iteration = results.state.Count ;
            if (commandPieces.Length > 3)
            {
                int.TryParse(commandPieces[3], out iteration);
            }

            Destination D = new Destination(SimulatorLibrary.initMiniDestination(results.g, DASN, false));
            if (D.Best[SASN] == null)
            {
                Console.WriteLine(SASN + " has no path to " + DASN);
                return;
            }

            if (iteration < results.state.Count)
            {
                D.UpdatePaths(results.state[iteration]);
                Console.WriteLine(D.GetPath(SASN, results.state[iteration]));
            }
            else
            {
                for (iteration = 0; iteration < results.state.Count; iteration++)
                {
                    D.UpdatePaths(results.state[iteration]);
                    Console.WriteLine(iteration+" : "+D.GetPath(SASN, results.state[iteration]));
                }
            }

        }

        public void getneighbors(string[] commandPieces)
        {
            if (commandPieces.Length < 2)
            {
                Console.WriteLine("usage: getneighbors [ASN] [customer/peer/provider] [nonstubs?/stubs? to get exclusivly 1 type of node]");
                return;
            }
            RelationshipType toget = RelationshipType.PeerOf;
            bool onlyNonStubs = false;
            bool onlyStubs = false;
            for (int i = 2; i < commandPieces.Length; i++)
            {
                if (commandPieces[i].IndexOf("customer") == 0)
                {
                    Console.WriteLine("printing customers: ");
                    toget = RelationshipType.ProviderTo;
                }
                if (commandPieces[i].IndexOf("peer") == 0)
                {

                
                    toget = RelationshipType.PeerOf;
                }
                if (commandPieces[i].IndexOf("provider") == 0)
                {

                    Console.WriteLine("printing providers: ");
                    toget = RelationshipType.CustomerOf;
                }
                if (commandPieces[i] == "nonstubs")
                    onlyNonStubs = true;
                if (commandPieces[i] == "stubs")
                    onlyStubs = true;
            }
            //separated out to handle default case.
            if(toget==RelationshipType.PeerOf)
                Console.WriteLine("printing peers: ");

            UInt32 ASN;
            if (!UInt32.TryParse(commandPieces[1], out ASN))
            {
                Console.WriteLine("malformed ASN");
                return;
            }
            AsNode ASNode;
            if ((ASNode = results.g.GetNode(ASN)) == null)
            {
                Console.WriteLine("This node was not in the graph");
                return;
            }
            var stubs = results.g.getStubs();
            var nonstubs = results.g.getNonStubs();

            var neighbors = ASNode.GetNeighborsByType(toget);
            int neighborcount = 0;
            foreach (var neighbor in neighbors)
            {
                if ((!onlyNonStubs || nonstubs.Contains(neighbor.NodeNum)) && (!onlyStubs || stubs.Contains(neighbor.NodeNum)))
                {
                    Console.Write(neighbor.NodeNum + ", ");
                    neighborcount++;
                }
            }
            Console.WriteLine("printed " + neighborcount + " neighbors");
        }

        public void summarizeSimulation()
        {
  
            Console.WriteLine("\tOn\tNonStub\tStub\tFlip:\tFlipNonStub\tFlipStub");
            int numOn = 0;
            int numOnStubs = 0;
            int numOnNonStubs = 0;
           
            List<UInt32> nonStubs = results.g.getNonStubs();
            List<UInt32> stubs = results.g.getStubs();

            for (int i = 0; i < results.state.Count; i++)
            {
                int currentOn = numberOn(results.state[i], new List<UInt32>());
                int currentNonStubsOn = numberOn(results.state[i], nonStubs);
                int currentStubsOn = numberOn(results.state[i], stubs);

                Console.WriteLine(i+"\t"+currentOn + "\t" + currentNonStubsOn + "\t" + currentStubsOn + "\t" + (currentOn - numOn) + "\t" + (currentNonStubsOn - numOnNonStubs) + "\t" + (currentStubsOn - numOnStubs));

                numOn = currentOn;
                numOnNonStubs = currentNonStubsOn;
                numOnStubs = currentStubsOn;
            }

            Console.WriteLine(numOn + "/" + results.g.NodeCount + " ASes turned on.");

        }

        public void asnInfo(string[] commandPieces)
        {
            if (commandPieces.Length < 2)
            {
                Console.WriteLine("usage: asninfo [ASN] [list? default is to not list]");
                return;
            }
            bool list = false;
            for (int i = 2; i < commandPieces.Length; i++)
            {
                if (commandPieces[i] == "list")
                    list = true;

            }
            UInt32 ASN;
            if (!UInt32.TryParse(commandPieces[1], out ASN))
            {
                Console.WriteLine("malformed ASN");
                return;
            }
            AsNode ASNode;
            if ((ASNode = results.g.GetNode(ASN)) == null)
            {
                Console.WriteLine("This node was not in the graph");
                return;
            }
            string stubstring = "stub";
            if (results.g.getNonStubs().Contains(ASN))
                stubstring = "nonstub";
            Console.WriteLine("ASN Info for "+stubstring+ " AS: " + ASN);
            Console.WriteLine("Customers: " + results.g.GetNode(ASN).GetNeighborsByType(RelationshipType.ProviderTo).Count());
            if (list)
            {
                string graphviz = "\n\n";
                foreach (var customer in results.g.GetNode(ASN).GetNeighborsByType(RelationshipType.ProviderTo))
                {
                    graphviz = graphviz+ customer.NodeNum + " -> " + ASN + ";\n";
                    Console.Write(customer.NodeNum + ", ");
                }
                Console.WriteLine(graphviz);
                Console.WriteLine();
            }
            Console.WriteLine("Peers: " + results.g.GetNode(ASN).GetNeighborsByType(RelationshipType.PeerOf).Count());
            if (list)
            {
                string graphviz = "\n\n";
                foreach (var peer in results.g.GetNode(ASN).GetNeighborsByType(RelationshipType.PeerOf))
                {
                    graphviz = graphviz + "edge[dir=\"none\"] " + ASN + " -> " + peer.NodeNum + ";\n";
                    Console.Write(peer.NodeNum + ", ");
                }
                Console.WriteLine(graphviz);
                Console.WriteLine();
            }
            Console.WriteLine("Providers: " + results.g.GetNode(ASN).GetNeighborsByType(RelationshipType.CustomerOf).Count());
            if (list)
            {
                string graphviz = "\n\n";
                foreach (var provider in results.g.GetNode(ASN).GetNeighborsByType(RelationshipType.CustomerOf))
                {
                    graphviz = graphviz +ASN + " -> " + provider.NodeNum + ";\n";
                    Console.Write(provider.NodeNum + ", ");
                }
                Console.WriteLine(graphviz);
                Console.WriteLine();
            }
            Console.WriteLine("Total degree: " + results.g.GetNode(ASN).GetAllNeighbors().Count());
        }

       

         /// <summary>
         /// given an ASN tells which of it's stub neighbors experienced path changed because of their flip.
         /// prints these paths. 
         /// </summary>
         /// <param name="commandPieces"></param>
        public void whosePathChanged(string[] commandPieces)
        {

            if (commandPieces.Length < 2)
            {
                Console.WriteLine("usage: whosePathChanges [ASN] [path from?ASN]");
                return;
            }
            if (commandPieces[1] == "help")
            {
                Console.WriteLine("tells you the paths through this ASN from the source to this ASN's stub neighbors (that were on before this ASN flipped) that changed when this ASN turned on. If you don't give a source ASN it will use the set of early adopters as sources.");
            }
            UInt32 ASN;
            if (!UInt32.TryParse(commandPieces[1], out ASN))
            {
                Console.WriteLine("malformed ASN");
                return;
            }
            AsNode ASNode;
            if ((ASNode = results.g.GetNode(ASN)) == null)
            {
                Console.WriteLine("This node was not in the graph");
                return;
            }
            List<UInt32> sourceNodes=results.earlyAdopters;
            if (commandPieces.Length > 2 && commandPieces[2]!= "neighbors" &&commandPieces[2]!="customers" 
                && commandPieces[2] != "tier1" && commandPieces[2] != "big5")
            {
                UInt32 ASN2;
                if (!UInt32.TryParse(commandPieces[2], out ASN2))
                {
                    Console.WriteLine("malformed source ASN");
                    return;
                }
                AsNode ASNode2;
                if ((ASNode2 = results.g.GetNode(ASN2)) == null)
                {
                    Console.WriteLine("The source AS was not in the graph");
                 return;
                }
                sourceNodes=new List<uint>();
                sourceNodes.Add(ASN2);
            }

            if (results.state[results.state.Count - 1][ASN] == false)
            {
                Console.WriteLine("ASN: " + ASN + " never flipped.");
             return;
            }
             //this node flipped,figure out which iteration.
                int flippedIter = -1;
                for (int i = 0; flippedIter < 0 && i < results.state.Count; i++)
                {
                    if (results.state[i][ASN])
                        flippedIter = i;
                }
            
            if(flippedIter ==0)
            {
                Console.WriteLine("ASN: "+ASN +" was on to begin with.");
                return;
            }
            List<UInt32> stubs = results.g.getStubs();
            var customers = ASNode.GetNeighborsByType(RelationshipType.ProviderTo);
            if (commandPieces.Length > 2)
            {
                if (commandPieces[2] == "neighbors")
                {
                    sourceNodes = new List<uint>();
                    foreach (var neighbor in ASNode.GetAllNeighbors())
                        sourceNodes.Add(neighbor.NodeNum);
                }
                if (commandPieces[2] == "customers")
                {
                    sourceNodes = new List<uint>();
                    foreach (var customer in customers)
                        sourceNodes.Add(customer.NodeNum);
                }
                if (commandPieces[2] == "big5")
                {
                    sourceNodes = new List<uint>();
                    sourceNodes.Add(15169);
                    sourceNodes.Add(8075);
                    sourceNodes.Add(32934);
                    sourceNodes.Add(20940);
                    sourceNodes.Add(22822);
                }
                if (commandPieces[2] == "tier1")
                {
                    sourceNodes = new List<uint>();
                    sourceNodes.Add(1239);
                    sourceNodes.Add(701);
                    sourceNodes.Add(7018);
                    sourceNodes.Add(174);
                    sourceNodes.Add(3356);
                }
            }

            Console.WriteLine("computing paths through " + ASN + " to its neighbors from : ");
            foreach (var sn in sourceNodes)
                Console.Write(sn + ", ");
            Console.WriteLine("\nthat change when "+ASN+" flipped. This may take some time. " + ASN + " has " + ASNode.GetAllNeighbors().Count() + " neighbors.");

            double progressMeter = 0.05;//variable to print out messages of progress.
            int doneNeighbors = 0;//same deal.
            int numPathsChanged = 0;
            int numPathsChangedOnSource = 0;
            List<UInt32> neighborsChangedFor = new List<uint>();
            List<UInt32> neighborsChangedForOnSource = new List<uint>();
            List<UInt32> sourcesChanged = new List<uint>();
            List<UInt32> sourcesChangedOnSource = new List<uint>();
            
            /** going to iterate over customers of this node that were on before this node flipped (or were stubs), ie. they were
             * a potential motivator for the flip. does this node actually gain their traffic? **/
            foreach (var neighbor in customers)
            {
                /** If this node was on before (ie. they would change their decision to the secure parent now)
                 * and they were a stub (just for ease of understanding for now)**/
                if (results.state[flippedIter - 1][neighbor.NodeNum] || stubs.Contains(neighbor.NodeNum))
                {
                    List<UInt32[]> pathsBefore = new List<uint[]>();
                    List<UInt32[]> pathsAfter = new List<uint[]>();
                    List<string> pathsBeforeStrings = new List<string>();
                    List<string> pathsAfterStrings = new List<string>();

                    Destination d = new Destination(SimulatorLibrary.initMiniDestination(results.g, neighbor.NodeNum, false));

                    //paths before the AS flipped (from early adopters to this stub.)
                    d.UpdatePaths(results.state[flippedIter - 1]);

                    foreach (var bigASN in sourceNodes)
                    {
                        if (bigASN != d.destination)
                        {

                            pathsBefore.Add(d.GetPathList(bigASN));//path from bigASN to the stub.
                            pathsBeforeStrings.Add(d.GetPath(bigASN, results.state[flippedIter - 1]));
                        }
                        else
                        {//dummy vals. this source is the destination.
                            pathsBefore.Add(new UInt32[0]);
                            pathsBeforeStrings.Add("");
                        }
                    }
                    //paths after AS flipped.
                    d.UpdatePaths(results.state[flippedIter]);
                    foreach (var bigASN in sourceNodes)
                    {
                        if (bigASN != d.destination)
                        {
                            pathsAfter.Add(d.GetPathList(bigASN));
                            pathsAfterStrings.Add(d.GetPath(bigASN, results.state[flippedIter]));
                        }
                        else
                        {
                            //dummy vals. this source is the destination.
                            pathsAfter.Add(new UInt32[0]) ;
                            pathsAfterStrings.Add("");
                        }
                    }
                   
                    for (int i = 0; i < sourceNodes.Count; i++)
                    {
                        var bigASN = sourceNodes[i];
                        if (bigASN != d.destination)
                        {
                            var pathBefore = pathsBefore[i];
                            var pathAfter = pathsAfter[i];
                            bool pathChanged = false;
                            for (int j = 0; j < pathBefore.Length; j++)
                            {
                                if (pathBefore[j] != pathAfter[j])
                                    pathChanged = true;
                            }

                            if (pathChanged)
                            {
                                if (pathsBeforeStrings[i].IndexOf(ASN.ToString()) >= 0 || pathsAfterStrings[i].IndexOf(ASN.ToString()) >= 0)
                                {
                                 
                                  
                                    numPathsChanged++;
                                    if (!neighborsChangedFor.Contains(neighbor.NodeNum))
                                        neighborsChangedFor.Add(neighbor.NodeNum);
                                    if (!sourcesChanged.Contains(bigASN))
                                        sourcesChanged.Add(bigASN);
                                    //the path after must have been fully secure save for the guy who flipped. and the path before cannot contain the guy who flipped.
                                    if (fullySecure(pathsAfter[i],ASNode.NodeNum,results.state[flippedIter-1],stubs) && !pathsBefore[i].Contains(ASNode.NodeNum))
                                    {
                                        Console.WriteLine("---");
                                        Console.WriteLine("Path from: " + bigASN + " to " + neighbor.NodeNum + " changed from: ");
                                        Console.WriteLine(pathsBeforeStrings[i]);
                                        Console.WriteLine("to: ");
                                        Console.WriteLine(pathsAfterStrings[i]);
                                        Console.WriteLine("---");


                                        numPathsChangedOnSource++;
                                        if (!neighborsChangedForOnSource.Contains(neighbor.NodeNum))
                                            neighborsChangedForOnSource.Add(neighbor.NodeNum);
                                        if (!sourcesChangedOnSource.Contains(bigASN))
                                            sourcesChangedOnSource.Add(bigASN);
                                    }
                                }
                            }
                        }
                    }
                    doneNeighbors++;
                    if (doneNeighbors > customers.Count() * (progressMeter))
                    {
                        Console.WriteLine(progressMeter * 100 + "% done.");
                        progressMeter += 0.05;
                        
                    }
                }
            }
            Console.WriteLine(numPathsChanged + " paths changed. to " + neighborsChangedFor.Count + " customers from " + sourcesChanged.Count + " sources");
            Console.WriteLine(numPathsChangedOnSource + " paths changed and had the source on before. these went to " + neighborsChangedForOnSource.Count + " customers from "+sourcesChangedOnSource.Count+ " sources");
        }

        public static bool fullySecure(UInt32[] path, UInt32 ASN,bool[] state,List<UInt32> stubs)
        {
            int nodesOff = 0;
            for (int i = 0; i < path.Length; i++)
            {
                if (!state[path[i]]  && !stubs.Contains(path[i]))
                   nodesOff++;
            }
            if (nodesOff > 1)
                return false;
            return true;
        }
        List<UInt32> flippedForBig5;
        public void whoFlippedForBig5(string[] commandPieces)
        {
            flippedForBig5 = new List<uint>();
            List<uint> big5 = new List<uint>();
            big5.Add(15169);
            big5.Add(22822);
            big5.Add(8075);
            big5.Add(32934);
            big5.Add(20940);
            List<UInt32> nonstubs = results.g.getNonStubs();
            Console.WriteLine("ASNs that flipped for Big 5 are: ");
            int totalFlippedForBig5 = 0;
            int totalTurnedOn = 0;
            double averageFlipIteration = 0;
            double averageIncrease = 0;
          
            int morethan10 = 0;
            int morethan15 = 0;
            int morethan20 = 0;
            int morethan30 = 0;
            int morethan10neighbors=0;
            int morethan15neighbors = 0;
            int morethan20neighbors = 0;
            int morethan30neighbors = 0;
            foreach (UInt32 ASN in nonstubs)
            {
                AsNode ASNode = results.g.GetNode(ASN);
                var neighbors = ASNode.GetAllNeighbors().ToList();
                var onNeighbors = new List<UInt32>();
                if (results.state[results.state.Count - 1][ASN] == true)
                { //this node flipped,figure out which iteration.
                    int flippedIter = -1;
                    for (int i = 0; flippedIter < 0 && i < results.state.Count; i++)
                    {
                        if (results.state[i][ASN])
                            flippedIter = i;
                    }

                    if (flippedIter > 0)
                    {
                        bool[] stateBeforeFlip = results.state[flippedIter - 1];
                        foreach (var neighbor in neighbors)
                        {
                            if (stateBeforeFlip[neighbor.NodeNum])
                                onNeighbors.Add(neighbor.NodeNum);
                        }

                    }
                    //could be only the big 5.
                    if (onNeighbors.Count <= 5 && !big5.Contains(ASN))
                    {
                        bool onForBig5 = true;
                      
                            foreach(var n in onNeighbors)
                            {
                                if(!big5.Contains(n))
                                    onForBig5=false;
                            }

                            if (onForBig5)
                            {
                                int turnedOnNeighbors = 0;
                                foreach (var neighbor in neighbors)
                                {
                                    if (!results.state[flippedIter][neighbor.NodeNum] &&
                                        results.state[flippedIter + 1][neighbor.NodeNum])
                                        turnedOnNeighbors++;
                                }
                                flippedForBig5.Add(ASN);
                                double fractionIncrease = (double)results.uafter[flippedIter - 1][ASN] / (double)results.ubefore[flippedIter - 1][ASN];
                                totalTurnedOn += turnedOnNeighbors;
                                Console.Write(ASN + "({0:#.##}), ", fractionIncrease);
                                totalFlippedForBig5++;
                                averageFlipIteration += flippedIter;
                                averageIncrease += fractionIncrease;
                                if (fractionIncrease > 1.1)
                                {
                                    morethan10++;
                                    morethan10neighbors += turnedOnNeighbors;
                                }
                                if (fractionIncrease > 1.15)
                                {
                                    morethan15++;
                                    morethan15neighbors += turnedOnNeighbors;
                                }
                                if (fractionIncrease > 1.2)
                                {
                                    morethan20neighbors += turnedOnNeighbors;
                                    morethan20++;
                                }
                                if (fractionIncrease > 1.3)
                                {
                                    morethan30++;
                                    morethan30neighbors += turnedOnNeighbors;
                                }
                            }
                    }

                }
            }
            Console.WriteLine();
            Console.WriteLine("Total of " + totalFlippedForBig5 + " ASNs flipped for the big 5. They turned on " + totalTurnedOn + " of their neighbors");
            averageFlipIteration /= (double)totalFlippedForBig5;
            Console.WriteLine("They flipped in iteration: " + averageFlipIteration + " on average");
            averageIncrease /= (double)totalFlippedForBig5;
            Console.WriteLine(morethan10 + " had increase of more than 10% they flipped " + morethan10neighbors);
            Console.WriteLine(morethan15 + " had increase of more than 15% they flipped " + morethan15neighbors);
            Console.WriteLine(morethan20 + " had increase of more than 20% they flipped " + morethan20neighbors);
            Console.WriteLine(morethan30 + " had increase of more than 30% they flipped " + morethan30neighbors);
   
        }

        public void getNeighborType(string[] commandPieces)
        {
            if (commandPieces.Length < 3)
            {
                Console.WriteLine("usage: getneighbortype [ASN1] [ASN2]");
                return;

            }

            UInt32 ASN;
            if (!UInt32.TryParse(commandPieces[1], out ASN))
            {
                Console.WriteLine("malformed  ASN");
                return;
            }
            AsNode ASNode;
            if ((ASNode = results.g.GetNode(ASN)) == null)
            {
                Console.WriteLine("The  AS was not in the graph");
                return;
            }

            UInt32 ASN2;
            if (!UInt32.TryParse(commandPieces[2], out ASN2))
            {
                Console.WriteLine("malformed  ASN");
                return;
            }
            AsNode ASNode2;
            if ((ASNode2 = results.g.GetNode(ASN2)) == null)
            {
                Console.WriteLine("The  AS was not in the graph");
                return;
            }

            
            Console.WriteLine("type of relationship " + ASN + " has with " +ASN2 +" is "+ ASNode.GetRelationshipTypeOfNeighbor(ASNode2));

        }

         /// <summary>
         /// given an iteration and optional "nonstubs" parameter
         /// tell us who flipped in an iteration.
         /// </summary>
         /// <param name="commandPieces"></param>
        public void whoFlipped(string[] commandPieces)
        {
            if (commandPieces.Length < 2)
            {
                Console.WriteLine("usage whoflipped [iteration] [nonstubs?]");
                return;
            }
            bool onlyNonStubs=false;
            for (int i = 2; i < commandPieces.Length; i++)
            {
                if (commandPieces[i].IndexOf("nonstub") >= 0)
                    onlyNonStubs = true;
            }
            int iteration;
            if(!int.TryParse(commandPieces[1],out iteration) || iteration>(results.state.Count-1))
            {
                Console.WriteLine("malformed iteration or iteration out of bounds.");
                    return;
            }
            var stubs = results.g.getStubs();
            Console.WriteLine("ASN (degree):\tUBefore:\tUAfter:\tIncrease:");
            for (int i = 0; i < results.state[iteration].Length; i++)
            {
                if (iteration == 0 || results.state[iteration - 1][i] != results.state[iteration][i])
                {
                    long ubefore = 0;
                    long uafter = 0;
                    double fractionincrease = 0;
                    if(iteration>0)
                    {
                     ubefore = results.ubefore[iteration-1][i];
                     uafter = results.uafter[iteration-1][i];
                     if (ubefore != 0)
                         fractionincrease = (double)uafter / (double)ubefore;
                     else
                         fractionincrease = (double)uafter;
                    }
                    int degree = results.g.GetNode((UInt32)i).GetAllNeighbors().Count();
                    string stubString = "";
                    if (stubs.Contains((uint)i))
                        stubString = "\t(stub)";
                    if(!onlyNonStubs || (onlyNonStubs && stubString.Length==0))
                        Console.WriteLine(i + " (" + degree + ")\t{0:#.####e+00}\t{1:#.####e+00}\t{2:0.0000}" + stubString, ubefore, uafter, fractionincrease);
                }
            }

        }

         /// <summary>
         /// given an ASN summarize their flip. which neighbors were on when they decided to flip.
         /// which of their neighbors flipped after they flipped.
         /// </summary>
         /// <param name="commandPieces"></param>
        public void flipSummary(string[] commandPieces)
        {
            if (commandPieces.Length < 2)
            {
                Console.WriteLine("usage: flipsummary [ASN] [nolist?/nostubs?] (add string 'nolist' to just print counts vs. full lists or 'nostubs' to print lists that omit stubs)");
                return;
            }

            UInt32 ASN;
            if (!UInt32.TryParse(commandPieces[1], out ASN))
            {
                Console.WriteLine("malformed ASN");
                return;
            }
            AsNode ASNode;
            if ((ASNode = results.g.GetNode(ASN)) == null)
            {
                Console.WriteLine("This node was not in the graph");
                return;
            }
            bool printLists = true;
            bool nostubs = false;
            List<UInt32> nonStubs = new List<uint>();
            for (int i = 2; i < commandPieces.Length; i++)
            {
                if (commandPieces[i] == "nolist")
                    printLists = false;
                if (commandPieces[i] == "nostubs")
                {
                    nostubs = true;
                    nonStubs = results.g.getNonStubs();
                }
            }


            var peers = ASNode.GetNeighborsByType(RelationshipType.PeerOf).ToList();
            var customers = ASNode.GetNeighborsByType(RelationshipType.ProviderTo).ToList();
            var providers = ASNode.GetNeighborsByType(RelationshipType.CustomerOf).ToList();
              List<UInt32> stubs = results.g.getStubs();
            if (results.state[results.state.Count - 1][ASN] == false)
            {
                Console.WriteLine("ASN: " + ASN + " never flipped.");
                Console.WriteLine("It had the following neighbors on at the end of the simulation: ");
                neighboursOn(results.state[results.state.Count - 1], printLists, nonStubs, ASN, customers, peers, providers,stubs);
            }
            else
            { //this node flipped,figure out which iteration.
                int flippedIter = -1;
                for (int i = 0; flippedIter < 0 && i < results.state.Count; i++)
                {
                    if (results.state[i][ASN])
                        flippedIter = i;
                }

              

                //print out who was on when this ASN decided to flip (ie iteration before they actually flipped)
                Console.WriteLine("ASN: " + ASN + " flipped in iteration: " + flippedIter);
                Console.WriteLine("It has a total of: " + customers.Count + " customers, " + peers.Count + " peers, " + providers.Count + " providers.");
                Console.WriteLine("*********************************************************");
                Console.WriteLine("The following ASNs were on when " + ASN + " decided to flip.");
                if (flippedIter > 0)
                    neighboursOn(results.state[flippedIter - 1], printLists, nonStubs, ASN, customers, peers, providers,stubs);
                else
                    neighboursOn(results.state[flippedIter], printLists, nonStubs, ASN, customers, peers, providers,stubs);


            
                //print out who was on after this ASN decided to flip.

                //Need to remove anyone who was already on
                //from the set of neighbors first...
                List<AsNode> customersToRemove = new List<AsNode>();
                List<AsNode> peersToRemove = new List<AsNode>();
                List<AsNode> providersToRemove = new List<AsNode>();
                for (int i = Math.Max(0, flippedIter - 1); i < flippedIter + 1; i++)
                {
                    foreach (var customer in customers)
                    {
                        if (results.state[i][customer.NodeNum])
                            customersToRemove.Add(customer);
                    }
                    foreach (var peer in peers)
                    {
                        if (results.state[i][peer.NodeNum])
                            peersToRemove.Add(peer);
                    }
                    foreach (var provider in providers)
                    {
                        if (results.state[i][provider.NodeNum])
                            providersToRemove.Add(provider);
                    }
                }
                foreach (var node in customersToRemove)
                    customers.Remove(node);
                foreach (var node in peersToRemove)
                    peers.Remove(node);
                foreach (var node in providersToRemove)
                    providers.Remove(node);
                Console.WriteLine("*********************************************************");
                Console.WriteLine("The following ASNs turned on after " + ASN + " flipped.");
                //now have set of neighbours that were not on before
                // and did not flip at same time as node we care about.
                neighboursOn(results.state[Math.Min(results.state.Count - 1, flippedIter+1)], printLists, nonStubs, ASN, customers, peers, providers,stubs);
            }




        }

        public void setResultDir(string[] commandPieces)
        {
            if (commandPieces.Length < 2)
            {
                Console.WriteLine("usage: setresultdir [path]");
                return;
            }
            baseDirectory = commandPieces[1];
            if (commandPieces[1] == "default")
            {
                baseDirectory = defaultResultsDirectory;
                graphDirectory = defaultGraphDirectory;
                Console.WriteLine("since you selected default, I'm also setting the graph directory to its default value.");
                Console.WriteLine("current graph directory is: " + graphDirectory);
            }
            if (baseDirectory[baseDirectory.Length - 1] != '\\')
                baseDirectory += '\\';
            Console.WriteLine("current working directory is: " + baseDirectory);
            
        }

        public void setGraphDir(string[] commandPieces)
        {
            if (commandPieces.Length < 2)
            {
                Console.WriteLine("usage: setgraphdir [path]");
                return;
            }
            graphDirectory = commandPieces[1];
            if (commandPieces[1] == "default")
                graphDirectory = defaultGraphDirectory;
            if (graphDirectory[graphDirectory.Length - 1] != '\\')
                graphDirectory += '\\';
            Console.WriteLine("current graph directory is: " + graphDirectory);
        }

         /// <summary>
         /// prints 1 line per iteration of how many stubs/nonstubs are on/flipped.
         /// </summary>
         /// <param name="commandPieces"></param>
         public void summarizeSimulationForNode(string[] commandPieces)
         {
             if (commandPieces.Length < 2)
             {
                 Console.WriteLine("No ASN given, going to summarize the simulation.");
                 summarizeSimulation();
                 return;
             }
             UInt32 ASN;
             if (!UInt32.TryParse(commandPieces[1], out ASN))
             {
                 Console.WriteLine("malformed ASN");
                 return;
             }
             if (results.g.GetNode(ASN) == null)
             {
                 Console.WriteLine("This node was not in the graph");
                 return;
             }
             if (results.g.getNonStubs().Contains(ASN))
                 Console.WriteLine("Simulation summary for ASN: " + ASN + " it is not a stub.");
             else
                 Console.WriteLine("Simulation summary for ASN: " + ASN + " it is a stub.");
             Console.WriteLine("State is the state it chose given the UBefore and UAfter columns");
             Console.WriteLine("iter\tS\tUBefore\tUAfter\tFraction increase");
             for (int i = 0; i < results.state.Count; i++)
             {

                 if (i > 0)
                 {
                     double fractionIncrease = results.uafter[i - 1][ASN];
                     if (results.ubefore[i - 1][ASN] > 0)
                         fractionIncrease = (double)results.uafter[i - 1][ASN] / (double)results.ubefore[i - 1][ASN];
                   //  Console.WriteLine(i + "\t" + results.state[i][ASN] + "\t" + results.ubefore[i - 1][ASN] + "\t" + results.uafter[i - 1][ASN]+"\t"+fractionIncrease);
                     Console.WriteLine(i + "\t" + results.state[i][ASN] + "\t{0:#.####e+00}\t{1:#.####e+00}\t{2:0.0000}", results.ubefore[i - 1][ASN], results.uafter[i - 1][ASN], fractionIncrease);
                    
                 
                 }
                 else
                     Console.WriteLine(i + "\t" + results.state[i][ASN] + "\t" + "XX" + "\t" + "XX");
             }

             long UBeforeAtStart = results.ubefore[0][ASN];
             int flippedIteration = getFlippedIteration(ASN);
             if (flippedIteration > 0)
             {
                
                 long UBeforeWhenFlipped = results.ubefore[flippedIteration][ASN];
                 long UBeforeBeforeFlipped = results.ubefore[flippedIteration - 1][ASN];

                 Console.WriteLine("Utility when flipped / Utility at start = {0:0.0000}", (double)UBeforeWhenFlipped / (double)UBeforeAtStart);
                 Console.WriteLine("Utility right before flip / Utility at start = {0:0.0000} " , (double)UBeforeBeforeFlipped / (double)UBeforeAtStart);

             }
             else
             {
                 Console.WriteLine(ASN + " never flipped.");
             }

             long UBeforeAtEnd = results.ubefore[results.ubefore.Count - 1][ASN];
             Console.WriteLine("Utility at end / Utility at start = {0:0.0000}" , (double)UBeforeAtEnd / (double)UBeforeAtStart);

                 
         }
        
         public resultObject loadSimulation(string[] commandPieces)
        {
            if (commandPieces.Length < 2)
            {
                Console.WriteLine("usage: loadsimulation [directory/chooser]");
                return new resultObject();
            }
            string directory = baseDirectory + commandPieces[1];
            if (commandPieces[1] == "chooser")
                directory=simulationDirectoryChooser(baseDirectory);
            if (!Directory.Exists(directory))
            {
                Console.WriteLine("error directory: " + directory + " does not exist");
                return new resultObject();
            }

            if (directory[directory.Length - 1] != '\\')
                directory = directory + "\\";

            if (!File.Exists(directory + "params.txt"))
            {
                Console.WriteLine("could not load params file for directory: " + directory);
                Console.WriteLine("params file did not exist");
                return new resultObject();
            }

            resultObject toreturn = readParamsFile(directory + "params.txt");
            toreturn.directory = directory;

            StreamReader input;
            if (!File.Exists(directory + toreturn.precursor + ".S200000.csv"))
            {
                Console.WriteLine("This directory has not been post-processed yet. I'm going to post process state before loading it, but before you use this data in R you will need to run the PostProcessing program.");
                List<bool[]> nonProcessedState = readStateFromFile(directory + toreturn.precursor + ".S000000.csv");
                toreturn.state = postProcessState(nonProcessedState, toreturn);
            }
            else
            {
                toreturn.state = readStateFromFile(directory + toreturn.precursor + ".S200000.csv"); 
            }

            toreturn.ubefore = readUTable(toreturn, "UBefore.csv");
            toreturn.uafter = readUTable(toreturn, "UAfter0.csv");

            toreturn.g = new NetworkGraph();
            if (File.Exists(graphDirectory + toreturn.graphFile))
            {
                InputFileReader ifr = new InputFileReader(graphDirectory + toreturn.graphFile, toreturn.g);
                ifr.ProcessFile();
                Console.WriteLine("loaded the graph: " + toreturn.graphFile + " it has " + toreturn.g.NodeCount + " nodes and " + toreturn.g.EdgeCount + " edges");

            }
            else
                Console.WriteLine("I was unable to load the graph " + toreturn.graphFile + " from directory " + graphDirectory + " please put the graph file in the directory or set the current graph directory with 'setgraphdir'");
              
            Console.WriteLine("********************************");
               Console.WriteLine("Loaded simulation with parameters: ");
                toreturn.printParams();
                Console.WriteLine("********************************");
            return toreturn;

        }
       
        public void printParams()
        {
            if (results.directory != null)
            {
                results.printParams();
            }
        }

        public void help()
        {
            Console.WriteLine("loadsimulation [file/path] - loads a simulation results directory");
            Console.WriteLine("setresultdir [path] - sets the default path so you don't have to always type it");
            Console.WriteLine("setgraphdir [path] - sets the default path so you don't have to always type it");
            Console.WriteLine("printparams - prints parameters of the results object you're viewing");
            Console.WriteLine("whoflipped [iteration] [nonstubs?] - who flipped in this iteration");
            Console.WriteLine("summarize - summarize how many flip in each iteration");
            Console.WriteLine("summarize [ASN] - summarize what happened for this ASN throughout the simulation");
            Console.WriteLine("asninfo [ASN] [list?] - number of neighbors of different types of this node, if you type 'list' it will list them all for you");
            Console.WriteLine("flipsummary [ASN] [nolist?nonstubs?] give a summary of how many neighbors were on when this AS decided to flip, and how many flipped in the iteration following its decision to flip");
            Console.WriteLine("whosepathchanged [ASN] [Source ASN] - type whosepathchanged help for more info about this one ...");
            Console.WriteLine("getneighbortype [ASN1] [ASN2] - tells you how these 2 ASNs are related.");
            Console.WriteLine("asninfo [ASN] [list?] - tells you how many peers, providers, customers this ASN has. if you say list it prints them for you.");
            Console.WriteLine("getneighbors [ASN] [peer/provider/customer] - tells you the neighbors of this ASN, default is peer if you forget to put the neighbor type");
            Console.WriteLine("getpath [ASN1] [ASN2] [iteration?]  - show the path between these 2 ASNs in a given iteration");
            Console.WriteLine("whoflippedforbig5 - summarizes who flipped for big 5. automatically stores this list of ASes for future comparisons and checks.");
            Console.WriteLine("big5flipperson - tells which of the big5flippers (from the last call to whoflippedforbig5) are on in the current simulation. Note, you can load a different sim between this call and whoflippedforbig5");
            Console.WriteLine("geototals - tells how many ASNs from each RIR were on at the end of this simulation. May not work if you don't have the TeamCymru data in adoptTraffic/Data/TeamCymru");
            Console.WriteLine("geosummarize - tells how many ASNs from each RIR flip in each iteration.");
            Console.WriteLine("countdiamonds - summarizes the number of diamonds formed by each early adopter + their 2 hop-away stubs.");
            Console.WriteLine("printdiamonds [ASN] [customer/peer/provider] - get some sample diamonds for this ASN. has optional parameters. call it with no args for more description.");
            Console.WriteLine("firstiterationdiamonds [ASN] - tell us diamonds for this ASN that are candidates for first iteration diamonds (one competitor flips in round 1)");
            Console.WriteLine("edgesummarize - tells us how many of each edge of different types light up in each iteration.");
            Console.WriteLine("findislands [iteration] - prints all secure islands with size less than 50 in a given iteration.");
            Console.WriteLine("neveron - tell us things about nodes that never turn on");
            Console.WriteLine("computeutility - tell us the utility of a certain node in a certain iteration for a specific dest. run command with no args for description.");
            Console.WriteLine("averagedegree [ASN] - tell us the average degree of this ASNs neighbors (all neighbors + stub specific)");
            Console.WriteLine("nonstubsnoutility -list ASNs that turned on who had utility=0 at start and end of sim.");
            Console.WriteLine("findl2diamonds - find layer 2 diamonds. run with no args to see parameters.");

        }

         /************ Utility functions *************/


        private int getFlippedIteration(UInt32 ASN)
        {
            
            for (int i = 0; i < results.state.Count; i++)
            {
                if (results.state[i][ASN])
                    return  i;
            }

            return -1;
        }
        private int numberOn(bool[] state,List<UInt32> limitingSet)
        {
            int numberOn = 0;
            if (limitingSet.Count == 0)
            {
                for (int i = 0; i < state.Length; i++)
                {
                    if (state[i])
                        numberOn++;
                }
            }else
            {
                foreach (UInt32 asn in limitingSet)
                {
                    if (state[asn])
                        numberOn++;
                }
            }
                
            return numberOn;
        }
        private void neighboursOn(bool[] flippedIteration, bool toprint, List<UInt32> nonstubs, uint ASN,
            List<AsNode> customers, List<AsNode> peers, List<AsNode> providers,List<UInt32> stubs)
        {
            string customersOn = "";
            string peersOn = "";
            string providersOn = "";
            int numCustomersOn = 0;
            int numPeersOn = 0;
            int numProvidersOn = 0;
            string stubstring;
            foreach (var customer in customers)
            {
                 stubstring = "";
                if (stubs.Contains(customer.NodeNum))
                    stubstring = "(stub)";

                if (nonstubs.Count == 0 || nonstubs.Contains(customer.NodeNum))
                {
                    if (flippedIteration[customer.NodeNum])
                    {
                        numCustomersOn++;

                        customersOn += customer.NodeNum + stubstring + ",";
                    }
                }
            }
            foreach (var peer in peers)
            {
                stubstring = "";
                if (stubs.Contains(peer.NodeNum))
                    stubstring = "(stub)";
                if (nonstubs.Count == 0 || nonstubs.Contains(peer.NodeNum))
                {
                    if (flippedIteration[peer.NodeNum])
                    {
                        numPeersOn++;
                        peersOn += peer.NodeNum + stubstring + ",";
                    }
                }
            }
            foreach (var provider in providers)
            {
                stubstring = "";
                if (stubs.Contains(provider.NodeNum))
                    stubstring = "(stub)";

                if (nonstubs.Count == 0 || nonstubs.Contains(provider.NodeNum))
                {
                    if (flippedIteration[provider.NodeNum])
                    {
                        numProvidersOn++;
                        providersOn += provider.NodeNum + stubstring+",";
                    }
                }
            }
            if (toprint)
            {
                Console.WriteLine("Customers that were on: " + customersOn);
                Console.WriteLine();
                Console.WriteLine("Peers that were on: " + peersOn);
                Console.WriteLine();
                Console.WriteLine("Providers that were on: " + providersOn);
                Console.WriteLine();
            }
            Console.WriteLine("Summary of neighbors that were on: ");
            Console.WriteLine("Customers: " + numCustomersOn + " Peers: " + numPeersOn + " Providers: " + numProvidersOn);
            Console.WriteLine("========================================");

        }

        private string simulationDirectoryChooser(string resultDirectory)
        {
            string[] simulationDirectories = Directory.GetDirectories(resultDirectory);
            for (int i = 0; i < simulationDirectories.Length; i++)
                Console.WriteLine(i + " " + simulationDirectories[i].Replace(resultDirectory,""));
            Console.WriteLine("Please enter the number of the file you would like to load: ");
          
          int selection = -1;
          while (!int.TryParse(Console.ReadLine(), out selection) || selection > simulationDirectories.Length-1) ;

          return simulationDirectories[selection];
        }

        public static resultObject readParamsFile(string paramsFile)
        {
            StreamReader input = new StreamReader(paramsFile);
            resultObject toreturn = new resultObject();

            while (!input.EndOfStream)
            {
                string line = input.ReadLine();
                string[] pieces = line.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                if (line.IndexOf("setutility") >= 0)
                {
                    //line pertaining to setting the utility computation
                    if (pieces.Length > 1)
                        toreturn.utility = pieces[1];
                }
                else if (line.IndexOf("Early adopters:") >= 0)
                {

                    toreturn.earlyAdopters = new List<uint>();
                    for (int i = 2; i < pieces.Length; i++)
                    {
                        UInt32 curr;
                        if (uint.TryParse(pieces[i].Replace(",",""), out curr))
                            toreturn.earlyAdopters.Add(curr);

                    }


                }
                else if (line.IndexOf("Filename") >= 0)
                {
                    toreturn.graphFile = pieces[1].ToLower();
                    if (toreturn.graphFile == "cyclops_2009_ixp.txt" ||
                        toreturn.graphFile == "cyclops_20101209_ixp.txt" || 
                        toreturn.graphFile=="cyclopsixp20101209-big5-nochildren.txt")
                    {
                        toreturn.f = "0";
                    }
                    else
                    {
                        //filename looks like: cyclopsixp2009-big5-0.1.txt
                        if (toreturn.graphFile.IndexOf("ixpsmart") < 0)
                        {
                            pieces = toreturn.graphFile.Split("-".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                            toreturn.f = pieces[2];
                        }
                        else
                            toreturn.f = "0.8";

                    }


                }
                else if (line.IndexOf("OutputDir") >= 0)
                {
                    //contains the directory tag. use this to derive
                    //the precursor for the standard filenames
                    toreturn.precursor = pieces[1].Substring(0, 20);

                }
                else if (line.IndexOf("percent") >= 0)
                {
                    //line setting the utility fraction
                    toreturn.u = pieces[1];
                }
                else if (line.IndexOf("K") >= 0)
                {
                    //line setting the K value
                    toreturn.k = pieces[1];
                }
                else if (line.IndexOf("Weighted Nodes:") >= 0)
                {
                    toreturn.weightedNodes = new List<uint>();
                    for (int i = 2; i < pieces.Length; i++)
                    {
                        UInt32 curr;
                        if (uint.TryParse(pieces[i].Replace(",",""), out curr))
                            toreturn.weightedNodes.Add(curr);

                    }

                }


            }

            input.Close();
            return toreturn;
        }

        private List<long[]> readUTable(resultObject Params, string ufile)
        {
            if (!File.Exists(Params.directory + Params.precursor + "." + ufile))
            {
                Console.WriteLine("error could not find utilities file: " + Params.directory + "." + Params.precursor + "." + ufile);
                return null;
            }
            List<long[]> UTable = new List<long[]>();
            StreamReader input = new StreamReader(Params.directory  + Params.precursor + "." + ufile);
            while (!input.EndOfStream)
                UTable.Add(lineToUtilities(input.ReadLine()));
            input.Close();
            return UTable;
        }

        private List<bool[]> postProcessState(List<bool[]> unprocessedState, resultObject Params)
        {
            List<UInt32> big5 = new List<uint>();
            big5.Add(22822);
            big5.Add(8075);
            big5.Add(15169);
            big5.Add(20940);
            big5.Add(32934);

            List<bool[]> processedState = new List<bool[]>();
            string graphFile = graphDirectory + Params.graphFile;
            if (!File.Exists(graphFile))
                Console.WriteLine("I could not find the graph file: " + graphFile);

           
            NetworkGraph g = new NetworkGraph();
            InputFileReader ifr = new InputFileReader(graphFile, g);
            ifr.ProcessFile();

            List<UInt32> stubs = g.getStubs();

            /*** Process and add the initial state of the simulation ***/
            bool[] initialState = SimulatorLibrary.initGlobalState(g, Params.earlyAdopters).S;

            //walk over stubs and re-evaluate their state.
            foreach (UInt32 AS in stubs)
            {
                if (!big5.Contains(AS))
                {
                    AsNode node = g.GetNode(AS);
                    initialState[AS] = false;//default to false for this stub.
                    var parents = node.GetNeighborsByType(RelationshipType.CustomerOf).ToList<AsNode>();
                    // parents.AddRange(node.GetNeighborsByType(RelationshipType.PeerOf)); //don't turn on for peers
                    foreach (AsNode parent in parents)
                    {
                        if (initialState[parent.NodeNum])
                        {//parent is true let it be true in the augmented state.
                            initialState[AS] = true;
                            break;
                        }

                    }
                }


            }
            foreach (var AS in Params.earlyAdopters)
                initialState[AS] = true;//make sure early adopters are "on"

            processedState.Add(initialState);

            for (int i = 0; i < unprocessedState.Count; i++)
            {

                bool[] currS = unprocessedState[i];
                //walk over stubs and re-evaluate their state.
                foreach (UInt32 AS in stubs)
                {
                    if (!big5.Contains(AS))
                    {
                        AsNode node = g.GetNode(AS);
                        currS[AS] = false;//default to false for this stub.
                        var parents = node.GetNeighborsByType(RelationshipType.CustomerOf).ToList<AsNode>();
                        // parents.AddRange(node.GetNeighborsByType(RelationshipType.PeerOf)); //don't turn on for peers
                        foreach (AsNode parent in parents)
                        {
                            if (currS[parent.NodeNum])
                            {//parent is true let it be true in the augmented state.
                                currS[AS] = true;
                                break;
                            }

                        }
                    }
                }

                foreach (var AS in Params.earlyAdopters)
                    currS[AS] = true;//make sure early adopters are "on"

                processedState.Add(currS);
            }

            return processedState;
        }

        public static List<bool[]> readStateFromFile(string filename)
        {
            List<bool[]> states = new List<bool[]>();
            StreamReader input = new StreamReader(filename);
            while (!input.EndOfStream)
                states.Add(lineToState(input.ReadLine()));
            input.Close();
            return states;
        }

        private static long[] lineToUtilities(string line)
        {
            char[] comma = { ',' };

            string[] pieces = line.Split(comma, StringSplitOptions.RemoveEmptyEntries);//state vector for iteration t
            long[] Utilities = new long[pieces.Length];
            for (int i = 0; i < pieces.Length; i++)
            {
                if (!long.TryParse(pieces[i], out Utilities[i]))
                    Console.WriteLine("could not parse utility: " + pieces[i]);
            }

            return Utilities;
        }

        private static bool[] lineToState(string line)
        {
            char[] comma = { ',' };

            string[] pieces = line.Split(comma, StringSplitOptions.RemoveEmptyEntries);//state vector for iteration t
            bool[] State = new bool[pieces.Length];
            for (int i = 0; i < pieces.Length; i++)
            {
                if (pieces[i] == "1")
                    State[i] = true;
                else
                    State[i] = false;
            }

            return State;
        }

        public  resultObject loadSimulationNonInteractive(String directory)
        {

            if (!Directory.Exists(directory))
            {
                Console.WriteLine("error directory: " + directory + " does not exist");
                return new resultObject();
            }

            if (directory[directory.Length - 1] != '\\')
                directory = directory + "\\";

            if (!File.Exists(directory + "params.txt"))
            {
                Console.WriteLine("could not load params file for directory: " + directory);
                Console.WriteLine("params file did not exist");
                return new resultObject();
            }

            resultObject toreturn = readParamsFile(directory + "params.txt");
            toreturn.directory = directory;

            StreamReader input;
            if (!File.Exists(directory + toreturn.precursor + ".S200000.csv"))
            {
                Console.WriteLine("This directory has not been post-processed yet. I'm going to post process state before loading it, but before you use this data in R you will need to run the PostProcessing program.");
                List<bool[]> nonProcessedState = readStateFromFile(directory + toreturn.precursor + ".S000000.csv");
                toreturn.state = postProcessState(nonProcessedState, toreturn);
            }
            else
            {
                toreturn.state = readStateFromFile(directory + toreturn.precursor + ".S200000.csv");
            }

            toreturn.ubefore = readUTable(toreturn, "UBefore.csv");
            toreturn.uafter = readUTable(toreturn, "UAfter0.csv");

            toreturn.g = new NetworkGraph();
            if (File.Exists(graphDirectory + toreturn.graphFile))
            {
                InputFileReader ifr = new InputFileReader(graphDirectory + toreturn.graphFile, toreturn.g);
                ifr.ProcessFile();
                Console.WriteLine("loaded the graph: " + toreturn.graphFile + " it has " + toreturn.g.NodeCount + " nodes and " + toreturn.g.EdgeCount + " edges");

            }
            else
                Console.WriteLine("I was unable to load the graph " + toreturn.graphFile + " from directory " + graphDirectory + " please put the graph file in the directory or set the current graph directory with 'setgraphdir'");

            Console.WriteLine("********************************");
            Console.WriteLine("Loaded simulation with parameters: ");
            toreturn.printParams();
            Console.WriteLine("********************************");
            return toreturn;

        }

    }
}
