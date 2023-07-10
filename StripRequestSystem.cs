using Kitchen;
using KitchenData;
using KitchenMods;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace KitchenDecorOnDemand
{
    public class StripRequest
    {
        public bool ReturnDecor;
    }
    public class StripRequestSystem : GenericSystemBase, IModSystem
    {
        static Queue<StripRequest> requests = new Queue<StripRequest>();
        EntityQuery DecorEvents;
        protected override void Initialise()
        {
            base.Initialise();
            DecorEvents = GetEntityQuery(typeof(CChangeDecorEvent));
        }
        protected override void OnUpdate()
        {
            using NativeArray<Entity> entities = DecorEvents.ToEntityArray(Allocator.Temp);
            using NativeArray<CChangeDecorEvent> decorEvents = DecorEvents.ToComponentDataArray<CChangeDecorEvent>(Allocator.Temp);
            if (requests.Count > 0)
            {
                StripRequest request = requests.Dequeue();
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity entity = entities[i];
                    CChangeDecorEvent decorEvent = decorEvents[i];
                    if (request.ReturnDecor)
                    {
                        SpawnRequestSystem.Request<Decor>(decorEvent.DecorID, SpawnPositionType.Door);
                    }
                    decorEvent.DecorID = 0;
                    Set(entity, decorEvent);
                }
                return;
            }
            for (int i = entities.Length - 1; i > -1; i--)
            {
                Entity entity = entities[i];
                CChangeDecorEvent decorEvent = decorEvents[i];
                if (decorEvent.DecorID == 0)
                {
                    EntityManager.DestroyEntity(entity);
                }
            }
        }
        public static void Request(bool returnDecor = true)
        {
            if (GameInfo.CurrentScene == SceneType.Kitchen)
            {
                requests.Enqueue(new StripRequest()
                {
                    ReturnDecor = returnDecor
                });
            }
        }
    }
}
