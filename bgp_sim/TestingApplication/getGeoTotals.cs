using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SecureSimulator;

namespace TestingApplication
{
    /// <summary>
    /// a class to tell us how many of the ASes that are on
    /// at the end of a results object are from the different
    /// RIRs.
    /// </summary>
    public static class getGeoTotals
    {

        //this file contains data downloaded from teamcymru
        static string RIRdata = "..\\..\\..\\..\\..\\Data\\TeamCymru\\";
        static string ripefile = RIRdata + "delegated-ripencc-latest.txt";
        static string arinfile = RIRdata + "delegated-arin-latest.txt";
        static string apnicfile = RIRdata + "delegated-apnic-latest.txt";
        static string lacnicfile = RIRdata + "delegated-lacnic-latest.txt";
        static string afrinicfile = RIRdata + "delegated-afrinic-latest.txt";

        static List<UInt32> ripeASNs = new List<UInt32>();
        static List<UInt32> arinASNs = new List<UInt32>();
        static List<UInt32> apnicASNs = new List<UInt32>();
        static List<UInt32> lacnicASNs = new List<UInt32>();
        static List<UInt32> afrinicASNs = new List<UInt32>();

        public static void printGeoTotalsPerIteration(string[] commandPieces, resultObject Result)
        {
          
            try
            {
                List<UInt32> allASNs = new List<UInt32>();
                foreach (var ASNode in Result.g.GetAllNodes())
                {
                    allASNs.Add(ASNode.NodeNum);
                }
                loadRIRASNs(allASNs);

                char sep = '\t';
                StreamWriter output = new StreamWriter(Console.OpenStandardOutput());
                if (commandPieces.Length > 1)
                {
                    output = new StreamWriter(ResultsExplorer.defaultOutputDirectory + commandPieces[1]);
                    Console.WriteLine("outputting results to: " + ResultsExplorer.defaultOutputDirectory + commandPieces[1]);
                    sep = ',';
                }

                output.WriteLine("iter"+sep+"ALL"+sep+"ARIN"+sep+"RIPE"+sep+"APNIC"+sep+"LACNIC"+sep+"AFRINIC");
                int lastARIN = howManyOn(Result.state[0], arinASNs);
                int lastRIPE = howManyOn(Result.state[0], ripeASNs);
                int lastAPNIC = howManyOn(Result.state[0], apnicASNs);
                int lastLACNIC = howManyOn(Result.state[0], lacnicASNs);
                int lastAFRINIC = howManyOn(Result.state[0], afrinicASNs);
                int lastALL = howManyOn(Result.state[0], allASNs);
                for (int s = 1; s < Result.state.Count; s++)
                {
                    bool[] currState = Result.state[s];

                    int onARIN = howManyOn(currState, arinASNs);
                    int onRIPE = howManyOn(currState, ripeASNs);
                    int onLACNIC = howManyOn(currState, lacnicASNs);
                    int onAPNIC = howManyOn(currState, apnicASNs);
                    int onAFRINIC = howManyOn(currState, afrinicASNs);
                    int onALL = howManyOn(currState, allASNs);


                    output.WriteLine("{0}"+sep+"{1}"+sep+"{2}"+sep+"{3}"+sep+"{4}"+sep+"{5}"+sep+"{6}",
                        s,
                                    onALL-lastALL,
                                   onARIN-lastARIN,
                                   onRIPE-lastRIPE,
                                   onAPNIC-lastAPNIC, 
                                   onLACNIC-lastLACNIC, 
                                   onAFRINIC-lastAFRINIC);
                    lastARIN = onARIN;
                    lastRIPE = onRIPE;
                    lastLACNIC = onLACNIC;
                    lastAPNIC = onAPNIC;
                    lastAFRINIC = onAFRINIC;
                    lastALL = onALL;
                    }
                output.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message + "\nAre the data files in " + RIRdata);
                return;

            }
        }

        public static void printGeoTotals(string[] commandPieces,resultObject Result)
        {
            try
            {
                List<UInt32> allASNs = new List<UInt32>();
                foreach (var ASNode in Result.g.GetAllNodes())
                {
                    allASNs.Add(ASNode.NodeNum);
                }
                    loadRIRASNs(allASNs);



                bool[] lastState = Result.state[Result.state.Count - 1];

                int onARIN = howManyOn(lastState, arinASNs);
                int onRIPE = howManyOn(lastState, ripeASNs);
                int onLACNIC = howManyOn(lastState, lacnicASNs);
                int onAPNIC = howManyOn(lastState, apnicASNs);
                int onAFRINIC = howManyOn(lastState, afrinicASNs);
                int onALL = howManyOn(lastState,allASNs);
                

                double fracARIN = (double)onARIN / (double)arinASNs.Count;
                double fracRIPE = (double)onRIPE / (double)ripeASNs.Count;
                double fracLACNIC = (double)onLACNIC / (double)lacnicASNs.Count;
                double fracAPNIC = (double)onAPNIC / (double)apnicASNs.Count;
                double fracAFRINIC = (double)onAFRINIC / (double)afrinicASNs.Count;
                double fracALL = (double)onALL / (double)Result.g.NodeCount;
                Console.WriteLine("ALL {0}/{1} = {2:0.0000}", onALL, Result.g.NodeCount, fracALL);
                Console.WriteLine("ARIN {0}/{1} = {2:0.0000}", onARIN, arinASNs.Count, fracARIN);
                Console.WriteLine("RIPE {0}/{1} = {2:0.0000}", onRIPE, ripeASNs.Count, fracRIPE);
                Console.WriteLine("APNIC {0}/{1} = {2:0.0000}", onAPNIC, apnicASNs.Count, fracAPNIC);
                Console.WriteLine("LACNIC {0}/{1} = {2:0.0000}", onLACNIC, lacnicASNs.Count, fracLACNIC);
                Console.WriteLine("AFRINIC {0}/{1} = {2:0.0000}", onAFRINIC, afrinicASNs.Count, fracAFRINIC);
             /*** Debugging code, tells us how many ASNs had no RIR owner or multiple owners.
                Console.WriteLine("DEBUG total of RIRs is: " + (arinASNs.Count + ripeASNs.Count + lacnicASNs.Count + afrinicASNs.Count + apnicASNs.Count));

                int[] ASNCounts = new int[6];
                foreach (var ASN in allASNs)
                {
                    int inASNs = 0;
                    if (arinASNs.Contains(ASN))
                        inASNs++;
                    if (ripeASNs.Contains(ASN))
                        inASNs++;
                    if (lacnicASNs.Contains(ASN))
                        inASNs++;
                    if (apnicASNs.Contains(ASN))
                        inASNs++;
                    if (afrinicASNs.Contains(ASN))
                        inASNs++;

                    ASNCounts[inASNs]++;

                }
                for (int i = 0; i < ASNCounts.Length; i++)
                    Console.WriteLine("{0} ASNs had {1} RIR associated with them.", ASNCounts[i], i);
              * **/
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message + "\nAre the data files in " + RIRdata);
                return;

            }



        }

        private static int howManyOn(bool[] lastState, List<UInt32> ASNs)
        {
            int toreturn =0;
            foreach (var ASN in ASNs)
            {
                if (lastState[ASN])
                    toreturn++;
            }
            return toreturn;
        }

        public static void loadRIRASNs(List<UInt32> allASNs)
        {
            
            readASNsFromFile(arinfile, ref arinASNs, ref allASNs);
            readASNsFromFile(ripefile, ref ripeASNs, ref allASNs);
            readASNsFromFile(lacnicfile, ref lacnicASNs, ref allASNs);
            readASNsFromFile(afrinicfile, ref afrinicASNs, ref allASNs);
            readASNsFromFile(apnicfile, ref apnicASNs, ref allASNs);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="ASNs"></param>
        /// <param name="allASNs">this should be the list of ASNs from a network graph that the simulation
        /// was run on. we'll assign RIRs to the ASNs in the graph only.</param>
        private static void readASNsFromFile(string filename,ref List<UInt32> ASNs,ref List<UInt32> allASNs)
        {
            StreamReader input = new StreamReader(filename);
            while (!input.EndOfStream)
            {
                string[] pieces = input.ReadLine().Split("|".ToCharArray(), StringSplitOptions.None);
                if (pieces.Length > 3)
                {
                    if (pieces[2] == "asn")
                    {
                        UInt32 curr;
                        if (UInt32.TryParse(pieces[3], out curr) && !ASNs.Contains(curr) 
                            && allASNs.Contains(curr)&& curr >= 0 && curr < 65536)
                            ASNs.Add(curr);
                    }
                }  
            }
            input.Close();
        }
    }
}
