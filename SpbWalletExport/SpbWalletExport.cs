using System;
using System.Configuration;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;

namespace SpbWalletExport
{
    class SpbWalletExport
    {
        private static readonly Encoding Encoding = Encoding.Unicode;
        static void Main(string[] args)
        {
            string spbWalletFilePath = ConfigurationManager.AppSettings["SpbWalletFilePath"];

            SQLiteConnection connection = new SQLiteConnection("Data Source=" + spbWalletFilePath);
            connection.Open();
            string sqlGetAllValues = "SELECT * FROM spbwlt_CardFieldValue";
            SQLiteCommand cmdGetAllValues = new SQLiteCommand(sqlGetAllValues, connection);
            SQLiteDataReader reader = cmdGetAllValues.ExecuteReader();

            /*
            const string sqlGetAllValues = "SELECT * FROM spbwlt_Card";
            SQLiteCommand cmdGetAllValues = new SQLiteCommand(sqlGetAllValues, connection);
            SQLiteDataReader reader = cmdGetAllValues.ExecuteReader();

            while (reader.Read())
            {
                byte[] baLengthPrefixedValueString = reader["Name"] as byte[];
                Debug.Assert(baLengthPrefixedValueString != null, "baLengthPrefixedValueString != null");
                Int32 prefix = BitConverter.ToInt32(baLengthPrefixedValueString, 0);
                Debug.WriteLine("Prefix = " + prefix);
                byte[] baValueString = baLengthPrefixedValueString.Skip(4).ToArray();
                Dump("baValueString", baValueString);
            }
            */

            while (reader.Read())
            {
                string id = reader["ID"].ToString();

                string cardId = reader["CardID"].ToString();

                byte[] baLengthPrefixedValueString = reader["ValueString"] as byte[];
                Debug.Assert(baLengthPrefixedValueString != null, "baLengthPrefixedValueString != null");
                Int32 prefix = BitConverter.ToInt32(baLengthPrefixedValueString, 0);
                Debug.WriteLine("Prefix = " + prefix);
                byte[] baValueString = baLengthPrefixedValueString.Skip(4).ToArray();
                Dump("baValueString", baValueString);

                const string strPassword = "TestPassword";
                byte[] bytesPassword = Encoding.UTF8.GetBytes(strPassword);


                Rijndael rijndael = Rijndael.Create();
                rijndael.Key = GenerateKey("TestPassword");
                rijndael.IV = new byte[0x10];
                rijndael.Padding = PaddingMode.Zeros;
                rijndael.Mode = CipherMode.ECB;
                var decryptor = rijndael.CreateDecryptor(rijndael.Key, rijndael.IV);
                MemoryStream stream = new MemoryStream();
                stream.Write(baLengthPrefixedValueString, 4, baLengthPrefixedValueString.Length - 4);
                stream.Position = 0L;
                CryptoStream stream2 = new CryptoStream(stream, decryptor, CryptoStreamMode.Read);
                byte[] buffer = new byte[baLengthPrefixedValueString.Length - 4];
                stream2.Read(buffer, 0, buffer.Length);
                Dump("buffer", buffer);
                //return Encoding.Unicode.GetString(buffer).TrimEnd(new char[1]);



                //using (AesCryptoServiceProvider aesAlg = new AesCryptoServiceProvider())
                //{
                //    //byte[] shortKey = Encoding.ASCII.GetBytes("TestPassword");
                //    //byte[] fullKey = new byte[32];
                //    //Array.Copy(shortKey, fullKey, shortKey.Length);

                //    aesAlg.Mode = CipherMode.ECB;
                //    aesAlg.Padding = PaddingMode.None;
                //    //aesAlg.BlockSize = 128;
                //    //aesAlg.KeySize = 256;

                //    const string strPassword = "TestPassword";
                //    byte[] bytesPassword = Encoding.UTF8.GetBytes(strPassword);
                //    //byte[] bytesPassword = Encoding.Unicode.GetBytes(strPassword);

                //    SHA256 hasher = SHA256.Create();
                //    //MD5 hasher = MD5.Create();
                //    //RIPEMD160 hasher = RIPEMD160Managed.Create();
                //    //SHA1 hasher = SHA1.Create();

                //    byte[] passwordHash = hasher.ComputeHash(bytesPassword);
                //    //Dump("passwordHash", passwordHash);

                //    byte[] keyBytes = new byte[32];
                //    Array.Copy(passwordHash, keyBytes, 32);

                //    //Dump("keyBytes", keyBytes);


                //    aesAlg.Key = keyBytes;

                //    const string text =
                //        "2222222222222222";
                //        //"22222222222222222222222222222222222222222222222222";
                //        //"22222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222";
                //        //"222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222";

                //    MemoryStream ms = new MemoryStream();
                //    ICryptoTransform encryptor = aesAlg.CreateEncryptor();
                //    CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
                //    byte[] clearTextBytes = Encoding.GetBytes(text);
                //    cs.Write(clearTextBytes, 0, clearTextBytes.Length);
                //    cs.FlushFinalBlock();
                //    cs.Close();
                //    byte[] encrypted = ms.ToArray();
                //    Dump("encrypted", encrypted);

                //    //string roundtrip = DecryptStringFromBytes_Aes(encrypted, myAes.Key, myAes.IV);

                //    //byte[] blocks = new byte[64];
                //    //Array.Copy(baValueString, blocks, baValueString.Length);
                //    //string test = DecryptStringFromBytes_Aes(baValueString, myAes.Key, myAes.IV);

                //}

                //Debug.WriteLine("id = {0}, cardId = {1}", id, cardId);
                break;
            }         
        }

        private static byte[] GenerateKey(string password)
        {
            string zeroTerminatedPassword = password + '\0';
            byte[] bytesPassword = Encoding.Unicode.GetBytes(zeroTerminatedPassword);
            byte[] hash = new SHA1CryptoServiceProvider().ComputeHash(bytesPassword);
            byte[] destination = new byte[32];
            Buffer.BlockCopy(hash, 0, destination, 0, hash.Length);
            Buffer.BlockCopy(hash, 0, destination, 20, 12);
            return destination;
        }

        // Block = 16 bytes, so mode = ECB
        // For ECB - IV does not play a role;
        // Looks at Subject!

        // This varies data every time!
        //var keyDerivationFunction = new Rfc2898DeriveBytes("TestPassword", 8);
        //byte[] keyBytes = keyDerivationFunction.GetBytes(32);
        //byte[] ivBytes = keyDerivationFunction.GetBytes(16);

        // Only 16 bytes hash
        //MD5 hasher = MD5.Create();

        //Prefix is data padding to 32 byte boundary
        //9 -> 14;
        //8 -> 0;
        //7 -> 2;
        //6 -> 4;
        //5 -> 6;
        //1 -> 14;

        // 16 byte key produces encrypted result as 0D-C0-0A-00-...
        
        private static void Dump(string name, byte[] array)
        {
            Debug.WriteLine(name + " (" + array.Length+ ") " + " = " + BitConverter.ToString(array));
        }
        static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] key)
        {
            // Check arguments. 
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (key == null || key.Length <= 0)
                throw new ArgumentNullException("key");

            // Declare the string used to hold 
            // the decrypted text. 
            string plaintext = null;

            // Create an AesCryptoServiceProvider object 
            // with the specified key and IV. 
            using (AesCryptoServiceProvider aesAlg = new AesCryptoServiceProvider())
            {
                aesAlg.Mode = CipherMode.ECB;
                aesAlg.Key = key;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor();

                // Create the streams used for decryption. 
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt, Encoding))
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
        static byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key)
        {
            // Check arguments. 
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            byte[] encrypted;
            // Create an AesCryptoServiceProvider object 
            // with the specified key and IV. 
            using (AesCryptoServiceProvider aesAlg = new AesCryptoServiceProvider())
            {
                aesAlg.Mode = CipherMode.ECB;
                aesAlg.Padding = PaddingMode.None;
                aesAlg.BlockSize = 128;
                aesAlg.KeySize = 256;
                aesAlg.Key = Key;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor();

                // Create the streams used for encryption. 
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt, Encoding))
                        {

                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                        Dump("encrypted_X", encrypted);
                    }
                }
            }

            // Return the encrypted bytes from the memory stream. 
            return encrypted;

        }
    }
}
