﻿using System;
using System.Collections.Generic;
using System.Linq;
using KSP.UI.Screens;
using LibNoise.Modifiers;
using UnityEngine;
using UnityEngine.UI;

namespace Bureaucracy
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]

    public class BankOfKerbinSc : BankOfKerbin
    {
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class BankOfKerbinFlight : BankOfKerbin
    {
    }

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class BankOfKerbinEditor : BankOfKerbin
    {
    }

    [KSPAddon(KSPAddon.Startup.TrackingStation, false)]
    public class BankOfKerbinTrackStation : BankOfKerbin
    {
    }

    public class BankOfKerbin : MonoBehaviour
    {
        private ApplicationLauncherButton toolbarButton;
        private PopupDialog dialogWindow;
        private double balance = 0;
        private int playerInput = 0;
        public static BankOfKerbin Instance;

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
            GameEvents.onGUIApplicationLauncherReady.Add(AddToolbarButton);
            GameEvents.onGUIApplicationLauncherUnreadifying.Add(RemoveToolbarButton);
        }

        private void RemoveToolbarButton(GameScenes data)
        {
            if (toolbarButton == null) return;
            ApplicationLauncher.Instance.RemoveModApplication(toolbarButton);
        }

        private void AddToolbarButton()
        {
            //TODO: Get an Icon
            if(HighLogic.CurrentGame.Mode == Game.Modes.CAREER) toolbarButton = ApplicationLauncher.Instance.AddModApplication(ToggleUI, ToggleUI, null, null, null, null, ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.FLIGHT, GameDatabase.Instance.GetTexture("Bureaucracy/BankIcon", false));
        }
        
        private void ToggleUI()
        {
            if (dialogWindow == null) dialogWindow = DrawUI();
        }

        private PopupDialog DrawUI()
        {
            List<DialogGUIBase> dialogElements = new List<DialogGUIBase>();
            List<DialogGUIBase> innerElements = new List<DialogGUIBase>();
            innerElements.Add(new DialogGUIImage(new Vector2(300, 147), new Vector2(0, 0), Color.gray, GameDatabase.Instance.GetTexture("Bureaucracy/Mortimer", false)));
            innerElements.Add(new DialogGUILabel(() => "银行余额：" + Math.Round(balance, 0))); // "Bank Balance: "
            innerElements.Add(new DialogGUITextInput(playerInput.ToString(), false, 30, s => SetPlayerInput(s), 300.0f, 30.0f));
            DialogGUIBase[] horizontal = new DialogGUIBase[3];
            horizontal[0] = new DialogGUIButton("提款", () => DepositFunds(playerInput), false); // "Deposit"
            horizontal[1] = new DialogGUIButton("存钱", () => WithdrawFunds(playerInput), false); // "Withdraw"
            horizontal[2] = new DialogGUIButton("关闭", null, true); // "Close"
            innerElements.Add(new DialogGUIHorizontalLayout(horizontal));
            DialogGUIVerticalLayout vertical = new DialogGUIVerticalLayout(innerElements.ToArray());
            dialogElements.Add(vertical);
            return PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new MultiOptionDialog("Bureaucracy", "", FlightGlobals.GetHomeBody().displayName + "的银行", UISkinManager.GetSkin("MainMenuSkin"), //"Bank of "+FlightGlobals.GetHomeBody().bodyName
                    new Rect(0.5f, 0.5f, 350, 265), dialogElements.ToArray()), false, UISkinManager.GetSkin("MainMenuSkin"), false);
        }

        private void WithdrawFunds(int playerInput)
        {
            double fundsToWithdraw = Math.Min(balance, playerInput);
            Funding.Instance.AddFunds(fundsToWithdraw, TransactionReasons.None);
            balance -= fundsToWithdraw;
        }

        private void DepositFunds(int playerInput)
        {
            if (!Funding.CanAfford(playerInput)) return;
            balance += playerInput;
            Funding.Instance.AddFunds(-playerInput, TransactionReasons.None);
        }

        private string SetPlayerInput(string s)
        {
            int.TryParse(s, out playerInput);
            return s;
        }

        public void OnSave(ConfigNode cn)
        {
            ConfigNode thisNode = new ConfigNode("BankOfKerbin");
            thisNode.AddValue("Balance", balance);
            cn.AddNode(thisNode);
        }

        public void OnLoad(ConfigNode cn)
        {
            ConfigNode thisNode = null;
            cn.TryGetNode("BankOfKerbin", ref thisNode);
            if (thisNode == null) return;
            thisNode.TryGetValue("Balance", ref balance);
        }
        private void OnDisable()
        {
            GameEvents.onGUIApplicationLauncherReady.Remove(AddToolbarButton);
            GameEvents.onGUIApplicationLauncherUnreadifying.Remove(RemoveToolbarButton);
            RemoveToolbarButton(HighLogic.LoadedScene);
        }
    }
}