using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SecureSimulator
{
    public struct MiniDestination
    {
        public List<UInt32>[][] BucketTable;
        public List<UInt32>[] Best;
        public List<UInt32>[] BestNew; //Added by Haseeb
        public byte[] BestRelation;
        public byte[] L;
        public float[] W;//May 3 - adding W to miniDests.
        public UInt32 destination;
    }

    [Serializable]
    public struct GlobalState
    {
        public bool[] S;
        public UInt16[] W;
        public List<UInt32> nonStubs;
    }

    [Serializable]
    public struct Message
    {
        public Int64[] UBefore;//results of the utility computation.
        public Int64[] UAfter;//results of utility with S~n

    }

    [Serializable]
    public struct nodeStateUtility
    {
        public nodeStateUtility(Int32 node, bool state, Int64 before, Int64 after)
        {
            this.node = node;
            this.state = state;
            this.before = before;
            this.after = after;
        }
        public Int32 node;
        public bool state;
        public Int64 before;
        public Int64 after;
    }

}
