using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TestingApplication
{
  public static  class secureIslands
    {

      public static void findSecureIslands(string[] commandPieces, resultObject Result)
      {
          if (commandPieces.Length < 2)
          {
              Console.WriteLine("usage: findsecureislands [iteration]");
              return;
          }
          int iteration;
          if (!int.TryParse(commandPieces[1], out iteration))
          {
              Console.WriteLine("malformed iteration value.");
              return;
          }

          StreamWriter output = new StreamWriter(Console.OpenStandardOutput());
          bool[] iterationstate = Result.state[iteration];

          List<List<UInt32>> allIslands = new List<List<UInt32>>();
          List<UInt32> allVisited = new List<UInt32>();
          Queue<UInt32> queue = new Queue<UInt32>();
        
          UInt32 root = getFirstNotVisited(iterationstate, allVisited);
          do
          {
              List<UInt32> currentIsland = new List<UInt32>();
              queue.Enqueue(root);
              allVisited.Add(root);
              currentIsland.Add(root);

              while (queue.Count > 0)
              {
                  UInt32 curr = queue.Dequeue();
                  var currNeighbors = Result.g.GetNode(curr).GetAllNeighbors();
                  foreach (var neighbor in currNeighbors)
                  {
                      //haven't visited this guy before and he is on.
                      if (!allVisited.Contains(neighbor.NodeNum) && iterationstate[neighbor.NodeNum])
                      {
                          allVisited.Add(neighbor.NodeNum);
                          queue.Enqueue(neighbor.NodeNum);
                          currentIsland.Add(neighbor.NodeNum);
                      }
                  }
              }

              allIslands.Add(currentIsland);
              root = getFirstNotVisited(iterationstate, allVisited);

          }
          while (root != UInt32.MaxValue);

          foreach (var island in allIslands)
          {
              if (island.Count < 50)
              {
                  printIsland(island, Result, output);
              }
          }
          output.Close();

      }

      private static void printIsland(List<UInt32> island, resultObject Result, StreamWriter output)
      {
          List<string> writtenEdges = new List<string>();
          foreach (var AS in island)
          {
              var thisAS = Result.g.GetNode(AS);
              var neighbors = thisAS.GetAllNeighbors();
              foreach (var neighbor in neighbors)
              {
                  if (island.Contains(neighbor.NodeNum) && !writtenEdges.Contains(makeEdge(neighbor.NodeNum,AS)))
                  {
                   writtenEdges.Add(makeEdge(neighbor.NodeNum,AS));
                      output.WriteLine(AS + " " + thisAS.GetRelationshipTypeOfNeighbor(neighbor) + " " + neighbor.NodeNum);
                  }
              }
          }
      }

      static string makeEdge(UInt32 as1, UInt32 as2)
      {
          if (as1 < as2)
              return as1 + "zzz" + as2;
          return as2 + "zzz" + as1;
      }
      
      private static UInt32 getFirstNotVisited(bool[] state, List<UInt32> allVisited)
      {
          for (int i = 0; i < state.Length; i++)
          {
              if (state[i] && !allVisited.Contains((UInt32)i))
                  return (UInt32)i;
          }

          return UInt32.MaxValue;
      }
    }
}
