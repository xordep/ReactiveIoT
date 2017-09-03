using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Sender.Model
{
    public class Common
    {
        public static string RandomString(int length)
        {
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static string Encrypt(String text)
        {
            RijndaelManaged RijndaelAlg = new RijndaelManaged();
            CryptoStream cStream = null;

            try
            {
                byte[] key = { 0x02, 0x1A, 0x03, 0x04C, 0x05, 0x06, 0xAB, 0x08, 0x09, 0xEF, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16 };
                byte[] IV = { 0x01, 0x11, 0x10, 0x99, 0x19, 0x06, 0x07, 0x28, 0x09, 0x10, 0x11, 0x12, 0x33, 0x14, 0x15, 0x56 };
                byte[] inputByteArray = Encoding.UTF8.GetBytes(text);

                MemoryStream memoryS = new MemoryStream();
                cStream = new CryptoStream(memoryS, RijndaelAlg.CreateEncryptor(key, IV), CryptoStreamMode.Write);
                cStream.Write(inputByteArray, 0, inputByteArray.Length);
                cStream.FlushFinalBlock();
                return Convert.ToBase64String(memoryS.ToArray());
            }
            catch (Exception e)
            {
                string y = e.Message.ToString();
                return null;
            }

            finally
            {
                if (cStream != null)
                    cStream.Close();
            }
        }

        public static string Decrypt(String text)
        {
            Rijndael RijndaelAlg = Rijndael.Create();
            CryptoStream cStream = null;

            try
            {
                byte[] key = { 0x02, 0x1A, 0x03, 0x04C, 0x05, 0x06, 0xAB, 0x08, 0x09, 0xEF, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16 };
                byte[] IV = { 0x01, 0x11, 0x10, 0x99, 0x19, 0x06, 0x07, 0x28, 0x09, 0x10, 0x11, 0x12, 0x33, 0x14, 0x15, 0x56 };
                byte[] inputByteArray = Convert.FromBase64String(text);
                MemoryStream memoryS = new MemoryStream();
                cStream = new CryptoStream(memoryS, RijndaelAlg.CreateDecryptor(key, IV), CryptoStreamMode.Write);
                cStream.Write(inputByteArray, 0, inputByteArray.Length);
                cStream.FlushFinalBlock();
                Encoding encode = Encoding.UTF8;
                return encode.GetString(memoryS.ToArray());
            }
            catch (Exception e)
            {
                string y = e.Message.ToString();
                return null;
            }
            finally
            {
                if (cStream != null)
                    cStream.Close();
            }
        }
    }
}
