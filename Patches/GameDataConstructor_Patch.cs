using HarmonyLib;
using Kitchen;
using KitchenData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace KitchenDecorOnDemand.Patches
{
    [HarmonyPatch]
    static class GameDataConstructor_Patch
    {
        [HarmonyPatch(typeof(GameDataConstructor), "BuildGameData")]
        [HarmonyPriority(int.MaxValue)]
        [HarmonyPrefix]
        static void BuildGameData_Prefix(ref List<GameDataObject> ___GameDataObjects)
        {
            if (___GameDataObjects == null)
                return;

            FieldInfo f_Item = typeof(CItemProvider).GetField("Item", BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (Item item in ___GameDataObjects.Where(gdo => typeof(Item).IsAssignableFrom(gdo.GetType())))
            {
                IEnumerable<Type> propertyTypes = item.DedicatedProvider?.Properties?.Select(x => x.GetType());
                if ((propertyTypes?.Contains(typeof(CItemProvider)) ?? true) || propertyTypes.Contains(typeof(CDynamicMenuProvider)))
                    continue;
                CItemProvider provider = CItemProvider.InfiniteItemProvider(item.ID);
                f_Item?.SetValueDirect(__makeref(provider), item.ID);
                item.DedicatedProvider.Properties.Add(provider);
                Main.LogInfo($"Populated CItemProvider for {item.name} in dedicated provider ({item.DedicatedProvider.name})");
            }
        }
    }
}
