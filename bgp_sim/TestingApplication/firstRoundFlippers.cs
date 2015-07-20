using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SecureSimulator;
using System.IO;


namespace TestingApplication
{
   public static class firstRoundFlippers
    {

       public static void normalizedStubIncrease(string[] commandPieces, resultObject Result)
       {
           if (commandPieces.Length==2 && commandPieces[1] == "help")
           {
               Console.WriteLine("usage stubincrease [outputfile?]");
               Console.WriteLine("this function tells you for each AS that flipped in the first iteration, " +
                   " how many more paths to its stubs were created from the target set by it flipping. " +
                   "this number is normalized by the number of stubs the AS has.");
               return;
           }

           StreamWriter output = new StreamWriter(Console.OpenStandardOutput());
           char sep = '\t';
           if (commandPieces.Length > 1)
           {
               sep = ',';
               output = new StreamWriter(ResultsExplorer.defaultOutputDirectory + commandPieces[1]);
               Console.WriteLine("outputting results to: " + ResultsExplorer.defaultOutputDirectory + commandPieces[1]);
           }

           List<UInt32> firstIterationFlippers = new List<UInt32>();
           var allASes = Result.g.GetAllNodes();
           var stubs=Result.g.getStubs();
           foreach (var AS in allASes)
           {
               if (Result.state[0][AS.NodeNum] != Result.state[1][AS.NodeNum])
               {
                   //he flipped in first iteration. *woo*
                   List<UInt32> newStubs=stubsThroughMeIncrease(Result.earlyAdopters, AS.NodeNum, Result.state, Result.g,output);
                   int stubChildren = numStubChildren(stubs, AS);
                   output.Write("{0}"+sep,AS.NodeNum);
                   foreach (var stubCount in newStubs)
                   {
                       double fractionOfStubs=(double)stubCount/(double)stubChildren;
                       output.Write("{0:0.0000}"+sep,fractionOfStubs);
                   }
                   output.Write("\n");
               }
           }

           output.Close();
       }

       private static List<UInt32> stubsThroughMeIncrease(List<UInt32> sourceNodes, UInt32 ASN, List<bool[]> state, NetworkGraph g,StreamWriter output)
       {
           var stubs = g.getStubs();

           var ASNode = g.GetNode(ASN);

           //this node flipped,figure out which iteration.
           int flippedIter = -1;
           for (int i = 0; flippedIter < 0 && i < state.Count; i++)
           {
               if (state[i][ASN])
                   flippedIter = i;
           }

           if (flippedIter == 0)
           {
               Console.WriteLine("ASN: " + ASN + " was on to begin with.");
               return new List<UInt32>();
           }
       
           //one variable per source node counting how many stubs of mine
           //route through me from this source now.
           List<UInt32> newCustomerPathsPerSourceNode = new List<UInt32>();
           for (int i = 0; i < sourceNodes.Count; i++)
               newCustomerPathsPerSourceNode.Add(0);


           foreach (var customer in ASNode.GetNeighborsByType(RelationshipType.ProviderTo))
           {

               List<UInt32[]> pathsBefore = new List<uint[]>();
               List<UInt32[]> pathsAfter = new List<uint[]>();
               List<string> pathsBeforeStrings = new List<string>();
               List<string> pathsAfterStrings = new List<string>();

               Destination d = new Destination(SimulatorLibrary.initMiniDestination(g, customer.NodeNum, false));
               //paths before the AS flipped (from early adopters to this stub.)
               d.UpdatePaths(state[flippedIter - 1]);

               foreach (var bigASN in sourceNodes)
               {
                   if (bigASN != d.destination)
                   {

                       pathsBefore.Add(d.GetPathList(bigASN));//path from bigASN to the stub.
                       pathsBeforeStrings.Add(d.GetPath(bigASN, state[flippedIter - 1]));
                   }
                   else
                   {//dummy vals. this source is the destination.
                       pathsBefore.Add(new UInt32[0]);
                       pathsBeforeStrings.Add("");
                   }
               }
               //paths after AS flipped.
               d.UpdatePaths(state[flippedIter]);
               foreach (var bigASN in sourceNodes)
               {
                   if (bigASN != d.destination)
                   {
                       pathsAfter.Add(d.GetPathList(bigASN));
                       pathsAfterStrings.Add(d.GetPath(bigASN, state[flippedIter]));
                   }
                   else
                   {
                       //dummy vals. this source is the destination.
                       pathsAfter.Add(new UInt32[0]);
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

                               //the path after must have been fully secure save for the guy who flipped. and the path before cannot contain the guy who flipped.
                               if (ResultsExplorer.fullySecure(pathsAfter[i], ASNode.NodeNum, state[flippedIter - 1], stubs) && !pathsBefore[i].Contains(ASNode.NodeNum))
                               {
                                  /** Console.WriteLine("---");
                                   Console.WriteLine("Path from: " + bigASN + " to " + customer.NodeNum + " changed from: ");
                                   Console.WriteLine(pathsBeforeStrings[i]);
                                   Console.WriteLine("to: ");
                                   Console.WriteLine(pathsAfterStrings[i]);
                                   Console.WriteLine("---");
                                   **///don't be verbose.

                                   newCustomerPathsPerSourceNode[i]++;
                               }
                           }
                       }
                   }
               }
              //put progress meter here 
           }

           return newCustomerPathsPerSourceNode;
       }

       private static int numStubChildren(List<UInt32> stubs, AsNode ASN)
       {
           int toreturn =0;
           foreach (var child in ASN.GetNeighborsByType(RelationshipType.ProviderTo))
           {
               if (stubs.Contains(child.NodeNum))
                   toreturn++;
           }
           return toreturn;
       }

    }
}
