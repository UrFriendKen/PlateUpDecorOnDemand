using Kitchen;
using KitchenData;
using KitchenMods;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace KitchenDecorOnDemand
{
    public enum SpawnType
    {
        Decor,
        Appliance
    }

    public enum SpawnPositionType
    {
        Door,
        Player
    }

    public abstract class SpawnHandlerSystemBase : GenericSystemBase
    {
        protected abstract Type GDOType { get; }
        protected virtual bool UseFallbackTile => false;
        EntityQuery Players;
        protected override void Initialise()
        {
            base.Initialise();
            Players = GetEntityQuery(typeof(CPlayer), typeof(CPosition));
        }

        protected override void OnUpdate()
        {
            if (GDOType != null && !SpawnRequestSystem.IsHandled && GDOType == SpawnRequestSystem.Current?.GDO?.GetType())
            {
                Vector3 position = UseFallbackTile ? GetFallbackTile() : GetFrontDoor(get_external_tile: true);
                using NativeArray<CPlayer> players = Players.ToComponentDataArray<CPlayer>(Allocator.Temp);
                using NativeArray<CPosition> playerPositions = Players.ToComponentDataArray<CPosition>(Allocator.Temp);
                switch (SpawnRequestSystem.Current.PositionType)
                {
                    case SpawnPositionType.Player:
                        Main.LogInfo("Using Player");
                        bool positionSet = false;
                        for (int i = 0; i < players.Length; i++)
                        {
                            CPlayer player = players[i];
                            CPosition playerPosition = playerPositions[i];
                            bool match = player.InputSource == SpawnRequestSystem.Current.InputIdentifier;
                            if (!positionSet || match)
                            {
                                positionSet = true;
                                position = playerPosition;
                            }
                            if (match)
                                break;
                        }
                        break;
                    case SpawnPositionType.Door:
                    default:
                        break;
                }
                SpawnRequestSystem.RequestHandled();
                Spawn(SpawnRequestSystem.Current.GDO, position, SpawnRequestSystem.Current.SpawnApplianceMode);
            }
        }
        protected abstract void Spawn(GameDataObject gameDataObject, Vector3 position, SpawnApplianceMode spawnApplianceMode);
    }

    public class SpawnRequest
    {
        public GameDataObject GDO;
        public SpawnPositionType PositionType;
        public int InputIdentifier;
        public SpawnApplianceMode SpawnApplianceMode;
    }

    public class SpawnRequestSystem : GenericSystemBase, IModSystem
    {
        static Queue<SpawnRequest> requests = new Queue<SpawnRequest>();
        public static SpawnRequest Current { get; private set; } = null;
        public static bool IsHandled { get; private set; } = false;
        protected override void OnUpdate()
        {
            if (requests.Count > 0)
            {
                IsHandled = false;
                Current = requests.Dequeue();
                
                return;
            }
            Current = null;
            IsHandled = true;
        }
        public static void Request<T>(int gdoID, SpawnPositionType positionType, int inputIdentifier = 0, SpawnApplianceMode spawnApplianceMode = default) where T : GameDataObject, new()
        {
            if (gdoID != 0 && GameInfo.CurrentScene == SceneType.Kitchen && GameData.Main.TryGet(gdoID, out T gdo, warn_if_fail: true))
            {
                requests.Enqueue(new SpawnRequest()
                {
                    GDO = gdo,
                    PositionType = positionType,
                    InputIdentifier = inputIdentifier,
                    SpawnApplianceMode = spawnApplianceMode
                });
            }
        }
        public static void RequestHandled()
        {
            IsHandled = true;
        }
    }
}
