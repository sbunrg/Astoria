using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SecureSimulator;
using System.IO;
using CloudLibrary;

namespace TestingApplication
{
    public static class TieBreakSetSize
    {


        public static void getTieBreakSetSize(string[] commandPieces, resultObject Result)
        {
            if (commandPieces.Length < 2)
            {
                Console.WriteLine("usage: tiebreakset [AS+ASN | number to sample | b5t5 + number to sample ]");
                return;

            }

            StreamWriter output = new StreamWriter(Console.OpenStandardOutput());
            string bucketTableFile = "C:\\Users\\phillipa\\Desktop\\adoptTraffic\\Code\\AzureSecureSimulator\\MakeAllBucketTables\\bin\\Debug\\destinations2010\\";

            if (commandPieces.Length > 2)
            {
                output.Close();
                output = new StreamWriter(ResultsExplorer.defaultOutputDirectory + commandPieces[2]);
                Console.WriteLine("outputting results to: " + ResultsExplorer.defaultOutputDirectory + commandPieces[2]);
            }
            string arg = commandPieces[1];

            List<AsNode> sampleASes = new List<AsNode>();
            if (arg.IndexOf("AS") == 0)
            {
                //sampling on single AS

            }
            else if (arg.IndexOf("all") < 0)
            {

                if (arg.IndexOf("b5t5") == 0)
                {
                    sampleASes.Add(Result.g.GetNode(22822));
                    sampleASes.Add(Result.g.GetNode(15169));
                    sampleASes.Add(Result.g.GetNode(8075));
                    sampleASes.Add(Result.g.GetNode(20940));
                    sampleASes.Add(Result.g.GetNode(32934));
                    sampleASes.Add(Result.g.GetNode(7018));
                    sampleASes.Add(Result.g.GetNode(701));
                    sampleASes.Add(Result.g.GetNode(174));
                    sampleASes.Add(Result.g.GetNode(1239));
                    sampleASes.Add(Result.g.GetNode(3356));
                }
                arg = arg.Replace("b5t5", "");
                int numSamples = 0;
                if (int.TryParse(arg, out numSamples))
                {
                    numSamples += sampleASes.Count;
                    while (sampleASes.Count < numSamples)
                    {
                        AsNode rand = Result.g.GetRandomNode();
                        if (!sampleASes.Contains(rand))
                            sampleASes.Add(rand);
                    }


                }
            }
            else if (arg.IndexOf("all") == 0)
                sampleASes = Result.g.GetAllNodes().ToList();

            Console.WriteLine("initiated sample set of size : " + sampleASes.Count);
            List<double> stubBestSizes = new List<double>();
            List<double> nonStubBestSizes = new List<double>();
            List<double> BestSizes = new List<double>();
            var stubs = Result.g.getStubs();
            var nonstubs = Result.g.getNonStubs();
            var allASNs = new List<UInt32>();
            foreach (var node in Result.g.GetAllNodes())
                allASNs.Add(node.NodeNum);
            foreach (var sample in sampleASes)
            {
                Destination curr = ObjectToText.destinationFromText(bucketTableFile + sample.NodeNum + ".csv");
                //SimulatorLibrary.initMiniDestination(Result.g, sample.NodeNum, false);
                var bestSizes = getAverageBestSizes(curr, stubs, nonstubs, allASNs, output);
                var avgBest = bestSizes.all;
                var sBest = bestSizes.stubs;
                var nsBest = bestSizes.nonstubs;
                Console.WriteLine("{0} {1:0.00} {2:0.00}(stubs) {3:0.00}(nonstubs)", sample.NodeNum, avgBest, sBest, nsBest);
                stubBestSizes.Add(sBest);
                nonStubBestSizes.Add(nsBest);
                BestSizes.Add(avgBest);
            }

            Console.WriteLine("-------ALL ASes--------");
            Console.WriteLine("AVG: {0:0.000} SD: {1:0.000} MIN: {2:0.000} MAX: {3:0.000}", getMean(BestSizes), getSD(BestSizes, getMean(BestSizes)), getMin(BestSizes), getMax(BestSizes));
            Console.WriteLine("-------STUB ASes--------");
            Console.WriteLine("AVG: {0:0.000} SD: {1:0.000} MIN: {2:0.000} MAX: {3:0.000}", getMean(stubBestSizes), getSD(stubBestSizes, getMean(stubBestSizes)), getMin(stubBestSizes), getMax(stubBestSizes));
            Console.WriteLine("-------NONSTUB ASes--------");
            Console.WriteLine("AVG: {0:0.000} SD: {1:0.000} MIN: {2:0.000} MAX: {3:0.000}", getMean(nonStubBestSizes), getSD(nonStubBestSizes, getMean(nonStubBestSizes)), getMin(nonStubBestSizes), getMax(nonStubBestSizes));
            output.Close();

        }

        struct avgBestSizes
        {
            public double all;
            public double stubs;
            public double nonstubs;
        }


        private static avgBestSizes getAverageBestSizes(Destination d, List<UInt32> stubs, List<UInt32> nonstubs,List<UInt32> ASNs,StreamWriter output)
        {
            double allavg = 0;
            double allpoints = 0;
            double savg = 0;
            double spoints = 0;
            double nsavg = 0;
            double nspoints = 0;
            for (int a = 0; a < ASNs.Count; a++)
            {
                UInt32 i = ASNs[a];
                if (d.Best[i] != null)
                {
                    double currVal = d.Best[i].Count;
                    allavg += currVal;
                    allpoints++;
                  //  output.WriteLine("{0},{1},{2}", currVal, i,d.destination);
                   if (stubs.Contains(i))
                    {
                        savg += currVal;
                        spoints++;
                        output.WriteLine("stub,{0},{1},{2}", currVal, i, d.destination);
                    }
                    else if (nonstubs.Contains(i))
                    {
                        output.WriteLine("nonstub,{0},{1},{2}", currVal, i, d.destination);
                        nsavg += currVal;
                        nspoints++;
                    }
                    
                }
            }
            avgBestSizes toreturn = new avgBestSizes();
            toreturn.all = allavg / allpoints;
            toreturn.stubs = savg  /spoints;
            toreturn.nonstubs = nsavg/ nspoints;
            return toreturn;
        }

        private static double getMean(List<double> vals)
        {
            double mean = 0;
            for (int i = 0; i < vals.Count; i++)
                mean += vals[i];

            return mean / vals.Count();
        }

        private static double getMin(List<double> vals)
        {
            double min = double.MaxValue;
            for (int i = 0; i < vals.Count; i++)
            {
                if (vals[i] < min)
                    min = vals[i];
            }
            return min;
        }

        private static double getMax(List<double> vals)
        {
            double max = double.MinValue;
           
            for (int i = 0; i < vals.Count; i++)
            {
                if (vals[i] >max)
                    max = vals[i];
            }
            return max;
        }

        private static double getSD(List<double> vals, double mean)
        {
            double var = 0;
            for (int i = 0; i < vals.Count; i++)
            {
                var += (vals[i] - mean) * (vals[i] - mean);
            }

            var /= vals.Count;
            return Math.Sqrt(var);
        }

        private static double getAverageBestSize(MiniDestination d)
        {
            double avg=0;
            double points=0;
            for (int i = 0; i < d.Best.Length; i++)
            {
                if(d.Best[i]!=null)
                {
                    avg+=d.Best[i].Count;
                    points++;
                }
            }
            return avg/points;
        }

        private static double getAverageBestSize(MiniDestination d,List<UInt32> careAbout)
        {
            double avg = 0;
            double points = 0;
            for (int i = 0; i < d.Best.Length; i++)
            {
                if (careAbout.Contains((UInt32)i))
                {
                    if (d.Best[i] != null)
                    {
                        avg += d.Best[i].Count;
                        points++;
                    }
                }
            }
            return avg / points;
        }
    }
}
