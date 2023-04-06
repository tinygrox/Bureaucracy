using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using KSP.Localization;

namespace Bureaucracy
{
    [SuppressMessage("ReSharper", "BuiltInTypeReferenceStyleForMemberAccess")]
    public class FacilityReport : Report
    {
        public FacilityReport()
        {
            ReportTitle = "设施报告"; // Facilities Report
        }

        public override string ReportBody()
        {
            ReportBuilder.Clear();
            for (int i = 0; i < FacilityManager.Instance.Facilities.Count; i++)
            {
                BureaucracyFacility bf = FacilityManager.Instance.Facilities.ElementAt(i);
                string s = bf.GetProgressReport(bf.Upgrade);
                if (bf.IsClosed) ReportBuilder.AppendLine(Localizer.Format("#Bureaucracy_" + bf.Name) + " 已停止运营"); // is closed
                if(s == String.Empty) continue;
                ReportBuilder.AppendLine(s);
            }
            string report = ReportBuilder.ToString();
            if (String.IsNullOrEmpty(report)) report = "无设施升级报告"; // No Facility updates to report
            return report;
        }
    }
}