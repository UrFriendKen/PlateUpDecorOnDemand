using Kitchen;
using KitchenData;
using KitchenMods;
using PreferenceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

// Namespace should have "Kitchen" in the beginning
namespace KitchenDecorOnDemand
{
    public class Main : IModInitializer
    {
        public const string MOD_GUID = "IcedMilo.PlateUp.DecorOnDemand";
        public const string MOD_NAME = "Decor on Demand";
        public const string MOD_VERSION = "0.1.3";

        private const string MENU_START_OPEN_ID = "menuStartOpen";
        private PreferenceSystemManager PrefManager;

        public Main()
        {
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
                .AddSpacer()
                .AddSpacer();

            PrefManager.RegisterMenu(PreferenceSystemManager.MenuType.MainMenu);
            PrefManager.RegisterMenu(PreferenceSystemManager.MenuType.PauseMenu);
        }

        public void PreInject()
        {
            GameObject gameObject = new GameObject(MOD_NAME);
            DecorGUI decorGUI = gameObject.AddComponent<DecorGUI>();
            decorGUI.showMenu = PrefManager.Get<bool>(MENU_START_OPEN_ID);
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

    public class DecorGUI : MonoBehaviour
    {
        public static Rect windowRect = new Rect(10, 600, 250, 600);
        private static Vector2 scrollPosition;
        private static string searchText = string.Empty;
        private static Dictionary<string, int> decors = new Dictionary<string, int>();
        private static List<string> decorNames;
        public bool showMenu = true;

        public void Update()
        {
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
        }

        private int? _windowID = null;
        public void OnGUI()
        {
            if (showMenu)
            {
                if (_windowID == null)
                {
                    _windowID = GetInt32HashCode(Main.MOD_GUID);
                }
                windowRect = GUILayout.Window(_windowID.Value, windowRect, DecorSpawnWindow, "Decor on Demand", GUILayout.Width(250f), GUILayout.Height(600f));
            }
            int GetInt32HashCode(string strText)
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
        }

        public static void DecorSpawnWindow(int windowID)
        {
            GUILayout.Space(2);
            searchText = GUILayout.TextField(searchText);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
            for (int i = 0; i < decorNames.Count; i++)
            {
                if (string.IsNullOrEmpty(searchText) || decorNames[i].ToLower().Contains(searchText.ToLower()))
                {
                    if (GUILayout.Button(new GUIContent(decorNames[i], $"Click button to spawn {decorNames[i]}")))
                    {
                        if (Session.CurrentGameNetworkMode == GameNetworkMode.Host && GameInfo.CurrentScene == SceneType.Kitchen)
                            SpawnRequestedDecor.RequestDecor(decors[decorNames[i]]);
                    }
                }
            }
            GUILayout.EndScrollView();

            GUILayout.Label("Press F3 to toggle this menu.");
            GUI.DragWindow();
        }
    }

}
