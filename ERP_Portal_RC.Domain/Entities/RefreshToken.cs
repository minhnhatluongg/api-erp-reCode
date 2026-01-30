namespace ERP_Portal_RC.Domain.Entities
{
    /// <summary>
    /// Entity cho Refresh Token - dùng để lưu trữ token và quản lý phiên đăng nhập
    /// </summary>
    public class RefreshToken
    {
        public int Id { get; set; }
        
        /// <summary>
        /// User Id liên kết với token
        /// </summary>
        public string UserId { get; set; } = string.Empty;
        
        /// <summary>
        /// Refresh token string
        /// </summary>
        public string Token { get; set; } = string.Empty;
        
        /// <summary>
        /// JWT ID để link với access token
        /// </summary>
        public string JwtId { get; set; } = string.Empty;
        
        /// <summary>
        /// Token đã được sử dụng chưa
        /// </summary>
        public bool IsUsed { get; set; }
        
        /// <summary>
        /// Token đã bị revoke chưa
        /// </summary>
        public bool IsRevoked { get; set; }
        
        /// <summary>
        /// Thời gian tạo
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Thời gian hết hạn
        /// </summary>
        public DateTime ExpiresAt { get; set; }
        
        /// <summary>
        /// IP address của client
        /// </summary>
        public string? IpAddress { get; set; }
        
        /// <summary>
        /// User agent của client
        /// </summary>
        public string? UserAgent { get; set; }
    }
}
