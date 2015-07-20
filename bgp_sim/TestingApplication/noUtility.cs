using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SecureSimulator;

namespace TestingApplication
{
    /// <summary>
    /// class to poke at nodes that had 0 utility either at beginning or end.
    /// </summary>
    public static class noUtility
    {
        /// <summary>
        /// write out ASNs for nodes that flipped but had no utility at beginning or end.
        /// </summary>
        /// <param name="commandPieces"></param>
        /// <param name="Result"></param>
        public static void getOnNonStubsWithNoUtility(string[] commandPieces,resultObject Result)
        {
            var nonStubs = Result.g.getNonStubs();
                  StreamWriter output = new StreamWriter(Console.OpenStandardOutput());
            foreach (var nS in nonStubs)
            {
                if (Result.state[Result.state.Count - 1][nS] && Result.ubefore[0][nS] == 0 && Result.ubefore[Result.ubefore.Count - 1][nS] == 0)
                    output.WriteLine(nS);
                    
            }
                output.Close();

        }

        public static void computeUtility(string[] commandPieces, resultObject Result)
        {
            //usage computeutility AS d iteration
            if (commandPieces.Length < 4)
            {
                Console.WriteLine("computeutility [ASN] [dest] [iteration]");
                return;
            }
          
            UInt32 ASN, dest;
            Int32 iter;
            if (!UInt32.TryParse(commandPieces[1], out ASN) || !UInt32.TryParse(commandPieces[2], out dest) || !Int32.TryParse(commandPieces[3], out iter))
            {
                Console.WriteLine("bad params");
                return;
            }
            if (iter > Result.state.Count)
            {
                Console.WriteLine("iteration too large.");
                return;
            }
            bool[] iterState = Result.state[iter];
            foreach (var stub in Result.g.getStubs())
                iterState[stub] = true;//turn on the stubs as in the sim
            SimulatorLibrary.setUtilityComputation(UtilityComputationType.outgoing);
            GlobalState initial = SimulatorLibrary.initGlobalState(Result.g, Result.earlyAdopters, Result.weightedNodes, short.Parse(Result.k));
            Destination d = new Destination(SimulatorLibrary.initMiniDestination(Result.g, dest, false));
            d.UpdatePaths(iterState);
            d.ComputeU(initial.W);
            Console.WriteLine("Utility for " + ASN + " in iteration: " + iter + " is " + d.U[ASN]);
            Worker w = new Worker();
           int afterFlip= w.ComputeUtility(d.BucketTable, d.Best, d.ChosenParent, d.SecP, iterState, ASN, d.L[ASN], d.BestRelation[ASN], initial.W);
            Console.WriteLine("Utility for " + ASN + " in iteration: " + iter + " if they flip is " +afterFlip);
        }
    }
}
