using Dapper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace LicenseServer.Models
{
    
    public class LicenseDetails
    {
        public string sqlusername { get; set; }
        public string sqlpassword { get; set; }
        public string sqlservername { get; set; }
        public string DatabaseName { get; set; }
        public bool Valid { get; set; }
        public IEnumerable<DatabaseKeyList> DatabasekeyList { get; set; }
        public string ErrorMsg { get; set; }
        public bool Exception { get; set; }
        public int LicenseTypeId { get; set; }
        public string LicenseType { get; set; }
        public string CompanyName { get; set; }
        public int LCCustomersId { get; set; }
        public int LicenseCount { get; set; }
        public int ActiveCount { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string LicenseKey { get; set; }
        public string DatabaseKey { get; set; }
        public string ProductName { get; set; }
        public string Username { get; set; }
        public bool IsSuccess { get; set; }
        public string Origin { get; set; }

        public bool FLabelBlackwhite { get; set; }
        public bool FTabQuick { get; set; }
        public bool FTransfer { get; set; }
        public bool FRequest { get; set; }
        public bool FAttachment { get; set; }
        public bool FRetention { get; set; }
        public bool FDashboard { get; set; }
        public async Task<LicenseDetails> GetLicenseProperties(string connectionStr, string key)
        {
            var lcmodel = new LicenseDetails();
            IEnumerable<FeaturesBinder> features; 
            using (var conn = new SqlConnection(connectionStr))
            {
                var param = new {@key = key };
                lcmodel = await conn.QuerySingleAsync<LicenseDetails>("select a.*, b.*, c.TypeName,c.Id as LicenseTypeId from LCFusionRMSLicense a join LCCustomers b on a.LCCustomersId = b.Id join LCLicenseType c on a.LicenseType = c.TypeName where a.DatabaseKey = @key", param);
                var param1 = new {@typeid = lcmodel.LicenseTypeId };
                features = await conn.QueryAsync<FeaturesBinder>("select a.Feature, a.[Enable] from LCFeatures a where LCLicenseTypeId = @typeid", param1);
            }
            
            foreach (var Item in features)
            {
                switch (Item.Feature)
                {
                    case "FLabelBlackwhite":
                        lcmodel.FLabelBlackwhite = Item.Enable;
                        break;
                    case "FTabQuick":
                        lcmodel.FTabQuick = Item.Enable;
                        break;
                    case "FTransfer":
                        lcmodel.FTransfer = Item.Enable;
                        break;
                    case "FRequest":
                        lcmodel.FRequest = Item.Enable;
                        break;
                    case "FAttachment":
                        lcmodel.FAttachment = Item.Enable;
                        break;
                    case "FRetention":
                        lcmodel.FRetention = Item.Enable;
                        break;
                    case "FDashboard":
                        lcmodel.FDashboard = Item.Enable;
                        break;
                    default:
                        break;
                }
            }
            var GetSql = Encryptions.DecryptKey(lcmodel.LicenseKey, Encryptions.FusionRMSkeyEncryption).Split("|");
            lcmodel.sqlservername = GetSql[0];
            lcmodel.DatabaseName = GetSql[1];
            lcmodel.sqlusername = GetSql[2];
            lcmodel.sqlpassword = GetSql[3];
         

            return lcmodel;
        }
        public async Task<LicenseDetails> CheckConditions(string connctionstr, LicenseDetails model)
        {
            //chack active users
            var activeusers = await GetCustomerUserCount(connctionstr, model);
            if (activeusers > model.LicenseCount)
            {
                model.Valid = false;
                model.ErrorMsg = $"The database '<span style='color: blue'>{model.DatabaseName}</span>' has {activeusers} users, but only {model.LicenseCount} licenses assigned. Please increase the licenses to cover all users.";
                return model;
            }
            //check for expiration date
            if(model.ExpiryDate.Date < DateTime.Now.Date)
            {
                model.Valid = false;
                model.ErrorMsg = $"Your license has expired as of {model.ExpiryDate.ToString("MMMM dd, yyyy")}";
                return model;
            }
            model.Valid = true;
            return model;
        }
        private async Task<int> GetCustomerUserCount(string LicensedbConnectionStr,LicenseDetails model)
        {
            var connectionStr = $"Data Source={model.sqlservername};Initial Catalog={model.DatabaseName};Persist Security Info=True;User ID={model.sqlusername};Password={model.sqlpassword};";
            int userCount = 0;   
            using (var conn = new SqlConnection(connectionStr))
            {
                userCount =  await conn.ExecuteScalarAsync<int>("select count(UserID) from SecureUser");
            }
            //update active users
            using (var conn1 = new SqlConnection(LicensedbConnectionStr))
            {
                var param = new { @activeuser = userCount, @databasekey = model.DatabaseKey };
                await conn1.ExecuteAsync("update LCFusionRMSLicense set ActiveCount = @activeuser where DatabaseKey = @databasekey", param);
            }
            

            return userCount;
        }
    }

    public class DatabaseKeyList
    {
        public string KeyName { get; set; }
        public int Index { get; set; }
    }

    public class FeaturesBinder
    {
        public string Feature { get; set; }
        public bool Enable { get; set; }
    }
}
