using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TrafficEngineeringCode
{
    class Program
    {
        static void Main(string[] args)
        {
            //bunch of stuff to point it at the canonical sim results directory on my computer.
            ResultsExplorer res = new ResultsExplorer();
            string[] simulationDirectories = Directory.GetDirectories(ResultsExplorer.defaultResultsDirectory);
            //just set directory to point to the canonical sim results folder.
            string directory = simulationDirectories[33];

            resultObject canonical = res.loadSimulationNonInteractive(directory);
          
            for (int i = 0; i < canonical.state.Count; i++)
            {
                double numOn = 0;
                bool[] currS = canonical.state[i];
                StreamWriter output = new StreamWriter("canonical-ASesON-" + i + ".txt");
                for (int AS = 0; AS < currS.Count(); AS++)
                {
                    if (currS[AS])
                    {
                        output.WriteLine(AS);
                        numOn++;
                    }
                }
                Console.WriteLine(numOn/currS.Count() + " of ASes are ON "+numOn+ " " + currS.Count());
                output.Close();
            }

          //  TrafficEngineering.trafficThroughSecureProviders(canonical);
        }
    }
}
