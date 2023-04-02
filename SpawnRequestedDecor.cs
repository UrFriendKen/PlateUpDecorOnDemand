using Kitchen;
using KitchenData;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace KitchenDecorOnDemand
{
    public class SpawnRequestedDecor : GameSystemBase
    {
        static Queue<int> requestedDecors = new Queue<int>();

        protected override void OnUpdate()
        {
            if (requestedDecors.Count > 0)
            {
                int id = requestedDecors.Dequeue();
                Decor decor = base.Data.Get<Decor>(id);
                
                if (decor != null)
                {
                    AddDecorationItem(decor.ApplicatorAppliance.ID, id, GetFrontDoor(get_external_tile: true), decor.Type);
                }
            }
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

        public static void RequestDecor(int decorId)
        {
            requestedDecors.Enqueue(decorId);
        }
    }
}
