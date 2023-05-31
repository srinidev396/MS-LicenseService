using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Smead.Security;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;

namespace LicenseServer.Models
{

    public class SecurityAccess
    {
        public SecurityAccess(IConfiguration config)
        {
            sqlUsername = config.GetSection("Sql").GetSection("userName").Value;
            sqlPassword = config.GetSection("Sql").GetSection("password").Value;
            sqlServername = config.GetSection("Sql").GetSection("serverName").Value;
            Secret = config.GetSection("JwtConfig").GetSection("secret").Value;
            ExpDate = config.GetSection("JwtConfig").GetSection("expirationInMinutes").Value;
        }
        public string sqlServername { get; set; }
        public string sqlUsername { get; set; }
        public string sqlPassword { get; set; }
        public string Secret { get; set; }
        public string ExpDate { get; set; }
        public Passport GetPassport(string userdata)
        {
            var passport = new Passport();

            var data = Encrypt.DecryptParameters(userdata);
            var ud = JsonConvert.DeserializeObject<UserData>(data);
            passport.SignIn(ud.UserName, "3kszs932ksdjjdjwqp00qkksj", string.Empty, sqlServername, ud.Database, sqlUsername, sqlPassword);
            return passport;
        }

    }

    public class UserData
    {
        IConfiguration _config;
        public UserData() { }
        public UserData(IConfiguration config)
        {
            _config = config;
        }
        public string UserName { get; set; }
        public int UserId { get; set; }
        public string Database { get; set; }

    }




}
