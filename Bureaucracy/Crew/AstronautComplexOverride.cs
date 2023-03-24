using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSP.UI;
using UnityEngine;

namespace Bureaucracy
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class AstronautComplexOverride : MonoBehaviour
    {
        public bool astronautComplexSpawned;
        public static AstronautComplexOverride Instance;
        public int updateCount = 4;

        private void Awake()
        {
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void LateUpdate()
        {
            if (!astronautComplexSpawned || updateCount <= 0) return;
            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            List<CrewListItem> crewItems = FindObjectsOfType<CrewListItem>().ToList();
            updateCount--;
            for (int i = 0; i < crewItems.Count; i++)
            {
                CrewListItem c = crewItems.ElementAt(i);
                if (c.GetCrewRef().type != ProtoCrewMember.KerbalType.Crew) continue;
                // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
                c.SetLabel(GenerateAstronautString(c.GetCrewRef().name));
            }
        }

        private string GenerateAstronautString(string kerbalName)
        {
            CrewMember c = CrewManager.Instance.Kerbals[kerbalName];
            //if for whatever reason we can't find the CrewMember just leave it at default
            if (c == null) return "可以进行下一次任务"; // "Available For Next Mission"
            StringBuilder sb = new StringBuilder();
            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            if (c.CrewReference().inactive) sb.AppendLine( "训练中 | " + "薪酬：" + c.Wage); // In TrainingWage
            else
            {
                float morale = (1 - (float) c.UnhappinessEvents.Count / c.MaxStrikes) * 100;
                if (float.IsNaN(morale)) morale = 100;
                if (float.IsNegativeInfinity(morale)) morale = 0;
                sb.AppendLine("信心：" + Math.Round(morale, 0) + "% | 薪酬：" + c.Wage); // Morale Wage
            }

            if (SettingsClass.Instance.RetirementEnabled)
            {
                KeyValuePair<int, string> retirementDate = Utilities.Instance.ConvertUtToRealTime(c.retirementDate - Planetarium.GetUniversalTime());
                sb.AppendLine("退役时间：" + retirementDate.Key + " " + retirementDate.Value); // 
            }

            return sb.ToString();
        }
    }
}