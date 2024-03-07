using System;
using System.Collections;
using System.Collections.Generic;

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
        public int CustomerId { get; set; }
        public int LicenseCount { get; set; }
        public int ActiveCount { get; set; }
        public DateTime ExpiryDate { get; set; }

        public bool FLabelBlackwhite { get; set; }
        public bool FTabQuick { get; set; }
        public bool FTransfer { get; set; }
        public bool FRequest { get; set; }
        public bool FAttachment { get; set; }
        public bool FRetention { get; set; }
        public bool FDashboard { get; set; }
    }

    public class DatabaseKeyList
    {
        public string KeyName { get; set; }
        public int Index { get; set; }
    }
}
