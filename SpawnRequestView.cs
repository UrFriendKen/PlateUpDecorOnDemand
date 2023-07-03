using Controllers;
using Kitchen;
using KitchenData;
using KitchenMods;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace KitchenDecorOnDemand
{
    [StructLayout(LayoutKind.Sequential, Size = 1)]
    public struct SSpawnRequestView : IComponentData, IModComponent { }

    public class SpawnRequestView : ResponsiveObjectView<SpawnRequestView.ViewData, SpawnRequestView.ResponseData>
    {
        public class UpdateView : ResponsiveViewSystemBase<ViewData, ResponseData>, IModSystem
        {
            EntityQuery Views;

            protected override void Initialise()
            {
                base.Initialise();
                Views = GetEntityQuery(typeof(CLinkedView), typeof(SSpawnRequestView));
            }

            protected override void OnUpdate()
            {
                EnsureView();
                using NativeArray<CLinkedView> linkedViews = Views.ToComponentDataArray<CLinkedView>(Allocator.Temp);
                foreach (CLinkedView view in linkedViews)
                {
                    ApplyUpdates(view.Identifier, PerformUpdateWithResponse, only_final_update: true);
                }
            }

            private void PerformUpdateWithResponse(ResponseData data)
            {
                if (!Main.PrefManager.Get<bool>(Main.HOST_ONLY_ID) || data.InputIdentifier == InputSourceIdentifier.Identifier)
                {
                    Action<int, SpawnPositionType, int, SpawnApplianceMode> spawnMethod = null;
                    switch (data.SpawnType)
                    {
                        case SpawnType.Appliance:
                            spawnMethod = SpawnRequestSystem.Request<Appliance>;
                            break;
                        case SpawnType.Decor:
                            spawnMethod = SpawnRequestSystem.Request<Decor>;
                            break;
                    }
                    if (spawnMethod == null)
                        return;
                    spawnMethod(data.GdoId, data.PositionType, data.InputIdentifier, data.SpawnApplianceMode);
                }
            }

            private void EnsureView()
            {
                if (!TryGetSingletonEntity<SSpawnRequestView>(out Entity entity))
                {
                    entity = EntityManager.CreateEntity();
                    Set<SSpawnRequestView>(entity);
                    Set(entity, new CPersistThroughSceneChanges());
                    Set(entity, new CDoNotPersist());
                    Set(entity, new CRequiresView()
                    {
                        ViewMode = ViewMode.Screen,
                        Type = Main.SpawnRequestViewType,
                        PhysicsDriven = false
                    });
                }
            }
        }

        [MessagePackObject(false)]
        public class ViewData : IViewData, IViewResponseData, IViewData.ICheckForChanges<ViewData>
        {
            public IUpdatableObject GetRelevantSubview(IObjectView view)
            {
                return view.GetSubView<SpawnRequestView>();
            }

            public bool IsChangedFrom(ViewData check)
            {
                return false;
            }
        }

        [MessagePackObject(false)]
        public class ResponseData : IResponseData, IViewResponseData
        {
            [Key(0)] public int InputIdentifier;
            [Key(1)] public int GdoId;
            [Key(2)] public SpawnType SpawnType;
            [Key(3)] public SpawnPositionType PositionType;
            [Key(4)] public SpawnApplianceMode SpawnApplianceMode;
        }

        private Queue<GameDataObject> _requestedGDO = new Queue<GameDataObject>();
        private static Dictionary<Type, SpawnType> _typeMap = new Dictionary<Type, SpawnType>()
        {
            { typeof(Decor), SpawnType.Decor },
            { typeof(Appliance), SpawnType.Appliance }
        };
        protected override void UpdateData(ViewData data)
        {
        }

        public override bool HasStateUpdate(out IResponseData state)
        {
            ActiveSpawnRequestView activeViewMarker = gameObject.GetComponent<ActiveSpawnRequestView>();
            if (activeViewMarker == null)
            {
                activeViewMarker = gameObject.AddComponent<ActiveSpawnRequestView>();
            }
            activeViewMarker.LinkedView = this;

            if (_requestedGDO.Count > 0)
            {
                if (!Enum.TryParse(Main.PrefManager.Get<string>(Main.SPAWN_AT_ID), out SpawnPositionType positionType))
                {
                    positionType = default;
                }
                if (!Enum.TryParse(Main.PrefManager.Get<string>(Main.APPLIANCE_SPAWN_AS_ID), out SpawnApplianceMode spawnApplianceMode))
                {
                    spawnApplianceMode = default;
                }
                GameDataObject gdo = _requestedGDO.Dequeue();

                if (_typeMap.TryGetValue(gdo.GetType(), out SpawnType spawnType))
                {
                    state = new ResponseData()
                    {
                        InputIdentifier = InputSourceIdentifier.Identifier,
                        GdoId = gdo.ID,
                        SpawnType = spawnType,
                        PositionType = positionType,
                        SpawnApplianceMode = spawnApplianceMode
                    };
                    return true;
                }
            }
            state = default;
            return false;
        }

        public void Request<T>(int gdoID) where T : GameDataObject, new()
        {
            if (GameData.Main.TryGet(gdoID, out T gdo, warn_if_fail: true))
            {
                _requestedGDO.Enqueue(gdo);
            }
        }
    }

    public class ActiveSpawnRequestView : MonoBehaviour
    {
        public SpawnRequestView LinkedView;
    }
}
