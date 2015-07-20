using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Security.Cryptography;
using System.Globalization;

namespace CloudLibrary
{
    /// <summary>
    /// incase of emergency this can be used to send http commands
    /// to use the azure rest interface.
    /// </summary>
    static class HTTPUtil
    {

        public static string issueHTTPRequest(string URI)
        {
            /** code borrowed & adapted from: http://www.csharp-station.com/HowTo/HttpWebFetch.aspx **/

            // used to build entire input
            StringBuilder sb = new StringBuilder();

            // used on each read operation
            byte[] buf = new byte[8192];

            // prepare the web page we will be asking for
            HttpWebRequest request = (HttpWebRequest)
                WebRequest.Create(URI);
            
            // execute the request
            HttpWebResponse response = (HttpWebResponse)
                request.GetResponse();

            // we will read data via the response stream
            Stream resStream = response.GetResponseStream();

            string tempString = null;
            int count = 0;

            do
            {
                // fill the buffer with data
                count = resStream.Read(buf, 0, buf.Length);

                // make sure we read some data
                if (count != 0)
                {
                    // translate from bytes to ASCII text
                    tempString = Encoding.ASCII.GetString(buf, 0, count);

                    // continue building the string
                    sb.Append(tempString);
                }
            }
            while (count > 0); // any more data to read?

            // print out page source
            return sb.ToString();

        }


        public static string issueAzureHTTPRequest(string URI)
        {
            /** code borrowed & adapted from: http://www.csharp-station.com/HowTo/HttpWebFetch.aspx **/

            // used to build entire input
            StringBuilder sb = new StringBuilder();

            // used on each read operation
            byte[] buf = new byte[8192];


            // prepare the web page we will be asking for
            HttpWebRequest request = (HttpWebRequest)
                WebRequest.Create(URI);

              String dateInRfc1123Format = DateTime.UtcNow.ToString("R", CultureInfo.InvariantCulture);

            WebHeaderCollection myWebHeaders = request.Headers;
            myWebHeaders.Add("x-ms-version","2009-09-19");
            myWebHeaders.Add("x-ms-date",dateInRfc1123Format);
            //do SHA-256 hash of key
            string toSign =
"GET\n\n\n\n\n\n\n\n\n\n\n\nx-ms-date:"+dateInRfc1123Format+"\nx-ms-version:2009-09-19\n/"+AccountInfo.AccountName+"/\ncomp:list";    /*CanonicalizedResource*/


            byte[] signBytes = System.Text.Encoding.UTF8.GetBytes(toSign);
            byte[] hash = new HMACSHA256(Convert.FromBase64String(AccountInfo.AccountKey)).ComputeHash(signBytes);
            string signature = System.Convert.ToBase64String(hash);
            string authHeaderValue = string.Format(CultureInfo.InvariantCulture, "SharedKey {0}:{1}", AccountInfo.AccountName, signature);

            myWebHeaders.Add("Authorization",authHeaderValue);
     

            // execute the request
            HttpWebResponse response = (HttpWebResponse)
                request.GetResponse();

            // we will read data via the response stream
            Stream resStream = response.GetResponseStream();

            string tempString = null;
            int count = 0;

            do
            {
                // fill the buffer with data
                count = resStream.Read(buf, 0, buf.Length);

                // make sure we read some data
                if (count != 0)
                {
                    // translate from bytes to ASCII text
                    tempString = Encoding.ASCII.GetString(buf, 0, count);

                    // continue building the string
                    sb.Append(tempString);
                }
            }
            while (count > 0); // any more data to read?

            // print out page source
            return sb.ToString();

        }

        private static String CreateAuthorizationHeader(String canonicalizedString)
{
    String signature = string.Empty;
    System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
    Byte[] keyBytes = encoding.GetBytes(AccountInfo.AccountKey);


    using (HMACSHA256 hmacSha256 = new HMACSHA256(keyBytes))
    {
        Byte[] dataToHmac = System.Text.Encoding.UTF8.GetBytes(canonicalizedString);
        signature = Convert.ToBase64String(hmacSha256.ComputeHash(dataToHmac));
    }

    String authorizationHeader = String.Format(
          CultureInfo.InvariantCulture,
          "Authorization: SharedKey {0}:{1}",
         
          AccountInfo.AccountName,
          signature);

    return authorizationHeader;
}



    }
}
