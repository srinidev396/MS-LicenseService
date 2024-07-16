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
using Microsoft.AspNetCore.Hosting.Server;
using System.ComponentModel;

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
            _logger.LogInformation("License Server is up and runnig!");
            return "License Server is up and runnig!";
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
                lic = await lic.CheckConditions(connectionStr, lic);
            }
            catch (Exception ex)
            {
                lic.Valid = false;
                lic.ErrorMsg = ex.Message;
            }
            
            return lic;
        }
        [HttpPost("InsertUserLogs")]
        public async Task InsertUserLogs(LicenseDetails model)
        {
            using (var conn = new SqlConnection(connectionStr))
            {
                var param = new {
                    @username = model.Username,
                    @product = model.ProductName, 
                    @issuccess = model.IsSuccess, 
                    @accounttype = "", 
                    @origion = model.Origin,
                    @datecreated = DateTime.Now, 
                    @customerid = model.LCCustomersId,
                    @databasekey = model.DatabaseKey
                };
              var rowaffected =  await conn.ExecuteAsync("" +
                    "insert into LCUsersLog (UserName, Product, IsSuccess, AccountType, Origion, DateCreated, LCCustomersId, Databasekey)" +
                    "values(@username, @product, @issuccess, @accounttype, @origion, @datecreated, @customerid, @databasekey)", param);
            }
        }
        [HttpGet("InsertFeaturelog")]
        public async Task InsertFeaturelog(string username, string productname, string featurename, bool hasaccess, int customerid, string databasekey)
        {
            try
            {
                using (var conn = new SqlConnection(connectionStr))
                {
                    var param = new
                    {
                        @username = username,
                        @datecreated = DateTime.Now,
                        @productname = "TABFUSIONRMS",
                        @featurename = featurename,
                        @hasaccess = hasaccess,
                        @customerid = customerid,
                        @databasekey = databasekey
                    };
                    var rowaffected = await conn.ExecuteAsync("" +
                          "insert into LCFeaturesLog (UserName, DateCreated, ProductName, FeatureName, HasAccess, LCCustomersId, Databasekey)" +
                          "values(@username,@datecreated,@productname,@featurename, @hasaccess,  @customerid, @databasekey)", param);
                }
            }
            catch (Exception ex)
            {
                var x = ex.Message;
            }
            
        }
        [HttpGet("GetAllCustomerDatabases")]
        public async Task<List<LicenseDetails>> GetAllCustomerDatabases()
        {
            var lcd = new LicenseDetails();
            var lcds = new List<LicenseDetails>();
            using (var conn = new SqlConnection(connectionStr))
            {
                lcd.DatabasekeyList = await conn.QueryAsync<DatabaseKeyList>("select Id as [Index], DatabaseKey as KeyName from LCFusionRMSLicense");
            }
            foreach (var item in lcd.DatabasekeyList)
            {
                var dbdetails = await lcd.GetLicenseProperties(connectionStr, item.KeyName);
                lcds.Add(await lcd.CheckConditions(connectionStr, dbdetails));
            }
            return lcds;
        }
    }
}
