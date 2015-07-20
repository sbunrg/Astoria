using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;
//using SecureSimulator;

namespace CloudLibrary
{

    public static class BlobLibrary
    {


        private const  string MimeTypeName = "text/plain"; // since these are assumed to 
      
        private const  string AccountKey = AccountInfo.AccountKey;  // use account settings from class.
        private const string AccountName = AccountInfo.AccountName;


//getobjectfromblob and put object from blob use crappy slow serialization.
        public static Object getObjectFromBlob(string containerName, string blobName)
        {
            string baseUri="";
            CloudBlobContainer blobContainer = initContainer(containerName,ref baseUri);
            if (!Exists(blobContainer))
                return null;

            string blobUriPath = blobName; // could also use paths, as in: “images/” + fileInfo.Name;
            CloudBlob blob = blobContainer.GetBlobReference(blobUriPath);
            if (!Exists(blob))
                return null;
            byte[] blobContents=blob.DownloadByteArray();
        
            Object toreturn = Serialize.deserializeObject(blobContents);
            return toreturn;

       
        }

      


        public static BlobInfo putObjectInBlob(string containerName, string blobName, Object o)
        {
            string baseUri = "";
            CloudBlobContainer blobContainer = initContainer(containerName,ref baseUri);
            
            bool didNotExistCreated = blobContainer.CreateIfNotExist();

            byte[] blobContent = Serialize.serializeObject(o);

            var perms = new BlobContainerPermissions
            {
                PublicAccess = BlobContainerPublicAccessType.Container // Blob (see files if you know the name) or Container (enumerate like a directory)
            };
            blobContainer.SetPermissions(perms); // This line makes the blob public so it is available from a web browser (no magic needed to read it)

            string blobUriPath = blobName; // could also use paths, as in: “images/” + fileInfo.Name;
            CloudBlob blob = blobContainer.GetBlobReference(blobUriPath);
            blob.DeleteIfExists();

            //write serialized object to our blob.
            blob.UploadByteArray(blobContent);

            blob.Properties.ContentType = MimeTypeName; // IMPORTANT: Mime Type here needs to match type of the uploaded file
                                         // e.g., *.png <=> image/png, *.wmv <=> video/x-ms-wmv (http://en.wikipedia.org/wiki/Internet_media_type)
            blob.SetProperties(); // REST call under the hood

            blob.Metadata["WhenFileUploadedUtc"] = DateTime.UtcNow.ToLongTimeString();
            blob.SetMetadata(); // REST call under the hood

          string url = String.Format("{0}/{1}/{2}", baseUri, containerName, blobUriPath);
            BlobInfo toreturn = new BlobInfo(url, containerName, blobName);
            return toreturn;

        }

  


        public static BlobStream getBlobWriteStream(string containerName, string blobName)
        {
            /*** set up the blob for writing ***/
            string baseURI = "";
            CloudBlobContainer blobContainer = BlobLibrary.initContainer(containerName, ref baseURI);
            bool didNotExistCreated = blobContainer.CreateIfNotExist();

            var perms = new BlobContainerPermissions
            {
                PublicAccess = BlobContainerPublicAccessType.Container // Blob (see files if you know the name) or Container (enumerate like a directory)
            };
            blobContainer.SetPermissions(perms); // This line makes the blob public so it is available from a web browser (no magic needed to read it)

            string blobUriPath = blobName; // could also use paths, as in: “images/” + fileInfo.Name;
            CloudBlob blob = blobContainer.GetBlobReference(blobUriPath);
            blob.DeleteIfExists();
            return blob.OpenWrite();

        }
        public static BlobStream getBlobReadStream(string containerName, string blobName)
        {
            /**** set up the blob for reading ****/
            string baseUri = "";
            CloudBlobContainer blobContainer = BlobLibrary.initContainer(containerName, ref baseUri);
            if (!BlobLibrary.Exists(blobContainer))
                return null;

            string blobUriPath = blobName; // could also use paths, as in: “images/” + fileInfo.Name;
            CloudBlob blob = blobContainer.GetBlobReference(blobUriPath);
            if (!BlobLibrary.Exists(blob))
                return null;
            return blob.OpenRead();
        }

        public static void logToBlog(string toLog, string blobName)
        {
            string baseUri = "";
            CloudBlobContainer blobContainer = initContainer(AccountInfo.LogContainer,ref baseUri);
            blobContainer.CreateIfNotExist();
            var perms = new BlobContainerPermissions
            {
                PublicAccess = BlobContainerPublicAccessType.Container // Blob (see files if you know the name) or Container (enumerate like a directory)
            };
            blobContainer.SetPermissions(perms); // This line makes the blob public so it is available from a web browser (no magic needed to read it)
            CloudBlob blob = blobContainer.GetBlobReference(blobName);

            appendToBlob(blob, toLog);
            
        }

        /// <summary>
        /// ugliness of downloading and reuploading whole blob.
        /// </summary>
        /// <param name="blob"></param>
        /// <param name="toAdd"></param>
        public static void appendToBlob(CloudBlob blob, string toAdd)
        {
            string oldLogData = "";
            if (Exists(blob))
                oldLogData = blob.DownloadText();
            blob.DeleteIfExists();
            blob.UploadText(oldLogData + "\r\n" + toAdd);
        }

        /// <summary>
        /// deletes the contents of a logging blob.
        /// </summary>
        /// <param name="blobName"></param>
        public static void clearLogBlob(string blobName)
        {
            string baseUri = "";
            CloudBlobContainer blobContainer = initContainer(AccountInfo.LogContainer, ref baseUri);
            blobContainer.CreateIfNotExist();
            var perms = new BlobContainerPermissions
            {
                PublicAccess = BlobContainerPublicAccessType.Container // Blob (see files if you know the name) or Container (enumerate like a directory)
            };
            blobContainer.SetPermissions(perms); // This line makes the blob public so it is available from a web browser (no magic needed to read it)
            CloudBlob blob = blobContainer.GetBlobReference(blobName);
            blob.DeleteIfExists();
        }

        public static void downloadContainer(string containerName, string localDirectory)
        {
            char[] slash = { '/' };
            string baseUri = "";
            CloudBlobContainer blobContainer = initContainer(containerName, ref baseUri);
            if (!Exists(blobContainer))
                return;

            if (!Directory.Exists(localDirectory))
                Directory.CreateDirectory(localDirectory);
            //make sure we have slashed on our directory name.
            if (localDirectory[localDirectory.Length - 1] != '\\')
                localDirectory = localDirectory + "\\";

            var blobs = blobContainer.ListBlobs();

            foreach (var blob in blobs)
            {

                string blobURI = blob.Uri.ToString();
                string[] blobURIPieces = blobURI.Split(slash, StringSplitOptions.RemoveEmptyEntries);
                string blobName = blobURIPieces[blobURIPieces.Length - 1];
                StreamReader blobInput = new StreamReader(getBlobReadStream(containerName, blobName));
                StreamWriter localOutput = new StreamWriter(localDirectory + blobName);
                localOutput.Write(blobInput.ReadToEnd());
                blobInput.Close();
                localOutput.Close();
            }

        }

        public static void deleteContainer(string containerName)
        {
            char[] slash = { '/' };
            string baseUri = "";
            CloudBlobContainer blobContainer = initContainer(containerName, ref baseUri);
            if (!Exists(blobContainer))
                return;

            blobContainer.Delete();
        }

        public static void downloadBlob(string containerName, string blobName, string localDirectory)
        {
            char[] slash = { '/' };
            string baseUri = "";
            CloudBlobContainer blobContainer = initContainer(containerName, ref baseUri);
            if (!Exists(blobContainer))
                return;

            if (!Directory.Exists(localDirectory))
                Directory.CreateDirectory(localDirectory);
            //make sure we have slashed on our directory name.
            if (localDirectory[localDirectory.Length - 1] != '\\')
                localDirectory = localDirectory + "\\";

            StreamReader blobInput = new StreamReader(getBlobReadStream(containerName, blobName));
            StreamWriter localOutput = new StreamWriter(localDirectory + blobName);
            localOutput.Write(blobInput.ReadToEnd());
            blobInput.Close();
            localOutput.Close();


        }

        public static void deleteBlob(string containerName, string blobName)
        {
            char[] slash = { '/' };
            string baseUri = "";
            CloudBlobContainer blobContainer = initContainer(containerName, ref baseUri);
            if (!Exists(blobContainer))
                return;


            CloudBlob blob = blobContainer.GetBlobReference(blobName);
            blob.Delete();
        
        }

        public static List<string> listBlobs(string containerName)
        {
            List<string> toreturn = new List<string>();
            string baseUri = "";
            char[] slash = { '/' };
            CloudBlobContainer blobContainer = initContainer(containerName, ref baseUri);
            if (!Exists(blobContainer))
                return new List<string>();

            var blobs = blobContainer.ListBlobs();
            foreach (var blob in blobs)
            {

                string blobURI = blob.Uri.ToString();
                string[] blobURIPieces = blobURI.Split(slash, StringSplitOptions.RemoveEmptyEntries);
                string blobName = blobURIPieces[blobURIPieces.Length - 1];
                toreturn.Add(blobName);

            }

            return toreturn;

        }

        /// <summary>
        /// this function actually issues the HTTP request to grab out the list
        /// of containers because i could not find any documentation on how to
        /// do this with the API
        /// </summary>
        /// <returns></returns>
        public static List<string> listContainers()
        {
  
            List<string> toreturn = new List<string>();
         
            string RESTUri="http://"+AccountInfo.AccountName+".blob.core.windows.net/?comp=list";
            string AzureResponseXML = HTTPUtil.issueAzureHTTPRequest(RESTUri);
            while (AzureResponseXML[0] == '?')//chop off any leading '?'s
            {
                AzureResponseXML = AzureResponseXML.Substring(1, AzureResponseXML.Length - 1);
            }


            XmlTextReader xmlReader = new XmlTextReader(new StringReader(AzureResponseXML));
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(xmlReader);
            XmlNodeList containerNameNodes = xmlDocument.GetElementsByTagName("Name");
            foreach (XmlNode containerNameNode in containerNameNodes)
                toreturn.Add(containerNameNode.InnerText);
            return toreturn;

        }

        public static CloudBlobContainer initContainer(string containerName, ref string baseUri)
        {
            baseUri = null;
            CloudBlobClient blobStorage = null;

            if (AccountKey == null)
            {
                var clientStorageAccount = CloudStorageAccount.DevelopmentStorageAccount; // use storage services in the Developer Fabric, not real cloud
                baseUri = clientStorageAccount.BlobEndpoint.AbsoluteUri;
                blobStorage = new CloudBlobClient(baseUri, clientStorageAccount.Credentials);
            }
            else
            {
                byte[] key = Convert.FromBase64String(AccountKey);
                var creds = new StorageCredentialsAccountAndKey(AccountName, key);
                baseUri = string.Format("http://{0}.blob.core.windows.net", AccountName);
                blobStorage = new CloudBlobClient(baseUri, creds);
            }
            
            return blobStorage.GetContainerReference(containerName);
        }

        public static bool Exists( CloudBlob blob)
        {
            try
            {
                blob.FetchAttributes();
                return true;
            }
            catch (StorageClientException e)
            {
                if (e.ErrorCode == StorageErrorCode.ResourceNotFound)
                {
                    return false;
                }
                else
                {
                    throw;
                }
            }
        }

        public static bool Exists(CloudBlobContainer blob)
        {
            try
            {
                blob.FetchAttributes();
                return true;
            }
            catch (StorageClientException e)
            {
                if (e.ErrorCode == StorageErrorCode.ResourceNotFound)
                {
                    return false;
                }
                else
                {
                    throw;
                }
            }
        }


        public static bool Exists(string containerName, string blobName)
        {
            /**** init blob ****/
            string baseUri = "";
            CloudBlobContainer blobContainer = BlobLibrary.initContainer(containerName, ref baseUri);
            if (!BlobLibrary.Exists(blobContainer))
                return false;

            string blobUriPath = blobName; // could also use paths, as in: “images/” + fileInfo.Name;
            CloudBlob blob = blobContainer.GetBlobReference(blobUriPath);
            if (!BlobLibrary.Exists(blob))
                return false;

            return true;
        }
    }
}
