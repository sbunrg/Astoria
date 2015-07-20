using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.IO;
using SecureSimulator;
using CloudLibrary;
using System.Diagnostics;

namespace TestingApplication
{
   public static class diamondFinder
    {

        const int _customerDiamonds = 1;
        const int _peerDiamonds = 2;
        const int _providerDiamonds = 3;


        static string bucketTableFile = "C:\\Users\\phillipa\\Desktop\\adoptTraffic\\Code\\AzureSecureSimulator\\MakeAllBucketTables\\bin\\Debug\\destinations2010\\";

        public static void FindL2Diamonds(string[] commandPieces,resultObject Result)
        {
            if (commandPieces.Length < 2)
            {
                Console.WriteLine("usage: findl2diamonds [pointASN] [diamondsfile] [outputfile?]");
                return;
            }
            UInt32 pointASN;
            if (!UInt32.TryParse(commandPieces[1], out pointASN))
            {
                Console.WriteLine("invalid ASN");
                return;
            }

            StreamWriter output;
            if (commandPieces.Length > 3)
            {
                output = new StreamWriter(ResultsExplorer.defaultOutputDirectory + commandPieces[2]);
                Console.WriteLine("outputting to " + ResultsExplorer.defaultOutputDirectory + commandPieces[2]);
            }
            else
                output = new StreamWriter(Console.OpenStandardOutput());

            Hashtable diamonds = readDiamonds(ResultsExplorer.defaultOutputDirectory + commandPieces[2]);

            foreach (var stub in diamonds.Keys)
            {

            }

            output.Close();
        }

       /// <summary>
       /// tells us if this ASN is connected to one of the big 5 or content providers.
       /// (excluding the point ASN)
       /// 
       /// returns the list of such neighbors.
       /// </summary>
       /// <param name="ASN"></param>
       /// <param name="g"></param>
       /// <returns></returns>
        private static List<UInt32> hasCPT1Neighbor(AsNode ASN,UInt32 pointASN)
        {
            UInt32[] CPT1 = { 15169, 8075, 22822, 20940, 32934, 1239, 7018, 701, 174, 3356 };
            List<UInt32> toreturn = new List<uint>();
            foreach (var neighbor in ASN.GetAllNeighbors())
            {
                if (neighbor.NodeNum != pointASN && CPT1.Contains(neighbor.NodeNum))
                    toreturn.Add(neighbor.NodeNum);
            }
            return toreturn ;
        }

       /// <summary>
       /// return to us the list of stubs connected to ASN's neighbors who were
       /// off in state S
       /// </summary>
       /// <param name="ASN"></param>
       /// <param name="g"></param>
       /// <param name="S"></param>
       private static void getOffGrandChildrenStubs(AsNode ASN,List<UInt32> stubs,bool[] S)
       {

       }

        private static Hashtable readDiamonds(string filename)
        {
            Hashtable diamonds = new Hashtable();

            StreamReader input = new StreamReader(filename);
            List<UInt32> currentCompetitors = new List<uint>();
            UInt32 currentStub = 0;
            while (!input.EndOfStream)
            {
                string line = input.ReadLine();
                if (line[0] != '-')
                {
                    string[] pieces = line.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    currentStub = UInt32.Parse(pieces[4]);
                    currentCompetitors.Add(UInt32.Parse(pieces[2]));
                }
                else
                {
                    //end of this diamond.
                    if (currentStub != 0)
                        diamonds.Add(currentStub, currentCompetitors);
                    currentStub = 0;
                    currentCompetitors = new List<uint>();
                }
            }

            input.Close();

            return diamonds;
        }

        public static void findFirstIterationDiamonds(string[] commandPieces, resultObject Result)
        {
            if (commandPieces.Length < 2)
            {
                Console.WriteLine("usage: firstiterationdiamonds [ASN]");
                return;
            }
            UInt32 ASN;
            if (!UInt32.TryParse(commandPieces[1], out ASN))
            {
                Console.WriteLine("invalid ASN. "); return;
            }
            if (Result.g.GetNode(ASN) == null)
            {
                Console.WriteLine("ASN not in graph. "); return;
            } 
           

                    
            Hashtable customerDiamonds = new Hashtable();
            Hashtable providerDiamonds = new Hashtable();
            Hashtable peerDiamonds = new Hashtable();
            getDiamonds(Result, ASN, _customerDiamonds, ref customerDiamonds);
            getDiamonds(Result, ASN, _providerDiamonds, ref providerDiamonds);
            getDiamonds(Result, ASN, _peerDiamonds, ref peerDiamonds);


            StreamWriter output;
            if (commandPieces.Length > 2)
            {
                output = new StreamWriter(ResultsExplorer.defaultOutputDirectory + commandPieces[2]);
                Console.WriteLine("outputting to " + ResultsExplorer.defaultOutputDirectory + commandPieces[2]);
            }
            else
                output = new StreamWriter(Console.OpenStandardOutput());

            output.WriteLine("going through provider diamonds --------");
            foreach (var stub in providerDiamonds.Keys)
            {
                if (isFirstIterationDiamond(ASN, (UInt32)stub, (List<UInt32>)providerDiamonds[stub], Result))
                {
                    output.WriteLine("---------------");
                    printDiamond(ASN, (UInt32)stub, (List<UInt32>)providerDiamonds[stub], Result.g, output);
                }
            }
            output.WriteLine("going through peer diamonds --------");
            foreach (var stub in peerDiamonds.Keys)
            {
                if (isFirstIterationDiamond(ASN, (UInt32)stub, (List<UInt32>)peerDiamonds[stub], Result))
                {
                    output.WriteLine("---------------");
                    printDiamond(ASN, (UInt32)stub, (List<UInt32>)peerDiamonds[stub], Result.g, output);
                }
            }
            output.WriteLine("going through customer diamonds --------");
            foreach (var stub in customerDiamonds.Keys)
            {
                if (isFirstIterationDiamond(ASN, (UInt32)stub, (List<UInt32>)customerDiamonds[stub], Result))
                {
                    output.WriteLine("---------------");
                    printDiamond(ASN, (UInt32)stub, (List<UInt32>)customerDiamonds[stub], Result.g, output);
                }
            }

            output.Close();
        }

        public static bool isFirstIterationDiamond(UInt32 pointASN, UInt32 stub, List<UInt32> competitors,
            resultObject Result)
        {

            foreach (var competitor in competitors)
            {
                if (Result.state[0][competitor] == true)/* cannot be a L1 diamond with a 
                                                         * competitor who is an early adopter. */
                    return false;
                
            }
            /** now we know all competitors for this diamond were off at the start. **/
            foreach (var competitor in competitors)
            {
                if (!Result.state[0][competitor] && Result.state[1][competitor])
                {
                    //now being a bit pickier. want 1 competitor flipping in round 1. all the others in
                    //round 2.
                    foreach (var othercompetitor in competitors)
                    {
                        if (othercompetitor != competitor)
                        {
                            if (!Result.state[2][othercompetitor])
                                return false;//a competitor was not on in round 2.
                        }
                    }
                    return true;
                }
                       
            }

            return false;
        }

       public static void printDiamonds(string[] commandPieces, resultObject Result)
        {


            if (commandPieces.Length < 3)
            {
                Console.WriteLine("usage: printdiamonds [AS#] [customer/peer/provider] [outputfile?]");
                return;
            }
            UInt32 ASN;
            if (!UInt32.TryParse(commandPieces[1], out ASN))
            {
                Console.WriteLine("invalid ASN. "); return;
            }
            if (Result.g.GetNode(ASN) == null)
            {
                Console.WriteLine("ASN not in graph. "); return;
            }

            int diamondType = 0;
            switch (commandPieces[2])
            {
                case "customer":
                    diamondType = _customerDiamonds;
                    break;
                case "peer":
                    diamondType = _peerDiamonds;
                    break;
                case "provider":
                    diamondType = _providerDiamonds;
                    break;
                default:
                    diamondType=-1;
                    //return;
                    break;
            }
            StreamWriter output = new StreamWriter(Console.OpenStandardOutput());
            if (commandPieces.Length > 3)
            {
                output.Close();
                Console.WriteLine("outputting diamonds to : " + ResultsExplorer.defaultOutputDirectory + commandPieces[3]);
                output = new StreamWriter(ResultsExplorer.defaultOutputDirectory + commandPieces[3]);


            }
            Hashtable diamonds = new Hashtable();
            if (diamondType > 0)
            {
                getDiamonds(Result, ASN, diamondType, ref diamonds);


                int numPrinted = 0;
                foreach (var key in diamonds.Keys)
                {
                    // if (((List<UInt32>)diamonds[key]).Count == degree)
                    //  {
                    numPrinted++;
                    output.WriteLine("------------ diamond " + numPrinted);
                    printDiamond(ASN, (UInt32)key, ((List<UInt32>)diamonds[key]), Result.g, output);


                    //   }
                    //  if (numPrinted >= numDiamonds)
                    //    break;
                }
                output.Close();
            }
            else
            {
                for (int Type = 1; Type < 4; Type++)
                {
                    diamonds = new Hashtable();
                    getDiamonds(Result, ASN, Type, ref diamonds);

                 
                  
                    int numPrinted = 0;
                    foreach (var key in diamonds.Keys)
                    {
                        // if (((List<UInt32>)diamonds[key]).Count == degree)
                        //  {
                        numPrinted++;
                        output.WriteLine("------------ diamond " + numPrinted);
                        printDiamond(ASN, (UInt32)key, ((List<UInt32>)diamonds[key]), Result.g, output);


                        //   }
                        //  if (numPrinted >= numDiamonds)
                        //    break;
                    }
                   
                }
                output.Close();
            }

        }

       private static void printDiamond(UInt32 pointASN,UInt32 stub, List<UInt32> competitors,SecureSimulator.NetworkGraph g, StreamWriter output)
       {
           SecureSimulator.AsNode pointASNode = g.GetNode(pointASN);
           SecureSimulator.AsNode stubASNode = g.GetNode(stub);
           foreach (UInt32 c in competitors)
           {
               SecureSimulator.AsNode cASNode = g.GetNode(c);
               output.WriteLine("{0} {1} {2} {3} {4}", pointASN,
                                        relationshipToString(cASNode.GetRelationshipTypeOfNeighbor(pointASNode)),
                                        c,
                                        relationshipToString(stubASNode.GetRelationshipTypeOfNeighbor(cASNode)),
                                        stub);
           }
       }

       private static string relationshipToString(SecureSimulator.RelationshipType rt)
       {
           if (rt == SecureSimulator.RelationshipType.CustomerOf)
               return "<-";
           if (rt == SecureSimulator.RelationshipType.ProviderTo)
               return "->";

           return "--";
       }

       public static void countDiamonds(string[] commandPieces,resultObject Result)
       {
        
           StreamWriter output = new StreamWriter(Console.OpenStandardOutput());
           char sep='\t';
           if (commandPieces.Length > 1)
           {
               sep=',';
               output = new StreamWriter(ResultsExplorer.defaultOutputDirectory + commandPieces[1]);
               Console.WriteLine("outputting results to: " + ResultsExplorer.defaultOutputDirectory + commandPieces[1]); 
           }

           foreach (UInt32 earlyAdopter in Result.earlyAdopters)
           {
               output.WriteLine(DateTime.Now + " :: Summarizing diamonds for: " + earlyAdopter);
               Hashtable diamonds = new Hashtable();
               int customerDiamonds, peerDiamonds, providerDiamonds;
               getDiamonds(Result, earlyAdopter, _customerDiamonds, ref diamonds);
               customerDiamonds = diamonds.Count;
               diamonds = new Hashtable();
               getDiamonds(Result, earlyAdopter, _peerDiamonds, ref diamonds);
               peerDiamonds = diamonds.Count;
               diamonds = new Hashtable();
               getDiamonds(Result, earlyAdopter, _providerDiamonds, ref diamonds);
               providerDiamonds = diamonds.Count;

               output.WriteLine("{0}" + sep + "{1}" + sep + "{2}" + sep +"{3}", earlyAdopter, customerDiamonds, peerDiamonds ,providerDiamonds);
             
           }
           output.Close();
       }

     

       private static void getDiamonds(resultObject Result, UInt32 pointASN, int diamondType,ref Hashtable diamonds)
       {
           var nonStubs = Result.g.getNonStubs();
           var Stubs = Result.g.getStubs();
            //neighbors depends on the type of diamond we want.
           IEnumerable<SecureSimulator.AsNode> neighborsOfPoint =null;
           switch (diamondType)
           {
               case _customerDiamonds:
                   neighborsOfPoint = Result.g.GetNode(pointASN).GetNeighborsByType(SecureSimulator.RelationshipType.ProviderTo);
                   break;
               case _peerDiamonds:
                   neighborsOfPoint = Result.g.GetNode(pointASN).GetNeighborsByType(SecureSimulator.RelationshipType.PeerOf);
                   break;
               case _providerDiamonds:
                   neighborsOfPoint = Result.g.GetNode(pointASN).GetNeighborsByType(SecureSimulator.RelationshipType.CustomerOf);
                   break;
               default:
                   break;
           }
                   
           var neighborASNs = new List<UInt32>();
           foreach (var neighbor in Result.g.GetNode(pointASN).GetAllNeighbors())
               neighborASNs.Add(neighbor.NodeNum);
           
           foreach (var neighbor in neighborsOfPoint)
           {
               //which neighbors our neighbors tell us about depends on the relationship type.
               IEnumerable<SecureSimulator.AsNode> grandchildren = null;
               switch (diamondType)
               {
                   case _customerDiamonds:
                       grandchildren = Result.g.GetNode(neighbor.NodeNum).GetNeighborsByType(SecureSimulator.RelationshipType.ProviderTo);
                       break;
                   case _peerDiamonds:
                       grandchildren = Result.g.GetNode(neighbor.NodeNum).GetNeighborsByType(SecureSimulator.RelationshipType.ProviderTo);
                       break;
                   case _providerDiamonds:
                       grandchildren = Result.g.GetNode(neighbor.NodeNum).GetAllNeighbors();
                       break;
                   default:
                       break;
               }   
               foreach (var grandchild in grandchildren)
               {
                   //this grandchild is a stub and is not directly connected to the point.
                   if (Stubs.Contains(grandchild.NodeNum) && !neighborASNs.Contains(grandchild.NodeNum))
                   {
                       if (diamonds.ContainsKey(grandchild.NodeNum))
                           ((List<UInt32>)diamonds[grandchild.NodeNum]).Add(neighbor.NodeNum);
                       else
                       {
                           List<UInt32> toadd = new List<UInt32>();
                           toadd.Add(neighbor.NodeNum);
                           diamonds.Add(grandchild.NodeNum, toadd);
                       }
                   }
               }
           }

           removeBadDiamonds(ref diamonds,pointASN,diamondType,Result);

        

       }

       /// <summary>
       /// remove degree 1 diamonds and diamonds not used for traffic.
       /// </summary>
       /// <param name="diamonds"></param>
       private static void removeBadDiamonds(ref Hashtable diamonds, UInt32 point,int diamondType,resultObject  Result)
       {
           NetworkGraph g = Result.g;
           //Remove level 1 diamonds.
           var keysToRemove = new List<UInt32>();
           foreach (var key in diamonds.Keys)
           {
               if (((List<UInt32>)diamonds[key]).Count == 1)
                   keysToRemove.Add((UInt32)key);
           }

           foreach (var key in keysToRemove)
               diamonds.Remove(key);

           Stopwatch stopwatch = new Stopwatch();
           stopwatch.Start();
           int keysProcessed = 0;
           keysToRemove = new List<uint>();
           //for diamonds that arer peer nad provider make sure
           //the point doesn't have a better path
           // to the stub.

           foreach (var key in diamonds.Keys)
           {
               Destination k = ObjectToText.destinationFromText(bucketTableFile + key + ".csv");
               if (diamondType == _peerDiamonds)
               {
                   if (k.BestRelation[point] == Destination._CUSTOMERCOLUMN)
                       keysToRemove.Add((UInt32)key);
               }
               else if (diamondType == _providerDiamonds)
               {
                   if (k.BestRelation[point] == Destination._CUSTOMERCOLUMN)
                       keysToRemove.Add((UInt32)key);
                   else if (k.BestRelation[point] == Destination._PEERCOLUMN)
                       keysToRemove.Add((UInt32)key);
               }
               if (!keysToRemove.Contains((UInt32)key) && !checkFlipOrder(k, (List<UInt32>)diamonds[key], point, Result))
                   keysToRemove.Add((UInt32)key);

               keysProcessed++;
               if (keysProcessed % 500 == 0)
                   Console.WriteLine("processed " + keysProcessed + " out of " + diamonds.Count + " in " + stopwatch.ElapsedMilliseconds + " ms" + DateTime.Now);
           }
           
           stopwatch.Stop();
           Console.WriteLine("processed " + diamonds.Count + " in " + stopwatch.ElapsedMilliseconds + " ms");

               foreach (var key in keysToRemove)
               diamonds.Remove(key);
       }

       private static bool checkFlipOrder(Destination d, List<UInt32> diamond, UInt32 pointASN,resultObject Result)
       {
           UInt32 winningAS = d.Best[pointASN][0];//who the point routes through originally.


           int winnerFlip = -1;
           List<int> competitorFlips = new List<int>();
           
               foreach (var competitor in diamond)
               {
                   int i = 0;
                   while (i < Result.state.Count && !Result.state[i][competitor])
                       i++;
                   competitorFlips.Add(i);
                   if (competitor == winningAS)
                       winnerFlip = i;
               }

               int minFlip = int.MaxValue;
               for (int i = 0; i < competitorFlips.Count; i++)
               {
                   if (competitorFlips[i] < minFlip)
                       minFlip = competitorFlips[i];
               }
               if (minFlip == winnerFlip && minFlip < Result.state.Count)
                   return false;//winner flipped first.
           return true;
       }



       private static string summarizeDiamonds(Hashtable diamonds)
       {
           int minDegree = int.MaxValue;
           int maxDegree = int.MinValue;
           double avgDegree = 0;
           foreach (var key in diamonds.Keys)
           {
               int degree=((List<UInt32>)diamonds[key]).Count;
               if (degree < minDegree)
                   minDegree = degree;
               if (degree > maxDegree)
                   maxDegree = degree;
               avgDegree += degree;
           }
           return String.Format("min: {0} max: {1} avg: {2}", minDegree, maxDegree, avgDegree / (double)diamonds.Count);
       }

       /// <summary>
       /// candidate to be deleted.
       /// </summary>
       /// <param name="Result"></param>
       /// <param name="pointASN"></param>
       /// <param name="output"></param>
       private static void countDiamonds(resultObject Result, UInt32 pointASN, StreamWriter output)
       {
           var nonStubs = Result.g.getNonStubs();
           var Stubs = Result.g.getStubs();

           var customersOfPoint = Result.g.GetNode(pointASN).GetNeighborsByType(SecureSimulator.RelationshipType.ProviderTo);
           var neighborASNs = new List<UInt32>();
           foreach (var neighbor in Result.g.GetNode(pointASN).GetAllNeighbors())
               neighborASNs.Add(neighbor.NodeNum);
           var customerDiamonds = new Hashtable();
           foreach (var customer in customersOfPoint)
           {
               //my customers will only tell me about edges that pay them.
               foreach (var grandchild in customer.GetNeighborsByType(SecureSimulator.RelationshipType.ProviderTo))//customer edge will share all paths.
               {
                   //this grandchild is a stub and is not directly connected to the point.
                   if (Stubs.Contains(grandchild.NodeNum) && !neighborASNs.Contains(grandchild.NodeNum))
                   {
                       if (customerDiamonds.ContainsKey(grandchild.NodeNum))
                           ((List<UInt32>)customerDiamonds[grandchild.NodeNum]).Add(customer.NodeNum);
                       else
                       {
                           List<UInt32> toadd = new List<UInt32>();
                           toadd.Add(customer.NodeNum);
                           customerDiamonds.Add(grandchild.NodeNum, toadd);
                       }
                   }
               }
           }
           var peersOfPoint = Result.g.GetNode(pointASN).GetNeighborsByType(SecureSimulator.RelationshipType.PeerOf);
           var peerDiamonds = new Hashtable();

           foreach (var peer in peersOfPoint)
           {
               //this peer AS will only tell point about edges that pay it.
               foreach (var grandchild in peer.GetNeighborsByType(SecureSimulator.RelationshipType.ProviderTo))
               {
                   //this grandchild is a stub and is not directly connected to the point.
                   if (Stubs.Contains(grandchild.NodeNum) && !neighborASNs.Contains(grandchild.NodeNum))
                   {
                       if (peerDiamonds.ContainsKey(grandchild.NodeNum))
                           ((List<UInt32>)peerDiamonds[grandchild.NodeNum]).Add(peer.NodeNum);
                       else
                       {
                           List<UInt32> toadd = new List<UInt32>();
                           toadd.Add(peer.NodeNum);
                           peerDiamonds.Add(grandchild.NodeNum, toadd);
                       }
                   }
               }
           }
           var providersOfPoint = Result.g.GetNode(pointASN).GetNeighborsByType(SecureSimulator.RelationshipType.CustomerOf);
           var providerDiamonds = new Hashtable();

           foreach (var provider in providersOfPoint)
           {
               //providers will tell us about all their edges.
               foreach (var grandchild in provider.GetAllNeighbors())
               {
                   //this grandchild is a stub and is not directly connected to the point.
                   if (Stubs.Contains(grandchild.NodeNum) && !neighborASNs.Contains(grandchild.NodeNum))
                   {
                       if (providerDiamonds.ContainsKey(grandchild.NodeNum))
                           ((List<UInt32>)providerDiamonds[grandchild.NodeNum]).Add(provider.NodeNum);
                       else
                       {
                           List<UInt32> toadd = new List<UInt32>();
                           toadd.Add(provider.NodeNum);
                           providerDiamonds.Add(grandchild.NodeNum, toadd);
                       }
                   }
               }
           }


           removeBadDiamonds(ref customerDiamonds, pointASN, _customerDiamonds, Result);
           removeBadDiamonds(ref peerDiamonds, pointASN, _peerDiamonds, Result);
           removeBadDiamonds(ref providerDiamonds, pointASN, _providerDiamonds, Result);

           output.WriteLine(pointASN + "," + customerDiamonds.Count + "," + peerDiamonds.Count + "," + providerDiamonds.Count);
           output.WriteLine(customerDiamonds.Count + " customer diamonds. " + summarizeDiamonds(customerDiamonds));
           output.WriteLine(peerDiamonds.Count + " peer diamonds. " + summarizeDiamonds(peerDiamonds));
           output.WriteLine(providerDiamonds.Count + " provider diamonds. " + summarizeDiamonds(providerDiamonds));


       }
    }
}
