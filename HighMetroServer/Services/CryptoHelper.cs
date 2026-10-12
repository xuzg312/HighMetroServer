using System;
using System.Security.Cryptography;
using System.Text;

namespace HighMetroServer.Services;

public class CryptoHelper
{
    private static readonly byte[] Key = [.. "Shgfs-NB-Zhmd-UnionDevelop2n09s0"u8]; 
    private static readonly byte[] Iv  = [.. "Shgfs-NB-Zhmd-Un"u8];                   
    
    public static string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = Key;
        aes.IV = Iv;
        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        return Convert.ToBase64String(cipherBytes);
    }

    public static string Decrypt(string cipherText)
    {
        try
        {
            using var aes = Aes.Create();
            aes.Key = Key;
            aes.IV = Iv;
            using var decryptor = aes.CreateDecryptor();
            var cipherBytes = Convert.FromBase64String(cipherText);
            var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}