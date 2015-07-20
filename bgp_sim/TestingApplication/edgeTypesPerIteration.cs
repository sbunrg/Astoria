using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SecureSimulator;


namespace TestingApplication
{
   public static class edgeTypesPerIteration
    {

       public static void edgeSummarize(string[] commandPieces, resultObject Result)
       {
           char sep = '\t';
           StreamWriter output = new StreamWriter(Console.OpenStandardOutput());
           if (commandPieces.Length > 1)
           {
               output = new StreamWriter(ResultsExplorer.defaultOutputDirectory + commandPieces[1]);
               Console.WriteLine("outputting results to: " + ResultsExplorer.defaultOutputDirectory + commandPieces[1]);
               sep = ',';
           }

           output.WriteLine("iter" + sep + "total" + sep + "cust" + sep + "peer" + sep + "prov"+sep + "total-ns" + sep + "cust-ns" + sep + "peer-ns" + sep + "prov-ns");
           for (int i = 1; i < Result.state.Count; i++)
               edgeIterationSummarize(Result, i, output, sep);
           output.Close();
       }

       private static void edgeIterationSummarize(resultObject Result, int iteration, StreamWriter output, char sep)
       {
           if (iteration == 0)
               return;

           bool[] lastIteration = Result.state[iteration - 1];
           bool[] currIteration = Result.state[iteration];

           var ASNodes = Result.g.GetAllNodes();

           int totalEdges = 0;
           int customerEdges = 0;
           int peerEdges = 0;
           int providerEdges = 0;
           int totalNonStubEdges = 0;
           int customerNonStubEdges = 0;
           int peerNonStubEdges = 0;
           int providerNonStubEdges = 0;

           var nonStubs = Result.g.getNonStubs();
           foreach (var ASNode in ASNodes)
           {
               //this AS has flipped.
               if (currIteration[ASNode.NodeNum] != lastIteration[ASNode.NodeNum])
               {
                   var customers = ASNode.GetNeighborsByType(RelationshipType.ProviderTo);
                   var peers = ASNode.GetNeighborsByType(RelationshipType.PeerOf);
                   var providers = ASNode.GetNeighborsByType(RelationshipType.CustomerOf);
                   foreach (var c in customers)
                   {
                       if (lastIteration[c.NodeNum])
                       {
                           customerEdges++;
                           totalEdges++;
                           if (nonStubs.Contains(c.NodeNum))
                           {
                               customerNonStubEdges++;
                               totalNonStubEdges++;
                           }
                       }
                   }
                   foreach (var p in peers)
                   {
                       if (lastIteration[p.NodeNum])
                       {
                           peerEdges++;
                           totalEdges++;
                           if (nonStubs.Contains(p.NodeNum))
                           {
                               peerNonStubEdges++;
                               totalNonStubEdges++;
                           }
                       }
                   }
                   foreach (var p in providers)
                   {
                       if (lastIteration[p.NodeNum])
                       {
                           providerEdges++;
                           totalEdges++;
                           if (nonStubs.Contains(p.NodeNum))
                           {
                               providerNonStubEdges++;
                               totalNonStubEdges++;
                           }
                       }
                   }

               }
           }

           output.WriteLine("{0}" + sep + "{1}" + sep + "{2}" + sep + "{3}" + sep + "{4}" + sep + "{5}" + sep + "{6}" + sep + "{7}" + sep + "{8}", 
               iteration, totalEdges, customerEdges, peerEdges, providerEdges, totalNonStubEdges, customerNonStubEdges, peerNonStubEdges, providerNonStubEdges);

       }

    }

    
}
