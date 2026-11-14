// Author : xuejie.guo
using System;
using System.IO;
using System.Security.Cryptography;

namespace MythMerge.Save
{
    public class RijndaelManager : Manager<RijndaelManager>
    {
        byte[] rijndaelKey = new byte[] { 0xC0, 0xBE, 0xD5, 0x25, 0xD4, 0x86, 0xFB, 0x1F, 0x62, 0xC1, 0x2A, 0xA6, 0xB2, 0xD5, 0x89, 0x7B, 0x9D, 0x06, 0x46, 0x1D, 0xBF, 0x71, 0xAE, 0x2D, 0x77, 0xCA, 0xF5, 0x83, 0xAE, 0xA7, 0xA9, 0x86 };
        public byte[] RijndaelKey
        {
            get
            {
                return rijndaelKey;
            }
            set
            {
                rijndaelKey = value;
            }
        }

        byte[] rijndaelIV = new byte[] { 0xE6, 0x3A, 0x28, 0xFA, 0x9D, 0x0F, 0xDC, 0x8B, 0x61, 0xB1, 0x5E, 0x7A, 0x63, 0xB9, 0x06, 0x48 };
        public byte[] RijndaelIV
        {
            get
            {
                return rijndaelIV;
            }
            set
            {
                rijndaelIV = value;
            }
        }

        public byte[] EncryptBytesToBytes(byte[] plainBytes)
        {
            // Check arguments.
            if (plainBytes == null || plainBytes.Length <= 0)
                throw new ArgumentNullException("plainBytes");
            if (RijndaelKey == null || RijndaelKey.Length <= 0)
                throw new ArgumentNullException("Key");
            if (RijndaelIV == null || RijndaelIV.Length <= 0)
                throw new ArgumentNullException("IV");
            byte[] encrypted;
            // Create an RijndaelManaged object
            // with the specified key and IV.
            using (RijndaelManaged rijAlg = new RijndaelManaged())
            {
                rijAlg.Key = RijndaelKey;
                rijAlg.IV = RijndaelIV;

                // Create an encryptor to perform the stream transform.
                ICryptoTransform encryptor = rijAlg.CreateEncryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (BinaryWriter swEncrypt = new BinaryWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(plainBytes);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }

            // Return the encrypted bytes from the memory stream.
            return encrypted;
        }

        public byte[] EncryptStringToBytes(string plainText)
        {
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (RijndaelKey == null || RijndaelKey.Length <= 0)
                throw new ArgumentNullException("Key");
            if (RijndaelIV == null || RijndaelIV.Length <= 0)
                throw new ArgumentNullException("IV");
            byte[] encrypted;
            // Create an RijndaelManaged object
            // with the specified key and IV.
            using (RijndaelManaged rijAlg = new RijndaelManaged())
            {
                rijAlg.Key = RijndaelKey;
                rijAlg.IV = RijndaelIV;

                // Create an encryptor to perform the stream transform.
                ICryptoTransform encryptor = rijAlg.CreateEncryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }


            // Return the encrypted bytes from the memory stream.
            return encrypted;

        }

        public byte[] DecryptBytesFromBytes(byte[] cipherBytes)
        {
            // Check arguments.
            if (cipherBytes == null || cipherBytes.Length <= 0)
                throw new ArgumentNullException("cipherBytes");
            if (RijndaelKey == null || RijndaelKey.Length <= 0)
                throw new ArgumentNullException("Key");
            if (RijndaelIV == null || RijndaelIV.Length <= 0)
                throw new ArgumentNullException("IV");

            // Declare the string used to hold
            // the decrypted text.
            byte[] plainBytes = null;

            // Create an RijndaelManaged object
            // with the specified key and IV.
            using (RijndaelManaged rijAlg = new RijndaelManaged())
            {
                rijAlg.Key = RijndaelKey;
                rijAlg.IV = RijndaelIV;

                // Create a decryptor to perform the stream transform.
                ICryptoTransform decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherBytes))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (BinaryReader srDecrypt = new BinaryReader(csDecrypt))
                        {
                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plainBytes = srDecrypt.ReadBytes(int.MaxValue);
                        }
                    }
                }

            }

            return plainBytes;

        }

        public string DecryptStringFromBytes(byte[] cipherText)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (RijndaelKey == null || RijndaelKey.Length <= 0)
                throw new ArgumentNullException("Key");
            if (RijndaelIV == null || RijndaelIV.Length <= 0)
                throw new ArgumentNullException("IV");

            // Declare the string used to hold
            // the decrypted text.
            string plaintext = null;

            // Create an RijndaelManaged object
            // with the specified key and IV.
            using (RijndaelManaged rijAlg = new RijndaelManaged())
            {
                rijAlg.Key = RijndaelKey;
                rijAlg.IV = RijndaelIV;

                // Create a decryptor to perform the stream transform.
                ICryptoTransform decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }

            }

            return plaintext;

        }
    }
}
