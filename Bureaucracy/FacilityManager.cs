using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Upgradeables;

namespace Bureaucracy
{
    public class FacilityManager : Manager
    {
        public List<BureaucracyFacility> Facilities = new List<BureaucracyFacility>();
        public static FacilityManager Instance;
        private bool modRequestedUpgrade = false;

        public FacilityManager()
        {
            InternalEvents.OnBudgetAwarded.Add(RunFacilityBudget);
            SpaceCenterFacility[] spaceCentreFacilities = (SpaceCenterFacility[]) Enum.GetValues(typeof(SpaceCenterFacility));
            for (int i = 0; i < spaceCentreFacilities.Length; i++)
            {
                SpaceCenterFacility spf = spaceCentreFacilities.ElementAt(i);
                
                if(spf == SpaceCenterFacility.LaunchPad || spf == SpaceCenterFacility.Runway) continue;
                spf.
                Facilities.Add(new BureaucracyFacility(spf));
            }
            Name = "Facility Manager";
            Instance = this;
        }

        public override Report GetReport()
        {
            return new FacilityReport();
        }

        private void RunFacilityBudget(double data0, double data1)
        {
            double facilityBudget = Utilities.Instance.GetNetBudget("Facilities");
            for (int i = 0; i < Facilities.Count; i++)
            {
                BureaucracyFacility bf = Facilities.ElementAt(i);
                if(!bf.Upgrading) continue;
                facilityBudget = bf.Upgrade.UpdateProgress(facilityBudget);
                if (facilityBudget <= 0.0f) return;
            }
        }

        public void OnLoad(ConfigNode cn)
        {
            ConfigNode[] facilityNodes = cn.GetNodes("FACILITY");
            for (int i = 0; i < Facilities.Count; i++)
            {
                BureaucracyFacility bf = Facilities.ElementAt(i);
                bf.OnLoad(facilityNodes);
            }
        }
        
        

        public void OnSave(ConfigNode cn)
        {
            for (int i = 0; i < Facilities.Count; i++)
            {
                BureaucracyFacility bf = Facilities.ElementAt(i);
                bf.OnSave(cn);
            }
        }

        public void StartUpgrade(UpgradeableFacility facility, int requestedLevel)
        {
            if (modRequestedUpgrade)
            {
                modRequestedUpgrade = false;
                return;
            }
            modRequestedUpgrade = true;
            Debug.Log("[Bureaucracy]: Upgrade of "+facility.id+" requested");
            facility.SetLevel(requestedLevel-1);
            Funding.Instance.AddFunds(facility.GetUpgradeCost(), TransactionReasons.StructureConstruction);
            for (int i = 0; i < Facilities.Count; i++)
            {
                BureaucracyFacility bf = Facilities.ElementAt(i);
                Debug.Log("[Bureaucracy]: Trying " + bf.Name);
                if (facility.id != bf.Name) continue;
                string s = facility.name;
                if (!bf.Upgrading) bf.StartUpgrade(facility, requestedLevel);
                else Funding.Instance.AddFunds(facility.GetUpgradeCost(), TransactionReasons.StructureConstruction);
            }
        }
    }
}