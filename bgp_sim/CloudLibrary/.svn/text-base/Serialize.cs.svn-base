using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace CloudLibrary
{
   public static class Serialize
    {
        public static byte[] serializeObject(Object o)
        {

            BinaryFormatter formatter = new BinaryFormatter();
            MemoryStream stream = new MemoryStream();
            formatter.Serialize(stream, o);
            byte[] toreturn = stream.ToArray();

            // System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();

            return toreturn;// encoding.GetBytes(command.commandString);
        }

        public static Object deserializeObject(byte[] blobContents)
        {
            BinaryFormatter formatter = new BinaryFormatter();

            MemoryStream stream = new MemoryStream(blobContents);
            Object fromBlob = formatter.Deserialize(stream);

            return fromBlob;
        }
    }
}
