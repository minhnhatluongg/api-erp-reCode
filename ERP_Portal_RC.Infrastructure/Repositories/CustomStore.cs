using Dapper;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using System.Data;
using System.Data.SqlClient;

namespace ERP_Portal_RC.Infrastructure.Repositories
{
    public class CustomStore : ICustomStore
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private const string BosConfigureDb = "BosConfigure";
        private const string BosOnlineDb = "BosOnline";

        public CustomStore(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<IEnumerable<ApplicationUser>> FindByLoginNameAsync(string loginName)
        {
            SqlConnection? connection = null;
            try
            {
                connection = _dbConnectionFactory.OpenConnection(BosConfigureDb);
                
                var result = await connection.QueryAsync<ApplicationUser>(
                    "bosConfigure.dbo.bosGetApplicationTools_ByGroupUser_Onl",
                    new { Model = loginName },
                    commandType: CommandType.StoredProcedure);

                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Lỗi khi tìm user: {loginName}", ex);
            }
            finally
            {
                if (connection != null)
                {
                    _dbConnectionFactory.CloseConnection(connection);
                }
            }
        }

        public async Task<IEnumerable<UserOnAp>> GetUserByLoginNameAsync(string loginName, string cmpnId)
        {
            SqlConnection? connection = null;
            try
            {
                connection = _dbConnectionFactory.OpenConnection(BosConfigureDb);

                var result = await connection.QueryAsync<UserOnAp>(
                    "bosConfigure.dbo.bosGetUserByLoginName_Onl",
                    new
                    {
                        LognName = loginName,
                        CmpnID = cmpnId,
                        LanguageDefault = "VN"
                    },
                    commandType: CommandType.StoredProcedure);

                return result;
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

        public async Task<IEnumerable<int>> CheckUserByLoginNameAsync(string loginName)
        {
            SqlConnection? connection = null;
            try
            {
                connection = _dbConnectionFactory.OpenConnection(BosConfigureDb);

                var result = await connection.QueryAsync<int>(
                    "bosConfigure.dbo.bos_ChkUser",
                    new { LoginName = loginName },
                    commandType: CommandType.StoredProcedure);

                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Lỗi khi check user: {loginName}", ex);
            }
            finally
            {
                if (connection != null)
                {
                    _dbConnectionFactory.CloseConnection(connection);
                }
            }
        }

        public async Task<int> ChkUser(string loginName)
        {
            SqlConnection? connection = null;
            try
            {
                connection = _dbConnectionFactory.OpenConnection(BosConfigureDb);

                return connection.QueryFirstOrDefault<int>(
                    "bosConfigure.dbo.bos_ChkUser",
                    new { LoginName = loginName },
                    commandType: CommandType.StoredProcedure);
            }
            catch (Exception)
            {
                return 0;
            }
            finally
            {
                if (connection != null)
                {
                    _dbConnectionFactory.CloseConnection(connection);
                }
            }
        }

        public async Task<IEnumerable<web_bosMenu_ByGroup>> GetApplicationToolsByGroupAsync(string groupList)
        {
            SqlConnection? connection = null;
            try
            {
                connection = _dbConnectionFactory.OpenConnection(BosConfigureDb);

                var result = await connection.QueryAsync<web_bosMenu_ByGroup>(
                    "bosConfigure.dbo.bosGetApplicationTools_ByGroupUser_Onl",
                    new { Grp_Code = groupList },
                    commandType: CommandType.StoredProcedure);

                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Lỗi khi lấy menu theo group: {groupList}", ex);
            }
            finally
            {
                if (connection != null)
                {
                    _dbConnectionFactory.CloseConnection(connection);
                }
            }
        }

        public int CreateUser(ApplicationUser user)
        {
            SqlConnection? connection = null;
            try
            {
                connection = _dbConnectionFactory.OpenConnection(BosConfigureDb);

                var result = connection.QueryFirstOrDefault<int>(
                    "bosConfigure.dbo.bosInsertUserOnApp",
                    new
                    {
                        App = "WINECONTRACT",
                        AppLoginCode = string.Empty,
                        AppLoginName = user.LoginName,
                        AppLoginPassword = user.Password,
                        AppLoginFullName = user.FullName,
                        AppLoginEmail = user.Email
                    },
                    commandType: CommandType.StoredProcedure);

                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Lỗi khi tạo user: {user.LoginName}", ex);
            }
            finally
            {
                if (connection != null)
                {
                    _dbConnectionFactory.CloseConnection(connection);
                }
            }
        }

        public async Task<int> AddUserToGroup(string userCode)
        {
            SqlConnection? connection = null;
            IDbTransaction? transaction = null;

            try
            {
                connection = _dbConnectionFactory.OpenConnection(BosOnlineDb);
                transaction = connection.BeginTransaction();

                var parameters = new DynamicParameters();
                parameters.Add("@DESCRIP", "NHÂN VIÊN.NHÂN VIÊN KINH DOANH.TOÀN HỆ THỐNG.CÔNG TY CP MONET");
                parameters.Add("@Grp_Code", "00006.00084.00121");
                parameters.Add("@UserCode", userCode);
                parameters.Add("@SignNumb", 0);
                parameters.Add("@SignDate", DateTime.Now);
                parameters.Add("@Crt_User", "000015");
                parameters.Add("@Crt_Date", DateTime.Now);
                parameters.Add("@ChgeUser", "000015");
                parameters.Add("@ChgeDate", DateTime.Now);
                parameters.Add("@CmpnID", "00");

                connection.QueryFirstOrDefault<int>(
                    "wpsInsert_onGroup",
                    param: parameters,
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure);

                transaction.Commit();
                return 1;
            }
            catch (Exception ex)
            {
                transaction?.Rollback();
                throw new InvalidOperationException($"Lỗi khi thêm user vào group: {userCode}", ex);
            }
            finally
            {
                transaction?.Dispose();
                if (connection != null)
                {
                    _dbConnectionFactory.CloseConnection(connection);
                }
            }
        }
    }
}
