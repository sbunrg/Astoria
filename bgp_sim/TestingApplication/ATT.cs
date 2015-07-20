using System;
using SecureSimulator;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TestingApplication
{
    static class ATT
    {
        static string ATTdir = "ATT\\";

        /// <summary>
        /// function that does all the things we want for AT&T
        /// pulls out ASes on at different points in simulation.
        /// computes different starting scenarios and writes them out.
        /// </summary>
        /// <param name="results"></param>
        public static void doATTThings(resultObject results)
        {
            printOnPerIteration(results);
            makeOutputScenarios(results);
        }

        public static void makeOutputScenarios(resultObject results)
        {
            /* Scenario 1: 
             - 1 file per large AS. File contains the AS and it's stub customers
             */
            var stubs = results.g.getStubs();
            UInt32[] highdegreeASes = { 701, 174, 3356, 7018, 1239, 209, 3549, 4323, 6939, 9002, 6461, 2828, 2914, 4589, 3856, 3561, 6762, 1299, 3320 };
            UInt32[] CPs = { 15169, 8075, 22822, 32934, 20940 };
            int numPrinted;
            foreach (UInt32 bigAS in highdegreeASes)
            {
                StreamWriter output = new StreamWriter(ATTdir + "AS" + bigAS + ".txt");
                numPrinted = 1;
                List<UInt32> printedASes = new List<UInt32>();
                output.WriteLine(bigAS);

                printedASes.Add(bigAS);
                AsNode curr = results.g.GetNode(bigAS);
                foreach (AsNode customer in curr.GetNeighborsByType(RelationshipType.ProviderTo))
                {
                    if (stubs.Contains(customer.NodeNum) && !printedASes.Contains(customer.NodeNum))
                    {
                        printedASes.Add(customer.NodeNum);
                        output.WriteLine(customer.NodeNum);
                        numPrinted++;
                    }
                }
                Console.WriteLine("printed " + numPrinted + " for AS " + bigAS);
                output.Close();
            }

            /* Scenario 2:
             * - 1 file per CP. File contains the CP and it's peers.
             * */
            foreach (UInt32 CP in CPs)
            {
                StreamWriter outputpeers = new StreamWriter(ATTdir + "CP-AS" + CP + "-peers.txt");
                StreamWriter outputpeersandstubs = new StreamWriter(ATTdir + "CP-AS" + CP + "-peers-stubs.txt");
                outputpeers.WriteLine(CP);
                outputpeersandstubs.WriteLine(CP);
                List<UInt32> printedASes1 = new List<UInt32>();
                List<UInt32> printedASes2 = new List<UInt32>();
                numPrinted = 1;
                AsNode curr = results.g.GetNode(CP);
                printedASes1.Add(CP);
                printedASes2.Add(CP);

                foreach (AsNode peer in curr.GetNeighborsByType(RelationshipType.PeerOf))
                {
                    if (!printedASes1.Contains(peer.NodeNum) && !printedASes2.Contains(peer.NodeNum))
                    {
                        printedASes1.Add(peer.NodeNum);
                        printedASes2.Add(peer.NodeNum);
                        numPrinted++;
                        outputpeers.WriteLine(peer.NodeNum);
                    }
                    foreach (AsNode peercustomer in peer.GetNeighborsByType(RelationshipType.ProviderTo))
                    {
                        if (stubs.Contains(peercustomer.NodeNum) && !printedASes2.Contains(peercustomer.NodeNum))
                        {
                            printedASes2.Add(peercustomer.NodeNum);
                            outputpeersandstubs.WriteLine(peercustomer.NodeNum);

                        }

                    }
                }
                Console.WriteLine("printed " + printedASes1.Count + "peers  for AS and "+ printedASes2.Count+ " peer/customers for "  + CP);
                outputpeers.Close();
                outputpeersandstubs.Close();
            }


        }
        public static void printOnPerIteration(resultObject results)
        {
            
            for (int i = 0; i < results.state.Count; i++)
            {
                int numPrinted = 0;
                bool[] currentIteration = results.state[i];
                StreamWriter output = new StreamWriter(ATTdir + "simulation-" + i + ".txt");
                for (int j = 0; j < currentIteration.Length; j++)
                {
                    if (currentIteration[j])
                    {
                        output.WriteLine(j);
                        numPrinted++;
                    }
                }
                Console.WriteLine("printed " + numPrinted + " for iteration " + i);
                output.Close();
            }
        }

    }
}
