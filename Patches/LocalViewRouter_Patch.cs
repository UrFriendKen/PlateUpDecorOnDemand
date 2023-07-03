using HarmonyLib;
using Kitchen;
using UnityEngine;

namespace KitchenDecorOnDemand.Patches
{
    [HarmonyPatch]
    static class LocalViewRouter_Patch
    {
        static GameObject _spawnRequestPrefab;

        [HarmonyPatch(typeof(LocalViewRouter), "GetPrefab")]
        [HarmonyPrefix]
        static bool GetPrefab_Prefix(ViewType view_type, ref GameObject __result)
        {
            if (_spawnRequestPrefab == null)
            {
                GameObject gameObject = new GameObject("Hider");
                gameObject.SetActive(false);
                _spawnRequestPrefab = new GameObject(((int)Main.SpawnRequestViewType).ToString());
                _spawnRequestPrefab.AddComponent<SpawnRequestView>();
                _spawnRequestPrefab.transform.SetParent(gameObject.transform);
            }
            if (view_type == Main.SpawnRequestViewType)
            {
                __result = _spawnRequestPrefab;
                return false;
            }
            return true;
        }
    }
}
