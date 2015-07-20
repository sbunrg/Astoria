using System;
using System.IO;

namespace SecureSimulator
{
    /// <summary>
    /// Class used to parse network graph input files into in-memory objects
    /// </summary>
    public class InputFileReader
    {
        /// <summary>
        /// Stores the filename to read graph data from
        /// </summary>
        public string Filename { get; private set; }
        /// <summary>
        /// The NetGraph instance that will be build from the input file contents
        /// </summary>
        public NetworkGraph NetGraph { get; private set; }
        /// <summary>
        /// Delimiter between fields in the input file, used to split the string
        /// </summary>
        private readonly char[] _cadiaFileFieldDelimiter = new[] {'|'};
        private readonly char[] _cyclopsfileFieldDelimiter = new[] { '\t' };
        private readonly char[] _ixpFileFieldDelimiter = new[] { ' ' };

        /// <summary>
        /// Constructor just saves the filename and graph
        /// </summary>
        public InputFileReader(string filename, NetworkGraph netGraph)
        {
            Filename = filename;
            NetGraph = netGraph;
        }


        public void ProcessFile()
        {
            /** only input files are cyclops format right now. **/
             if (Filename.IndexOf("yclops") >= 0)
            {
                ProcessCyclopsFile();
            }
            else if (Filename.EndsWith(".txt"))
            {
                ProcessCadiaFile();
            }
           
        }

        public void ProcessIXPfile()
        {
            Int32 numNewEdgesAdded = 0;
            Int32 numBadConfidenceLines = 0;
            Int32 numMissingASes=0;
            Int32 numExistingEdges =0;
            Int32 numMalformedLines = 0;

            using (StreamReader sr = new StreamReader(Filename))
            {
                string line;
                int lineNumber = 0;

                // Read a single line at a time, which corresponds to a single edge
                while ((line = sr.ReadLine()) != null)
                {
                    // Line number housekeeping and debug output
                    lineNumber++;
                    if (lineNumber % 1000 == 0)
                    {
                    //    OutputLog.LogMessage(LogLevelType.LogLevelDebug, "Processing line {0}", lineNumber);
                    }

                    // Split the line into 3 strings using space as the delimiter
                    string[] fields = line.Split(_ixpFileFieldDelimiter);

                    // Check for comment
                    if ((fields.Length > 0) && (fields[0].StartsWith("#")))
                    {
                        continue;
                    }

                    // Should be 5 fields
                    if (fields.Length != 5)
                    {
                      //  OutputLog.LogMessage(LogLevelType.LogLevelWarning, "Line {0} does not have the proper number of fields", lineNumber);
                        continue;
                    }

                    // Check if the confidence is good 
                    string confidence = (string)fields[3];
                    if (confidence != "good")
                    {
                        numBadConfidenceLines++;
                     //   OutputLog.LogMessage(LogLevelType.LogLevelDebug,
                      //      "Ignored line {0}. It has med/bad confidence. ", lineNumber);
                        continue;
                    }

                    // Validate the ASN fields
                    UInt32 vertex1, vertex2;
                    if (!StringIsNumber(fields[1], null, out vertex1) ||
                        !StringIsNumber(fields[2], null, out vertex2) )
                    {
                        numMalformedLines++;
                       // OutputLog.LogMessage(LogLevelType.LogLevelDebug, "Line {0} has a malformed field", lineNumber);
                        continue;
                    }
                    
                    // if the ASes don't exist in the graph ignore the line.
                    if ((NetGraph.GetNode(vertex1) == null) ||
                        (NetGraph.GetNode(vertex2) == null) )
                    {
                        numMissingASes++;
                       // OutputLog.LogMessage(LogLevelType.LogLevelDebug,
                       //     "Ignored line {0}. Refers to ASes not in graph.", lineNumber);
                        continue;
                    }


                    // if the ASes already have a edge between them, ignore the line
                    RelationshipType existingRel = NetGraph.GetNode(vertex1).GetRelationshipTypeOfNeighbor(NetGraph.GetNode(vertex2));
                    if (existingRel != RelationshipType.NullRelationship)
                    {
                        numExistingEdges++;
                       // OutputLog.LogMessage(LogLevelType.LogLevelDebug,
                         //   "Ignored line {0}. AS {1} and AS {2} already connected by RelationshipType {3}.", 
                          //  lineNumber, vertex1, vertex2, existingRel);
                        continue;
                    }    
                    
                    // Add the new edge to the graph
                    numNewEdgesAdded++;
                    NetGraph.AddEdge(vertex1, vertex2, RelationshipType.PeerOf);
                    NetGraph.AddEdge(vertex2, vertex1, RelationshipType.PeerOf);
                    continue;
                }
            }
            //OutputLog.LogMessage(LogLevelType.LogLevelStdOut,
           //     "Augmented graph with {0} peer edges.\n"+
           //     "Ignored the following entries in the IXP file: \n" +
           //     "Lines with med or bad confidence: {1}\n" +
           //     "Lines refering to missing ASes: {2} \n" +
           //     "Lines refering to edges that already exist in the graph: {3}\n"+
            //    "Lines with malformed entries: {4}",
            //    numNewEdgesAdded, numBadConfidenceLines, numMissingASes, numExistingEdges, numMalformedLines);
        }


        /// <summary>
        /// Reads and parses a CADIA file and populates the graph object
        /// </summary>
        private void ProcessCadiaFile()
        {
            using (StreamReader sr = new StreamReader(Filename))
            {
                string line;
                int lineNumber = 0;
                // Read a single line at a time, which corresponds to a single edge
                while ((line = sr.ReadLine()) != null)
                {
                    // Line number housekeeping and debug output
                    lineNumber++;
                    if (lineNumber % 1000 == 0)
                    {
                      //  OutputLog.LogMessage(LogLevelType.LogLevelDebug, "Processing line {0}", lineNumber);
                    }

                    // Split the line into 3 strings using space as the delimiter
                    string[] fields = line.Split(_cadiaFileFieldDelimiter);

                    // Check for comment
                    if ((fields.Length > 0) && (fields[0].StartsWith("#")))
                    {
                        continue;
                    }

                    // Should be 3 fields
                    if (fields.Length != 3)
                    {
                      //  OutputLog.LogMessage(LogLevelType.LogLevelWarning, "Line {0} does not have the proper number of fields", lineNumber);
                        continue;
                    }

                    // Validate each field
                    UInt32 vertex1, vertex2;
                    Int32 edgeDescriptor;
                    if (!StringIsNumber(fields[0], null, out vertex1) ||
                        !StringIsNumber(fields[1], null, out vertex2) ||
                        !StringIsNumber(fields[2], null, out edgeDescriptor))
                    {
                     //   OutputLog.LogMessage(LogLevelType.LogLevelWarning, "Line {0} has a malformed field", lineNumber);
                        continue;
                    }

                    // Decode the edge direction
                    RelationshipType relationshipType;
                    switch (edgeDescriptor)
                    {
                        case 1:
                            relationshipType = RelationshipType.CustomerOf;
                            break;
                        case 0:
                            relationshipType = RelationshipType.PeerOf;
                            break;
                        case -1:
                            relationshipType = RelationshipType.ProviderTo;
                            break;
                        case 2:
                            relationshipType = RelationshipType.SiblingOf;
                            break;
                        default:
                          //  OutputLog.LogMessage(LogLevelType.LogLevelWarning, "Line {0} has an unknown edge type", lineNumber);
                            continue;
                    }

                    // Add the new edge to the graph
                    NetGraph.AddEdge(vertex1, vertex2, relationshipType);
                }
            }
        }


        /// <summary>
        /// Reads and parses the cyclops file and populates the graph object
        /// Ignores 4-byte ASNs
        /// </summary>
        public void ProcessCyclopsFile()
        {
            Int32 numBadVertex = 0;
            Int32 num4byteASNs = 0;
            Int32 numbadReltypes = 0;
            using (StreamReader sr = new StreamReader(Filename))
            {
                string line;
                int lineNumber = 0;
                // Read a single line at a time, which corresponds to a single edge
                while ((line = sr.ReadLine()) != null)
                {
                    // Line number housekeeping and debug output
                    lineNumber++;
                    if (lineNumber % 1000 == 0)
                    {
                       // OutputLog.LogMessage(LogLevelType.LogLevelDebug, "Processing line {0}", lineNumber);
                    }

                    // Split the line into 3 strings using space as the delimiter
                    string[] fields = line.Split(_cyclopsfileFieldDelimiter);

                    // Check for comment
                    if ((fields.Length > 0) && (fields[0].StartsWith("#")))
                    {
                        continue;
                    }

                    // Should be 3 fields
                    if (fields.Length != 3)
                    {
                      //  OutputLog.LogMessage(LogLevelType.LogLevelWarning, "Line {0} does not have the proper number of fields", lineNumber);
                        continue;
                    }

                    // Validate each field
                    UInt32 vertex1, vertex2;
                    if (!StringIsNumber(fields[0], null, out vertex1) ||
                        !StringIsNumber(fields[1], null, out vertex2))
                    {

                        float junkfloat;
                        if (float.TryParse(fields[0], out junkfloat))
                        {
                            num4byteASNs++;
                         //   OutputLog.LogMessage(LogLevelType.LogLevelDebug, "Ignored 4 byte ASN at Line {0}.", lineNumber);
                        }
                        else
                        {
                            numBadVertex++;
                        //    OutputLog.LogMessage(LogLevelType.LogLevelWarning, "Line {0} has a malformed field", lineNumber);
                        }
                        continue;
                    }

                    // Decode the edge direction
                    string edgeDescriptor = fields[2];


                    RelationshipType relationshipType;
                    switch (edgeDescriptor)
                    {
                        case "c2p":
                            relationshipType = RelationshipType.CustomerOf;
                            break;
                        case "p2p":
                            relationshipType = RelationshipType.PeerOf;
                            break;
                        case "p2c":
                            relationshipType = RelationshipType.ProviderTo;
                            break;
                        case "-":
                            numbadReltypes++;
                            // cyclops can't characterterize this relationship, so ignore.
                            continue;
                        default:
                          //  OutputLog.LogMessage(LogLevelType.LogLevelDebug, "Line {0} has an unknown edge type", lineNumber);
                            continue;
                    }

                    //ignore vertices with too high ASNs
                    if (vertex1 > Constants._numASNs || vertex2 > Constants._numASNs)
                        continue;
                    
                    // Add the new edge to the graph
                    NetGraph.AddEdge(vertex1, vertex2, relationshipType);
                    NetGraph.AddEdge(vertex2, vertex1, AsNode.GetOppositeRelationshipType(relationshipType));
                }
            }
          //  OutputLog.LogMessage(LogLevelType.LogLevelWarning,
            //    "Cyclops data has the following `bad entries' that were NOT loaded to graph:\n"+
            //    "{0} malformed entries\n"+
           //     "{1} 4-byte ASNs,\n"+
            //    "{2} '-' relationships",
            //    numBadVertex, num4byteASNs, numbadReltypes);
        }

        /// <summary>
        /// Verifies that the input string is actually a number (less than maxVal, if maxVal is not null)
        /// and stores the number in the numberValue output parameter.  Returns true if successful, false otherwise.
        /// </summary>
        private static bool StringIsNumber(string input, UInt32? maxVal, out UInt32 numberValue)
        {
            // Parse the string as an integer
            if (!UInt32.TryParse(input, out numberValue))
            {
                numberValue = 0;
                return false;
            }

            // Check boundary
            if ((maxVal != null) && (numberValue > maxVal))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Verifies that the input string is actually a number (less than maxVal, if maxVal is not null)
        /// and stores the number in the numberValue output parameter.  Returns true if successful, false otherwise.
        /// </summary>
        private static bool StringIsNumber(string input, Int32? maxVal, out Int32 numberValue)
        {
            // Parse the string as an integer
            if (!Int32.TryParse(input, out numberValue))
            {
                numberValue = 0;
                return false;
            }

            // Check boundary
            if ((maxVal != null) && (numberValue > maxVal))
            {
                return false;
            }

            return true;
        }
    }
}
