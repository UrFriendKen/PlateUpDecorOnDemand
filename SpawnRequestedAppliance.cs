using Kitchen;
using KitchenData;
using KitchenMods;
using System;
using UnityEngine;

namespace KitchenDecorOnDemand
{
    public enum SpawnApplianceMode
    {
        Blueprint,
        Parcel
    }

    public class SpawnRequestedAppliance : SpawnHandlerSystemBase, IModSystem
    {

        protected override Type GDOType => typeof(Appliance);

        protected override bool UseFallbackTile => true;

        protected override void Spawn(GameDataObject gdo, Vector3 position, SpawnApplianceMode spawnApplianceMode)
        {
            if (gdo is Appliance appliance)
            {
                switch (spawnApplianceMode)
                {
                    case SpawnApplianceMode.Parcel:
                        AddApplianceParcel(appliance, position);
                        break;
                    case SpawnApplianceMode.Blueprint:
                    default:
                        AddApplianceBlueprint(appliance, position);
                        break;
                }
            }
        }

        protected void AddApplianceBlueprint(Appliance appliance, Vector3 position)
        {
            PostHelpers.CreateOpenedLetter(new EntityContext(EntityManager), position, appliance.ID, 0f);
        }

        protected void AddApplianceParcel(Appliance appliance, Vector3 position)
        {
            PostHelpers.CreateApplianceParcel(EntityManager, position, appliance.ID);
        }
    }
}
