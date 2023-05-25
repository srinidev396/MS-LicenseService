using System;
using System.Text;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using Smead.Security;
using Microsoft.AspNetCore.Http;
using System.IdentityModel;
using LicenseServer.Models;

namespace LicenseServer.Services
{
    public class JwtService
    {
        public SecurityAccess bmodel { get; set; }
        public JwtService(SecurityAccess m)
        {
            bmodel = m;
        }

        public string GenerateSecurityToken(string userprops)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(bmodel.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, userprops),
                }),
                Expires = DateTime.UtcNow.AddMinutes(double.Parse(bmodel.ExpDate)),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);

        }

    }
}
