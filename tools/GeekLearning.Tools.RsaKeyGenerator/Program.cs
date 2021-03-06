﻿namespace GeekLearning.Tools.RsaKeyGenerator
{
    using System;
    using System.IO;
    using System.Security.Cryptography;

    public class Program
    {
        private static RandomNumberGenerator rng = RandomNumberGenerator.Create();

        public static void Main(string[] args)
        {
            string alg = "rsa";
            var outputPath = Directory.GetCurrentDirectory();
            byte[] password = null;
            byte[] salt = new byte[32];
            string kId = Guid.NewGuid().ToString("N");
            rng.GetBytes(salt);

            //ArgumentSyntax.Parse(args, syntax =>
            //{
            //    syntax.DefineOption("o|outputPath", ref outputPath, "The output path where to write new keys");
            //    syntax.DefineOption("p|password", ref password, str => GetPasswordDerivedBytes(str, salt), "The password to protect the private key");
            //    syntax.DefineOption("a|alg", ref alg, "Algorithm to use (rsa or ecdsa)");
            //});

            if (password == null)
            {
                var passwordBytes = new byte[32];
                rng.GetBytes(passwordBytes);
                var passwordString = Convert.ToBase64String(passwordBytes);
                password = GetPasswordDerivedBytes(passwordString, salt);
                Console.WriteLine($"Generated Password : {passwordString}");
            }

            if (alg == "rsa")
            {
                GenerateRSAKey(outputPath, password, salt, kId);
            }
            else if (alg == "ecdsa")
            {
                GenerateECDSAKey(outputPath, password, salt, kId);
            }

            Console.ReadLine();
        }

        private static void GenerateRSAKey(string outputPath, byte[] password, byte[] salt, string kId)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048);
            ExportPrivateKey(rsa, kId, outputPath, password, salt);
            ExportPublicKey(rsa, kId, outputPath);
        }

        private static byte[] GetPasswordDerivedBytes(string password, byte[] salt)
        {
            var passwordDeriver = new Rfc2898DeriveBytes(password, salt);
            return passwordDeriver.GetBytes(32);
        }

        public static void ExportPublicKey(RSACryptoServiceProvider rsa, string kId, string outputPath)
        {
            var rsaParams = rsa.ExportParameters(false);
            var ms = new MemoryStream();
            var noBytes = new byte[0];
            BinaryWriter w = new BinaryWriter(ms);
            w.WriteLengthPrefixedBuffer(rsaParams.D ?? noBytes);
            w.WriteLengthPrefixedBuffer(rsaParams.DP ?? noBytes);
            w.WriteLengthPrefixedBuffer(rsaParams.DQ ?? noBytes);
            w.WriteLengthPrefixedBuffer(rsaParams.Exponent ?? noBytes);
            w.WriteLengthPrefixedBuffer(rsaParams.InverseQ ?? noBytes);
            w.WriteLengthPrefixedBuffer(rsaParams.Modulus ?? noBytes);
            w.WriteLengthPrefixedBuffer(rsaParams.P ?? noBytes);
            w.WriteLengthPrefixedBuffer(rsaParams.Q ?? noBytes);
            w.Flush();
            File.WriteAllText(Path.Combine(outputPath, kId + ".key.public"), Convert.ToBase64String(ms.ToArray()));
        }

        public static void ExportPrivateKey(RSACryptoServiceProvider rsa, string kId, string outputPath, byte[] password, byte[] salt)
        {
            var rsaParams = rsa.ExportParameters(true);
            var ms = new MemoryStream();
            BinaryWriter w = new BinaryWriter(ms);
            w.WriteLengthPrefixedBuffer(rsaParams.D);
            w.WriteLengthPrefixedBuffer(rsaParams.DP);
            w.WriteLengthPrefixedBuffer(rsaParams.DQ);
            w.WriteLengthPrefixedBuffer(rsaParams.Exponent);
            w.WriteLengthPrefixedBuffer(rsaParams.InverseQ);
            w.WriteLengthPrefixedBuffer(rsaParams.Modulus);
            w.WriteLengthPrefixedBuffer(rsaParams.P);
            w.WriteLengthPrefixedBuffer(rsaParams.Q);
            w.Flush();

            var aes = Aes.Create();
            aes.Key = password;
            aes.GenerateIV();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            var transform = aes.CreateEncryptor();
            var encrypted = new MemoryStream();
            encrypted.Write(aes.IV, 0, (int)aes.IV.Length);
            using (CryptoStream stream = new CryptoStream(encrypted, transform, CryptoStreamMode.Write))
            {
                stream.Write(ms.ToArray(), 0, (int)ms.Length);
                stream.FlushFinalBlock();
            }

            File.WriteAllText(Path.Combine(outputPath, kId + ".key.private"),
                Convert.ToBase64String(salt) + "." + Convert.ToBase64String(encrypted.ToArray()));
        }

        private static void GenerateECDSAKey(string outputPath, byte[] password, byte[] salt, string kId)
        {
            ECDsaCng ecdsa = new ECDsaCng(256);
            ExportECDSAPrivateKey(ecdsa, kId, outputPath, password, salt);
            ExportECDSAPublicKey(ecdsa, kId, outputPath);
        }

        public static void ExportECDSAPrivateKey(ECDsaCng ecdsa, string kId, string outputPath, byte[] password, byte[] salt)
        {
            var key = ecdsa.Key.Export(CngKeyBlobFormat.EccPrivateBlob);

            var aes = Aes.Create();
            aes.Key = password;
            aes.GenerateIV();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            var transform = aes.CreateEncryptor();
            var encrypted = new MemoryStream();
            encrypted.Write(aes.IV, 0, aes.IV.Length);
            using (CryptoStream stream = new CryptoStream(encrypted, transform, CryptoStreamMode.Write))
            {
                stream.Write(key, 0, key.Length);
                stream.FlushFinalBlock();
            }

            File.WriteAllText(Path.Combine(outputPath, kId + ".key.private"),
                Convert.ToBase64String(salt) + "." + Convert.ToBase64String(encrypted.ToArray()));
        }

        public static void ExportECDSAPublicKey(ECDsaCng ecdsa, string kId, string outputPath)
        {
            var key = ecdsa.Key.Export(CngKeyBlobFormat.EccPublicBlob);
            File.WriteAllText(Path.Combine(outputPath, kId + ".key.public"), Convert.ToBase64String(key));
        }
    }
}
