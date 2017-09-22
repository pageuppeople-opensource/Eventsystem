using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using Amazon;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using NSubstitute.Core;

namespace BusinessEvents.SubscriptionEngine.Core.Security
{
    public static class AesEncryptionServiceStringExtension
    {
        private static string _encryptionKey = string.Empty;
        private static readonly byte[] SaltBytes = { 105, 117, 113, 176, 123,  74, 170, 157 };

        public static void SetEncryption()
        {
            if (!string.IsNullOrEmpty(_encryptionKey)) return;

            var kmsClient = new AmazonKeyManagementServiceClient(RegionEndpoint.GetBySystemName(Environment.GetEnvironmentVariable("AWS_REGION")));
            var decryptResponse = kmsClient.DecryptAsync(new DecryptRequest()
            {
                CiphertextBlob = new MemoryStream(Convert.FromBase64String(Environment.GetEnvironmentVariable("AWS_REGION")))
            }).Result;

            _encryptionKey = Convert.ToBase64String(decryptResponse.Plaintext.ToArray());
        }

        public static string Encrypt(this string input)
        {
            SetEncryption();

            if (string.IsNullOrWhiteSpace(input))
                return input;

            // Get the bytes of the string
            var bytesToBeEncrypted = Encoding.UTF8.GetBytes(input);

            string result;
            using (var aes = AesCreator(_encryptionKey, SaltBytes))
            {
                var bytesEncrypted = AES_EncryptDecrypt(bytesToBeEncrypted, aes.CreateEncryptor());
                result = Convert.ToBase64String(bytesEncrypted);
            }

            return result;
        }

        public static string Decrypt(this string input)
        {
            SetEncryption();

            if (string.IsNullOrWhiteSpace(input))
                return input;

            // Get the bytes of the string
            var bytesToBeDecrypted = Convert.FromBase64String(input);

            string result;
            using (var aes = AesCreator(_encryptionKey, SaltBytes))
            {
                var bytesDecrypted = AES_EncryptDecrypt(bytesToBeDecrypted, aes.CreateDecryptor());
                result = Encoding.UTF8.GetString(bytesDecrypted);
            }

            return result;
        }


        private static byte[] AES_EncryptDecrypt(byte[] inputBytes, ICryptoTransform createEncryptor)
        {
            byte[] decryptedBytes;

            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, createEncryptor, CryptoStreamMode.Write))
                {
                    cs.Write(inputBytes, 0, inputBytes.Length);
                    cs.Dispose();
                }
                decryptedBytes = ms.ToArray();
            }

            return decryptedBytes;
        }


        private static Aes AesCreator(string encryptionKey, byte[] saltBytes)
        {
            var encryptionKeyBytes = Encoding.UTF8.GetBytes(encryptionKey);

            // Hash the encryptionKey with SHA256
            encryptionKeyBytes = SHA256.Create().ComputeHash(encryptionKeyBytes);

            var aes = Aes.Create();
            // ReSharper disable once PossibleNullReferenceException
            aes.KeySize = 256;
            aes.BlockSize = 128;

            var key = new Rfc2898DeriveBytes(encryptionKeyBytes, saltBytes, 1000);
            aes.Key = key.GetBytes(aes.KeySize / 8);
            aes.IV = key.GetBytes(aes.BlockSize / 8);
            aes.Mode = CipherMode.CBC;

            return aes;
        }
    }
}
