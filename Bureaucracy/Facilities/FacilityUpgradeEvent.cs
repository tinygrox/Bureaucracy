using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using Upgradeables;

namespace Bureaucracy
{
    public class FacilityUpgradeEvent : BureaucracyEvent
    {
        private string facilityId;
        private float remainingInvestment;
        private int levelRequested = 1;
        private float originalCost;
        private readonly BureaucracyFacility parentFacility;
        [UsedImplicitly] private PopupDialog kctWarning;
        public bool UpgradeHeld;

        public FacilityUpgradeEvent(string id, BureaucracyFacility passingFacility)
        {
            facilityId = id;
            List<UpgradeableFacility> upgradeables = GetFacilityById(id);
            for (int i = 0; i < upgradeables.Count; i++)
            {
                UpgradeableFacility potentialUpgrade = upgradeables.ElementAt(i);
                if (potentialUpgrade.GetUpgradeCost() <= 0) continue;
                remainingInvestment = potentialUpgrade.GetUpgradeCost();
                originalCost = potentialUpgrade.GetUpgradeCost();
                levelRequested = potentialUpgrade.FacilityLevel + 1;
                break;
            }
            parentFacility = passingFacility;
        }
        
        public float RemainingInvestment => remainingInvestment;

        public float OriginalCost => originalCost;

        public float ProgressUpgrade(double funding)
        {
            double remainingFunding = funding - remainingInvestment;
            if (remainingFunding > 0)
            {
                OnEventCompleted();
                ScreenMessages.PostScreenMessage(parentFacility.Name + ": 升级完成"); // Upgrade Complete
                return  (float)remainingFunding;
            }
            remainingInvestment -= (float)funding;
            return 0.0f;
        }

        private List<UpgradeableFacility> GetFacilityById(string id)
        {
            return ScenarioUpgradeableFacilities.protoUpgradeables[id].facilityRefs;
        }
        public override void OnEventCompleted()
        {
            List<UpgradeableFacility> facilitiesToUpgrade = GetFacilityById(facilityId);
            for (int i = 0; i < facilitiesToUpgrade.Count; i++)
            {
                UpgradeableFacility facilityToUpgrade = facilitiesToUpgrade.ElementAt(i);
                if (facilityToUpgrade.FacilityLevel != levelRequested - 1 && Directory.Exists(KSPUtil.ApplicationRootPath + "/GameData/KerbalConstructionTime"))
                {
                    UpgradeHeld = true;
                    kctWarning = GenerateKctWarning(facilityToUpgrade.FacilityLevel);
                    return;
                }
                facilityToUpgrade.SetLevel(levelRequested);
                UpgradeHeld = false;
            }
            parentFacility.OnUpgradeCompleted();
        }

        private PopupDialog GenerateKctWarning(int facilityLevel)
        {
            List<DialogGUIBase> dialogElements = new List<DialogGUIBase>();
            dialogElements.Add(new DialogGUILabel("设施级别与升级申请的级别不匹配！")); // Facility level doesn't match requested upgrade!
            dialogElements.Add(new DialogGUILabel("申请升级到 "+levelRequested+" "+parentFacility.Name+" -  当前级别为 "+(facilityLevel+1))); // Expected a level Got level
            dialogElements.Add(new DialogGUILabel("如果安装了KCT，请确认你已切换到了正确的发射台")); // If KCT is installed, make sure you have the right launchpad selected
            dialogElements.Add(new DialogGUILabel("准备完成时，右键点击发射台然后按下“升级”继续")); // When you are ready, right click the launchpad and click \"Upgrade\" to proceed
            dialogElements.Add(new DialogGUIButton("OK", () => { }, true)); // 
            return PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new MultiOptionDialog("ResearchDialog", "", "Bureaucracy: 研究", UISkinManager.GetSkin("MainMenuSkin"), new Rect(0.5f, 0.5f, 200, 200), dialogElements.ToArray()), false, UISkinManager.GetSkin("MainMenuSkin")); // Research
        }

        public void OnSave(ConfigNode facilityNode)
        {
            ConfigNode upgradeNode = new ConfigNode("UPGRADE");
            upgradeNode.SetValue("ID", facilityId, true);
            upgradeNode.SetValue("cost", remainingInvestment, true);
            upgradeNode.SetValue("originalCost", originalCost, true);
            upgradeNode.SetValue("level", levelRequested, true);
            upgradeNode.SetValue("upgradeHeld", UpgradeHeld, true);
            facilityNode.AddNode(upgradeNode);
        }

        public void OnLoad(ConfigNode upgradeNode)
        {
            facilityId = upgradeNode.GetValue("ID");
            float.TryParse(upgradeNode.GetValue("cost"), out remainingInvestment);
            int.TryParse(upgradeNode.GetValue("level"), out levelRequested);
            float.TryParse(upgradeNode.GetValue("originalCost"), out originalCost);
            bool.TryParse(upgradeNode.GetValue("upgradeHeld"), out UpgradeHeld);
        }
    }
}