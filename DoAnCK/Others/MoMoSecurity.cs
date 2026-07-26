using System;
using System.Text;
using System.Security.Cryptography;

namespace TMDT_NoiThatB2C.Others // Đổi namespace cho khớp project của bạn
{
    public class MoMoSecurity
    {
        public static string signSHA256(string message, string key)
        {
            byte[] keyByte = Encoding.UTF8.GetBytes(key);
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
                string hex = BitConverter.ToString(hashmessage);
                hex = hex.Replace("-", "").ToLower();
                return hex;
            }
        }
    }
}