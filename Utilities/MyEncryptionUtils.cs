using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ChatBotZ.Utilities
{
    internal class MyEncryptionUtils
    {
        internal static string RSAEncrypt(string plainText, string key)
        {
            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                rsa.FromXmlString(key);
                var plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
                var cipherBytes = rsa.Encrypt(plainBytes, RSAEncryptionPadding.OaepSHA512);
                return Convert.ToBase64String(cipherBytes);
            }
        }

        internal static string RSADecrypt(string cipherText, string key)
        {
            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                rsa.FromXmlString(key);
                var cipherBytes = Convert.FromBase64String(cipherText);
                var plainBytes = rsa.Decrypt(cipherBytes, RSAEncryptionPadding.OaepSHA512);
                return System.Text.Encoding.UTF8.GetString(plainBytes);
            }
        }

        internal static readonly string RSAkey = @"<RSAKeyValue>" + @"<Modulus>5mKj5G5PO5o9X5vbF2Eizy+o/yJW0mv5N1m5jFam5//m0zXJh5f73tkm+rq/KjtG" + @"p/N0RjKZ1QaX9zL/goyLuSVXXyAKHxTdcTKjvA4Ax4YqfBJkkl8zvZjKUcG97yh0a" + 
            @"KM6QJYVZnLdUW8VvFzLQJjKAWH2Qh5lJx23cgKjJ5+5l5k=" + @"<Exponent>AQAB</Exponent>" + @"<P>7MzK+//m0Kj5G5PO5o9X5vbF2Eizy+o/yJW0mv5N1m5jFam5//m0zXJh5f73tkm+" + 
            @"rq/KjtGp/N0RjKZ1QaX9zL/goyLuSVXXyAKHxTdcTKjvA4Ax4YqfBJkkl8zvZjKUc" + @"G97yh0aKM6QJYVZnLdUW8VvFzLQJjKAWH2Qh5lJx23cgKjJ5+5l5k=</P>" + @"<Q>5mKj5G5PO5o9X5vbF2Eizy+o/yJW0mv5N1m5jFam5//m0zXJh5f73tkm+rq/KjtG" +
            @"p/N0RjKZ1QaX9zL/goyLuSVXXyAKHxTdcTKjvA4Ax4YqfBJkkl8zvZjKUcG97yh0a" + @"KM6QJYVZnLdUW8VvFzLQJjKAWH2Qh5lJx23cgKjJ5+5l5k=</Q>" + 
            @"<DP>2mKj5G5PO5o9X5vbF2Eizy+o/yJW0mv5N1m5jFam5//m0zXJh5f73tkm+rq/KjtG" + @"p/N0RjKZ1QaX9zL/goyLuSVXXyAKHxTdcTKjvA4Ax4YqfBJkkl8zvZjKUcG97yh0a" + @"KM6QJYVZnLdUW8VvFzLQJjKAWH2Qh5lJx23cgKjJ5+5l5k=</DP>";
    }
}