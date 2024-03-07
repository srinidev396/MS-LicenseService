using Dapper;
using LicenseServer.Properties;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Smead.Security;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LicenseServer.Models
{
    public class FusionRMSLicenseModel
    {
        public FusionRMSLicenseModel(Passport passport)
        {
            pass = passport;
        }
        [JsonIgnore]
        public Passport pass { get; set; }
        public async Task<JsonReturn> NewCustomerLicense(UserInterfaceModel model)
        {
            var jsr = new JsonReturn();
            using (var conn = new SqlConnection(pass.ConnectionString))
            {
                await conn.OpenAsync();
                var trans = conn.BeginTransaction();
                try
                {
                    var check = await CheckConditions(model, conn, trans, true);
                    if (check.IsError)
                    {
                        jsr = check;
                        return jsr;
                    }
                    await InsertCustomer(model, trans, conn);
                    model.LCCustomers.Id = await GetNewCustomerId(model, trans, conn);
                    await InsertContact(model, trans, conn);
                    await InsertFusionRMSLicense(model, trans, conn);
                    await trans.CommitAsync();
                }
                catch (Exception ex)
                {
                    await trans.CommitAsync();
                    throw new Exception(ex.Message);
                }

            }
            return jsr;
        }
        public async Task<JsonReturn> ExistingCustomerLicense(UserInterfaceModel model)
        {
            var jsr = new JsonReturn();
            using (var conn = new SqlConnection(pass.ConnectionString))
            {
                await conn.OpenAsync();
                var trans = conn.BeginTransaction();
                try
                {
                    var check = await CheckConditions(model, conn, trans, false);
                    if (check.IsError)
                    {
                        jsr = check;
                        return jsr;
                    }
                    model.LCCustomers.Id = await GetNewCustomerId(model, trans, conn);
                    await InsertFusionRMSLicense(model, trans, conn);
                    await trans.CommitAsync();
                }
                catch (Exception ex)
                {
                    await trans.RollbackAsync();
                    throw new Exception(ex.Message);
                }
            }

            return jsr;
        }
        private async Task<bool> IsProductAndTypeExist(UserInterfaceModel model, SqlConnection conn, SqlTransaction trans)
        {
            var query = "select COUNT(*) from LCTabProductList a join LCLicenseType b on a.Id = b.LCTabProductListId where a.ProductName = @productName and b.TypeName = @licenseType";
            var param = new { @productName = "TABFUSIONRMS", @licenseType = model.LCLicenseType.TypeName };
            return await conn.ExecuteScalarAsync<bool>(query, param, trans);
        }
        private async Task<bool> IsCustomerExist(UserInterfaceModel model, SqlConnection conn, SqlTransaction trans)
        {
            var query = "select * from LCCustomers a where a.CompanyName = @companyname";
            var param = new { @companyname = model.LCCustomers.CompanyName };
            return await conn.ExecuteScalarAsync<bool>(query, param, trans);
        }
        private async Task<bool> IsDatabaseKeyExist(UserInterfaceModel model, SqlConnection conn, SqlTransaction trans)
        {
            var query = "select * from LCFusionRMSLicense a where a.DatabaseKey = @databasekey";

            var param = new { @databasekey = model.LCFusionRMSLicense.DatabaseKey };
            return await conn.ExecuteScalarAsync<bool>(query, param, trans);
        }
        private async Task InsertCustomer(UserInterfaceModel model, SqlTransaction trans, SqlConnection conn)
        {
            var param = new
            {
                @CompanyName = model.LCCustomers.CompanyName,
                @Address = model.LCCustomers.Address,
                @DateCreated = DateTime.Now,
                @City = model.LCCustomers.City,
                @StateProvince = model.LCCustomers.StateProvince,
                @Country = model.LCCustomers.Country,
                @Comment = model.LCCustomers.Comment,
                @ZipCode = model.LCCustomers.ZipCode
            };
            await conn.QueryAsync(Resources.InsertIntoCustomer, param, trans);
        }
        private async Task<int> GetNewCustomerId(UserInterfaceModel model, SqlTransaction trans, SqlConnection conn)
        {
            var param = new { @companyname = model.LCCustomers.CompanyName };
            var customer = await conn.QueryAsync<LCCustomers>("SELECT * FROM LCCustomers a where a.CompanyName = @companyname",param,trans);
            return customer.FirstOrDefault().Id;
        }
        private async Task InsertContact(UserInterfaceModel model, SqlTransaction trans, SqlConnection conn)
        {
            var param = new
            {
                @title = model.LCContact.Title,
                @phone = model.LCContact.Phone,
                @email = model.LCContact.Email,
                @customerid = model.LCCustomers.Id,
                @fullname = model.LCContact.FullName,
            };
            await conn.QueryAsync(Resources.InsertIntoContact, param, trans);
        }
        private async Task InsertFusionRMSLicense(UserInterfaceModel model, SqlTransaction trans, SqlConnection conn)
        {
            string sqlDetails = Encryptions.EncryptKey($"{model.LCFusionRMSLicense.SqlServerName} | {model.LCFusionRMSLicense.DataBaseName} | {model.LCFusionRMSLicense.SqlUser} | {model.LCFusionRMSLicense.SqlPassword}", Encryptions.FusionRMSkeyEncryption);
            var param = new
            {
                @licensecount = model.LCFusionRMSLicense.LicenseCount,
                @activecount = model.LCFusionRMSLicense.ActiveCount,
                @datecreated = DateTime.Now,
                @expirydate = model.LCFusionRMSLicense.ExpiryDate,
                @databasekey = model.LCFusionRMSLicense.DatabaseKey,
                @customerid = model.LCCustomers.Id,
                @licensetype = model.LCLicenseType.TypeName,
                @productname = "TABFUSIONRMS",
                @licensekey = sqlDetails
            };

            await conn.QueryAsync(Resources.InsertIntoFusionRMSLicense, param, trans);
        }
        public async Task<int> GetCustomerUserCount(UserInterfaceModel model)
        {
            var connectionStr = $"Data Source={model.LCFusionRMSLicense.SqlServerName};Initial Catalog={model.LCFusionRMSLicense.DataBaseName};Persist Security Info=True;User ID={model.LCFusionRMSLicense.SqlUser};Password={model.LCFusionRMSLicense.SqlPassword};";
            using (var conn = new SqlConnection(connectionStr))
            {
                return await conn.ExecuteScalarAsync<int>("select count(UserID) from SecureUser");
            }
        }
        private async Task<JsonReturn> CheckConditions(UserInterfaceModel model, SqlConnection conn, SqlTransaction trans, bool isnew)
        {
            var jsr = new JsonReturn();
            /////conditions
            //check for active users
            model.LCFusionRMSLicense.ActiveCount = await GetCustomerUserCount(model);
            if (model.LCFusionRMSLicense.ActiveCount > model.LCFusionRMSLicense.LicenseCount)
            {
                jsr.IsError = true;
                jsr.ErrorType = ErrorType.ActiveUser;
                jsr.Message = $"The database '<span style='color: blue'>{model.LCFusionRMSLicense.DataBaseName}</span>' has {model.LCFusionRMSLicense.ActiveCount} users, but only {model.LCFusionRMSLicense.LicenseCount} licenses assigned. Please increase the licenses to cover all users.";
                return jsr;
            }
            //chack if license model exist
            if (!await IsProductAndTypeExist(model, conn, trans))
            {
                jsr.IsError = true;
                jsr.ErrorType = ErrorType.ProductNotExist;
                jsr.Message = $"this Product is not exist in TabFusionRMS license model!";
                return jsr;
            }
            //check for customer duplication
            if (isnew)
            {
                if (await IsCustomerExist(model, conn, trans))
                {
                    jsr.IsError = true;
                    jsr.ErrorType = ErrorType.CustomerExist;
                    jsr.Message = $"A customer with the name <span style='color:blue'>{model.LCCustomers.CompanyName}</span> already exists in our system. If you'd like to add an additional license for this customer, please return to the main menu and select the option to add a license to an existing customer.";
                    return jsr;
                }
            }
            //check for database key duplication
            if (await IsDatabaseKeyExist(model, conn, trans))
            {
                jsr.IsError = true;
                jsr.ErrorType = ErrorType.DatabasekeyExist;
                jsr.Message = $"The database key <span style='color:blue'> {model.LCFusionRMSLicense.DatabaseKey}</span> already exists and must be unique. Please provide a different key to proceed.";
                return jsr;
            }
           
            return jsr;
        }
    }
}
public class JsonReturn
{
    public ErrorType ErrorType { get; set; }
    public string Message { get; set; }
    public bool IsError { get; set; }
    public IEnumerable<LCLicenseType> LicenseType { get; set; }
    public IEnumerable<LCCustomers> Customers { get; set; }
}

public enum ErrorType
{
    General = -1,
    Exception = 0,
    ProductNotExist = 100,
    CustomerExist = 200,
    DatabasekeyExist = 300,
    SigninFaild = 400,
    ActiveUser = 500
}

public class UserInterfaceModel
{
    public LCCustomers LCCustomers { get; set; }
    public LCContact LCContact { get; set; }
    public LCLicenseType LCLicenseType { get; set; }
    public LCTabProductList LCTabProductList { get; set; }
    public LCFeaturesLog LCFeaturesLog { get; set; }
    public FCFeatures FCFeatures { get; set; }
    public LCFusionRMSLicense LCFusionRMSLicense { get; set; }
    public LCUserLog LCUserLog { get; set; }
    public bool IsNewCustomer { get;set; }
}
public class LCCustomers
{
    public int Id { get; set; }
    public string CompanyName { get; set; }
    public string Address { get; set; }
    public DateTime DateCreated { get; set; }
    public string City { get; set; }
    public string StateProvince { get; set; }
    public string Country { get; set; }
    public string Comment { get; set; }
    public string ZipCode { get; set; }
}

public class LCContact
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
    public int CustomerId { get; set; }
    public string FullName { get; set; }
}

public class LCLicenseType
{
    public int Id { get; set; }
    public string TypeName { get; set; }
    public int TabProductListId { get; set; }
    public DateTime DateCreated { get; set; }
}

public class LCTabProductList
{
    public int Id { get; set; }
    public string ProductName { get; set; }
    public DateTime DateCreated { get; set; }
}

public class LCFeaturesLog
{
    public int Id { get; set; }
    public string UserName { get; set; }
    public DateTime DateCreated { get; set; }
    public string FeatureName { get; set; }
    public int CustomerId { get; set; }
    public bool HasAccess { get; set; }
}

public class FCFeatures
{
    public int Id { get; set; }
    public string Feature { get; set; }
    public bool Enable { get; set; }
    public DateTime DateCreated { get; set; }
    public int LicenseTypeId { get; set; }
}

public class LCFusionRMSLicense
{
    public int Id { get; set; }
    public int LicenseCount { get; set; }
    public int ActiveCount { get; set; }
    public DateTime DateCreated { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string DatabaseKey { get; set; }
    public string LCCustomersId { get; set; }
    public string LicenseType { get; set; }
    public string ProductName { get; set; }
    public string LicenseKey { get; set; }
    //encrypt the 4 properties below and save it inside Licensekey
    public string SqlServerName { get; set; }
    public string DataBaseName { get; set; }
    public string SqlUser { get; set; }
    public string SqlPassword { get; set; }

}


public class LCUserLog
{
    public int Id { get; set; }
    public string UserName { get; set; }
    public string Product { get; set; }
    public bool IsSuccess { get; set; }
    public string AccountType { get; set; }
    public string Origin { get; set; }
    public DateTime DateCreated { get; set; }
    public int CustomerId { get; set; }
}
