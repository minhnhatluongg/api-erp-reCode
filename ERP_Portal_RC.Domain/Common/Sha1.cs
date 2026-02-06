using System.Security.Cryptography;
using System.Text;

namespace ERP_Portal_RC.Domain.Common
{
    /// <summary>
    /// SHA1/DES Encryption và Decryption utility
    /// </summary>
    public static class Sha1
    {
        private static readonly byte[] _key = Encoding.ASCII.GetBytes("Nghe!Con");

        public static string Encrypt(string inputString)
        {
            if (string.IsNullOrEmpty(inputString))
            {
                throw new ArgumentNullException(nameof(inputString), "Input string không được null hoặc rỗng");
            }

            using var cryptoProvider = new DESCryptoServiceProvider();
            using var memoryStream = new MemoryStream();
            using var cryptoStream = new CryptoStream(
                memoryStream,
                cryptoProvider.CreateEncryptor(_key, _key),
                CryptoStreamMode.Write);
            using var writer = new StreamWriter(cryptoStream);
            
            writer.Write(inputString);
            writer.Flush();
            cryptoStream.FlushFinalBlock();
            
            return Convert.ToBase64String(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
        }

        public static string Decrypt(string encryptedString)
        {
            if (string.IsNullOrEmpty(encryptedString))
            {
                throw new ArgumentNullException(nameof(encryptedString), "Encrypted string không được null hoặc rỗng");
            }

            try
            {
                using var cryptoProvider = new DESCryptoServiceProvider();
                using var memoryStream = new MemoryStream(Convert.FromBase64String(encryptedString));
                using var cryptoStream = new CryptoStream(
                    memoryStream,
                    cryptoProvider.CreateDecryptor(_key, _key),
                    CryptoStreamMode.Read);
                using var reader = new StreamReader(cryptoStream);
                
                return reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Không thể giải mã chuỗi", ex);
            }
        }
        public static string? TryDecrypt(string encryptedString)
        {
            try
            {
                return Decrypt(encryptedString);
            }
            catch
            {
                return null;
            }
        }
    }
}
