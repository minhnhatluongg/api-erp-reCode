using ERP_Portal_RC.Domain.Entities;

namespace ERP_Portal_RC.Domain.Interfaces
{
    public interface IConnectionRepository
    {
        /// <summary>
        /// Build connection string tới BosEVAT / BosTVAN theo MST.
        /// system: "EVAT" | "EVATNEW" | "TVAN" | "ERP"
        /// </summary>
        string GetCnServerByMST(string mst, string? cccd, string system);

        /// <summary>
        /// Trả về IP server thuần — dùng build JSON payload gửi WebApp.
        /// </summary>
        string GetIPServerByMST(string mst, string? cccd, string system);

        /// <summary>
        /// Trả về toàn bộ thông tin server từ SP — dùng cho CheckServer.
        /// null nếu không gọi được SP.
        /// </summary>
        ServerInfoRow? GetServerInfo(string mst, string? cccd);

        /// <summary>
        /// Connection string cố định tới Server234 — chạy ImportTools_V1.
        /// </summary>
        string GetConnectionStringServer234();
    }
}
