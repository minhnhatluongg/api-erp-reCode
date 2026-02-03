using Dapper;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Infrastructure.Repositories
{
    public class DSignaturesRepository : IDSignaturesRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private const string BosConfigureDb = "bosConfigure";
        public DSignaturesRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }
        public async Task<DSMenuViewModel> GetDSMenuByID(string loginname, string grp_code)
        {
            var model = new DSMenuViewModel();
            var bosMenu = new List<bosMenuRight?>();

            SqlConnection? connection = null;
            try
            {
                connection = _dbConnectionFactory.OpenConnection(BosConfigureDb);

                string[] groups = (grp_code ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var group in groups)
                {
                    var cleanedGroup = group.Replace("'", "").Trim();

                    var parameters = new DynamicParameters();
                    parameters.Add("@loginname", loginname, DbType.String);
                    parameters.Add("@CmpnID", "00");
                    parameters.Add("@LanguageDefault", "VN");
                    parameters.Add("@grp_code", cleanedGroup);

                    using (var result = await connection.QueryMultipleAsync("bosConfigure.dbo.bosGetUserByLoginName_Onl_Grp",
                        parameters,
                        commandType: CommandType.StoredProcedure))
                    {
                        var userData = await result.ReadFirstOrDefaultAsync<ApplicationUser>();
                        if (model.ApplicationUser == null) model.ApplicationUser = userData;
                        var rights = (await result.ReadAsync<bosMenuRight>()).ToList();
                        bosMenu.AddRange(rights);
                    }

                    model.bosMenuRight = bosMenu;

                    if (model.bosMenuRight != null && model.bosMenuRight.Any())
                    {
                        bool has81003 = model.bosMenuRight.Any(m => m != null && m.MenuID == "81003");
                        bool has81004 = model.bosMenuRight.Any(m => m != null && m.MenuID == "81004");

                        if (has81003 && has81004)
                        {
                            model.mode = 3;
                        }
                        else if (has81003)
                        {
                            model.mode = 1;
                        }
                        else if (has81004)
                        {
                            model.mode = 2;
                        }
                    }
                }
                return model;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Lỗi GetDSMenuByID cho user: {loginname}", ex);
            }
            finally
            {
                if (connection != null)
                {
                    _dbConnectionFactory.CloseConnection(connection);
                }
            }
        }
    }
}
