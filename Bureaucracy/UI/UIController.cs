using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;
using KSP.UI.Screens;
using Steamworks;
using UnityEngine;
using UnityEngine.UI;

namespace Bureaucracy
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class UiControllerSpaceCentre : UiController
    {
        
    }
    
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class UiControllerFlight : UiController
    {
        
    }
    
    public class UiController : MonoBehaviour
    {
        private ApplicationLauncherButton toolbarButton;
        public static UiController Instance;
        private PopupDialog mainWindow;
        private PopupDialog facilitiesWindow;
        private PopupDialog researchWindow;
        public PopupDialog allocationWindow;
        public PopupDialog crewWindow;
        private int fundingAllocation;
        private int constructionAllocation;
        private int researchAllocation;
        [UsedImplicitly] public PopupDialog errorWindow;
        private int padding;
        private const int PadFactor = 10;

        private void Awake()
        {
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            //SetAllocation("Budget", "40");
            //SetAllocation("Research", "30");
            //SetAllocation("Construction", "30");
            GameEvents.onGUIApplicationLauncherReady.Add(SetupToolbarButton);
            GameEvents.onGUIApplicationLauncherUnreadifying.Add(RemoveToolbarButton);
        }

        private int GetAllocation(Manager manager)
        {
            return (int)Math.Round((double)(manager.FundingAllocation * 100f), 0);
        }


        public void SetupToolbarButton()
        {
            //TODO: Rename the icon file
            if(HighLogic.CurrentGame.Mode == Game.Modes.CAREER) toolbarButton = ApplicationLauncher.Instance.AddModApplication(ToggleUI, ToggleUI, null, null, null, null, ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.FLIGHT, GameDatabase.Instance.GetTexture("Bureaucracy/MainIcon", false));
        }

        private void ToggleUI()
        {
            if(UiInactive()) ActivateUi("main");
            else DismissAllWindows();
        }

        private bool UiInactive()
        {
            return mainWindow == null && facilitiesWindow == null && researchWindow == null && crewWindow == null;
        }

        private void ActivateUi(string screen)
        {
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER) return;
           DismissAllWindows();
            switch (screen)
            {
                case "main":
                    mainWindow = DrawMainUi();
                    break;
                case "facility":
                    facilitiesWindow = DrawFacilityUi();
                    break;
                case "research":
                    researchWindow = DrawResearchUi();
                    break;
                case "allocation":
                    allocationWindow = DrawBudgetAllocationUi();
                    break;
                case "crew":
                    crewWindow = DrawCrewUI();
                    break;
            }
        }

        private PopupDialog DrawCrewUI()
        {
            List<DialogGUIBase> dialogElements = new List<DialogGUIBase>();
            List<DialogGUIBase> innerElements = new List<DialogGUIBase>();
            innerElements.Add(new DialogGUIContentSizer(ContentSizeFitter.FitMode.Unconstrained, ContentSizeFitter.FitMode.PreferredSize, true));
            DialogGUIBase[] horizontal;
            for (int i = 0; i < CrewManager.Instance.Kerbals.Count; i++)
            {
                KeyValuePair<string, CrewMember> crew = CrewManager.Instance.Kerbals.ElementAt(i);
                if (crew.Value.CrewReference().rosterStatus != ProtoCrewMember.RosterStatus.Available) continue;
                if (crew.Value.CrewReference().inactive) continue;
                if (crew.Value.CrewReference().experienceLevel >= 5) continue;
                horizontal = new DialogGUIBase[5];
                horizontal[0] = new DialogGUISpace(10);
                horizontal[1] = new DialogGUILabel(crew.Key, MessageStyle(true));
                horizontal[2] = new DialogGUIFlexibleSpace();
                horizontal[3] = new DialogGUIButton("训练", () => TrainKerbal(crew.Value), false); // Train
                horizontal[4] = new DialogGUISpace(20);
                innerElements.Add(new DialogGUIHorizontalLayout(horizontal));
            }
            DialogGUIVerticalLayout vertical = new DialogGUIVerticalLayout(innerElements.ToArray());
            dialogElements.Add(new DialogGUIScrollList(new Vector2(300, 300), false, true, vertical));
            dialogElements.Add(GetBoxes("crew"));
            return PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new MultiOptionDialog("Bureaucracy", "", "Bureaucracy: 人员管理", UISkinManager.GetSkin("MainMenuSkin"), // Crew Manager
                    new Rect(0.5f, 0.5f, 350, 265), dialogElements.ToArray()), false, UISkinManager.GetSkin("MainMenuSkin"), false);
        }
        
        private void TrainKerbal(CrewMember crewMember)
        {
            int newLevel = crewMember.CrewReference().experienceLevel + 1;
            float trainingFee = newLevel * SettingsClass.Instance.BaseTrainingFee;
            if (crewMember.CrewReference().inactive)
            {
                ScreenMessages.PostScreenMessage(crewMember.Name + " 已经在训练中"); // is already in training
                return;
            }
            if (!Funding.CanAfford(trainingFee))
            {
                ScreenMessages.PostScreenMessage("无法支付训练费用 $" + trainingFee); // Cannot afford training fee of
                return;
            }
            Funding.Instance.AddFunds(-trainingFee, TransactionReasons.CrewRecruited);
            ScreenMessages.PostScreenMessage(crewMember.Name + " 要训练 " + newLevel + " 月"); // months
            crewMember.Train();
        }

        private PopupDialog DrawBudgetAllocationUi()
        {
            padding = 0;
            List<DialogGUIBase> dialogElements = new List<DialogGUIBase>();
            List<DialogGUIBase> innerElements = new List<DialogGUIBase>();
            DialogGUIBase[] horizontalArray = new DialogGUIBase[5];
            horizontalArray[0] = new DialogGUISpace(10);
            horizontalArray[1] = new DialogGUILabel("预算", MessageStyle(true)); // Budget
            horizontalArray[2] = new DialogGUIFlexibleSpace();// DialogGUISpace(70);
            // horizontalArray[3] = new DialogGUITextInput( fundingAllocation.ToString(), false, 3, s => SetAllocation("Budget", s), 40.0f, 30.0f);
            horizontalArray[3] = new DialogGUITextInput(GetAllocation(BudgetManager.Instance).ToString(), false, 3, s => SetAllocation("Budget", s), 40.0f, 30.0f);
            horizontalArray[4] = new DialogGUISpace(20);
            innerElements.Add(new DialogGUIHorizontalLayout(horizontalArray));
            horizontalArray = new DialogGUIBase[5];
            horizontalArray[0] = new DialogGUISpace(10);
            horizontalArray[1] = new DialogGUILabel("建造", MessageStyle(true)); // Construction
            horizontalArray[2] = new DialogGUIFlexibleSpace();// DialogGUISpace(10);
            //horizontalArray[3] = new DialogGUITextInput(constructionAllocation.ToString(), false, 3, s => SetAllocation("Construction", s), 40.0f, 30.0f);
            horizontalArray[3] = new DialogGUITextInput(GetAllocation(FacilityManager.Instance).ToString(), false, 3, s => SetAllocation("Construction", s), 40.0f, 30.0f);
            horizontalArray[4] = new DialogGUISpace(20);
            innerElements.Add(new DialogGUIHorizontalLayout(horizontalArray));
            horizontalArray = new DialogGUIBase[5];
            horizontalArray[0] = new DialogGUISpace(10);
            horizontalArray[1] = new DialogGUILabel("研究", MessageStyle(true)); // Research
            horizontalArray[2] = new DialogGUIFlexibleSpace();//DialogGUISpace(45);
            //horizontalArray[3] = new DialogGUITextInput(researchAllocation.ToString(), false, 3, s => SetAllocation("Research", s), 40.0f, 30.0f);
            horizontalArray[3] = new DialogGUITextInput(GetAllocation(ResearchManager.Instance).ToString(), false, 3, s => SetAllocation("Research", s), 40.0f, 30.0f);
            horizontalArray[4] = new DialogGUISpace(20);
            innerElements.Add(new DialogGUIHorizontalLayout(horizontalArray));
            for (int i = 0; i < Bureaucracy.Instance.registeredManagers.Count; i++)
            {
                Manager m = Bureaucracy.Instance.registeredManagers.ElementAt(i);
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (Utilities.Instance.GetNetBudget(m.Name) == -1.0f) continue;
                horizontalArray = new DialogGUIBase[3];
                horizontalArray[0] = new DialogGUISpace(10);
                string name = "";
                if (m.Name == "Budget")
                    name = "预算";
                else if (m.Name == "Construction")
                    name = "建造";
                else if (m.Name == "Research")
                    name = "研究";
                horizontalArray[1] = new DialogGUILabel(name + ": ");//m.Name
                horizontalArray[2] = new DialogGUILabel(() => ShowFunding(m));
                innerElements.Add(new DialogGUIHorizontalLayout(horizontalArray));
            }
            horizontalArray = new DialogGUIBase[3];
            horizontalArray[0] = new DialogGUISpace(10);
            horizontalArray[1] = new DialogGUIButton("载入设置", () => SettingsClass.Instance.InGameLoad(), false);  // Load Settings
            horizontalArray[2] = new DialogGUISpace(10);
            innerElements.Add(new DialogGUIHorizontalLayout(horizontalArray));
            DialogGUIVerticalLayout vertical = new DialogGUIVerticalLayout(innerElements.ToArray());
            dialogElements.Add(new DialogGUIScrollList(-Vector2.one, false, false, vertical));
            dialogElements.Add(GetBoxes("allocation"));
            return PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new MultiOptionDialog("Bureaucracy", "", "Bureaucracy: 预算拨款", UISkinManager.GetSkin("MainMenuSkin"), // Budget Allocation
                    GetRect(dialogElements), dialogElements.ToArray()), false, UISkinManager.GetSkin("MainMenuSkin"), false);
        }

        private string ShowFunding(Manager manager)
        {
            return "$"+Math.Round(Utilities.Instance.GetNetBudget(manager.Name),0).ToString(CultureInfo.CurrentCulture);
        }

        private string SetAllocation(string managerName, string passedString)
        {
            int.TryParse(passedString, out int i);
            float actualAllocation = i / 100.0f;
            Manager m = Utilities.Instance.GetManagerByName(managerName);
            m.FundingAllocation = actualAllocation;
            switch (managerName)
            {
                case "Budget":
                    fundingAllocation = i;
                    break;
                case "Research":
                    researchAllocation = i;
                    break;
                case "Construction":
                    constructionAllocation = i;
                    break;
            }

            return passedString;
        }

        private void DismissAllWindows()
        {
            if (mainWindow != null) mainWindow.Dismiss();
            if (facilitiesWindow != null) facilitiesWindow.Dismiss();
            if (researchWindow != null) researchWindow.Dismiss();
            if (allocationWindow != null) allocationWindow.Dismiss();
            if(crewWindow != null) crewWindow.Dismiss();
        }

        private PopupDialog DrawMainUi()
        {
            padding = 0;
            List<DialogGUIBase> dialogElements = new List<DialogGUIBase>();
            List<DialogGUIBase> innerElements = new List<DialogGUIBase>();
            if(HighLogic.CurrentGame.Mode != Game.Modes.CAREER)  innerElements.Add(new DialogGUILabel("Bureaucracy 只在生涯模式可用")); // is only available in Career Games
            else
            {
                innerElements.Add(new DialogGUISpace(10));
                innerElements.Add(new DialogGUIHorizontalLayout(PaddedLabel("下笔拨款: " + Utilities.Instance.ConvertUtToKspTimeStamp(BudgetManager.Instance.NextBudget.CompletionTime), false))); // 
                innerElements.Add(new DialogGUIHorizontalLayout(PaddedLabel("下笔拨款总预算: $" + Utilities.Instance.GetGrossBudget(), false))); // GrosNext Budgets Budget
                innerElements.Add(new DialogGUIHorizontalLayout(PaddedLabel("薪资成本: $" + Costs.Instance.GetWageCosts(), false))); // Wage Costs
                innerElements.Add(new DialogGUIHorizontalLayout(PaddedLabel("设施维护成本: $" + Costs.Instance.GetFacilityMaintenanceCosts(), false))); // Facility Maintenance Costs
                innerElements.Add(new DialogGUIHorizontalLayout(PaddedLabel("发射成本: $"+Costs.Instance.GetLaunchCosts(), false))); // Launch Costs
                innerElements.Add(new DialogGUIHorizontalLayout(PaddedLabel("任务奖励: $" + GetBonusesToPay(), false))); // Mission Bonuses
                for (int i = 0; i < Bureaucracy.Instance.registeredManagers.Count; i++)
                {
                    Manager m = Bureaucracy.Instance.registeredManagers.ElementAt(i);
                    if (m.Name == "Budget") continue;
                    double departmentFunding = Math.Round(Utilities.Instance.GetNetBudget(m.Name), 0);
                    if (departmentFunding < 0.0f) continue;
                    string name = "";
                    if (m.Name == "Construction")
                        name = "建造";
                    else if (m.Name == "Research")
                        name = "研究";
                    innerElements.Add(new DialogGUIHorizontalLayout(PaddedLabel(name + " 部门资金: $" + departmentFunding, false))); //m.Name Department Funding
                }
                innerElements.Add(new DialogGUIHorizontalLayout(PaddedLabel("净值预算: $"+Utilities.Instance.GetNetBudget("Budget"), false))); // Net Budget
                DialogGUIVerticalLayout vertical = new DialogGUIVerticalLayout(innerElements.ToArray());
                vertical.AddChild(new DialogGUIContentSizer(widthMode: ContentSizeFitter.FitMode.Unconstrained, heightMode: ContentSizeFitter.FitMode.MinSize));
                dialogElements.Add(new DialogGUIScrollList(new Vector2(300, 300), false, true, vertical));
                DialogGUIBase[] horizontal = new DialogGUIBase[6];
                horizontal[0] = new DialogGUILabel("拨款分配: "); // Allocations
                horizontal[1] = new DialogGUILabel("资金: "+fundingAllocation+"%"); // 
                horizontal[2] = new DialogGUILabel("|");
                horizontal[3] = new DialogGUILabel("建造: "+constructionAllocation+"%");  //
                horizontal[4] = new DialogGUILabel("|");
                horizontal[5] = new DialogGUILabel("研究: "+researchAllocation+"%"); // 
                dialogElements.Add(new DialogGUIHorizontalLayout(horizontal));
                dialogElements.Add(GetBoxes("main"));
            }
            return PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new MultiOptionDialog("BureaucracyMain", "", "Bureaucracy: 预算", UISkinManager.GetSkin("MainMenuSkin"),
                    GetRect(dialogElements), dialogElements.ToArray()), false, UISkinManager.GetSkin("MainMenuSkin"), false);
        }

        private Rect GetRect(List<DialogGUIBase> dialogElements)
        {
            return new Rect(0.5f, 0.5f, 300, 265) {height = 150 + 50 * dialogElements.Count, width = Math.Max(padding, 280)};
        }

        private DialogGUIBase[] PaddedLabel(string stringToPad, bool largePrint)
        {
            DialogGUIBase[] paddedLayout = new DialogGUIBase[2];
            paddedLayout[0] = new DialogGUISpace(10);
            EvaluatePadding(stringToPad);
            paddedLayout[1] = new DialogGUILabel(stringToPad, MessageStyle(largePrint));
            return paddedLayout;
        }

        private void EvaluatePadding(string stringToEvaluate)
        {
            if (stringToEvaluate.Length *PadFactor > padding) padding = stringToEvaluate.Length * PadFactor;
        }

        private UIStyle MessageStyle(bool largePrint)
        {
            UIStyle style = new UIStyle
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.LowerCenter,
                stretchWidth = false,
                normal = new UIStyleState
                {
                    textColor = new Color(0.89f, 0.86f, 0.72f)
                }
            };
            if (largePrint) style.fontSize = 23;
            return style;
        }

        private int GetBonusesToPay()
        {
            int bonusesToPay = 0;
            for (int i = 0; i < CrewManager.Instance.Kerbals.Count; i++)
            {
                CrewMember c = CrewManager.Instance.Kerbals.ElementAt(i).Value;
                bonusesToPay += c.GetBonus(false);
            }
            return bonusesToPay;
        }

        private PopupDialog DrawFacilityUi()
        {
            padding = 0;
            List<DialogGUIBase> dialogElements = new List<DialogGUIBase>();
            List<DialogGUIBase> innerElements = new List<DialogGUIBase>();
            int upgradeCount = 0;
            innerElements.Add(new DialogGUISpace(10));
            float investmentNeeded = 0;
            innerElements.Add(new DialogGUIContentSizer(ContentSizeFitter.FitMode.Unconstrained, ContentSizeFitter.FitMode.PreferredSize, true));
            innerElements.Add(new DialogGUIHorizontalLayout(PaddedLabel("本月预算: $"+Math.Round(FacilityManager.Instance.ThisMonthsBudget, 0), false))); // 
            for (int i = 0; i < FacilityManager.Instance.Facilities.Count; i++)
            {
                BureaucracyFacility bf = FacilityManager.Instance.Facilities.ElementAt(i);
                if (!bf.Upgrading) continue;
                upgradeCount++;
                investmentNeeded += bf.Upgrade.RemainingInvestment;
                float percentage = bf.Upgrade.OriginalCost - bf.Upgrade.RemainingInvestment;
                percentage = (float)Math.Round(percentage / bf.Upgrade.OriginalCost * 100,0);
                innerElements.Add(new DialogGUIHorizontalLayout(PaddedLabel(bf.Name + " "+percentage + "% ($" + bf.Upgrade.RemainingInvestment + " 需要)", false))); // 
            }
            if (upgradeCount == 0) innerElements.Add(new DialogGUIHorizontalLayout(PaddedLabel("无设施在升级", false))); // No Facility Upgrades in progress
            DialogGUIVerticalLayout vertical = new DialogGUIVerticalLayout(innerElements.ToArray());
            dialogElements.Add(new DialogGUIScrollList(new Vector2(300, 300), false, true, vertical));
            DialogGUIBase[] horizontal = new DialogGUIBase[3];
            horizontal[0] = new DialogGUILabel("所需投资总额: $"+investmentNeeded); // 
            horizontal[1] = new DialogGUILabel("|");
            horizontal[2] = new DialogGUILabel("消防隐患: "+Math.Round(FacilityManager.Instance.FireChance*100, 0)+"%"); // Chance of Fire
            dialogElements.Add(new DialogGUIHorizontalLayout(horizontal));
            dialogElements.Add(GetBoxes("facility"));
            return PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new MultiOptionDialog("FacilitiesDialog", "", "Bureaucracy: 设施", UISkinManager.GetSkin("MainMenuSkin"), new Rect(0.5f, 0.5f, 320, 350), dialogElements.ToArray()), false, UISkinManager.GetSkin("MainMenuSkin"));//Facilities
        }

        private PopupDialog DrawResearchUi()
        {
            padding = 0;
            float scienceCount = 0;
            List<DialogGUIBase> dialogElements = new List<DialogGUIBase>();
            List<DialogGUIBase> innerElements = new List<DialogGUIBase>();
            innerElements.Add(new DialogGUIContentSizer(ContentSizeFitter.FitMode.Unconstrained, ContentSizeFitter.FitMode.PreferredSize, true));
            innerElements.Add(new DialogGUISpace(10));
            if(ResearchManager.Instance.ProcessingScience.Count == 0) innerElements.Add(new DialogGUIHorizontalLayout(PaddedLabel("无研究在进行", false))); // No research in progress
            for (int i = 0; i < ResearchManager.Instance.ProcessingScience.Count; i++)
            {
                ScienceEvent se = ResearchManager.Instance.ProcessingScience.ElementAt(i).Value;
                if (se.IsComplete) continue;
                scienceCount += se.RemainingScience;
                innerElements.Add(new DialogGUIHorizontalLayout(PaddedLabel(se.UiName+": "+Math.Round(se.OriginalScience-se.RemainingScience, 1)+"/"+Math.Round(se.OriginalScience, 1), false)));
            }

            dialogElements.Add(new DialogGUIScrollList(new Vector2(300, 300), false, true, new DialogGUIVerticalLayout(10, 100, 4, new RectOffset(6, 24, 10, 10), TextAnchor.UpperLeft, innerElements.ToArray())));
            DialogGUIBase[] horizontal = new DialogGUIBase[3];
            horizontal[0] = new DialogGUILabel("处理中的科学点数: " + Math.Round(scienceCount, 1)); // Processing Science
            horizontal[1] = new DialogGUILabel("|");
            double scienceOutput = ResearchManager.Instance.ThisMonthsBudget / SettingsClass.Instance.ScienceMultiplier * ResearchManager.Instance.ScienceMultiplier;
            horizontal[2] = new DialogGUILabel("研究产出: "+Math.Round(scienceOutput, 1)); // Research Output
            dialogElements.Add(new DialogGUIHorizontalLayout(horizontal));
            dialogElements.Add(GetBoxes("research"));
            return PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new MultiOptionDialog("ResearchDialog", "", "Bureaucracy: 研究", UISkinManager.GetSkin("MainMenuSkin"), GetRect(dialogElements), dialogElements.ToArray()), false, UISkinManager.GetSkin("MainMenuSkin"));//Research
        }

        private DialogGUIHorizontalLayout GetBoxes(string passingUi)
        {
            int arrayPointer = 0;
            DialogGUIBase[] horizontal = new DialogGUIBase[5];
            if (passingUi != "main")
            {
                horizontal[arrayPointer] = new DialogGUIButton("预算", ()=> ActivateUi("main")); // Budget
                arrayPointer++;
            }
            if (passingUi != "facility")
            {
                horizontal[arrayPointer] = new DialogGUIButton("建造", () => ActivateUi("facility")); // Construction
                arrayPointer++;
            }
            if (passingUi != "research")
            {
             horizontal[arrayPointer] = new DialogGUIButton("研究", () => ActivateUi("research")); // Research
             arrayPointer++;
            }
            if (passingUi != "allocation")
            {
                horizontal[arrayPointer] = new DialogGUIButton("拨款", () => ActivateUi("allocation")); // Allocation
                arrayPointer++;
            }
            if (passingUi != "crew")
            {
                horizontal[arrayPointer] = new DialogGUIButton("员工", () => ActivateUi("crew")); //Crew
                arrayPointer++;
            }
            horizontal[arrayPointer] = new DialogGUIButton("关闭", ValidateAllocations, false); // Close
            return new DialogGUIHorizontalLayout(280, 35, horizontal);
        }

        public void ValidateAllocations()
        {
            int allocations = fundingAllocation + constructionAllocation + researchAllocation;
            if (allocations != 100) errorWindow = AllocationErrorWindow();
            else DismissAllWindows();
        }

        private PopupDialog AllocationErrorWindow()
        {
            List<DialogGUIBase> dialogElements = new List<DialogGUIBase>();
            dialogElements.Add(new DialogGUILabel("拨款总金额未达到 100%")); // Allocations do not add up to 100%
            dialogElements.Add(new DialogGUIButton("OK", () => { }, true));
            return PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new MultiOptionDialog("AllocationError", "", "Bureaucracy: 错误", UISkinManager.GetSkin("MainMenuSkin"), new Rect(0.5f, 0.5f, 200,90), dialogElements.ToArray()), false, UISkinManager.GetSkin("MainMenuSkin")); // Error
        }

        public void RemoveToolbarButton(GameScenes data)
        {
            if (toolbarButton == null) return;
            ApplicationLauncher.Instance.RemoveModApplication(toolbarButton);
        }

        private void OnDisable()
        {
            RemoveToolbarButton(HighLogic.LoadedScene);
        }

        public PopupDialog NoHireWindow()
        {
            List<DialogGUIBase> dialogElements = new List<DialogGUIBase>();
            dialogElements.Add(new DialogGUILabel("由于减少了雇员预算，我们现在无法承担招募新人的花费")); // Due to reduced staffing levels we are unable to take on any new kerbals at this time
            dialogElements.Add(new DialogGUIButton("OK", () => { }, true));
            return PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new MultiOptionDialog("NoHire", "", "无法招募", UISkinManager.GetSkin("MainMenuSkin"), new Rect(0.5f, 0.5f, 100, 200), dialogElements.ToArray()), false, UISkinManager.GetSkin("MainMenuSkin")); // Can't Hire!
        }
        
        public PopupDialog GeneralError(string error)
        {
            List<DialogGUIBase> dialogElements = new List<DialogGUIBase>();
            dialogElements.Add(new DialogGUILabel(error));
            dialogElements.Add(new DialogGUIButton("OK", () => { }, true));
            return PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new MultiOptionDialog("GeneralErrorDialog", "", "Bureaucracy: 错误", UISkinManager.GetSkin("MainMenuSkin"), new Rect(0.5f, 0.5f, 200,200), dialogElements.ToArray()), false, UISkinManager.GetSkin("MainMenuSkin")); // Error
        }
        public void OnSave(ConfigNode cn)
        {
            ConfigNode uiNode = new ConfigNode("UI");
            uiNode.SetValue("FundingAllocation", fundingAllocation, true);
            uiNode.SetValue("ResearchAllocation", researchAllocation, true);
            uiNode.SetValue("ConstructionAllocation", constructionAllocation, true);
            cn.AddNode(uiNode);
        }

        public void OnLoad(ConfigNode cn)
        {
            ConfigNode uiNode = cn.GetNode("UI");
            if (uiNode == null) return;
            int.TryParse(uiNode.GetValue("FundingAllocation"), out fundingAllocation);
            SetAllocation("Budget", fundingAllocation.ToString());
            int.TryParse(uiNode.GetValue("ResearchAllocation"), out researchAllocation);
            SetAllocation("Research", researchAllocation.ToString());
            int.TryParse(uiNode.GetValue("ConstructionAllocation"), out constructionAllocation);
            SetAllocation("Construction", constructionAllocation.ToString());
        }

        public PopupDialog NoLaunchesWindow()
        {
            List<DialogGUIBase> dialogElements = new List<DialogGUIBase>();
            dialogElements.Add(new DialogGUILabel("因为当前的资金水平，我们买不起燃料了"));  // Due to reduced funding levels, we were unable to afford any fuel
            dialogElements.Add(new DialogGUISpace(20));
            dialogElements.Add(new DialogGUILabel("到月底前不会有燃料供应")); // No fuel will be available until the end of the month.
            dialogElements.Add(new DialogGUIButton("OK", () => { }, true));
            return PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new MultiOptionDialog("NoFuel", "", "无燃料可用！", UISkinManager.GetSkin("MainMenuSkin"), new Rect(0.5f, 0.5f, 200,160), dialogElements.ToArray()), false, UISkinManager.GetSkin("MainMenuSkin")); // No Fuel Available!
        }

        public PopupDialog KctError()
        {
            List<DialogGUIBase> dialogElements = new List<DialogGUIBase>();
            dialogElements.Add(new DialogGUILabel("看来你已安装了Kerbal Construction Time。你不应该同时启用KCT和Bureaucracy的设施升级功能。不好的事情会发生")); // It looks like you have Kerbal Construction Time installed. You should not use KCT's Facility Upgrade and Bureaucracy's Facility Upgrade at the same time. Bad things will happen.
            dialogElements.Add(new DialogGUIButton("OK", () => { }, true));
            return PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new MultiOptionDialog("KCTError", "", "检测到KCT!", UISkinManager.GetSkin("MainMenuSkin"), new Rect(0.5f, 0.5f, 400,100), dialogElements.ToArray()), false, UISkinManager.GetSkin("MainMenuSkin")); // KCT Detected
        }

        private void OnDestroy()
        {
            GameEvents.onGUIApplicationLauncherReady.Remove(SetupToolbarButton);
            GameEvents.onGUIApplicationLauncherUnreadifying.Remove(RemoveToolbarButton);
        }
    }
}