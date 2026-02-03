using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using System.Data.SqlClient;
using System.Data;

namespace ERP_Portal_RC.Infrastructure.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private const string BosOnlineDb = "BosOnline";
        private const string BosConfigureDb = "bosConfigure";

        public AccountRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }


        //Store này không dùng được -> exec bosGetUserByLoginName_Onl 'TranThanhThien' //Db BosOnline
        public async Task<IEnumerable<UserOnAp>> GetUserByLoginNameAsync(string loginName)
        {
            var users = new List<UserOnAp>();
            SqlConnection? connection = null;

            try
            {
                connection = _dbConnectionFactory.OpenConnection(BosOnlineDb);

                using var command = new SqlCommand("bosGetUserByLoginName_Onl", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@LoginName", loginName ?? string.Empty);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    users.Add(new UserOnAp
                    {
                        LoginName = reader["LoginName"]?.ToString(),
                        APIlogin = reader["APIlogin"]?.ToString(),
                        UserCode = reader["UserCode"]?.ToString(),
                        FullName = reader["FullName"]?.ToString()
                    });
                }

                return users;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Lỗi khi lấy thông tin user: {loginName}", ex);
            }
            finally
            {
                if (connection != null)
                {
                    _dbConnectionFactory.CloseConnection(connection);
                }
            }
        }

        public async Task<IEnumerable<ApplicationToolMenu>> GetApplicationMenuByGroupAsync(string groupList)
        {
            var menus = new List<ApplicationToolMenu>();
            SqlConnection? connection = null;
            try
            {
                connection = _dbConnectionFactory.OpenConnection(BosConfigureDb);

                using var command = new SqlCommand("bosGetApplicationTools_ByGroupUser_Onl", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@Grp_Code", groupList ?? string.Empty);

                using var reader = await command.ExecuteReaderAsync();

                // Lấy danh sách cột thực tế trả về từ Store để check an toàn
                var columnNames = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();

                while (await reader.ReadAsync())
                {
                    var menu = new ApplicationToolMenu
                    {
                        MenuID = reader["MenuID"]?.ToString() ?? "",
                        ParentID = reader["ParentID"]?.ToString() ?? "",
                        MenuDscpt = reader["MenuDscpt"]?.ToString() ?? "",
                        MenuIcon = reader["MenuIcon"]?.ToString() ?? "",
                        AcssForm = reader["AcssForm"]?.ToString() ?? "",
                        AppID = reader["AppID"]?.ToString() ?? "",
                        AcssRght = reader["AcssRght"] != DBNull.Value ? Convert.ToInt32(reader["AcssRght"]) : 0,
                        ViewRght = reader["ViewRght"] != DBNull.Value ? Convert.ToInt32(reader["ViewRght"]) : 0,
                        IsGroup = columnNames.Contains("IsGroup") && reader["isGroup"] != DBNull.Value && Convert.ToBoolean(reader["IsGroup"]),
                        IsFunct = columnNames.Contains("IsFunct") && reader["isGroup"] != DBNull.Value && Convert.ToBoolean(reader["IsFunct"]),
                        InToolBar = columnNames.Contains("InToolBar") && reader["isGroup"] != DBNull.Value && Convert.ToBoolean(reader["InToolBar"]),
                        MnCtType = reader["MnCtType"].ToString() ?? "",
                        AcssReport = reader["AcssReport"].ToString() ?? "",
                    };

                    // 1. Đọc 50 Parameters
                    for (int i = 1; i <= 50; i++)
                    {
                        string fieldName = $"Param{i:D2}";
                        if (columnNames.Contains(fieldName))
                        {
                            var val = reader[fieldName]?.ToString();
                            if (!string.IsNullOrWhiteSpace(val))
                            {
                                typeof(ApplicationToolMenu).GetProperty(fieldName)?.SetValue(menu, val);
                            }
                        }
                    }

                    // 2. Đọc 30 Variants [CẬP NHẬT MỚI]
                    for (int i = 1; i <= 30; i++)
                    {
                        string fieldName = $"Variant{i:D2}";
                        if (columnNames.Contains(fieldName))
                        {
                            var val = reader[fieldName]?.ToString();
                            if (!string.IsNullOrWhiteSpace(val))
                            {
                                typeof(ApplicationToolMenu).GetProperty(fieldName)?.SetValue(menu, val);
                            }
                        }
                    }

                    menus.Add(menu);
                }
                return menus;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Lỗi khi lấy menu theo group: {groupList}", ex);
            }
            finally
            {
                if (connection != null) _dbConnectionFactory.CloseConnection(connection);
            }
        }

        public async Task<IEnumerable<ApplicationToolMenu>> GetApplicationMenuByGroupAndSiteAsync(string groupList, string appSite)
        {
            return await GetApplicationMenuByGroupAsync(groupList);
        }
    }
}
