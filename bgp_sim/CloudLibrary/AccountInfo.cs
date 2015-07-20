using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudLibrary
{
    public static class AccountInfo
    {
        #if false
        public const string AccountKey = null;//Put a real Storage Account Key – find it on http://windows.azure.com dev portal for your Storage Service”;
        public const string AzureStorageKey = null;
        #else
        //you will see these when you deploy the storage service in the developers portal.
        public const string AccountKey = "Hy7bicYE7PFM+IQbjErPd2bhDqvzsXjed+NkfW80IvMsEidTi1/lo8laHuP3rg3rjhSX8eqfhknSt594Tx1oyg==";
       
        public const string AccountKeyOld = "FLpPMgVaF60fgYA2/a7d58lnUYK82G/UsBhgmKEdKKrst3nx5LuFihYYPbsw/saCSRERN6foG1ccBbG7y6YBlQ==";  // use local storage in the Dev Fabric
        public const string AzureStorageKeyOld = "FLpPMgVaF60fgYA2/a7d58lnUYK82G/UsBhgmKEdKKrst3nx5LuFihYYPbsw/saCSRERN6foG1ccBbG7y6YBlQ==";
        #endif

        public const string AccountName = "6732storage1";

        /** container names **/
        public const string LogContainer = "logs"; //holds output logs
        public const string GraphContainer = "graphs";//holds graph text files
        public const string DestinationContainer = "destinations";//holds destination objects
        public const string testContainer = "testing";
        public const string StateContainer = "state";
        public const string MessageContainer = "messages";
    
        /** blob names **/
        //state blobs live in GlobalState-t.txt in statecontainer
        public const string masterLog = "master.txt";
        public const string servantLog = "servant.txt";
      
        public const string errorBlob = "errors.txt";

        /** queue names **/
        public const string MasterToServant = "mastertoservant";
        public const string ServantToMaster = "servanttomaster";
        public const string CLIToMaster = "clitomaster";
        public const string MasterToCLI = "mastertocli";
      
       
    }
}
