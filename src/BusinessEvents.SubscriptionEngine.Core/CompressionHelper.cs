using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace BusinessEvents.SubscriptionEngine.Core
{
    public static class CompressionHelper
    {
        /// <summary>
        /// Returns the byte array of a compressed string
        /// </summary>
        public static byte[] ToCompressedByteArray(this string source)
        {
            // convert the source string into a memory stream
            using (
                MemoryStream inMemStream = new MemoryStream(Encoding.UTF8.GetBytes(source)),
                    outMemStream = new MemoryStream())
            {
                // create a compression stream with the output stream
                using (var zipStream = new GZipStream(outMemStream, CompressionMode.Compress, true))
                    // copy the source string into the compression stream
                    inMemStream.WriteTo(zipStream);

                // return the compressed bytes in the output stream
                return outMemStream.ToArray();
            }
        }
        /// <summary>
        /// Returns the base64 encoded string for the compressed byte array of the source string
        /// </summary>
        public static string ToCompressedBase64String(this string source)
        {
            return Convert.ToBase64String(source.ToCompressedByteArray());
        }

        /// <summary>
        /// Returns the original string for a compressed base64 encoded string
        /// </summary>
        public static string ToUncompressedString(this string source)
        {
            // get the byte array representation for the compressed string
            var compressedBytes = Convert.FromBase64String(source);

            // load the byte array into a memory stream
            using (var inMemStream = new MemoryStream(compressedBytes))
                // and decompress the memory stream into the original string
            using (var decompressionStream = new GZipStream(inMemStream, CompressionMode.Decompress))
            using (var streamReader = new StreamReader(decompressionStream))
                return streamReader.ReadToEnd();
        }
    }
}