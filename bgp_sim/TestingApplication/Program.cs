using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SecureSimulator;
using System.IO;

using System.Net;
using System.Net.Sockets;


/*
ostarov@compute:~/bgp_sim2/bgp_sim$ mono TestingApplication/bin/Release/TestingApplication.exe -bulk Cyclops_caida_new.txt 174 32490 -q 32490 174 15169 32490 
Initialized and added 174
Initialized and added 32490
ASes from 32490 to 174, length: 3
32490
11071
174
26253
-
ASes from 15169 to 32490, length: 4
15169
174
11071
32490
26253
209
6461
-

mono TestingApplication/bin/Release/TestingApplication.exe -server11000 TestingApplication/bin/Release/Cyclops_caida_cons.txt precomp/US-precomp367.txt cache/exit_asns.txt
*/
namespace TestingApplication
{
    class Program
    {
        // Incoming data from the client.
        public static string data = null;

        public static void StartListening(String p) {
            
            int port;
            if (!Int32.TryParse(p, out port)) {
               port = 11000;
            } 

            // Data buffer for incoming data.
            byte[] bytes = new Byte[1000000];

            // Establish the local endpoint for the socket.
            // Dns.GetHostName returns the name of the 
            // host running the application.
            //IPostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
            //IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(Dns.Resolve("localhost").AddressList[0], port);

            // Create a TCP/IP socket.
            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and 
            // listen for incoming connections.
            try {
                listener.Bind(localEndPoint);
                listener.Listen(10);

                // Start listening for connections.
                while (true) {
                    Console.WriteLine("Waiting for a connection on {0} ...", port);
                    // Program is suspended while waiting for an incoming connection.
                    Socket handler = listener.Accept();
                    data = null;

                    // An incoming connection needs to be processed.
                    while (true) {
                        bytes = new byte[1000000];
                        int bytesRec = handler.Receive(bytes);
                        data += Encoding.ASCII.GetString(bytes,0,bytesRec);
                        if (data.IndexOf("<EOFc>") > -1) {
                            break;
                        }
                    }

                    // Show the data on the console.
                    Console.WriteLine("Text received : {0}", data);

                    string[] args = data.Split(' ');

                    int i;
                    for (i = 0; i < args.Length-1; ++i) {
                        if ("-q" == args[i]) break;
                        if (dests.Contains(args[i])) continue;
                        dests.Add(args[i]);

                        Destination newD = new Destination();
                        if (initDestination(ref g, ref newD, args[i]))
                        {
                            d.Add(args[i], newD);
                            Console.WriteLine("Initialized and added " + newD.destination);
                        }   
                    }   

                    Console.WriteLine("DESTS " + dests.Count);

                    // Approaching queries
                   
                    StringBuilder res = new StringBuilder(1000000);
    
                    int k = 0;
                    for (i = i+1; i < args.Length-1; i += 2) { 
                        if ("<EOFc>" == args[i]) break;
                        Console.WriteLine(k);
                        string key = args[i] + "-" + args[i+1];
                        if (cache.ContainsKey(key)) {
                            res.Append(cache[key]);
                            k++;
                            continue;
                        }
                       
                        StringBuilder tmp = new StringBuilder();

                        /*
                        //int l = getPath(ref d, args[i], args[i+1]);
                        //getAllPathsOfLength(ref d, l, args[i], args[i+1], ref tmp);
                        getPath2(ref d, args[i], args[i+1], ref tmp);
                        */

                        getBestPaths(ref d, args[i], args[i+1], ref tmp);   

                        res.Append(tmp);
                        cache.Add(key, tmp.ToString());
                        k++;
                    }

                    res.Append("<EOFs>");

                    // Echo the data back to the client.
                    byte[] msg = Encoding.ASCII.GetBytes(res.ToString());
                    
                    //Console.WriteLine(res.ToString());

                    int sent = handler.Send(msg);
                    Console.WriteLine("Sent: " + sent);

                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }
            
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();
        
        }

        private static NetworkGraph g = new NetworkGraph();
        private static HashSet<string> dests = new HashSet<string>();
        private static Dictionary<string, Destination> d = new Dictionary<string, Destination>();
        // Second level of caching
        private static Dictionary<string, string> cache = new Dictionary<string, string>();

        private static void cacheDestinations(string path)
        {
            System.IO.StreamReader file = new System.IO.StreamReader(path);
            string line;
            // whois bulk output format
            file.ReadLine();
            while((line = file.ReadLine()) != null) {
                string[] tmp = line.Split(' ');
                string asn = tmp[0];
                if (dests.Contains(asn)) continue;
                dests.Add(asn);
                Destination newD = new Destination();
                if (initDestination(ref g, ref newD, asn)) {
                    d.Add(asn, newD);
                    Console.WriteLine("Initialized and added " + newD.destination);
                }
            }
            file.Close();
            Console.WriteLine("CACHE: " + dests.Count);
        }

        private static void loadPrecomputation(string path)
        {
            System.IO.StreamReader file = new System.IO.StreamReader(path);
            string line;
            while((line = file.ReadLine()) != null) {
                if ("<EOFs>" == line) break;
                //Console.WriteLine(line);
                string[] tmp = line.Replace(":", " ").Split(' ');
                string src = tmp[2];
                string dst = tmp[4];
                string key = src + "-" + dst;
                StringBuilder buf = new StringBuilder(line + "\n");
                while ((line = file.ReadLine()) != null) {
                    buf.Append(line + "\n");
                    if ("-" == line) break;
                }
                cache.Add(key, buf.ToString());
                // + init the destination
                if (dests.Contains(dst)) continue;
                dests.Add(dst);
                Destination newD = new Destination();
                if (initDestination(ref g, ref newD, dst)) {
                    d.Add(dst, newD);
                    Console.WriteLine("Initialized and added " + newD.destination);
                }
            }
            file.Close();
            Console.WriteLine("PRECOMP: " + cache.Count);
        }

        static void Main(string[] args)
        {
		if (args.Length == 0) {
                    Console.WriteLine("USAGE:");
		    Console.WriteLine("mono TestingApplication.exe -bulk <input file> <dest1> ... <dstN> -q <src1> <dst1> ... <srcN> <dstN>");
		    Console.WriteLine("mono TestingApplication.exe -serverPORT <input file> <precomp file> <cache file>");
                    Console.WriteLine("mono TestingApplication.exe -cmd");
                    return;
		}

                if ("-cmd" == args[0]) {
                    TestingClass test = new TestingClass();
                    test.CLI(false); 
                    return;  
                }

		// Graph initialization
        	//NetworkGraph g = new NetworkGraph();
		
		// E.g., input Cyclops_caida.txt
		if (File.Exists(args[1]))
                {
                    InputFileReader iFR = new InputFileReader(args[1], g);
                    iFR.ProcessFile();
                    Int32 p2pEdges = 0;
                    Int32 c2pEdges = 0;
                    foreach(var ASNode in g.GetAllNodes())
                    {
                        p2pEdges += ASNode.GetNeighborTypeCount(RelationshipType.PeerOf);
                        c2pEdges += ASNode.GetNeighborTypeCount(RelationshipType.CustomerOf);
                        c2pEdges += ASNode.GetNeighborTypeCount(RelationshipType.ProviderTo);
                    }
		    
                    //Console.WriteLine("Read in the graph, it has " + g.NodeCount + " nodes and " + g.EdgeCount + " edges.");
                    //Console.WriteLine("P2P: " + p2pEdges + " C2P: " + c2pEdges);
		}
                else
                {
                    Console.WriteLine("The file " + args[1] +  " does not exist.");
                    return;
                }
		
                if ("-bulk" == args[0]) {
		    // Setting destinations		
		    //HashSet<string> dests = new HashSet<string>();
                    //List<Destination> d = new List<Destination>();

		    int i = 1;
		    for (i = 1; i < args.Length; ++i) {
		        if ("-q" == args[i]) break;
                        if (dests.Contains(args[i])) continue;
                        dests.Add(args[i]);
		        
                        Destination newD = new Destination();
                        if (initDestination(ref g, ref newD, args[i]))
		        {
		            d.Add(args[i], newD);
			    Console.WriteLine("Initialized and added " + newD.destination);
		        }
		    }

                    Console.WriteLine("DESTS " + dests.Count);
		
		    // Approaching queries
		    for (i = i+1; i < args.Length; i += 2) {
		       
                        //StringBuilder res = new StringBuilder(); 
		        //int l = getPath(ref d, args[i], args[i+1]);
		        //getAllPathsOfLength(ref d, l, args[i], args[i+1], ref res);
		        
                        List<List<UInt32>> allPaths = new List<List<UInt32>>();

                        if (d.ContainsKey(args[i+1])) {
                            UInt32 src;
                            UInt32 dst;
                            if (UInt32.TryParse(args[i], out src) && UInt32.TryParse(args[i+1], out dst)) {
                                Console.WriteLine("ASes from " + src + " to " + dst); 
                                d[args[i+1]].GetAllBestPaths(src, dst, ref allPaths);  
                            }
                        }

                        foreach (List<UInt32> path in allPaths) {
                            for (int j = 0; j < path.Count; ++j) {
                                Console.WriteLine(path[j]);
                            }
                            Console.WriteLine("-");
                        }
                        
                    }

                    return;         
                }

                if (args[0].StartsWith("-server")) {
                    
                    if (args.Length > 2) {
                        loadPrecomputation(args[2]);
                        cacheDestinations(args[3]);
                    }

                    String port = args[0].Replace("-server", "");
                    StartListening(port);
                }
        }

	private static bool initDestination(ref NetworkGraph g, ref Destination d, string dest)
        {
            UInt32 destNum;
            if (!UInt32.TryParse(dest, out destNum))
            {
		/*
                Console.WriteLine("Invalid ASN!");
                */
		return false;
            }
            if (g.GetNode(destNum) == null)
            {
		/*
                Console.WriteLine("WARNING: Could not retrieve destination " + d + " from the graph.");
                */
		return false;
            }

	    /*
            Console.WriteLine("Initializing variables and running RTA");
            */
	    MiniDestination miniDest = SimulatorLibrary.initMiniDestination(g, destNum, false);
            d = new Destination(miniDest);
            bool[] tempS = new bool[Constants._numASNs];
            for (int i = 0; i < tempS.Length; i++) {
                 tempS[i] = false;
            }
	    d.UpdatePaths(tempS);
            /*
	    Console.WriteLine("Done initializing. Current active destination is: " + destNum);
            */
	    return true;
        }

        private static void getBestPaths(ref Dictionary<string, Destination> ds, string src, string dst, ref StringBuilder res)
        {
            res.Append("ASes from " + src + " to " + dst + ":\n");
            Console.WriteLine("ASes from " + src + " to " + dst);
        
            UInt32 dstNum;
            UInt32 srcNum;
            
            if (ds.ContainsKey(dst)) {
                Destination d = ds[dst];
                if (UInt32.TryParse(src, out srcNum) && UInt32.TryParse(dst, out dstNum)) {
                    List<List<UInt32>> allPaths = new List<List<UInt32>>();
                    d.GetAllBestPaths(srcNum, dstNum, ref allPaths);
                    HashSet<UInt32> asnSet = new HashSet<UInt32>();
                    foreach (List<UInt32> path in allPaths) {
                        for (int i = 0; i < path.Count; ++i) {
                            asnSet.Add(path[i]);
                        }
                    }
                    foreach (UInt32 asn in asnSet) {
                        res.Append(asn + "\n");
                    } 
                }
            }

            res.Append("-\n");
            return;
        }

	private static int getPath(ref Dictionary<string, Destination> ds, string src, string dst)
        {
            int dstNum;
            UInt32 ASN;
            if (!UInt32.TryParse(src, out ASN) || !int.TryParse(dst, out dstNum))
            {
		/*
                Console.WriteLine("Invalid ASN or destination.");
                */
		return 0;
            }

            if (ds.ContainsKey(dst))
            {
                //if (d.destination == dstNum)
                {
                    //Console.WriteLine("> Path from " + ASN + " to " + d.destination + " is " + d.GetPath(ASN));
                    
                    Destination d = ds[dst];
		    string tmp = d.GetPath(ASN);

		    tmp = tmp.Replace("-", "");
		    tmp = tmp.Replace("<", "");
		    tmp = tmp.Replace(">", ""); 
		    tmp = tmp.Replace("  ", " ");

		    //Console.WriteLine(tmp);
		    string[] ases = tmp.Split(' ');
		    return ases.Length;
                }
            }
	    /*
            Console.WriteLine("WARNING: Could not find destination!");
            */
	    return 0;
        }

        private static void getPath2(ref Dictionary<string, Destination> ds, string src, string dst, ref StringBuilder res)
        {
            int dstNum;
            UInt32 ASN;

            res.Append("ASes from " + src + " to " + dst);
             Console.WriteLine("ASes from " + src + " to " + dst);

            if (!UInt32.TryParse(src, out ASN) || !int.TryParse(dst, out dstNum)) return;

            if (ds.ContainsKey(dst))
            {
                Destination d = ds[dst];
                string tmp = d.GetPath(ASN);
                tmp = tmp.Replace("-", "");
                tmp = tmp.Replace("<", "");
                tmp = tmp.Replace(">", "");
                tmp = tmp.Replace("  ", " ");
                tmp = tmp.Replace(" ", "\n");
                res.Append(tmp);
            }

            res.Append("-\n");
            return;
        }

	private static void getAllPathsOfLength(ref Dictionary<string, Destination> ds, int length, string src, string dst, ref StringBuilder res)
	{
                res.Append("ASes from " + src + " to " + dst + ", length: " + length + "\n");
		Console.WriteLine("ASes from " + src + " to " + dst + ", length: " + length);

                if (length == 0) {
                    res.Append("-\n");
                    Console.WriteLine("-");
                    return;
                }

		int dstNum;
        	UInt32 ASN;
            	if (!UInt32.TryParse(src, out ASN) || !int.TryParse(dst, out dstNum))
            	{
                	/*
                	Console.WriteLine("Invalid ASN or destination.");
                	*/
                        res.Append("-\n");
                        Console.WriteLine("-");
                        return;
            	}

		if (ds.ContainsKey(dst))
		{
			//if (d.destination == dstNum)
			{
                                Destination d = ds[dst];
				if (d.BestNew[ASN] != null)
				{
					HashSet<string> pathSet = new HashSet<string>(); 

					//d.Best[0]++;
					List<List<UInt32>> allPaths = new List<List<UInt32>>();
					List<UInt32> pathNew = new List<UInt32>();
					UInt32 first = (UInt32)((ASN << 3) + Destination._NOREL);
					pathNew.Add(first);
					//Console.Write("First: " + (UInt32)(((uint)first) >> 3) + "\n");
					
					TextWriter tw = null;	// Looks like not needed anymore
					int count = 0;
					d.GetAllPaths(ASN, (UInt32)dstNum, ref allPaths, pathNew, ref tw, ref count);

					int counter = 0;
					for (int j = 0; j < allPaths.Count; j++)
					{
						if (allPaths [j].Count == length) {
							pathToSet(ref pathSet, allPaths[j]);
							counter++;
						}
					}
					allPaths.Clear();
					
					//Console.WriteLine(counter + " Paths from " + ASN + " to " + dstNum);
					
					string[] arr = pathSet.ToArray();
					
					if (arr.Length > 0) {
                                                res.Append(string.Join("\n", arr) + "\n");
						Console.WriteLine(string.Join("\n", arr));
						res.Append("-\n");
                                                Console.WriteLine("-");
					        return;
                                        }
                                        else {
                                            res.Append("-\n");
                                            Console.WriteLine("-");
                                            return;
                                        }
                                        
				}
				else
				{
					//Console.WriteLine("No path from " + ASN);
				}
			}
		}

                res.Append("-\n");
		Console.WriteLine("-");

		//Console.WriteLine("could not find destination");
	}

	private static void pathToSet(ref HashSet<string> res, List<UInt32> path)
        {
            for (int j = 0; j < path.Count; j++)
            {
                //int col = (int)(path[j] & 7);
     		res.Add(Convert.ToString((UInt32)(((uint)path[j]) >> 3)));
            }
        }

    }

}
