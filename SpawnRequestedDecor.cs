using Kitchen;
using KitchenData;
using KitchenMods;
using System;
using Unity.Entities;
using UnityEngine;

namespace KitchenDecorOnDemand
{
    public class SpawnRequestedDecor : SpawnHandlerSystemBase, IModSystem
    {
        protected override Type GDOType => typeof(Decor);

        protected override void Spawn(GameDataObject gdo, Vector3 position, SpawnApplianceMode spawnApplianceMode)
        {
            if (gdo is Decor decor)
                AddDecorationItem(decor.ApplicatorAppliance.ID, decor.ID, position, decor.Type);
        }

        protected void AddDecorationItem(int applicator_id, int wallpaper_id, Vector3 position, LayoutMaterialType type)
        {
            Entity entity = base.EntityManager.CreateEntity();
            base.EntityManager.AddComponentData(entity, new CCreateAppliance
            {
                ID = applicator_id
            });
            base.EntityManager.AddComponentData(entity, new CPosition(position));
            base.EntityManager.AddComponentData(entity, new CApplyDecor
            {
                ID = wallpaper_id,
                Type = type
            });
            base.EntityManager.AddComponentData(entity, new CDrawApplianceUsing
            {
                DrawApplianceID = wallpaper_id
            });
            base.EntityManager.AddComponentData(entity, default(CShopEntity));
        }
    }
}
