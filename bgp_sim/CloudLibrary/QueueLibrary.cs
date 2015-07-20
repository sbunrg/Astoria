using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure;
//using SecureSimulator;
using System.Diagnostics;

namespace CloudLibrary
{
   public class QueueLibrary
    {
          private const string AzureStorageKey = AccountInfo.AccountKey;
        
        private const string AccountName = AccountInfo.AccountName;

      public  static CloudQueue InitializeQueue(string queueName)
        {
            CloudQueueClient queueStorage = null;

            if (AzureStorageKey == null)
            {
                var clientStorageAccount = CloudStorageAccount.DevelopmentStorageAccount;
                queueStorage = new CloudQueueClient(clientStorageAccount.QueueEndpoint.AbsoluteUri, clientStorageAccount.Credentials);
            }
            else
            {
                byte[] key = Convert.FromBase64String(AzureStorageKey);
                var creds = new StorageCredentialsAccountAndKey(AccountName, key);
                queueStorage = new CloudQueueClient(String.Format("http://{0}.queue.core.windows.net", AccountName), creds);
            }

            CloudQueue queue = queueStorage.GetQueueReference(queueName);
            queue.CreateIfNotExist();
            return queue;
        }


    }
}
