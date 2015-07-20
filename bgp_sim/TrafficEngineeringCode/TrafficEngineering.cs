using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SecureSimulator;
using System.IO;

namespace TrafficEngineeringCode
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
            List<UInt32> multihomedStubs = new List<UInt32>();
            foreach(UInt32 stubNum in stubs)
            {
                AsNode stub = results.g.GetNode(stubNum);
                //if this stub is multihomed, init a destination. add it to the list.
                if (stub.GetNeighborsByType(RelationshipType.CustomerOf).ToArray().Length > 1)
                    multihomedStubs.Add(stubNum);

            }

            Console.WriteLine(multihomedStubs.Count + " stubs out of " + stubs.Count + " are multihomed.");

            StreamWriter output = new StreamWriter("trafficThroughSecureProvider.txt");

            /** Second, go through each iteration... **/
            int iteration=0;
            foreach (bool[] S in results.state)
            {
                DateTime IterationStart = DateTime.Now;
                Int32 numDone = 0;
                foreach (UInt32 multihomedStubNum in multihomedStubs)
                {
                    /** for this multhomed stub, see how much traffic
                     * goes through secure providers **/
                    AsNode multihomedStub = results.g.GetNode(multihomedStubNum);
                    Destination multihomedStubDest = new Destination(SimulatorLibrary.initMiniDestination(results.g, multihomedStubNum, false));

                    //computer the paths and utilities.
                    multihomedStubDest.UpdatePaths(S);
                    multihomedStubDest.ComputeU(GS.W);

                    //get the providers.
                    var Providers = multihomedStub.GetNeighborsByType(RelationshipType.CustomerOf);
                    
                    //count traffic through secure providers (and number of secure providers).
                    Int64 TotalU = 0;
                    Int64 SecureProviderU = 0;
                    Int32 TotalProviders = Providers.Count();
                    Int32 SecureProviders = 0;
                    foreach (var Provider in Providers)
                    {
                        if (S[Provider.NodeNum])
                        {
                            SecureProviderU += multihomedStubDest.U[Provider.NodeNum];
                            SecureProviders++;
                        }
                        TotalU += multihomedStubDest.U[Provider.NodeNum];
                    }

                    /*write out summary of how much traffic went through secure providers. */
               output.WriteLine(iteration + " :: " + multihomedStubNum + " " + SecureProviders + " " + TotalProviders + " " + SecureProviderU + " " + TotalU);
                    numDone++;
                    if ((numDone % 100) == 0)
                        Console.WriteLine("Done " + numDone + " at " + DateTime.Now);
                }
                //some benchmarking.
                Console.WriteLine(DateTime.Now + " done iteration " + iteration + " it started at " + IterationStart);

                iteration++;
            }

            output.Close();
        }

   

    }

}
