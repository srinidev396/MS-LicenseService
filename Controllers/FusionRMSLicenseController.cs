using LicenseServer.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Data.SqlClient;
using Dapper;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Logging;

namespace LicenseServer.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class FusionRMSLicenseController : ControllerBase
    {
        private IConfiguration config;
        private ILogger<FusionRMSValidationController> _logger;
        public FusionRMSLicenseController(IConfiguration config, ILogger<FusionRMSValidationController> logger)
        {
            this.config = config;
            _logger = logger;

        }
        [HttpPost("GenerateFusionRMSLicense")]
        public async Task<JsonReturn> GenerateFusionRMSLicense(UserInterfaceModel model)
        {
            var jsr = new JsonReturn();
            try
            {
                var u = new SecurityAccess(config);
                var pass = u.GetPassport(User.Identity.Name);
                var m = new FusionRMSLicenseModel(pass);
                if (!pass.SignedIn)
                {
                    jsr.Message = "Faild to authenticate!";
                    jsr.IsError = true;
                    jsr.ErrorType = ErrorType.SigninFaild;
                    return jsr;
                }
                else
                {
                    if (model.IsNewCustomer)
                    {
                        jsr = await m.NewCustomerLicense(model);
                    }
                    else
                    {
                        jsr = await m.ExistingCustomerLicense(model);
                    }

                }
            }
            catch (Exception ex)
            {
                jsr.IsError = true;
                jsr.ErrorType = ErrorType.Exception;
                jsr.Message = ex.Message;
                _logger.LogError(ex.Message, ex);
            }

            return jsr;
        }
        [HttpGet("GetDropdowns")]
        public async Task<JsonReturn> GetDropdowns()
        {
            var jsr = new JsonReturn();

            try
            {
                var u = new SecurityAccess(config);
                var pass = u.GetPassport(User.Identity.Name);
                using (var conn = new SqlConnection(pass.ConnectionString))
                {
                    jsr.LicenseType = await conn.QueryAsync<LCLicenseType>("select id, TypeName from LCLicenseType");
                    jsr.Customers = await conn.QueryAsync<LCCustomers>("select Id, CompanyName from LCCustomers");
                }
            }
            catch (Exception ex)
            {
                jsr.IsError = true;
                jsr.ErrorType = ErrorType.General;
                jsr.Message = ex.Message;
                _logger.LogError(ex.Message, ex);
            }
            return jsr;
        }
        [HttpGet("IsCustomerExist")]
        public async Task<bool> IsCustomerExist(string companyname)
        {
            var u = new SecurityAccess(config);
            var pass = u.GetPassport(User.Identity.Name);
            using (var conn = new SqlConnection(pass.ConnectionString))
            {
                var query = "select * from LCCustomers where LOWER(CompanyName) = LOWER(@customer)";
                var param = new { @customer = companyname };
                return await conn.ExecuteScalarAsync<bool>(query, param);
            }
        }

    }
}
