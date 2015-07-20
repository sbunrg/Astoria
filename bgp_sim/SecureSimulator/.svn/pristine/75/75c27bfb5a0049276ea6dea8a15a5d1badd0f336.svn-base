using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SecureSimulator
{
    public enum UtilityComputationType
    {
         basic=0,
     incoming = 1,
     outgoing = 2,
        source=3,
    }


    
    public class UtilityComputations
    {
        public delegate Int32 ComputeUtility(Int32 CustomerTreeSize, Int32 CustomerWeightedTreeSize,
            Int32 PeerWeightedTreeSize, Int32 ProviderWeightedTreeSize, byte BestRelation,UInt16 dWeight);

      

        public static Int32 basicUtility(Int32 CustomerTreeSize, Int32 CustomerWeightedTreeSize, Int32 PeerWeightedTreeSize, Int32 ProviderWeightedTreeSize, byte BestRelation,UInt16 dWeight)
        {
            return CustomerTreeSize;
        }

        public static Int32 incomingUtility(Int32 CustomerTreeSize, Int32 CustomerWeightedTreeSize, Int32 PeerWeightedTreeSize, Int32 ProviderWeightedTreeSize, byte BestRelation,UInt16 dWeight)
        {

            return CustomerTreeSize*dWeight;
        }

        public static Int32 outgoingUtility(Int32 CustomerTreeSize, Int32 CustomerWeightedTreeSize, Int32 PeerWeightedTreeSize, Int32 ProviderWeightedTreeSize, byte BestRelation, UInt16 dWeight)
        {
            if (BestRelation != Destination._CUSTOMERCOLUMN)/*path to d isn't through customer.*/
                return 0;
            return (CustomerWeightedTreeSize + PeerWeightedTreeSize + ProviderWeightedTreeSize);
        }

        public static Int32 sourceUtility(Int32 CustomerTreeSize, Int32 CustomerWeightedTreeSize, Int32 PeerWeightedTreeSize, Int32 ProviderWeightedTreeSize, byte BestRelation, UInt16 dWeight)
        {
            
            return CustomerWeightedTreeSize;
        }
    }
}
