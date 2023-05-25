using System.Diagnostics;
using System;
using Smead.Security;

namespace LicenseServer.Models
{
    public class LogErrorMessages
    {
        public static void LogErrorMessage(Exception ex)
        {
            EventLog log = new EventLog();
            log.Source = "FusionLicenseServer";
            log.WriteEntry($"Error:{ex.Message}", EventLogEntryType.Error, 1);
        }
    }
}
