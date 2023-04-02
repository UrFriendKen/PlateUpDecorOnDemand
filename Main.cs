using Kitchen;
using KitchenData;
using KitchenLib;
using KitchenLib.Event;
using KitchenLib.Utils;
using KitchenMods;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

// Namespace should have "Kitchen" in the beginning
namespace KitchenDecorOnDemand
{
    public class Main : BaseMod, IModSystem
    {
        // GUID must be unique and is recommended to be in reverse domain name notation
        // Mod Name is displayed to the player and listed in the mods menu
        // Mod Version must follow semver notation e.g. "1.2.3"
        public const string MOD_GUID = "IcedMilo.PlateUp.DecorOnDemand";
        public const string MOD_NAME = "Decor on Demand";
        public const string MOD_VERSION = "0.1.1";
        public const string MOD_AUTHOR = "IcedMilo";
        public const string MOD_GAMEVERSION = ">=1.1.3";
        // Game version this mod is designed for in semver
        // e.g. ">=1.1.3" current and all future
        // e.g. ">=1.1.3 <=1.2.3" for all from/until

        public static Rect windowRect = new Rect(10, 600, 250, 600);
        private static Vector2 scrollPosition;
        private static string searchText = string.Empty;
        private static Dictionary<string, int> decors = new Dictionary<string, int>();
        private static List<string> decorNames = new List<string>();
        public static bool showMenu = true;

        public Main() : base(MOD_GUID, MOD_NAME, MOD_AUTHOR, MOD_VERSION, MOD_GAMEVERSION, Assembly.GetExecutingAssembly()) { }

        protected override void OnInitialise()
        {
            GameObject gameObject = new GameObject();
            gameObject.AddComponent<DecorGUI>();
        }

        private void AddGameData()
        {
            LogInfo("Attempting to register game data...");

            // AddGameDataObject<MyCustomGDO>();

            LogInfo("Done loading game data.");
        }

        protected override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.F3))
            {
                showMenu = !showMenu;
            }
        }

        protected override void OnPostActivate(KitchenMods.Mod mod)
        {
            // TODO: Uncomment the following if you have an asset bundle.
            // TODO: Also, make sure to set EnableAssetBundleDeploy to 'true' in your ModName.csproj

            // LogInfo("Attempting to load asset bundle...");
            // Bundle = mod.GetPacks<AssetBundleModPack>().SelectMany(e => e.AssetBundles).First();
            // LogInfo("Done loading asset bundle.");

            // Register custom GDOs
            // AddGameData();

            // Perform actions when game data is built
            Events.BuildGameDataEvent += (s, args) =>
            {
                foreach (Decor decor in args.gamedata.Get<Decor>().Where(x => x.IsAvailable))
                {
                    string decorName = $"{decor.name}";

                    if (!decorNames.Contains(decorName))
                    {
                        decors.Add(decorName, decor.ID);
                        decorNames.Add(decorName);
                    }
                }
                decorNames.Sort();
            };
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
        public void OnGUI()
        {
            if (Main.showMenu)
            {
                Main.windowRect = GUILayout.Window(VariousUtils.GetID(Main.MOD_GUID), Main.windowRect, Main.DecorSpawnWindow, "Decor on Demand", GUILayout.Width(250f), GUILayout.Height(600f));
            }
        }
    }

}
