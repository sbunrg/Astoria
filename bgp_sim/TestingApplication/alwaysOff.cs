using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SecureSimulator;
namespace TestingApplication
{
    /// <summary>
    /// class to explore the guys that are always off.
    /// </summary>
    public static class alwaysOff
    {
        public static void neverOn(string[] commandPieces)
        {
            string[] ResultDirs = Directory.GetDirectories(ResultsExplorer.defaultResultsDirectory);
            List<UInt32> neverOn = new List<UInt32>();
             StreamWriter output = new StreamWriter(Console.OpenStandardOutput());
            if(commandPieces.Length>1)
            {
                output.Close();
             output = new StreamWriter(ResultsExplorer.defaultOutputDirectory+commandPieces[1]);
            }
            bool includestubs = false;
            if (commandPieces.Length > 2 && commandPieces[2].IndexOf("stubs") >= 0)
                includestubs = true;
            NetworkGraph g = new NetworkGraph();
            foreach (var reDir in ResultDirs)
            {
                if (File.Exists(reDir + "\\params.txt"))
                {
                    resultObject curr = ResultsExplorer.readParamsFile(reDir + "\\params.txt");
                    double u=-1;
                    int f=-1;
                    int k = -1;
                    double.TryParse(curr.u,out u);
                    int.TryParse(curr.f,out f);
                    int.TryParse(curr.k, out k);
                    if (u == 0 && f==0 && k==9)//for simplicity vanilla graph + k=9
                    {
                        if (File.Exists(reDir +"\\"+ curr.precursor + ".S200000.csv"))
                        {
                            curr.state = ResultsExplorer.readStateFromFile(reDir + "\\"+curr.precursor + ".S200000.csv");
                        }
                        if (neverOn.Count == 0)//init the set of nodes that are never on if needed.
                        {
                           
                            if (File.Exists(ResultsExplorer.defaultGraphDirectory + curr.graphFile))
                            {
                                InputFileReader ifr = new InputFileReader(ResultsExplorer.defaultGraphDirectory + curr.graphFile, g);
                                ifr.ProcessFile();

                            }
                            var nonstubs = g.getNonStubs();
                            foreach (var n in g.GetAllNodes())
                            {
                                if(includestubs || nonstubs.Contains(n.NodeNum))
                                neverOn.Add(n.NodeNum);
                            }
                        }

                        //go through and remove anyone we saw as on from the set of nodes that are never on.
                        bool[] lastState=curr.state[curr.state.Count-1];
                        for (int i = 0; i < lastState.Length; i++)
                        {
                            if (lastState[i])
                            {
                                if (neverOn.Contains((UInt32)i))
                                    neverOn.Remove((UInt32)i);
                            }
                        }
                    }
                }
            }
            foreach (var no in neverOn)
                output.WriteLine(no);
            output.Close();

            double avgDegreeOfNeverOn = 0;
            foreach (var no in neverOn)
            {
                double deg = g.GetNode(no).GetAllNeighbors().Count();
                avgDegreeOfNeverOn += deg;

            }

            avgDegreeOfNeverOn /= neverOn.Count;

            Console.WriteLine(neverOn.Count + " nodes never turn on. their average degree is "+avgDegreeOfNeverOn);
            /** See who has competition **/
            List<UInt32> haveCompetition = new List<UInt32>();
            foreach (var no in neverOn)
            {
                var alwaysOffNode = g.GetNode(no);
                var customers = alwaysOffNode.GetNeighborsByType(RelationshipType.ProviderTo);
                foreach (var c in customers)
                {
                    var providers = c.GetNeighborsByType(RelationshipType.CustomerOf);
                    if (providers.Count() > 1 && !haveCompetition.Contains(no))//this customer of the never on guy has more than 1 provider. this never on guy had competition.
                        haveCompetition.Add(no);
                }
            }
            //convert from list of nodes with competition to nodes without competition.
            List<UInt32> nocompetition = new List<uint>();
            foreach (var no in neverOn)
            {
                if (!haveCompetition.Contains(no))
                    nocompetition.Add(no);
            }
            Console.WriteLine( nocompetition.Count + " of these guys had no competition for their customers.");

            /** See who is next to ASes with no competition **/
            List<UInt32> nexttonocompetition = new List<UInt32>();
            foreach (var no in neverOn)
            {
                if (!nocompetition.Contains(no))
                {
                    //this guy had competition. see if he is connected to someone without competition.
                    var alwaysOffNode = g.GetNode(no);
                    foreach (var neighbor in alwaysOffNode.GetAllNeighbors())
                    {
                        if (nocompetition.Contains(neighbor.NodeNum) && !nexttonocompetition.Contains(no))
                            nexttonocompetition.Add(no);
                    }
                }
            }
            Console.WriteLine(nexttonocompetition.Count + " of the remaining " + haveCompetition.Count + " ASes are next to one with no competition.");
            output=new StreamWriter(ResultsExplorer.defaultOutputDirectory+"neveron-withcomp.txt");
            List<UInt32> withCompetitionNotNextToNoCompetition = new List<UInt32>();
            foreach (var asn in haveCompetition)
            {
                if (!nexttonocompetition.Contains(asn))
                {
                    output.WriteLine(asn);
                    withCompetitionNotNextToNoCompetition.Add(asn);
                }
            }
            output.Close();

            /** See which of the remaining ASes are a part of the "jump level" topology **/
            List<UInt32> inJumpLevel = new List<uint>();
            foreach (var asn in withCompetitionNotNextToNoCompetition)
            {
                var alwaysOffNode = g.GetNode(asn);
                var providers = alwaysOffNode.GetNeighborsByType(RelationshipType.CustomerOf);
                List<UInt32> providerASNs = new List<uint>();
                foreach (var p in providers)
                    providerASNs.Add(p.NodeNum);
                foreach (var c in alwaysOffNode.GetNeighborsByType(RelationshipType.ProviderTo))
                {
                    var customersProviders = c.GetNeighborsByType(RelationshipType.CustomerOf);
                    foreach (var cP in customersProviders)
                    {
                        if (providerASNs.Contains(cP.NodeNum)&&!inJumpLevel.Contains(asn))
                        {
                            inJumpLevel.Add(asn);

                        }
                    }
                }
            }
            output = new StreamWriter(ResultsExplorer.defaultOutputDirectory + "neveron-nojumplevel.txt");
            foreach (var asn in withCompetitionNotNextToNoCompetition)
            {
                if (!inJumpLevel.Contains(asn))
                    output.WriteLine(asn);
            }
            output.Close();
            /** to be continued***/
            Console.WriteLine(inJumpLevel.Count + " of the remaining " + withCompetitionNotNextToNoCompetition.Count + " ASes are in jump level topologies");
        }
    }
}
