﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using SecureSimulator;
using CloudLibrary;

namespace TestingApplication
{
    class PathChecker
    {
        public bool serializeBestNew(List<UInt32>[] bestNew, int dest, string path)
        {

            try
            {
                //Console.WriteLine("Path: " + path);
                using (Stream stream = File.Open(path + dest+ ".bin", FileMode.Create))
                {
                    BinaryFormatter bin = new BinaryFormatter();
                    bin.Serialize(stream, bestNew);
                }

            }
            catch (IOException)
            {
                Console.WriteLine("Something went wrong in serialiser!");
                return false;
            }

            return true;
        }

        public List<UInt32>[] deserializeBestNew(int dest, string path)
        {
            try
            {
                using (Stream stream = File.Open(path + dest +".bin", FileMode.Open))
                {
                    BinaryFormatter bin = new BinaryFormatter();

                    List<UInt32>[] toRet = (List<UInt32>[])bin.Deserialize(stream);
                    return toRet;
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }

        public string pathAnalysis(string path, ref Destination dest)
        {
            int pathExists;
            int pathViolate = 0;
            int GR = 0;
            int bestrln = 0;
            int shortest = 0;
            int bucket;
            List<UInt32> relPath = new List<UInt32>();

            //Check if path exists from best new and make a relPath
            string[] asPath = path.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            UInt32 AS;
            UInt32.TryParse(asPath[0], out AS);

            if (dest.BestNew[AS] != null)
            {
                pathExists = 1;
                UInt32 first = (UInt32)((AS << 3) + Destination._NOREL);
                relPath.Add(first);

                for (int i = 0; i < (asPath.Length - 1); i++)
                {
                    int asNum, asTo;
                    int.TryParse(asPath[i], out asNum);
                    int.TryParse(asPath[i + 1], out asTo);

                    if (dest.BestNew[asNum] != null)
                    {
                        UInt32 encoded0 = (UInt32)((asTo << 3) + Destination._PROVIDERCOLUMN);
                        UInt32 encoded1 = (UInt32)((asTo << 3) + Destination._CUSTOMERCOLUMN);
                        UInt32 encoded2 = (UInt32)((asTo << 3) + Destination._PEERCOLUMN);
                        UInt32 encoded3 = (UInt32)((asTo << 3) + Destination._NOREL);

                        if (dest.BestNew[asNum].Exists(element => element == encoded0))
                        {
                            relPath.Add(encoded0);
                        }
                        else if (dest.BestNew[asNum].Exists(element => element == encoded1))
                        {
                            relPath.Add(encoded1);
                        }
                        else if (dest.BestNew[asNum].Exists(element => element == encoded2))
                        {
                            relPath.Add(encoded2);
                        }
                        else if (dest.BestNew[asNum].Exists(element => element == encoded3))
                        {
                            relPath.Add(encoded3);
                        }
                        else
                        {
                            pathExists = 0;
                            break;
                        }

                        // Now do perform a sanity check
                        int size = relPath.Count;
                        int rel, prevRel;
                        UInt32 temp;
                        dest.unjoin(relPath[size - 1], out temp, out rel);
                        dest.unjoin(relPath[size - 2], out temp, out prevRel);

                        if ((rel == Destination._PEERCOLUMN && prevRel == Destination._CUSTOMERCOLUMN) || (prevRel == Destination._PEERCOLUMN && rel == Destination._PROVIDERCOLUMN) || (prevRel == Destination._PEERCOLUMN && rel == Destination._PEERCOLUMN) || (prevRel == Destination._CUSTOMERCOLUMN && (rel == Destination._PEERCOLUMN || rel == Destination._PROVIDERCOLUMN)))
                        {
                            pathViolate = 1;
                            pathExists = 0;
                            break;
                        }
                    }
                    else
                    {
                        pathExists = 0;
                        break;
                    }
                }
            }
            else
            {
                pathExists = 0;
            }

            //GR path and compare
            string GR_string = dest.GetPathCommaSep(AS);
            if (pathAvailable_GR(path, ref dest.Best) && (pathExists == 1))
            {
                GR = 1;
            }

            //if not GR check for best rel
            if ((GR == 0) && (pathExists == 1))
            {
                bestrln = 1;
                for (int j = 0; j < (relPath.Count - 1); j++)
                {
                    UInt32 fromAS, tempAS;
                    int rel, temprel, bestRelation;
                    dest.unjoin(relPath[j], out fromAS, out temprel);
                    dest.unjoin(relPath[j + 1], out tempAS, out rel);

                    bestRelation = bestrel(fromAS, ref dest);

                    if (bestRelation == -1) { throw new Exception(); }

                    if (rel > bestRelation)
                    {
                        bestrln = 0;
                        break;
                    }

                }
            }

            //Check for shortest
            if ((GR == 0) && (bestrln == 0) && (pathExists == 1))
            {
                shortest = 1;
                for (int i = 1; i < asPath.Length; i++)
                {
                    List<List<UInt32>> allPaths = new List<List<UInt32>>();
                    List<UInt32> pathNew = new List<UInt32>();
                    UInt32 first = (UInt32)((AS << 3) + Destination._NOREL);
                    pathNew.Add(first);
                    //Console.Out.WriteLine("Checking length: " + i);
                    if (dest.pathoflength(AS, dest.destination, ref allPaths, pathNew, i))
                    {
                        shortest = 0;
                        //Console.Out.WriteLine(i + " BHORSI KA: " + allPaths.Count + " " + dest.pathString(allPaths[0]));
                        break;
                    }
                }
            }

            //space seperated: path bucket
            //bucket -1 -> Export Violation
            //bucket  0 -> Missing Link
            //bucket  1 -> GR
            //bucket  2 -> Best Relation
            //bucket  3 -> Shortest
            //bucket  4 -> None but doesnt violate export

            if (pathExists == 1)
            {
                if (GR == 1)
                {
                    bucket = 1;
                }
                else if (bestrln == 1)
                {
                    bucket = 2;
                }
                else if (shortest == 1)
                {
                    bucket = 3;
                }
                else
                {
                    bucket = 4;
                }
            }
            else
            {
                if (pathViolate == 1)
                {
                    bucket = -1;
                }
                else
                {
                    bucket = 0;
                }
            }
            //Console.Out.WriteLine(path + " " + bucket);
            //Console.WriteLine("Path: " + dest.pathString(relPath) + "\n" + GR_string + "    Exitsts: " + pathExists + "    GR: " + GR + "    BestReln: "+ bestrln + "     Shortest: " + shortest + "    Violate: " + pathViolate);            
            return path + " " + bucket;
        }

        public string pathAnalysis(string path, ref Destination dest, ref TextWriter Missing, ref TextWriter randomFile, ref TextWriter ViolationType)
        {
            int pathExists;
            int pathViolate = 0;
			int GR = 0;
            int bestrln = 0;
			int bestrlnHop = 0;
            int shortest = 0;
			int shortestHop = 0;
            int shortestLen = 0;
            int bucket;
            List<UInt32> relPath = new List<UInt32>();
            
            //Check if path exists from best new and make a relPath
            string[] asPath = path.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            UInt32 AS;
            UInt32.TryParse(asPath[0], out AS);

            if (dest.BestNew[AS] != null)
            {
                pathExists = 1;
                UInt32 first = (UInt32)((AS << 3) + Destination._NOREL);
                relPath.Add(first);

                for (int i = 0; i < (asPath.Length - 1); i++) 
                {
                    int asNum, asTo;
                    int.TryParse(asPath[i], out asNum);
                    int.TryParse(asPath[i + 1], out asTo);

                    if (dest.BestNew[asNum] != null)
                    {
                        UInt32 encoded0 = (UInt32)((asTo << 3) + Destination._PROVIDERCOLUMN);
                        UInt32 encoded1 = (UInt32)((asTo << 3) + Destination._CUSTOMERCOLUMN);
                        UInt32 encoded2 = (UInt32)((asTo << 3) + Destination._PEERCOLUMN);
                        UInt32 encoded3 = (UInt32)((asTo << 3) + Destination._NOREL);

                        if (dest.BestNew[asNum].Exists(element => element == encoded0))
                        {
                            relPath.Add(encoded0);
                        }
                        else if (dest.BestNew[asNum].Exists(element => element == encoded1))
                        {
                            relPath.Add(encoded1);
                        }
                        else if (dest.BestNew[asNum].Exists(element => element == encoded2))
                        {
                            relPath.Add(encoded2);
                        }
                        else if (dest.BestNew[asNum].Exists(element => element == encoded3))
                        {
                            relPath.Add(encoded3);
                        }
                        else
                        {
                            pathExists = 0;
                            break;
                        }

                        // Now do perform a sanity check
                        int size = relPath.Count;
                        int rel, prevRel;
                        UInt32 temp;
                        dest.unjoin(relPath[size-1], out temp, out rel);
                        dest.unjoin(relPath[size-2], out temp, out prevRel);

                        if ((rel == Destination._PEERCOLUMN && prevRel == Destination._CUSTOMERCOLUMN) || (prevRel == Destination._PEERCOLUMN && rel == Destination._PROVIDERCOLUMN) || (prevRel == Destination._PEERCOLUMN && rel == Destination._PEERCOLUMN) || (prevRel == Destination._CUSTOMERCOLUMN && (rel == Destination._PEERCOLUMN || rel == Destination._PROVIDERCOLUMN)))
                        {
                            pathViolate = 1;
                            pathExists = 0;
                            ViolationType.WriteLine(pathString(relPath));
                            ViolationType.Flush();
                            break;
                        }
                    }
                    else
                    {
                        pathExists = 0;
                        break;
                    }
                }
            }
            else
            {
                pathExists = 0;
            }

            //Check hop count for missing link
			/*
			if(pathExists == 0)
            {
                int hopsfromend = 0, hopsfromstart = 0;
                string tempPath = "";

                for (int i = 1; i < asPath.Length; i++)
                {
                    tempPath = "";
                    for (int j = asPath.Length - i; j < asPath.Length; j++)
                    {
                        if (j == (asPath.Length - 1))
                        { tempPath = tempPath + asPath[j]; }
                        else { tempPath = tempPath + asPath[j] + ","; }
                    }
                    
                    //Check if path exists
                    if(!pathAvailable(tempPath, dest.BestNew))
                    {
                        hopsfromend = i;
                        hopsfromstart = asPath.Length - i;
                        break;
                    }
                    else
                    {
                        hopsfromend = i;
                        hopsfromstart = asPath.Length - i;
                    }
                }

                Missing.WriteLine(hopsfromstart + " " + hopsfromend);
                Missing.Flush();
            }
            */

            //GR path and compare
            string GR_string = dest.GetPathCommaSep(AS);
			if (pathAvailable_GR(path, ref dest.Best) && (pathExists == 1)) 
			{
				GR = 1;
			}
            
            //if not GR check for best rel
            if ((GR == 0) && (pathExists == 1))
            {
                bestrln = 1;
                for (int j = 0; j < (relPath.Count - 1); j++)
                {
                    UInt32 fromAS, tempAS;
                    int rel, temprel, bestRelation;
                    dest.unjoin(relPath[j], out fromAS, out temprel);
                    dest.unjoin(relPath[j+1], out tempAS, out rel);

                    bestRelation = bestrel(fromAS, ref dest);

                    if (bestRelation == -1) { throw new Exception(); }

                    if (rel > bestRelation)
                    {
                        bestrln = 0;
                        break;
                    }

                }
            }

			//checking for bestRl for first hop
			if (1 == 1) {
				bestrlnHop = 1;
				UInt32 fromAS, tempAS;
				int rel, temprel, bestRelation;
				dest.unjoin (relPath [0], out fromAS, out temprel);
				dest.unjoin (relPath [1], out tempAS, out rel);

				bestRelation = bestrel (fromAS, ref dest);

				if (bestRelation == -1) {
					throw new Exception ();
				}

				if (rel > bestRelation) {
					bestrlnHop = 0;
				}
			}
            
            //Check for shortest
			if ((GR == 0) && (bestrln == 0) && (pathExists == 1)) {
				shortest = 1;
				shortestLen = asPath.Length -1; // to correct for the next comment
				for (int i = 1; i < asPath.Length-1; i++) { //i=1 essentially means checking for path length 2, therefore the -1
					List<List<UInt32>> allPaths = new List<List<UInt32>> ();
					List<UInt32> pathNew = new List<UInt32> ();
					UInt32 first = (UInt32)((AS << 3) + Destination._NOREL);
					pathNew.Add (first);
					//Console.Out.WriteLine("Checking length: " + i);
					if (dest.pathoflength (AS, dest.destination, ref allPaths, pathNew, i)) {
						shortest = 0;
						shortestLen = i;
						if (shortestLen == 1) {
							shortest = 1;
						}
						//Console.Out.WriteLine ("Path length: " + asPath.Length);
						//Console.Out.WriteLine(i + " shortest: " + shortest+ " B KA: " + allPaths.Count + " " + dest.pathString(allPaths[0]));
						break;
					}
				}
			} else { // Added to compute shortest lenght path for all buckets and for per hop shortest
				shortestLen = asPath.Length-1;
				for (int i = 1; i < asPath.Length; i++) {
					List<List<UInt32>> allPaths = new List<List<UInt32>> ();
					List<UInt32> pathNew = new List<UInt32> ();
					UInt32 first = (UInt32)((AS << 3) + Destination._NOREL);
					pathNew.Add (first);
					//Console.Out.WriteLine ("Checking : " + i);
					if (dest.pathoflength (AS, dest.destination, ref allPaths, pathNew, i)) {
						shortestLen = i;
						break;
					}
				}
			}

			shortestLen = shortestLen + 1; //shortestLen is now the number of AS in shortest path

			if (shortestLen == asPath.Length) {
				shortestHop = 1;
			}

            //space seperated: path bucket
            //bucket -1 -> Export Violation
            //bucket  0 -> Missing Link
            //bucket  1 -> GR
            //bucket  2 -> Best Relation
            //bucket  3 -> Shortest
            //bucket  4 -> None but doesnt violate export

            if (pathExists == 1)
            {
                if (GR == 1)
                {
                    bucket = 1;
                }
                else if (bestrln == 1)
                {
                    bucket = 2;
                }
                else if (shortest == 1)
                {
                    bucket = 3;
                }
                else
                {
                    bucket = 4;
                    //Console.WriteLine("Difference: " + (asPath.Length - shortestLen) + "\n Length: " + asPath.Length + "\nShortest: " + shortestLen);
                    //ShortestDiff.WriteLine((asPath.Length - shortestLen));
                    //ShortestDiff.Flush();
                }
            }
            else
            {
                if (pathViolate == 1)
                {
                    bucket = -1;
                }
                else
                {
                    bucket = 0;
                }
            }
            //Console.Out.WriteLine(path + " " + bucket);
			// Format: Original Path | Bucket | GR Path | Shortest Length | lastHop info : BestRln+Shortest
			randomFile.WriteLine(dest.pathString(relPath) + "|" + bucket + "|" + dest.GetPath(AS) + "|" + shortestLen + "|" + bestrlnHop + "" + shortestHop);
            randomFile.Flush();
            //Console.WriteLine("Path: " + dest.pathString(relPath) + "\n" + GR_string + "    Exitsts: " + pathExists + "    GR: " + GR + "    BestReln: "+ bestrln + "     Shortest: " + shortest + "    Violate: " + pathViolate + "\n");            
            return path + " " + bucket;
        }

        public bool pathAvailable(string path, List<UInt32>[] bestNew)
        {
            string[] asPath = path.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            int AS;
            int.TryParse(asPath[0], out AS);

            if (bestNew[AS] == null)
            {
                return false;
            }

            for (int i = 0; i < (asPath.Length - 1); i++)
            {
                int asNum, asTo;
                int.TryParse(asPath[i], out asNum);
                int.TryParse(asPath[i+1], out asTo);

                if (bestNew[asNum] != null)
                {
                    UInt32 encoded0 = (UInt32)((asTo << 3) + Destination._PROVIDERCOLUMN);
                    UInt32 encoded1 = (UInt32)((asTo << 3) + Destination._CUSTOMERCOLUMN);
                    UInt32 encoded2 = (UInt32)((asTo << 3) + Destination._PEERCOLUMN);
                    UInt32 encoded3 = (UInt32)((asTo << 3) + Destination._NOREL);

                    if (!(bestNew[asNum].Exists(element => element == encoded0) || bestNew[asNum].Exists(element => element == encoded1) || bestNew[asNum].Exists(element => element == encoded2) || bestNew[asNum].Exists(element => element == encoded3)))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        public int bestrel(UInt32 AS, ref Destination dst)
        {
            int rel = -1;

            for (int row = 0; row < dst.BucketTable.GetLength(0); row++)
            {
                for (int col = 0; col < dst.BucketTable[0].GetLength(0); col++)
                {
                    if (dst.BucketTable[row][col] != null)
                    {
                        foreach (UInt32 ASN in dst.BucketTable[row][col])
                        {
                            if (ASN == AS)
                                return col;
                        }
                    }
                }
            }

            return rel;
        }

        private string pathString(List<UInt32> path)
        {
            string toreturn = "";
            toreturn = toreturn + Convert.ToString((UInt32)(((uint)path[0]) >> 3));
            for (int j = 1; j < path.Count; j++)
            {
                UInt32 asn;
                int col;
                asn = (UInt32)(((uint)path[j]) >> 3);
                col = (int)(path[j] & 7);

                switch (col)
                {
                    case 0:
                        toreturn = toreturn + " <- ";
                        break;
                    case 1:
                        toreturn = toreturn + " -- ";
                        break;
                    case 2:
                        toreturn = toreturn + " -> ";
                        break;
                    case 3:
                        break;
                }

                toreturn = toreturn + asn;
            }

            return toreturn;

        }

        public bool pathAvailable_GR(string path, ref List<UInt32>[] bestNew)
        {
            string[] asPath = path.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            int AS;
            int.TryParse(asPath[0], out AS);

            if (bestNew[AS] == null)
            {
                return false;
            }

            for (int i = 0; i < (asPath.Length - 1); i++)
            {
                UInt32 asNum, asTo;
                UInt32.TryParse(asPath[i], out asNum);
                UInt32.TryParse(asPath[i + 1], out asTo);

                if (bestNew[asNum] != null)
                {
                    //Console.WriteLine("DEBUG: asNum -> " + asNum + " asTo -> " + asTo);
                    if (!(bestNew[asNum].Exists(element => element == asTo)))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            return true;
        }
    }

}
