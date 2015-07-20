using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SecureSimulator;
using System.IO;

namespace TestingApplication
{
    /**
     * Some functions to take a basic look at traffic engineering.
     * 
     * **/
    static class TrafficEngineering
    {

        public static void trafficThroughSecureProviders(resultObject results)
        {
            //get what the simulation state would look like.
            GlobalState GS = SimulatorLibrary.initGlobalState(results.g, results.earlyAdopters, results.weightedNodes, short.Parse(results.k));
      
            /** First, get a list of multihomed stubs as destinations **/
            var stubs=results.g.getStubs();
            List<Destination> multihomedStubs = new List<Destination>();
            foreach(UInt32 stubNum in stubs)
            {
                AsNode stub = results.g.GetNode(stubNum);
                if (stub.GetNeighborsByType(RelationshipType.CustomerOf).ToArray().Length > 1)
                    multihomedStubs.Add(new Destination(SimulatorLibrary.initMiniDestination(results.g, stub.NodeNum, true)));

            }
            Console.WriteLine(multihomedStubs.Count + " stubs out of " + stubs.Count + " are multihomed.");

            /** Second, go through each iteration... **/
            int iteration=0;
            foreach (bool[] S in results.state)
            {
                DateTime IterationStart = DateTime.Now;
                Int32 finishedDests = 0;
                foreach (Destination multihomedStub in multihomedStubs)
                {
                    /** for this multhomed stub, see how much traffic
                     * goes through secure providers **/
                    multihomedStub.UpdatePaths(S);
                    multihomedStub.ComputeU(GS.W);

                    var Providers = results.g.GetNode(multihomedStub.destination).GetNeighborsByType(RelationshipType.CustomerOf);
                    Int64 TotalU = 0;
                    Int64 SecureProviderU = 0;
                    Int32 TotalProviders = Providers.Count();
                    Int32 SecureProviders = 0;
                    foreach (var Provider in Providers)
                    {
                        if (S[Provider.NodeNum])
                        {
                            SecureProviderU += multihomedStub.U[Provider.NodeNum];
                            SecureProviders++;
                        }
                        TotalU += multihomedStub.U[Provider.NodeNum];
                    }
               //     Console.WriteLine(iteration + " :: " + multihomedStub.destination + " " + SecureProviders + " " + TotalProviders + " " + SecureProviderU + " " + TotalU);
                    finishedDests++;
                    if ((finishedDests % 1000) == 0)
                        Console.WriteLine("Finished " + finishedDests + " at " + DateTime.Now + " iteration started at " + IterationStart);

                }
                Console.WriteLine(DateTime.Now + " done iteration " + iteration + " it started at " + IterationStart);

                iteration++;
            }

            
        }

   

    }

}
