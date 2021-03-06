﻿namespace GeekLearning.Security.Cryptography
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using Utilities;

    public class RsaKeyHelper
    {
        public static RSAParameters ReadParameters(string key, string password)
        {
            var keyParts = key.Split('.');
            var salt = Convert.FromBase64String(keyParts[0]);
            var encrypted = new MemoryStream(Convert.FromBase64String(keyParts[1]));
            var iv = new byte[16];
            encrypted.Read(iv, 0, 16);

            var passwordDeriveBytes = new Rfc2898DeriveBytes(password, salt);

            var aes = Aes.Create();
            aes.IV = iv;
            aes.Key = passwordDeriveBytes.GetBytes(32);
            aes.Mode = CipherMode.CBC;
            var decrypted = new MemoryStream();

            using (var transform = aes.CreateDecryptor())
            {
                using (var cryptoStream = new CryptoStream(encrypted, transform, CryptoStreamMode.Read))
                {

                    var buffer = new byte[16];
                    int readBytes = int.MaxValue;
                    do
                    {
                        readBytes = cryptoStream.Read(buffer, 0, buffer.Length);
                        if (readBytes > 0)
                        {
                            decrypted.Write(buffer, 0, readBytes);
                        }

                    } while (readBytes > 0);
                }
            }

            decrypted.Seek(0, SeekOrigin.Begin);

            RSAParameters rsaParams = new RSAParameters();
            using (var reader = new System.IO.BinaryReader(decrypted))
            {
                rsaParams.D = reader.ReadLengthPrefixedBuffer();
                rsaParams.DP = reader.ReadLengthPrefixedBuffer();
                rsaParams.DQ = reader.ReadLengthPrefixedBuffer();
                rsaParams.Exponent = reader.ReadLengthPrefixedBuffer();
                rsaParams.InverseQ = reader.ReadLengthPrefixedBuffer();
                rsaParams.Modulus = reader.ReadLengthPrefixedBuffer();
                rsaParams.P = reader.ReadLengthPrefixedBuffer();
                rsaParams.Q = reader.ReadLengthPrefixedBuffer();
            }

            return rsaParams;
        }

        public static RSAParameters ReadPublicParameters(string key)
        {
            var decrypted = new MemoryStream(Convert.FromBase64String(key));

            RSAParameters rsaParams = new RSAParameters();
            using (var reader = new System.IO.BinaryReader(decrypted))
            {
                rsaParams.D = reader.ReadLengthPrefixedBufferNullIfEmpty();
                rsaParams.DP = reader.ReadLengthPrefixedBufferNullIfEmpty();
                rsaParams.DQ = reader.ReadLengthPrefixedBufferNullIfEmpty();
                rsaParams.Exponent = reader.ReadLengthPrefixedBufferNullIfEmpty();
                rsaParams.InverseQ = reader.ReadLengthPrefixedBufferNullIfEmpty();
                rsaParams.Modulus = reader.ReadLengthPrefixedBufferNullIfEmpty();
                rsaParams.P = reader.ReadLengthPrefixedBufferNullIfEmpty();
                rsaParams.Q = reader.ReadLengthPrefixedBufferNullIfEmpty();
            }

            return rsaParams;
        }
    }
}
