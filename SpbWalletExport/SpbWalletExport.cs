using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using SQLite;

namespace SpbWalletExport
{
    class SpbWalletExport
    {
        private static ICryptoTransform _decryptor;
        private static SQLiteConnection _db;

        private static List<Category> _categories;
        private static List<Card> _cards;
        private static List<CardFieldValue> _cardFieldValues;
        private static List<TemplateField> _templateFields;

        private static XDocument _xDoc;

        static void Main(string[] args)
        {
            var rijndael = Rijndael.Create();
            rijndael.Key = GenerateKey();
            rijndael.IV = new byte[0x10];
            rijndael.Padding = PaddingMode.Zeros;
            rijndael.Mode = CipherMode.ECB;
            Debug.Assert(rijndael.Key != null, "rijndael.Key != null");
            _decryptor = rijndael.CreateDecryptor(rijndael.Key, rijndael.IV);

            _db = new SQLiteConnection(ConfigurationManager.AppSettings["SPBWalletFilePath"]);
            _categories = _db.Table<Category>().ToList();
            _cards = _db.Table<Card>().ToList();
            _cardFieldValues = _db.Table<CardFieldValue>().ToList();
            _templateFields = _db.Table<TemplateField>().ToList();

            _xDoc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"));
            XElement root = new XElement("Root");
            _xDoc.Add(root);

            ProduceCategories("", root);

            _xDoc.Save("Export.xml");
        }

        [Table("spbwlt_Category")]
        public class Category
        {
            public string ID { get; set; }
            public byte[] Name { get; set; }
            public string ParentCategoryID { get; set; }
        }

        [Table("spbwlt_Card")]
        public class Card
        {
            public string ID { get; set; }
            public byte[] Name { get; set; }
            public byte[] Description { get; set; }
            public string ParentCategoryID { get; set; }
            public string TemplateID { get; set; }
        }
        [Table("spbwlt_CardFieldValue")]
        public class CardFieldValue
        {
            public string ID { get; set; }
            public string CardID { get; set; }
            public string TemplateFieldID { get; set; }
            public byte[] ValueString { get; set; }
        }
        [Table("spbwlt_TemplateField")]
        public class TemplateField
        {
            public string ID { get; set; }
            public byte[] Name { get; set; }
            public string TemplateID { get; set; }
        }
        private static void ProduceCategories(string categoryId, XElement xParent)
        {
            // Process inner categories (folders)
            foreach (var category in _categories.Where(x => x.ParentCategoryID == categoryId))
            {
                string name = Decrypt(category.Name);
                XElement xCategory = new XElement("Category");
                xCategory.SetAttributeValue("Name", name);
                xParent.Add(xCategory);
                ProduceCategories(category.ID, xCategory);
            }

            ProduceCards(categoryId, xParent);
        }

        private static void ProduceCards(string categoryId, XElement xParent)
        {
            // Process cards under given category (folder)
            foreach (var card in _cards.Where(x => x.ParentCategoryID == categoryId))
            {
                string name = Decrypt(card.Name);
                //Debug.WriteLine("    " + name);
                XElement xCard = new XElement("Card");
                xCard.SetAttributeValue("Name", name);
                xParent.Add(xCard);
                ProduceCardFields(card.ID, xCard);
            }
        }
        private static void ProduceCardFields(string cardId, XElement xParent)
        {
            // Process cards under given category (folder)
            foreach (var card in _cardFieldValues.Where(x => x.CardID == cardId))
            {
                XElement xFieldValue = new XElement("Field");

                TemplateField templateField = _templateFields.FirstOrDefault(x => x.ID == card.TemplateFieldID);
                if (templateField != null)
                {
                    string fieldName = Decrypt(templateField.Name);
                    xFieldValue.SetAttributeValue("Name", fieldName);
                }

                string fieldValue = Decrypt(card.ValueString);
                xFieldValue.SetAttributeValue("Value", fieldValue);

                xParent.Add(xFieldValue);
            }
        }
        private static string Decrypt(byte[] paddingPrefixedBlob)
        {
            using (MemoryStream ms = new MemoryStream(paddingPrefixedBlob, 4, paddingPrefixedBlob.Length - 4))
            {
                using (CryptoStream cs = new CryptoStream(ms, _decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader csReader = new StreamReader(cs, Encoding.Unicode))
                    {
                        string paddedString = csReader.ReadToEnd();
                        Int32 lengthOfPadding = BitConverter.ToInt32(paddingPrefixedBlob, 0);
                        return paddedString.Substring(0, paddedString.Length - lengthOfPadding/2);
                    }
                }
            }
        }

        //private static string Decrypt(byte[] paddingPrefixedBlob)
        //{
        //    return Encoding.Unicode.GetString(RawDecrypt(paddingPrefixedBlob)).TrimEnd('\0');
        //}

        //private static byte[] RawDecrypt(byte[] paddingPrefixedBlob)
        //{
        //    using (MemoryStream ms = new MemoryStream(paddingPrefixedBlob, 4, paddingPrefixedBlob.Length - 4))
        //    {
        //        using (CryptoStream cs = new CryptoStream(ms, _decryptor, CryptoStreamMode.Read))
        //        {
        //            using (StreamReader csReader = new StreamReader(cs))
        //            {
        //                plaintext = csReader.ReadToEnd();
        //            }
        //            byte[] buffer = new byte[paddingPrefixedBlob.Length - 4];
        //            cs.Read(buffer, 0, buffer.Length);
        //            Dump("buffer", buffer);
        //            return buffer;
        //        }
        //    }
        //}
        private static byte[] GenerateKey()
        {
            string zeroTerminatedPassword = ConfigurationManager.AppSettings["Password"] + '\0';
            byte[] bytesPassword = Encoding.Unicode.GetBytes(zeroTerminatedPassword);
            byte[] hash = new SHA1CryptoServiceProvider().ComputeHash(bytesPassword);
            byte[] destination = new byte[32];
            Buffer.BlockCopy(hash, 0, destination, 0, hash.Length);
            Buffer.BlockCopy(hash, 0, destination, 20, 12);
            return destination;
        }

        private static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        private static void Dump(string name, byte[] array)
        {
            Debug.WriteLine(name + " (" + array.Length+ ") " + " = " + BitConverter.ToString(array).Replace("-", ""));
        }
    }
}
