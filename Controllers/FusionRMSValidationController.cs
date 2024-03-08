using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Dapper;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using LicenseServer.Models;
using System;
using System.Xml.Linq;

namespace LicenseServer.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class FusionRMSValidationController : ControllerBase
    {
        private string connectionStr = "";
        private ILogger<FusionRMSValidationController> _logger;
        private readonly IConfiguration _config;
        public FusionRMSValidationController(ILogger<FusionRMSValidationController> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
            connectionStr = _config.GetSection("ConnectionStr").Value;
        }
        //FusionRMS calls
        [HttpGet("Ping")]
        public async Task<string> Ping()
        {
            using (var conn = new SqlConnection(connectionStr))
            {
                await conn.QueryAsync<string>("SELECT * FROM sys.databases WHERE name = 'License'");
            }
            return "Server is up and runnig!";
        }
        [HttpGet("GetListOfRegisterDatabases")]
        public async Task<LicenseDetails> GetListOfRegisterDatabases()
        {
            var lcd = new LicenseDetails();
            using (var conn = new SqlConnection(connectionStr))
            {
                lcd.DatabasekeyList = await conn.QueryAsync<DatabaseKeyList>("select Id as [Index], DatabaseKey as KeyName from LCFusionRMSLicense");
            }

            return lcd;
        }
        [HttpGet("ValidateFusionRMSLicense")]
        public async Task<LicenseDetails> ValidateFusionRMSLicense(string dbKeyname)
        {
            var lic = new LicenseDetails();
            try
            {
                lic = await lic.GetLicenseProperties(connectionStr, dbKeyname);
                lic = await lic.CheckConditions(lic);
            }
            catch (Exception ex)
            {
                lic.Valid = false;
                lic.ErrorMsg = ex.Message;
            }
            
            return lic;
        }
        [HttpPost("InsertUserLogs")]
        public async Task InsertUserLogs(LicenseLogger model)
        {
            using (var conn = new SqlConnection(connectionStr))
            {
                var param = new {
                    @username = model.Username,
                    @product = model.Product, 
                    @issuccess = model.IsSuccess, 
                    @accounttype = model.AccountType, 
                    @origion = model.Origin,
                    @datecreated = DateTime.Now, 
                    @customerid = model.CustomerId,
                    @databasekey = model.DatabaseKey
                };
              var rowaffected =  await conn.ExecuteAsync("" +
                    "insert into LCUsersLog (UserName, Product, IsSuccess, AccountType, Origion, DateCreated, LCCustomersId, Databasekey)" +
                    "values(@username, @product, @issuccess, @accounttype, @origion, @datecreated, @customerid, @databasekey)", param);
            }
        }
    }
}
