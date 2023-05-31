using LicenseServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Smead.Security;
using System;
using System.Collections.Generic;

namespace LicenseServer.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class LicenseGeneratorController : ControllerBase
    {
        private IConfiguration config;
        public LicenseGeneratorController(IConfiguration configuration)
        {
            config = configuration;
        }
        [HttpPost("GenerateLicense")]
        public string GenerateLicense(GenerateNewLicense model)
        {
            try
            {
                var u = new SecurityAccess(config);
                var pass = u.GetPassport(User.Identity.Name);
                if (!pass.SignedIn)
                {
                   return "Faild to authenticate!";
                }
                if (LicenseAccess.IsProductAndTypeExist(model, pass))
                {
                    return LicenseAccess.GenerateNewLicense(model, pass, config);
                }
                else
                {
                    return $"this Product is not exist in Tab fusion license model!";
                }
            }
            catch (Exception ex)
            {
                LogErrorMessages.LogErrorMessage(ex);
                return ex.Message;
            }

        }
        [HttpPost("LicenseValidation")]
        public LicenseDetails LicenseValidation(LicenseValidation model)
        {
            try
            {
                var u = new SecurityAccess(config);
                var pass = u.GetPassport(User.Identity.Name);
                return LicenseAccess.LicenseValidation(model, pass);
            }
            catch (Exception ex)
            {
                LogErrorMessages.LogErrorMessage(ex);
                var d = new LicenseDetails();
                d.valid = false;
                d.Message = ex.Message;
                return d;
            }
        }
        [HttpGet("GetListOfProducts")]
        public List<LicenseProduct> GetListOfProducts()
        {
            var list = new List<LicenseProduct>();
            try
            {
                var u = new SecurityAccess(config);
                var pass = u.GetPassport(User.Identity.Name);
                list = DropDown.GetProduct(pass);
            }
            catch (Exception ex)
            {
                LogErrorMessages.LogErrorMessage(ex);
            }
            return list;
        }
        [HttpGet("GetListOfLicenseTypes")]
        public List<licenseType> GetListOfLicenseTypes(int prodId)
        {
            var list = new List<licenseType>();
            try
            {
                var u = new SecurityAccess(config);
                var pass = u.GetPassport(User.Identity.Name);
                list = DropDown.GetLicenseType(pass, prodId);
            }
            catch (Exception ex)
            {
                LogErrorMessages.LogErrorMessage(ex);
            }
            return list;
        }
    }
}
