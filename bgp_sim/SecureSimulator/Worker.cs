using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SecureSimulator
{
    public  class Worker
    {

         public UInt32[] ChosenParent;
         public bool[] SecP;

         const byte CustomerOf = 1;
         const byte PeerOf = 2;
         const byte ProviderTo = 3;

         List<int> columns;

         public Worker()
         {
             columns = new List<int>();

             columns.Add(Destination._CUSTOMERCOLUMN);
             columns.Add(Destination._PEERCOLUMN);
             columns.Add(Destination._PROVIDERCOLUMN);
         }
        /// <summary>
        /// implements the recomputing utility function. figures out what the utility for n would be if n flipped its state
        /// from its present state in S.
        /// </summary>
        /// <param name="BucketTable"></param>
        /// <param name="Best"></param>
        /// <param name="ChosenParent"></param>
        /// <param name="SecP"></param>
        /// <param name="S"></param>
        /// <param name="n"></param>
        /// <param name="L"></param>
        /// <param name="BestRelation"></param>
        /// <param name="W"></param>
        /// <returns></returns>
         public int ComputeUtility(List<UInt32>[][] BucketTable, List<UInt32>[] Best, UInt32[] param_ChosenParent, bool[] param_SecP, bool[] S, UInt32 n, int L, byte BestRelation, UInt16[] W)
         {
             if (L == 0)
                 return 0;//no utility for routing to itself

             int UNTilda = 0;//utility for S with n's state flipped

             Int32 CustomerTreeSize = 0;
             Int32 CustomerWeightedTreeSize = 0;
             Int32 PeerWeightedTreeSize = 0;
             Int32 ProviderWeightedTreeSize = 0;

             //DON'T LET US OVERWRITE THINGS; we can overwrite S and flip it back at the end.
             ChosenParent = (UInt32[])param_ChosenParent.Clone();
             SecP = (bool[])param_SecP.Clone();

             S[n] = !S[n];//reverse n's state (we revert this before we return).

             //update n's path. if it has options
             if (Best[n].Count > 1)
             {
                 if (S[n]) //n became secure it cares about security in picking its path
                     updateParentWorker(n, ref Best, ref S, ref SecP, ref ChosenParent);//, ref tieBreakSet);
                 else
                 {
                     //n became insecure; re-evaluate its path options from the set of all paths.
                     UInt32 newParent = Best[n][0];
                     ChosenParent[n] = newParent;
                 }
             }
          //   if (S[n])//can only have secP if n is secure. (using an and so we revoke SecP for n flipping back.
                 SecP[n] = S[n]&SecP[ChosenParent[n]];

             byte[] throughN = new byte[Constants._numASNs];

             for (int i = 0; i < Constants._numASNs; i++)
                 throughN[i] = 0;//empty value.

            

             for (int row = L + 1; row < BucketTable.GetLength(0); row++)
             {
                 foreach (int col in columns)
                 {
                     if (BucketTable[row][col] == null)
                         continue;
                     foreach (UInt32 i in BucketTable[row][col])
                     {
                         /*only secure nodes will change their parents based on security. We still need to update
                          * whether or not they gothrough n though because someone before them may have changed to go 
                          * through n*/
                         if (Best[i].Count > 1)//update path *only* if you have options.
                             updateParentWorker(i, ref Best, ref S, ref SecP, ref ChosenParent);//, ref tieBreakSet);

                         if (S[i])//only say your path is secure if you are secure
                             SecP[i] = SecP[ChosenParent[i]];

                         /* done updating our parents need to update whether we go through n or not */

                         if (row == L + 1 && ChosenParent[i] == n && col == Destination._PROVIDERCOLUMN)
                             throughN[i] = CustomerOf;//i is a customer of N
                         else if (row == L + 1 && ChosenParent[i] == n && col == Destination._PEERCOLUMN)
                             throughN[i] = PeerOf;
                         else if (row == L + 1 && ChosenParent[i] == n && col == Destination._CUSTOMERCOLUMN)
                             throughN[i] = ProviderTo;//i is a provider to N 
                         else if (row > (L + 1))
                             throughN[i] = throughN[ChosenParent[i]];

                         //update utility values on how we pass through n
                         switch (throughN[i])
                         {
                             case CustomerOf:
                                 CustomerTreeSize++;
                                 CustomerWeightedTreeSize += W[i];
                                 break;
                             case PeerOf:
                                 PeerWeightedTreeSize += W[i];
                                 break;
                             case ProviderTo:
                                 ProviderWeightedTreeSize += W[i];
                                 break;
                         }


                     }
                 }
             }

             S[n] = !S[n];//flip n back
             UInt16 dWeight = W[BucketTable[0][ 0][0]];
             UNTilda = SimulatorLibrary.utilityComputation(CustomerTreeSize,
                 CustomerWeightedTreeSize,
                 PeerWeightedTreeSize,
                 ProviderWeightedTreeSize,
                 BestRelation,dWeight);

             return UNTilda;
         }

        /// <summary>
         ///       //this function assumes that Best's are sorted according to the tiebreak rule (hash or AS number)
         //so the first secure entry, or first entry in general "wins"
        /// </summary>
        /// <param name="i"></param>
        /// <param name="Best"></param>
        /// <param name="S"></param>
        /// <param name="SecP"></param>
        /// <param name="ChosenParent"></param>
         private void updateParentWorker(UInt32 i, ref List<UInt32>[] Best, ref bool[] S, ref bool[] SecP, ref UInt32[] ChosenParent)
         {
             //this function assumes that Best's are sorted according to the tiebreak rule (hash or AS number)
             //so the first secure entry, or first entry in general "wins"

             if (S[i])
             {

                 foreach (UInt32 parent in Best[i])
                 {

                     //Nov. 19 mod:
                     if (SecP[parent])//this is our new parent. 
                     {
                         ChosenParent[i] = parent;
                         return;

                     }
                 }


                 //if we made it here nobody was secure. Nov 19 mod.
                 ChosenParent[i] = Best[i][0];

             }
             //  else 
             /** S[i]=false this does not need to be computed all the time!!! 
                   * if I am not secure now, I wasn't secure before 
                   * my decision will not change
                   * This only needs to be computed for the n that is being flipped. **/
             /*  {
                    //we are not secure, figure out who we should have as parents
                    tieBreakSet = Best[i];
                    int newParent = ModifiedBfs.tieBreak(tieBreakSet);
                    SecP[i] = false; // we are not secure we cannot have a SecP 
                    ChosenParent[i] = newParent;
                }*/

             //debug
             // printTieBreakSet(tieBreakSet);
         }

        /// <summary>
        /// debugging function to print the path for a node n through its parents
        /// </summary>
        /// <param name="n"></param>
        public string GetPath(UInt32 n)
        {
            try
            {
                string toprint = "";

                UInt32 tmp = n;
                while (tmp != ChosenParent[tmp])
                {

                    toprint = toprint + tmp + " -?- ";
                    tmp = ChosenParent[tmp];
                }
                toprint = toprint + tmp;
                return toprint;
            }
            catch (Exception e)
            {

            }
            return "an error was encountered generating path for " + n;
        }

        /// <summary>
        /// debugging function to tell us the parent of a node
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public string GetParent(int n)
        {
            return Convert.ToString(ChosenParent[n]);
        }

        /// <summary>
        /// debugging function to tell us if a node has a secure path
        /// to the destination.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public string GetSecP(int n)
        {
            if (SecP[n])
                return "true";
            return "false";
        }


    }
}
