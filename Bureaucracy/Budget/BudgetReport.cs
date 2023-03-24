using System;

namespace Bureaucracy
{
    public class BudgetReport : Report
    {
        public BudgetReport()
        {
            ReportTitle = "预算报告"; // "Budget Report"
        }

        public override string ReportBody()
        {
            ReportBuilder.Clear();
            ReportBuilder.AppendLine("总预算：" + Utilities.Instance.GetGrossBudget()); // "Gross Budget: "
            ReportBuilder.AppendLine("员工薪资：" + Costs.Instance.GetWageCosts()); // "Staff Wages: "
            ReportBuilder.AppendLine("设施维护成本：" + Costs.Instance.GetFacilityMaintenanceCosts()); // "Facility Maintenance Costs: "
            ReportBuilder.AppendLine("发射成本：" + Costs.Instance.GetLaunchCosts()); // "Launch Costs: "
            ReportBuilder.AppendLine("总维护成本：" + Costs.Instance.GetTotalMaintenanceCosts()); // "Total Maintenance Costs: "
            ReportBuilder.AppendLine("任务奖励：" + CrewManager.Instance.LastBonus); // "Mission Bonuses: "
            ReportBuilder.AppendLine("建设部门：" + FacilityManager.Instance.GetAllocatedFunding()); // "Construction Department: "
            ReportBuilder.AppendLine("研发部门：" + ResearchManager.Instance.GetAllocatedFunding()); // "Research Department: "
            double netBudget = Utilities.Instance.GetNetBudget("预算"); // Budget
            ReportBuilder.AppendLine("净值预算：" + Math.Max(0, netBudget)); // "Net Budget: "
            if (netBudget > 0 && netBudget < Funding.Instance.Funds) ReportBuilder.AppendLine("我们认为没有理由加大你的预算"); // "We can't justify extending your funding"
            // ReSharper disable once InvertIf
            if (netBudget < 0)
            {
                ReportBuilder.AppendLine("现预算无法继续支撑你的太空计划。"); // "The budget didn't fully cover your space programs costs."
                ReportBuilder.Append("将获得" + Math.Round(netBudget, 0) + "的惩罚"); //"A penalty of " + Math.Round(netBudget, 0) + " will be applied"
            }
            return ReportBuilder.ToString();
        }
    }
}