using System.IO;
using System.Security.Cryptography;
using System.Text;
using System;
using System.Globalization;
using System.ComponentModel.DataAnnotations;
using Smead.Security;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Hosting.Server;
using LicenseServer.Properties;
using System.Data;
using System.Threading.Tasks;
using System.Reflection;
using System.Transactions;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

namespace LicenseServer.Models
{
    public class Encryptions
    {
        public const string  FusionRMSkeyEncryption = "TABsqlD2V77WTEKey";
        public static string EncryptKey(string clearText, string encryKey)
        {
            try
            {
                var clearBytes = Encoding.Unicode.GetBytes(clearText);
                using (var encryptor = Aes.Create())
                {
                    using (var pdb = new Rfc2898DeriveBytes(encryKey, new byte[] { 0x49, 0x76, 0x61, 0x6E, 0x20, 0x4D, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 }, 1000, HashAlgorithmName.SHA256))
                    {
                        encryptor.Key = pdb.GetBytes(32);
                        encryptor.IV = pdb.GetBytes(16);
                        using (var ms = new MemoryStream())
                        {
                            using (var cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                            {
                                cs.Write(clearBytes, 0, clearBytes.Length);
                                cs.FlushFinalBlock();
                            }

                            clearText = Convert.ToBase64String(ms.ToArray());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
              
            }
            return clearText;
        }
        public static string DecryptKey(string cipherText, string encryKey)
        {
            try
            {
                var cipherBytes = Convert.FromBase64String(cipherText);

                using (var encryptor = Aes.Create())
                {
                    using (var pdb = new Rfc2898DeriveBytes(encryKey, new byte[] { 0x49, 0x76, 0x61, 0x6E, 0x20, 0x4D, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 }, 1000, HashAlgorithmName.SHA256))
                    {
                        encryptor.Key = pdb.GetBytes(32);
                        encryptor.IV = pdb.GetBytes(16);
                        using (var ms = new MemoryStream())
                        {
                            using (var cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                            {
                                cs.Write(cipherBytes, 0, cipherBytes.Length);
                                cs.FlushFinalBlock();
                            }
                            cipherText = Encoding.Unicode.GetString(ms.ToArray());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
              
            }

            return cipherText;
        }
    }
}
