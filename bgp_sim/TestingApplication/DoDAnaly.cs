using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SecureSimulator;

namespace TestingApplication
{
    static class DoDAnaly
    {

        public static void traverseDoD(NetworkGraph g)
        {
            //starting at 721 print out providers to DoD people.

            AsNode DoDMain = g.GetNode(721);
            List<UInt32> DoDProviders = new List<UInt32>();
            List<UInt32> DoDPeers = new List<UInt32>();
            List<UInt32> DoDASNs = new List<UInt32>();
            Queue<UInt32> ASesToProcess = new Queue<UInt32>();

                DoDASNs.Add(721);
                ASesToProcess.Enqueue(721);
           

            while (ASesToProcess.Count > 0)
            {
                AsNode curr = g.GetNode(ASesToProcess.Dequeue());
                Console.WriteLine("Processing: " + curr.NodeNum);
                foreach (var provider in curr.GetNeighborsByType(RelationshipType.CustomerOf))
                {
                    if (!DoDASNs.Contains(provider.NodeNum) && !DoDProviders.Contains(provider.NodeNum))
                    {
                        DoDProviders.Add(provider.NodeNum);
                        Console.WriteLine(curr.NodeNum + " has non-DoD provider: " + provider.NodeNum);
                        Console.ReadLine();
                    }
                }
                foreach (var customer in curr.GetNeighborsByType(RelationshipType.ProviderTo))
                {
                    if (!DoDASNs.Contains(customer.NodeNum) && !ASesToProcess.Contains(customer.NodeNum))
                    {
                        ASesToProcess.Enqueue(customer.NodeNum);
                        DoDASNs.Add(customer.NodeNum);
                    }
                }
                foreach (var peer in curr.GetNeighborsByType(RelationshipType.PeerOf))
                {
                    if (!DoDASNs.Contains(peer.NodeNum))
                        DoDPeers.Add(peer.NodeNum);
                }

            }

            Console.WriteLine("DoDProviders: ");
            foreach (var provider in DoDProviders)
            {
             if(!DoDASNs.Contains(provider))
                Console.Write(provider + ", ");

            } Console.WriteLine();
            Console.WriteLine("DoDPeers: ");
            foreach (var peer in DoDPeers)
                Console.Write(peer + ", ");
            Console.WriteLine();


        }
    }
}
