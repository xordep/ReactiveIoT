using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ContextProcessor.Model
{
    public class Common
    {
        public static string GenerateSalt()
        {
            var random = new RNGCryptoServiceProvider();
            int max_length = 32;
            byte[] salt = new byte[max_length];
            random.GetNonZeroBytes(salt);
            return Convert.ToBase64String(salt);
        }

        public static string GenerateToken()
        {
            using (var cryptoProvider = new RNGCryptoServiceProvider())
            {
                var secretKeyByteArray = new byte[32];
                cryptoProvider.GetBytes(secretKeyByteArray);
                return Convert.ToBase64String(secretKeyByteArray);
            }
        }
    }
}
