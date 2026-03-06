using System.Security.Cryptography;
using System.Text;

namespace ERP_Portal_RC.Domain.Common
{
    /// <summary>
    /// Helper mã hóa/giải mã DES dùng cho ApiInfo trả về SignApp.
    /// Port 1:1 từ VB.NET EncryptBOS / DecryptBOS của hệ thống Gonsa cũ.
    /// Key mặc định: "Nghe!Con" — dùng ASCII bytes làm cả Key lẫn IV cho DES.
    /// </summary>
    public static class BosEncryptionHelper
    {
        private const string DEFAULT_KEY = "Nghe!Con";

        /// <summary>
        /// Mã hóa plain text bằng DES (giống EncryptBOS VB.NET).
        /// writer.Write → writer.Flush → cryptoStream.FlushFinalBlock → writer.Flush
        /// → Convert.ToBase64String(memoryStream.GetBuffer(), 0, memoryStream.Length)
        /// </summary>
        public static string BosEncrypt(string plainText, string key = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(key))
                    key = DEFAULT_KEY;

                if (string.IsNullOrEmpty(plainText))
                    throw new ArgumentNullException(nameof(plainText), "Null Input String");

                byte[] keyBytes = Encoding.ASCII.GetBytes(key);

                var cryptoProvider = new DESCryptoServiceProvider();
                var memoryStream   = new MemoryStream();

                // Giống VB.NET: key không rỗng nên dùng keyToBytes
                var cryptoStream = new CryptoStream(
                    memoryStream,
                    cryptoProvider.CreateEncryptor(keyBytes, keyBytes),
                    CryptoStreamMode.Write);

                var writer = new StreamWriter(cryptoStream);
                writer.Write(plainText);
                writer.Flush();
                cryptoStream.FlushFinalBlock();
                writer.Flush();

                // Dùng GetBuffer() + Length giống VB.NET (CType(memoryStream.Length, Integer))
                return Convert.ToBase64String(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Giải mã Base64 DES-encrypted về plain text (giống DecryptBOS VB.NET).
        /// </summary>
        public static string BosDecrypt(string encryptedText, string key = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(key))
                    key = DEFAULT_KEY;

                if (string.IsNullOrEmpty(encryptedText))
                    throw new ArgumentNullException(nameof(encryptedText), "Null Input String");

                byte[] keyBytes = Encoding.ASCII.GetBytes(key);

                var cryptoProvider = new DESCryptoServiceProvider();
                var memoryStream   = new MemoryStream(Convert.FromBase64String(encryptedText.Trim()));
                var cryptoStream   = new CryptoStream(
                    memoryStream,
                    cryptoProvider.CreateDecryptor(keyBytes, keyBytes),
                    CryptoStreamMode.Read);
                var reader = new StreamReader(cryptoStream);

                return reader.ReadToEnd();
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    /// <summary>
    /// Helper tạo và giải mã JWT token cho App signing.
    /// Port từ Gonsa.Application.Helpers.JwtHelper.
    ///
    /// SECRET_KEY: "THVvbmdNaW5oTmhhdFdpbnRlY2hEZXZBcHBLeQ==" (Base64 encoded)
    /// Claim "data": payload string chứa thông tin ký (join bằng ';')
    /// Claim "iss":  "self-app"
    /// </summary>
    public static class JwtHelper
    {
        // Base64 encoded secret key — giống hệt Gonsa JwtHelper
        private const string SECRET_KEY   = "THVvbmdNaW5oTmhhdFdpbnRlY2hEZXZBcHBLeQ==";
        private const string CLAIM_DATA   = "data";
        private const string CLAIM_ISS    = "iss";

        // ─── Generate ────────────────────────────────────────────────────────────

        /// <summary>
        /// Tạo JWT token với payload string, HMAC-SHA256, expiry (phút).
        /// Output: header.payload.signature — chuẩn RFC 7519.
        /// Giống JwtSecurityTokenHandler.CreateToken() của Gonsa.
        /// </summary>
        public static string GenerateJwtToken(string payloadString, int expiryMinutes = 60)
        {
            try
            {
                long now    = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                long expiry = now + (expiryMinutes * 60);

                // Header
                var headerObj = new { alg = "HS256", typ = "JWT" };
                string headerJson    = System.Text.Json.JsonSerializer.Serialize(headerObj);
                string headerEncoded = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));

                // Payload — claims: iss, data, iat, exp
                // Serialize thủ công để đảm bảo thứ tự claim giống JwtSecurityTokenHandler
                string payloadJson =
                    $"{{" +
                    $"\"{CLAIM_ISS}\":\"self-app\"," +
                    $"\"{CLAIM_DATA}\":{System.Text.Json.JsonSerializer.Serialize(payloadString)}," +
                    $"\"iat\":{now}," +
                    $"\"exp\":{expiry}" +
                    $"}}";
                string payloadEncoded = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));

                // Signature — HMAC-SHA256
                string signingInput = $"{headerEncoded}.{payloadEncoded}";
                byte[] keyBytes     = Convert.FromBase64String(SECRET_KEY);   // key theo Gonsa: UTF8 decode of base64
                byte[] sigBytes;
                using (var hmac = new System.Security.Cryptography.HMACSHA256(keyBytes))
                    sigBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(signingInput));

                string signature = Base64UrlEncode(sigBytes);
                return $"{signingInput}.{signature}";
            }
            catch
            {
                return string.Empty;
            }
        }

        // ─── Decode/Validate ─────────────────────────────────────────────────────

        /// <summary>
        /// Lấy phần payload (segment thứ 2) của JWT dưới dạng JSON string thuần.
        /// Không kiểm tra signature hoặc expiry – chỉ Base64Url decode đúng chuẩn RFC 7519.
        ///
        /// LƯU Ý: KHÔNG dùng Split(':') để tiền xử lý JWT vì payload JSON
        /// hoàn toàn có thể chứa ký tự ':' hợp lệ, gây cắt sai token.
        /// </summary>
        public static string GetPayloadFromJwt(string jwt)
        {
            if (string.IsNullOrWhiteSpace(jwt))
                return string.Empty;

            try
            {
                // JWT có dạng: header.payload.signature (3 segment phân cách bằng '.')
                var parts = jwt.Trim().Split('.');
                if (parts.Length < 2)
                    return string.Empty;

                string payload = parts[1];

                // Chuyển Base64Url → Base64 chuẩn (RFC 4648)
                payload = payload.Replace('-', '+').Replace('_', '/');
                switch (payload.Length % 4)
                {
                    case 2: payload += "=="; break;
                    case 3: payload += "=";  break;
                    // case 0: không cần padding
                    // case 1: JWT không hợp lệ nhưng cứ thử decode
                }

                byte[] bytes = Convert.FromBase64String(payload);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Lấy giá trị claim "data" từ JWT payload JSON.
        /// Dùng để extract payload_string truyền cho SignApp.
        /// </summary>
        public static string? GetDataClaimFromJwt(string jwt)
        {
            try
            {
                string payloadJson = GetPayloadFromJwt(jwt);
                if (string.IsNullOrEmpty(payloadJson)) return null;

                var doc = System.Text.Json.JsonDocument.Parse(payloadJson);
                if (doc.RootElement.TryGetProperty(CLAIM_DATA, out var dataProp))
                    return dataProp.GetString();

                return null;
            }
            catch
            {
                return null;
            }
        }

        // ─── Private ─────────────────────────────────────────────────────────────

        private static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }

    /// <summary>
    /// Helper chuyển số thành chữ (tiếng Việt) – dùng khi tạo XML hợp đồng.
    /// </summary>
    public static class NumberToTextHelper
    {
        private static readonly string[] _digits =
            { "không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín" };

        private static readonly string[] _unit =
            { "", " nghìn", " triệu", " tỷ", " nghìn tỷ" };

        public static string Convert(decimal total)
        {
            try
            {
                if (total == 0) return "Không đồng chẵn./.";

                string result = "";
                long number = (long)Math.Abs(total);
                int unitIndex = 0;

                while (number > 0)
                {
                    int group = (int)(number % 1000);
                    if (group > 0)
                    {
                        string groupText = "";
                        int h = group / 100;
                        int t = (group % 100) / 10;
                        int u = group % 10;

                        if (h > 0 || number > 1000) groupText += _digits[h] + " trăm ";
                        if (t > 1) groupText += _digits[t] + " mươi ";
                        else if (t == 1) groupText += "mười ";
                        else if (h > 0 && u > 0) groupText += "lẻ ";

                        if (t > 1 && u == 1) groupText += "mốt ";
                        else if (t > 0 && u == 5) groupText += "lăm ";
                        else if (u > 0) groupText += _digits[u] + " ";

                        result = groupText + _unit[unitIndex] + " " + result;
                    }
                    number /= 1000;
                    unitIndex++;
                }

                result = result.Trim().Replace("  ", " ");
                if (string.IsNullOrEmpty(result)) return "Không đồng chẵn./.";

                return char.ToUpper(result[0]) + result[1..];
            }
            catch
            {
                return "Lỗi đọc số";
            }
        }
    }
}
