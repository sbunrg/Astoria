using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SecureSimulator;
using System.IO;

namespace TestingApplication
{
   public  class testShortestPaths
    {
       NetworkGraph g = new NetworkGraph();
       bool[] S = new bool[0];
       GlobalState gs = new GlobalState();
       public void testSPInterface()
       {
           SimulatorLibrary.setHash(true);
           SimulatorLibrary.setUtilityComputation(UtilityComputationType.outgoing);
            Console.WriteLine("Welcome to the short paths testing interface: ");
            bool exitNow = false;
           

            while (!exitNow)
            {

                Console.Write(">>");
                string command = Console.ReadLine().ToLower();
                string[] pieces = command.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (command.IndexOf("input") == 0)
                {
                    g = input(pieces);
                }
                else if (command.IndexOf("getpath") == 0)
                {
                    getpath(pieces);
                }
                else if (command.IndexOf("setstate") == 0)
                    S = setstate(pieces);
                else if (command.IndexOf("init") == 0)
                {
                    List<UInt32> ea = new List<uint>();
                    ea.Add(1239);
                    gs = SimulatorLibrary.initGlobalState(g, ea);
                    
                }
                else if (command.IndexOf("iterate") == 0)
                {
                    List<MiniDestination> miniDs = new List<MiniDestination>();
                    foreach (var AS in g.GetAllNodes())
                    {
                        miniDs.Add(SimulatorLibrary.initMiniDestinationSP(g,AS.NodeNum,false));
                        Console.WriteLine("initialized AS " + AS.NodeNum);
                    }

                    List<Message> results= new List<Message>();
                    foreach(var mD in miniDs)
                    {
                        results.Add(SimulatorLibrary.ComputeOnDestination(mD,gs));
                        Console.WriteLine("computed on: " + mD.destination);
                    }
                    Console.WriteLine("updating global state.");
                    Int64[] Before = new Int64[Constants._numASNs];
                   Int64[] After = new Int64[Constants._numASNs];


                    SimulatorLibrary.updateGlobalState(ref gs, results,(float)0,ref Before,ref After);
                    for (int i = 0; i < gs.S.Length; i++)
                        if (gs.S[i])
                            Console.WriteLine("AS " + i + " is on.");
                    }
            }
       }

       public static bool[] setstate(string[] pieces)
       {
           bool[] toreturn = new bool[Constants._numASNs];
           if (pieces.Length < 2)
               return toreturn;
           for (int i = 0; i < pieces[1].Length; i++)
           {
               if (pieces[1][i] == '1')
                   toreturn[i] = true;
               else
                   toreturn[i] = false;
               Console.WriteLine("AS: " + i + " is " + toreturn[i]);
           }
           Console.WriteLine("successfully set state");

           return toreturn;
       }

       private void getpath(string[] pieces)
       {
           if (pieces.Length < 3)
               return;

           uint as1, as2;
           if (!uint.TryParse(pieces[1], out as1) || !uint.TryParse(pieces[2], out as2))
           {
               return;
           }

           if (g.GetNode(as1) == null || g.GetNode(as2) == null)
               return;

           Destination as2_dst = new Destination(SimulatorLibrary.initMiniDestinationSP(g, as2, false));
           bool[] dummyS = new bool[Constants._numASNs];
           as2_dst.UpdatePaths(dummyS);
           Console.WriteLine("shortest path:");
           Console.WriteLine(as2_dst.GetPath(as1,g));
           Console.WriteLine("regular path:");
           as2_dst = new Destination(SimulatorLibrary.initMiniDestination(g, as2, false));
           as2_dst.UpdatePaths(dummyS);
           Console.WriteLine(as2_dst.GetPath(as1));
       }

       private NetworkGraph input(string[] pieces)
       {
           if (pieces.Length < 2)
           {
               return null;
           }

           NetworkGraph g = new NetworkGraph();
           if (!File.Exists(pieces[1]))
               pieces[1] = "C:\\Users\\phillipa\\Desktop\\adoptTraffic\\AugmentedGraphs\\" + pieces[1];
           if (File.Exists(pieces[1]))
           {
               InputFileReader ifr = new InputFileReader(pieces[1], g);
               ifr.ProcessFile();
               Console.WriteLine("read graph: " + g.EdgeCount + " edges " + g.NodeCount + " nodes");
               return g;
           }
           return null;

       }

    }
}
