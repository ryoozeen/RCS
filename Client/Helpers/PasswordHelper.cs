using System;
using System.Security.Cryptography;
using System.Text;

namespace MyApp.Helpers
{
    public static class SecurityHelper
    {
        // SHA256 해싱 함수
        public static string ComputeSHA256(string input)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                return Convert.ToBase64String(bytes);
            }
        }
    }
}
