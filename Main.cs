﻿using HarmonyLib;
using Kitchen;
using KitchenData;
using KitchenMods;
using PreferenceSystem;
using PreferenceSystem.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

// Namespace should have "Kitchen" in the beginning
namespace KitchenDecorOnDemand
{
    public class Main : IModInitializer
    {
        public const string MOD_GUID = "IcedMilo.PlateUp.DecorOnDemand";
        public const string MOD_NAME = "Stuff on Demand";
        public const string MOD_VERSION = "0.2.1";

        internal const string MENU_START_OPEN_ID = "menuStartOpen";
        internal const string HOST_ONLY_ID = "hostOnly";
        internal const string APPLIANCE_SPAWN_AS_ID = "applianceSpawnAs";
        internal const string SPAWN_AT_ID = "spawnAt";
        internal static PreferenceSystemManager PrefManager;

        internal static ViewType SpawnRequestViewType = (ViewType)Main.GetInt32HashCode("SpawnRequestViewType");

        Harmony harmony;
        static List<Assembly> PatchedAssemblies = new List<Assembly>();

        public Main()
        {
            if (harmony == null)
                harmony = new Harmony(MOD_GUID);
            Assembly assembly = Assembly.GetExecutingAssembly();
            if (assembly != null && !PatchedAssemblies.Contains(assembly))
            {
                harmony.PatchAll(assembly);
                PatchedAssemblies.Add(assembly);
            }
        }

        public void PostActivate(KitchenMods.Mod mod)
        {
            LogWarning($"{MOD_GUID} v{MOD_VERSION} in use!");

            PrefManager = new PreferenceSystemManager(MOD_GUID, MOD_NAME);
            PrefManager
                .AddLabel("Menu Starts")
                .AddOption<bool>(
                    MENU_START_OPEN_ID,
                    true,
                    new bool[] { false, true },
                    new string[] { "Closed", "Opened" })
                .AddLabel("Can Spawn")
                .AddOption<bool>(
                    HOST_ONLY_ID,
                    false,
                    new bool[] { false, true },
                    new string[] { "Everyone", "Only Host" })
                .AddLabel("Spawn At")
                .AddOption<string>(
                    SPAWN_AT_ID,
                    SpawnPositionType.Door.ToString(),
                    Enum.GetNames(typeof(SpawnPositionType)),
                    Enum.GetNames(typeof(SpawnPositionType)))
                .AddSpacer()
                .AddSubmenu("Appliance", "appliance")
                    .AddLabel("Spawn As")
                    .AddOption<string>(
                        APPLIANCE_SPAWN_AS_ID,
                        SpawnApplianceMode.Blueprint.ToString(),
                        Enum.GetNames(typeof(SpawnApplianceMode)),
                        Enum.GetNames(typeof(SpawnApplianceMode)))
                .SubmenuDone()
                .AddSubmenu("Decor", "decor")
                    .AddButtonWithConfirm("Remove Applied Decor", "Strip applied wallpapers and flooring? This only works for the host.",
                        delegate(GenericChoiceDecision decision)
                        {
                            if (Session.CurrentGameNetworkMode == GameNetworkMode.Host && decision == GenericChoiceDecision.Accept)
                            {
                                StripRequestSystem.Request();
                            }
                        })
                    .AddSpacer()
                    .AddSpacer()
                .SubmenuDone()
                .AddSpacer()
                .AddSpacer();

            PrefManager.RegisterMenu(PreferenceSystemManager.MenuType.MainMenu);
            PrefManager.RegisterMenu(PreferenceSystemManager.MenuType.PauseMenu);
        }

        public void PreInject()
        {
            if (GameObject.FindObjectOfType<SpawnGUI>() == null)
            {
                GameObject gameObject = new GameObject(MOD_NAME);
                SpawnGUI spawnGUI = gameObject.AddComponent<SpawnGUI>();
                spawnGUI.showMenu = PrefManager.Get<bool>(MENU_START_OPEN_ID);
            }
        }
        public static int GetInt32HashCode(string strText)
        {
            SHA1 hash = new SHA1CryptoServiceProvider();
            if (string.IsNullOrEmpty(strText))
            {
                return 0;
            }

            byte[] bytes = Encoding.Unicode.GetBytes(strText);
            byte[] value = hash.ComputeHash(bytes);
            uint num = BitConverter.ToUInt32(value, 0);
            uint num2 = BitConverter.ToUInt32(value, 8);
            uint num3 = BitConverter.ToUInt32(value, 16);
            uint num4 = num ^ num2 ^ num3;
            return BitConverter.ToInt32(BitConverter.GetBytes(uint.MaxValue - num4), 0);
        }

        public void PostInject() { }

        #region Logging
        public static void LogInfo(string _log) { Debug.Log($"[{MOD_NAME}] " + _log); }
        public static void LogWarning(string _log) { Debug.LogWarning($"[{MOD_NAME}] " + _log); }
        public static void LogError(string _log) { Debug.LogError($"[{MOD_NAME}] " + _log); }
        public static void LogInfo(object _log) { LogInfo(_log.ToString()); }
        public static void LogWarning(object _log) { LogWarning(_log.ToString()); }
        public static void LogError(object _log) { LogError(_log.ToString()); }
        #endregion
    }

    public class SpawnGUI : MonoBehaviour
    {
        private const int MAX_DUPLICATE_NAMES = 100;

        private static Dictionary<string, int> decors = new Dictionary<string, int>();
        private static List<string> decorNames;
        private static Dictionary<string, int> appliances = new Dictionary<string, int>();
        private static List<string> applianceNames;

        public Rect windowRect = new Rect(10, 10, 250, 600);
        private Vector2 scrollPosition;
        private string searchText = string.Empty;
        private SpawnType currentMode = SpawnType.Decor;
        public bool showMenu = true;

        private SpawnRequestView spawnRequestView;

        public void Update()
        {
            if (spawnRequestView == null)
            {
                spawnRequestView = GameObject.FindObjectOfType<ActiveSpawnRequestView>()?.LinkedView;
            }

            if (Input.GetKeyDown(KeyCode.F3))
            {
                showMenu = !showMenu;
            }

            if (decorNames == null)
            {
                decorNames = new List<string>();
                foreach (Decor decor in GameData.Main.Get<Decor>().Where(x => x.IsAvailable))
                {
                    string decorName = $"{decor.name}";

                    if (!decorNames.Contains(decorName))
                    {
                        decors.Add(decorName, decor.ID);
                        decorNames.Add(decorName);
                    }
                }
                decorNames.Sort();
            }

            if (applianceNames == null)
            {
                applianceNames = new List<string>();
                foreach (Appliance appliance in GameData.Main.Get<Appliance>())
                {
                    if (appliance.Properties.Select(x => x.GetType()).Contains(typeof(CImmovable)))
                        continue;
                    string applianceName = $"{(appliance.Name.IsNullOrEmpty() ? appliance.name : appliance.Name)}";
                    for (int i = 1; i < MAX_DUPLICATE_NAMES + 1; i++)
                    {
                        string tempName = $"{applianceName}{(i > 1 ? i.ToString() : "")}";
                        if (!applianceNames.Contains(tempName))
                        {
                            appliances.Add(tempName, appliance.ID);
                            applianceNames.Add(tempName);
                            break;
                        }
                    }
                }
                applianceNames.Sort();
            }
        }

        private int? _windowID = null;
        public void OnGUI()
        {
            if (showMenu)
            {
                if (_windowID == null)
                {
                    _windowID = Main.GetInt32HashCode(Main.MOD_GUID);
                }
                windowRect = GUILayout.Window(_windowID.Value, windowRect, SpawnWindow, "Decor on Demand", GUILayout.Width(250f), GUILayout.Height(600f));
            }
        }

        public void SpawnWindow(int windowID)
        {
            GUILayout.Space(2);
            foreach (SpawnType mode in Enum.GetValues(typeof(SpawnType)))
            {
                if (GUILayout.Button(mode.ToString()))
                {
                    currentMode = mode;
                }
            }
            List<string> gdoNames;
            Dictionary<string, int> gdoDict;
            Action<int> spawnMethod = null;
            switch (currentMode)
            {
                case SpawnType.Appliance:
                    gdoNames = applianceNames;
                    gdoDict = appliances;
                    if (spawnRequestView != null)
                        spawnMethod = spawnRequestView.Request<Appliance>;
                    break;
                case SpawnType.Decor:
                default:
                    gdoNames = decorNames;
                    gdoDict = decors;
                    if (spawnRequestView != null)
                        spawnMethod = spawnRequestView.Request<Decor>;
                    break;
            }
            GUILayout.Space(1);
            GUILayout.Label("Search:");
            searchText = GUILayout.TextField(searchText);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
            for (int i = 0; i < gdoNames.Count; i++)
            {
                if (string.IsNullOrEmpty(searchText) || gdoNames[i].ToLower().Contains(searchText.ToLower()))
                {
                    if (GUILayout.Button(new GUIContent(gdoNames[i], $"Click button to spawn {gdoNames[i]}")))
                    {
                        if (spawnMethod != null)
                            spawnMethod(gdoDict[gdoNames[i]]);
                    }
                }
            }
            GUILayout.EndScrollView();

            GUILayout.Label("Press F3 to toggle this menu.");
            GUI.DragWindow();
        }
    }
}
