using System.IO;
using System.Security.Cryptography;
using System.Text;
using System;
using System.Globalization;
using System.ComponentModel.DataAnnotations;
using Smead.Security;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Hosting.Server;
using LicenseServer.Properties;
using System.Data;
using System.Threading.Tasks;
using System.Reflection;
using System.Transactions;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

namespace LicenseServer.Models
{
    public class LicenseAccess
    {
        private const string LicEncryptionKey = "TABLicSA2V66WTEKey";
        private const string AutEncryptionKey = "TABAutSD2V77WTEKey";
        private static string EncryptKey(string clearText, string encryKey)
        {
            try
            {
                var clearBytes = Encoding.Unicode.GetBytes(clearText);
                using (var encryptor = Aes.Create())
                {
                    using (var pdb = new Rfc2898DeriveBytes(encryKey, new byte[] { 0x49, 0x76, 0x61, 0x6E, 0x20, 0x4D, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 }, 1000, HashAlgorithmName.SHA256))
                    {
                        encryptor.Key = pdb.GetBytes(32);
                        encryptor.IV = pdb.GetBytes(16);
                        using (var ms = new MemoryStream())
                        {
                            using (var cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                            {
                                cs.Write(clearBytes, 0, clearBytes.Length);
                                cs.FlushFinalBlock();
                            }

                            clearText = Convert.ToBase64String(ms.ToArray());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogErrorMessages.LogErrorMessage(ex);
            }
            return clearText;
        }
        private static string DecryptKey(string cipherText, string encryKey)
        {
            try
            {
                var cipherBytes = Convert.FromBase64String(cipherText);

                using (var encryptor = Aes.Create())
                {
                    using (var pdb = new Rfc2898DeriveBytes(encryKey, new byte[] { 0x49, 0x76, 0x61, 0x6E, 0x20, 0x4D, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 }, 1000, HashAlgorithmName.SHA256))
                    {
                        encryptor.Key = pdb.GetBytes(32);
                        encryptor.IV = pdb.GetBytes(16);
                        using (var ms = new MemoryStream())
                        {
                            using (var cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                            {
                                cs.Write(cipherBytes, 0, cipherBytes.Length);
                                cs.FlushFinalBlock();
                            }
                            cipherText = Encoding.Unicode.GetString(ms.ToArray());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogErrorMessages.LogErrorMessage(ex);
            }

            return cipherText;
        }
        //purpose: to make sure Tab support this product and type
        //##################################################################################
        internal static bool IsProductAndTypeExist(GenerateNewLicense model, Passport pass)
        {
            var table = new DataTable();
            var conn = new SqlConnection(pass.ConnectionString);
            conn.Open();
            var cmd = new SqlCommand(Resources.IsProductAndTypeExist, conn);
            cmd.Parameters.AddWithValue("@productid", model.ProductId);
            cmd.Parameters.AddWithValue("@enumid", model.LicenseTypeEnumid);
            var adp = new SqlDataAdapter(cmd);
            adp.Fill(table);
            conn.Close();

            if (table.Rows.Count == 1)
            {
                model.ProductName = table.Rows[0].Field<string>("ProductName");
                model.LicenseType = table.Rows[0].Field<string>("TypeName");
                return true;
            }
            else
            {
                return false;
            }
        }
        //##################################################################################
        //purpose: to create a new license
        //##################################################################################
        internal static string GenerateNewLicense(GenerateNewLicense model, Passport pass, IConfiguration config)
        {
            var conn = new SqlConnection(pass.ConnectionString);
            conn.Open();
            var transaction = conn.BeginTransaction();
            int newId = 0;
            try
            {
                //to create license with existing customer.
                if(model.CustomerId == 0)
                {
                    //insert data into customer table 
                    InsertIntoCustomer(conn, transaction, model);
                    newId = GetNewCustomerId(conn, transaction);
                    //insert data into contact table
                    InsertIntoContact(conn, transaction, model, newId);
                }
                else
                {
                    newId = model.CustomerId;
                }
               
                //insert data into license table
                string licensekeyAndAuth = InsertIntoLicense(conn, transaction, model,config, newId);
                transaction.Commit();
                conn.Close();
                return $"License key: {licensekeyAndAuth}";
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                conn.Close();
                return ex.Message;
            }
        }
        //internal static string GenerateNewLicense(GenerateNewLicense model, Passport pass, IConfiguration config)
        //{
        //    var conn = new SqlConnection(pass.ConnectionString);
        //    conn.Open();
        //    var transaction = conn.BeginTransaction();
        //    int newId = 0;
        //    try
        //    {
        //        string credential = $"{config.GetSection("Apiauth").GetSection("userName").Value},{config.GetSection("Apiauth").GetSection("password").Value}, {config.GetSection("Apiauth").GetSection("Database").Value}";
        //        //insert data into customer table 
        //        InsertIntoCustomer(conn, transaction, model);
        //        newId = GetNewId(conn, transaction);
        //        string licenseKey = EncryptKey(newId.ToString(), LicEncryptionKey);
        //        string autkey = EncryptKey(credential, AutEncryptionKey);
        //        string licensekeyAndAuth = $"{autkey}{(char)225}|{(char)225}{licenseKey}";
        //        //insert data into contact table
        //        InsertIntoContact(conn, transaction, model, newId);
        //        //insert data into license table
        //        InsertIntoLicense(conn, transaction, model, licensekeyAndAuth, newId);
        //        transaction.Commit();
        //        conn.Close();
        //        return $"License key: {autkey}{(char)225}|{(char)225}{licenseKey}";
        //    }
        //    catch (Exception ex)
        //    {
        //        transaction.Rollback();
        //        conn.Close();
        //        return ex.Message;
        //    }
        //}
        internal static LicenseDetails LicenseValidation(LicenseValidation model, Passport pass)
        {
            var Id = Convert.ToInt32(DecryptKey(model.LicenseKey, LicEncryptionKey));
            var conn = new SqlConnection(pass.ConnectionString);
            conn.Open();
            //update active count anytime you validate license
            UpdateActiveCount(conn, model, Id);
            //return all license details
            var lic = ReturnLicenseDetails(conn, Id);
            //first check if license exist in the database
            if (lic.rowCount == 0)
            {
                lic.valid = false;
                lic.Message = "We coudln't find license for the database!";
            }
            //check if user in database greater than user purchased 
            else if (model.ActiveCount > lic.LicenseCount)
            {
                lic.valid = false;
                lic.Message = "The number of named users within your database is in violation with your product license.  Please contact TAB for further support.!";
            }
            //check if it is the first connection to the database if yes, then update database name and validate the license  
            else if (lic.SqlServer == null && lic.Database == model.Database || lic.SqlServer == "" && lic.Database == model.Database)
            {
                lic.valid = true;
                lic.SqlServer = model.SqlServer;
                lic.Message = "This is the first connection to the database!";
                UpdateServerAndActiveCount(conn, model, Id);
            }
            //check expiry date
            else if (DateTime.Now > lic.ExpiryDate)
            {
                lic.valid = false;
                lic.Message = "license has expired, Please contact Tab support";
            }
            //check if it is a valide license, when it is not the first time call.
            else if (lic.SqlServer == model.SqlServer && lic.Database == model.Database)
            {
                lic.valid = true;
                lic.Message = "This license is valid";
                //UpdateActiveCount(conn, model, Id);
            }
            else
            {
                lic.valid = false;
                lic.Message = "The license is not valid, Please, contact Tab support!";
            }
            conn.Close();
            return lic;
        }
        private static void UpdateActiveCount(SqlConnection conn, LicenseValidation model, int id)
        {
            var cmd1 = new SqlCommand("UPDATE License SET ActiveCount = @activecount  WHERE id = @id", conn);
            cmd1.Parameters.AddWithValue("@id", id);
            cmd1.Parameters.AddWithValue("@activecount", model.ActiveCount);
            cmd1.ExecuteNonQuery();
        }
        private static void UpdateServerAndActiveCount(SqlConnection conn, LicenseValidation model, int id)
        {
            var cmd1 = new SqlCommand("UPDATE License SET SqlServer = @sqlserver, ActiveCount = @activecount  WHERE id = @id", conn);
            cmd1.Parameters.AddWithValue("@id", id);
            cmd1.Parameters.AddWithValue("@activecount", model.ActiveCount);
            cmd1.Parameters.AddWithValue("@sqlserver", model.SqlServer);
            cmd1.ExecuteNonQuery();
        }
        //##################################################################################
        //purpose: this method return license properties to check if license is valid and return 
        // object back to the caller
        //##################################################################################
        private static LicenseDetails ReturnLicenseDetails(SqlConnection conn, int id)
        {
            var table = new DataTable();
            var model = new LicenseDetails();
            var cmd = new SqlCommand(Resources.GetLicensDetails, conn);
            cmd.Parameters.AddWithValue("@id", id);
            var adp = new SqlDataAdapter(cmd);
            adp.Fill(table);
            if (table.Rows.Count == 1)
            {
                var row = table.Rows[0];
                model.rowCount = table.Rows.Count;
                model.CompanyName = row.Field<string>("CompanyName");
                model.LicenseType = row.Field<string>("LicenseType");
                model.SqlServer = row.Field<string>("SqlServer");
                model.LicenseKey = row.Field<string>("LicenseKey");
                model.Database = row.Field<string>("Database");
                model.LicenseCount = row.Field<int>("LicenseCount");
                model.ExpiryDate = row.Field<DateTime>("ExpiryDate");
                model.ProductName = row.Field<string>("Product");
                model.LicenseType = row.Field<string>("licenseType");
                //GET FEATURES
                var table1 = new DataTable();
                var licenseTypeEnum = row.Field<int>("LicenseTypeEnumid");
                var cmd1 = new SqlCommand("SELECT * FROM LicenseType WHERE Enum = @enumid", conn);
                cmd1.Parameters.AddWithValue("@enumid", licenseTypeEnum);
                var adp1 = new SqlDataAdapter(cmd1);
                adp1.Fill(table1);
                var row1 = table1.Rows[0];
                model.Features.FLabelBlackwhite = row1.Field<bool>("FLabelBlackwhite");
                model.Features.FTabQuick = row1.Field<bool>("FTabQuick");
                model.Features.FTransfer = row1.Field<bool>("FTransfer");
                model.Features.FRequest = row1.Field<bool>("FRequest");
                model.Features.FAttachment = row1.Field<bool>("FAttachment");
                model.Features.FRetention = row1.Field<bool>("FRetention");
                model.Features.FDashboard = row1.Field<bool>("FDashboard");
            }

            return model;
        }
        private static int GetNewLicenseId(SqlConnection conn, SqlTransaction trans)
        {
            var table = new DataTable();
            var cmd = new SqlCommand("SELECT TOP(1) [Id] FROM License ORDER BY id DESC", conn, trans);
            var adp = new SqlDataAdapter(cmd);
            adp.Fill(table);
            return table.Rows[0].Field<int>("Id");
        }
        private static int GetNewCustomerId(SqlConnection conn, SqlTransaction trans)
        {
            var table = new DataTable();
            var cmd = new SqlCommand("SELECT TOP(1) [Id] FROM Customers ORDER BY id DESC", conn, trans);
            var adp = new SqlDataAdapter(cmd);
            adp.Fill(table);
            return table.Rows[0].Field<int>("Id");
        }
        private static void InsertIntoCustomer(SqlConnection conn, SqlTransaction transaction, GenerateNewLicense model)
        {
            var cmdcustomer = new SqlCommand(Resources.InsertIntoCustomer, conn, transaction);
            cmdcustomer.Parameters.AddWithValue("@companyName", model.CompanyName);
            cmdcustomer.Parameters.AddWithValue("@address", model.Address);
            cmdcustomer.Parameters.AddWithValue("@datecreated", DateTime.Now);
            cmdcustomer.Parameters.AddWithValue("@city", model.City);
            cmdcustomer.Parameters.AddWithValue("@stateprovice", model.StateProvice);
            cmdcustomer.Parameters.AddWithValue("@country", model.Country);
            cmdcustomer.Parameters.AddWithValue("@zipcode", model.ZipCode);
            cmdcustomer.ExecuteNonQuery();
        }
        private static void InsertIntoContact(SqlConnection conn, SqlTransaction transaction, GenerateNewLicense model, int newId)
        {
            var cmdcontact = new SqlCommand(Resources.InsertIntoContact, conn, transaction);
            cmdcontact.Parameters.AddWithValue("@title", model.ContactTitle);
            cmdcontact.Parameters.AddWithValue("@phone", model.ContactPhone);
            cmdcontact.Parameters.AddWithValue("@email", model.ContactEmail);
            cmdcontact.Parameters.AddWithValue("@fullname", model.ContactFullName);
            cmdcontact.Parameters.AddWithValue("@customerid", newId);
            cmdcontact.ExecuteNonQuery();
        }
        internal static string InsertIntoLicense(SqlConnection conn, SqlTransaction transaction, GenerateNewLicense model, IConfiguration config, int newId)
        {
            //insert new license
            var cmdlicense = new SqlCommand(Resources.InsertIntoLicense, conn, transaction);
            cmdlicense.Parameters.AddWithValue("@product", model.ProductName);
            cmdlicense.Parameters.AddWithValue("@licensetype", model.LicenseType);
            //cmdlicense.Parameters.AddWithValue("@licensekey", licenseKey);
            cmdlicense.Parameters.AddWithValue("@licensecount", model.LicenseCount);
            cmdlicense.Parameters.AddWithValue("@database", model.Database);
            cmdlicense.Parameters.AddWithValue("@datecreated", DateTime.Now);
            cmdlicense.Parameters.AddWithValue("@expirydate", model.ExpiryDate);
            cmdlicense.Parameters.AddWithValue("@customerid", newId);
            cmdlicense.Parameters.AddWithValue("@licensetypeenumid", model.LicenseTypeEnumid);
            cmdlicense.Parameters.AddWithValue("@comment", model.Comment);
            cmdlicense.ExecuteNonQuery();

            var newlicensid = GetNewLicenseId(conn, transaction);
            string credential = $"{config.GetSection("Apiauth").GetSection("userName").Value},{config.GetSection("Apiauth").GetSection("password").Value}, {config.GetSection("Apiauth").GetSection("Database").Value}";
            string licenseKey = EncryptKey(newlicensid.ToString(), LicEncryptionKey);
            string autkey = EncryptKey(credential, AutEncryptionKey);
            string licensekeyAndAuth = $"{autkey}{(char)225}|{(char)225}{licenseKey}";

            var cmdlicenseUpdate = new SqlCommand("update License set LicenseKey = @licensekey where id = @licid", conn, transaction);
            cmdlicenseUpdate.Parameters.AddWithValue("@licensekey", licensekeyAndAuth);
            cmdlicenseUpdate.Parameters.AddWithValue("@licid", newlicensid);
            cmdlicenseUpdate.ExecuteNonQuery();

            return licensekeyAndAuth;
        }
        
        //private static void InsertIntoLicense(SqlConnection conn, SqlTransaction transaction, GenerateNewLicense model, string licenseKey, int newId)
        //{
        //    var cmdlicense = new SqlCommand(Resources.InsertIntoLicense, conn, transaction);
        //    cmdlicense.Parameters.AddWithValue("@product", model.ProductName);
        //    cmdlicense.Parameters.AddWithValue("@licensetype", model.LicenseType);
        //    cmdlicense.Parameters.AddWithValue("@licensekey", licenseKey);
        //    cmdlicense.Parameters.AddWithValue("@licensecount", model.LicenseCount);
        //    cmdlicense.Parameters.AddWithValue("@database", model.Database);
        //    cmdlicense.Parameters.AddWithValue("@datecreated", DateTime.Now);
        //    cmdlicense.Parameters.AddWithValue("@expirydate", model.ExpiryDate);
        //    cmdlicense.Parameters.AddWithValue("@customerid", newId);
        //    cmdlicense.Parameters.AddWithValue("@licensetypeenumid", model.LicenseTypeEnumid);
        //    cmdlicense.Parameters.AddWithValue("@comment", model.Comment);
        //    cmdlicense.ExecuteNonQuery();
        //}

    }

    public class GenerateNewLicense
    {

        [Required] public string CompanyName { get; set; }
        [Required] public string Address { get; set; }
        [Required] public string City { get; set; }
        [Required] public string StateProvice { get; set; }
        [Required] public string Country { get; set; }
        [Required] public string ZipCode { get; set; }
        [Required] public string ContactTitle { get; set; }
        [Required] public string ContactPhone { get; set; }
        [Required] public string ContactEmail { get; set; }
        [Required] public string ContactFullName { get; set; }
        public string ProductName { get; set; }
        public string LicenseType { get; set; }
        public string SqlServer { get; set; }
        [Required] public string Database { get; set; }
        [Required] public int LicenseCount { get; set; }
        [Required] public DateTime ExpiryDate { get; set; }
        [Required] public int ProductId { get; set; }
        [Required] public int LicenseTypeEnumid { get; set; }
        public int CustomerId { get; set; } = 0;
        public string Comment { get; set; } 
    }

    public class LicenseValidation
    {
        [Required] public string SqlServer { get; set; }
        [Required] public string Database { get; set; }
        [Required] public string LicenseKey { get; set; }
        [Required] public int ActiveCount { get; set; }
    }
    public class LicenseDetails : LicenseValidation
    {
        public LicenseDetails()
        {
            Features = new Features();
        }
        public string CompanyName { get; set; }
        public string LicenseType { get; set; }
        public string Message { get; set; }
        public bool valid { get; set; }
        public int rowCount { get; set; }
        public int LicenseCount { get; set; }
        public string ProductName { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string ExipryDate { get; set; }
        public Features Features { get; set; }   
    }
    public class Features
    {
        public bool FLabelBlackwhite {get; set;}
        public bool FTabQuick { get; set; }
        public bool FTransfer { get; set; }
        public bool FRequest { get; set; }
        public bool FAttachment { get; set; }
        public bool FRetention { get; set; }
        public bool FDashboard { get; set; }
    }
    public class IsValid
    {
        public string Message { get; set; }
        public bool valid { get; set; }
    }

    public class DropDown
    {
        public string message { get; set; }
        public static List<LicenseProduct> GetProduct(Passport pass)
        {
            var lst = new List<LicenseProduct>();
            var conn = new SqlConnection(pass.ConnectionString);
            conn.Open();
            var table = new DataTable();
            var cmd = new SqlCommand("SELECT Id, ProductName FROM TabProductList", conn);
            var adp = new SqlDataAdapter(cmd);
            adp.Fill(table);
            conn.Close();
            foreach (DataRow row in table.Rows)
            {
                lst.Add(new LicenseProduct { Id = row.Field<int>("Id"), Name = row.Field<string>("ProductName") });
            }
            return lst;
        }
        public static List<licenseType> GetLicenseType(Passport pass, int prodid)
        {
            var lst = new List<licenseType>();
            var table = new DataTable();
            var conn = new SqlConnection(pass.ConnectionString);
            conn.Open();
            var cmd = new SqlCommand("SELECT Enum, TypeName FROM LicenseType WHERE TabProductListId = @prodid", conn);
            cmd.Parameters.AddWithValue("@prodid", prodid);
            var adp = new SqlDataAdapter(cmd);
            adp.Fill(table);
            conn.Close();
            foreach (DataRow row in table.Rows)
            {
                lst.Add(new licenseType { EnumId = row.Field<int>("Enum"), Name = row.Field<string>("TypeName") });
            }
            return lst;
        }
    }
    public class LicenseProduct
    {
        public string Name { get; set; }
        public int Id { get; set; }
    }
    public class licenseType
    {
        public int EnumId { get; set; }
        public string Name { get; set; }
    }
    public class CustomersModel
    {

        public CustomersModel()
        {
            listofName = new List<string>();  
        }
        public List<string> listofName { get; set; }
        public bool IsError { get; set; }
        public string Message { get; set; }
        public int customerId { get; set; }
        public bool IsCustomerFound { get; set; }
        public string CustomerName { get; set; }    

        public List<string> GetCustomerList(string key, Passport pass)
        {
            var table = new DataTable();
            var conn = new SqlConnection(pass.ConnectionString);
            conn.Open();
            var cmd = new SqlCommand("select * from Customers where CompanyName like '%' + @key + '%'", conn);
            cmd.Parameters.AddWithValue("@key", key);
            var adp = new SqlDataAdapter(cmd);
            adp.Fill(table);
            conn.Close();
            foreach (DataRow row in table.Rows)
            {
                listofName.Add(row.Field<string>("CompanyName"));
            }

            return listofName;
        }
        public CustomersModel IsCustomerExist(string name, Passport pass)
        {
            var cs = new CustomersModel();
            var table = new DataTable();
            var conn = new SqlConnection(pass.ConnectionString);
            conn.Open();
            var cmd = new SqlCommand("select * from Customers where CompanyName = @name", conn);
            cmd.Parameters.AddWithValue("@name", name.Trim());
            var adp = new SqlDataAdapter(cmd);
            adp.Fill(table);
            if(table.Rows.Count == 1)
            {
                cs.IsCustomerFound = true;
                cs.CustomerName = table.Rows[0].Field<string>("CompanyName");
                cs.customerId = table.Rows[0].Field<int>("Id");
            }
            else if(table.Rows.Count > 1)
            {
                cs.IsError = true;
                cs.Message = $"duplicate customer name. Customer with the same name {name} appeared {table.Rows.Count} in the database!";
            }
            else
            {
                cs.IsCustomerFound = false;
                cs.Message = "No customer found";
            }
          
            conn.Close();
            return cs;
           
        }
        public string GenerateLicenseToExistCustomer(GenerateNewLicense model, IConfiguration config, Passport pass)
        {
            string licensekey = string.Empty;
            var cs = new CustomersModel();
            var conn = new SqlConnection(pass.ConnectionString);
            conn.Open();
            var transaction = conn.BeginTransaction();
            licensekey = LicenseAccess.InsertIntoLicense(conn, transaction, model, config, model.CustomerId);
            transaction.Commit();
            conn.Close();

            return licensekey;
        }

    }


}
