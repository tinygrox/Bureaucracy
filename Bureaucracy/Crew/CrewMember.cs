using System.Collections.Generic;
using System.Linq;
using Expansions.Missions;
using KSPAchievements;
using UnityEngine;

namespace Bureaucracy
{
    public class CrewMember
    {
        private double bonusAwaitingPayment;
        private ProtoCrewMember crewRef;
        public readonly int MaxStrikes;
        public readonly List<CrewUnhappiness> UnhappinessEvents = new List<CrewUnhappiness>();
        public bool Unhappy;
        public float WageModifier = 1.0f;
        //private bool onVacation;
        public double retirementDate;
        public bool aboutToRetire = false;

        public string Name { get; private set; }

        public double Wage
        {
            get
            {
                float experienceLevel = crewRef.experienceLevel;
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (experienceLevel == 0) experienceLevel = 0.5f;
                return experienceLevel * SettingsClass.Instance.KerbalBaseWage*WageModifier;
            }
        }

        public CrewMember(string kerbalName)
        {
            Name = kerbalName;
            MaxStrikes = (int)(SettingsClass.Instance.BaseStrikesToQuit * CrewReference().stupidity);
            if (SettingsClass.Instance.RetirementEnabled)
            {
                int minTerm = SettingsClass.Instance.MinimumTerm;
                int maxTerm = SettingsClass.Instance.MaximumTerm;
                if (SettingsClass.Instance.RetirementEnabled) retirementDate = Utilities.Instance.Randomise.Next(minTerm, maxTerm) * FlightGlobals.GetHomeBody().orbit.period + Planetarium.GetUniversalTime();
                else retirementDate = -1;
            }
            Debug.Log("[Bureaucracy]: New CrewMember setup: "+kerbalName);
        }
        
        public void AllocateBonus(double timeOnMission)
        {
            KeyValuePair<int, string> kvp = Utilities.Instance.ConvertUtToRealTime(timeOnMission);
            double payout;
            // if (kvp.Value == "years") payout = kvp.Key * SettingsClass.Instance.LongTermBonusYears;
            if (kvp.Value == "年") payout = kvp.Key * SettingsClass.Instance.LongTermBonusYears;
            else payout = kvp.Key * SettingsClass.Instance.LongTermBonusDays;
            bonusAwaitingPayment += payout;
            Debug.Log("[Bureaucracy]: Assigned Bonus of "+(int)payout+" to "+Name);
        }

        public ProtoCrewMember CrewReference()
        {
            List<ProtoCrewMember> crew = HighLogic.CurrentGame.CrewRoster.Crew.ToList();
            for (int i = 0; i < crew.Count; i++)
            {
                ProtoCrewMember p = crew.ElementAt(i);
                if (p.name != Name) continue;
                crewRef = p;
                break;
            }

            //Newly hired crew members aren't actually in the crew yet, so we need to check the Applicants too.
            if (crewRef == null)
            {
                // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
                Debug.Log("[Bureaucracy]: Couldn't find " + Name + " in the crew list. Checking Applicants");
                crew = HighLogic.CurrentGame.CrewRoster.Applicants.ToList();
                for (int i = 0; i < crew.Count; i++)
                {
                    ProtoCrewMember p = crew.ElementAt(i);
                    if (p.name != Name) continue;
                    crewRef = p;
                    break;
                }
            }

            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            if (crewRef == null) Debug.Log("[Bureaucracy]: Couldn't find a crew ref for " + Name);
            return crewRef;
        }

        public void Train()
        {
            int newLevel = CrewReference().experienceLevel + 1;
            KerbalRoster.SetExperienceLevel(CrewReference(), newLevel);
            double trainingTime = newLevel * Utilities.Instance.GetMonthLength();
            CrewReference().SetInactive(trainingTime);
            Utilities.Instance.NewStockAlarm(Name+" - 培训", Name+" 已经完成他们的培训",Planetarium.GetUniversalTime()+trainingTime); //Training | has completed their training
            Debug.Log("[Bureaucracy]: "+Name+" entered training for "+trainingTime+", new Level: "+newLevel);
        }

        public int GetBonus(bool clearBonus)
        {
            double payment = bonusAwaitingPayment;
            if(clearBonus) bonusAwaitingPayment = 0;
            return (int)payment;
        }

        public void OnSave(ConfigNode crewManagerNode)
        {
            ConfigNode crewNode = new ConfigNode("CREW_MEMBER");
            crewNode.SetValue("Name", Name, true);
            crewNode.SetValue("Bonus", bonusAwaitingPayment, true);
            crewNode.SetValue("WageModifier", WageModifier, true);
            crewNode.SetValue("RetirementDate", retirementDate, true);
            for (int i = 0; i < UnhappinessEvents.Count; i++)
            {
                CrewUnhappiness cu = UnhappinessEvents.ElementAt(i);
                cu.OnSave(crewNode);
            }
            crewManagerNode.AddNode(crewNode);
        }

        public void OnLoad(ConfigNode crewConfig)
        {
            Name = crewConfig.GetValue("Name");
            double.TryParse(crewConfig.GetValue("Bonus"), out bonusAwaitingPayment);
            double.TryParse(crewConfig.GetValue("RetirementDate"), out retirementDate);
            if(retirementDate == -1 && SettingsClass.Instance.RetirementEnabled) retirementDate = Utilities.Instance.Randomise.Next(SettingsClass.Instance.MinimumTerm, SettingsClass.Instance.MaximumTerm) * FlightGlobals.GetHomeBody().orbit.period + Planetarium.GetUniversalTime();
            else if (!SettingsClass.Instance.RetirementEnabled) retirementDate = -1;
            if (!crewConfig.TryGetValue("WageModifier", ref WageModifier)) WageModifier = 1.0f;
            ConfigNode[] unhappyNodes = crewConfig.GetNodes("UNHAPPINESS");
            for (int i = 0; i < unhappyNodes.Length; i++)
            {
                CrewUnhappiness cu = new CrewUnhappiness("装载", this); // loading
                cu.OnLoad(unhappyNodes.ElementAt(i));
                UnhappinessEvents.Add(cu);
                Unhappy = true;
            }
        }

        public string UnhappyOutcome()
        {
            if (CrewReference().rosterStatus == ProtoCrewMember.RosterStatus.Assigned) return " 当前心情并不好，但仍会为了任务继续"; // is not happy but will continue for the sake of the mission
            if(UnhappinessEvents.Count >= MaxStrikes) return " 因为 "+UnhappinessEvents.Last().Reason + " 而退出了太空计划" ;// Quit the space program due to 
            return "因为 " + UnhappinessEvents.Last().Reason + "而不高兴"; // is not happy due to
        }

        public void MonthWithoutIncident()
        {
            for (int i = UnhappinessEvents.Count-1; i >= 0; i--)
            {
                CrewUnhappiness cu = UnhappinessEvents.ElementAt(i);
                if (cu.ClearStrike()) UnhappinessEvents.Remove(cu);
            }
        }

        public void AddUnhappiness(string reason)
        {
            UnhappinessEvents.Add(new CrewUnhappiness(reason, this));
            Unhappy = true;
        }

        public void ExtendRetirementAge(double extension)
        {
            retirementDate += extension;
        }
    }
}