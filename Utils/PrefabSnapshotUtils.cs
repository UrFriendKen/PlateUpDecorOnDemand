using Kitchen;
using Kitchen.Layouts;
using KitchenData;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using static UnityEngine.InputSystem.LowLevel.InputStateHistory;

namespace KitchenDecorOnDemand.Utils
{
    public static class PrefabSnapshotUtils
    {
        private static Dictionary<int, Texture2D> Snapshots = new Dictionary<int, Texture2D>();

        private static float NightFade;

        private static readonly int Fade = Shader.PropertyToID("_NightFade");

        private static int CacheMaxSize = 20;

        private static Dictionary<int, Texture2D> _CachedImages = new Dictionary<int, Texture2D>();

        private static void CacheShaderValues()
        {
            NightFade = Shader.GetGlobalFloat(Fade);
            Shader.SetGlobalFloat(Fade, 0f);
        }

        private static void ResetShaderValues()
        {
            Shader.SetGlobalFloat(Fade, NightFade);
        }

        public static void ClearCache()
        {
            Snapshots.Clear();
        }

        public static Texture2D GetDecorSnapshot(int decorID)
        {
            int instanceID = decorID;
            if (!GameData.Main.TryGet(decorID, out Decor decor, warn_if_fail: true))
                return null;

            if (Snapshots == null)
            {
                Snapshots = new Dictionary<int, Texture2D>();
            }

            if (!Snapshots.ContainsKey(instanceID) || Snapshots[instanceID] == null)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                MeshRenderer meshRenderer = cube.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    CacheShaderValues();
                    meshRenderer.materials = new Material[] { decor.Material };
                    Quaternion rotation = Quaternion.LookRotation(new Vector3(0f, 0f, 1f), Vector3.up);
                    SnapshotTexture snapshotTexture;
                    switch (decor.Type)
                    {
                        case LayoutMaterialType.Wallpaper:
                            snapshotTexture = SnapshotUtils.RenderPrefabToTextureForward(128, 128, cube, rotation, 0.5f, 0.5f);
                            break;
                        case LayoutMaterialType.Floor:
                        default:
                            snapshotTexture = SnapshotUtils.RenderPrefabToTexture(128, 128, cube, rotation, 0.5f, 0.5f);
                            break;
                    }
                    Snapshots[instanceID] = snapshotTexture.Snapshot;
                    ResetShaderValues();
                }
                GameObject.Destroy(cube);
            }
            return Snapshots[instanceID];
        }

        public static Texture2D GetApplianceSnapshot(int applianceID)
        {
            int instanceID = applianceID;
            if (!GameData.Main.TryGet(applianceID, out Appliance appliance, warn_if_fail: true) || appliance.Prefab == null)
                return null;
            return GetApplianceSnapshot(appliance.Prefab);
        }

        public static Texture2D GetSnapshot(GameObject prefab)
        {
            int instanceID = prefab.GetInstanceID();
            if (Snapshots == null)
            {
                Snapshots = new Dictionary<int, Texture2D>();
            }

            if (!Snapshots.ContainsKey(instanceID) || Snapshots[instanceID] == null)
            {
                CacheShaderValues();
                Quaternion rotation = Quaternion.LookRotation(new Vector3(0f, 0f, 1f), Vector3.up);
                SnapshotTexture snapshotTexture = SnapshotUtils.RenderPrefabToTexture(128, 128, prefab, rotation, 0.5f, 0.5f);
                ResetShaderValues();
                Snapshots[instanceID] = snapshotTexture.Snapshot;
            }

            return Snapshots[instanceID];
        }

        public static Texture2D GetCardSnapshot(UnlockCardElement element, ICard card, int width = 512, int height = 512)
        {
            int num = (card as UnlockCard)?.ID ?? (card as Dish)?.ID ?? 0;
            if (Snapshots == null)
            {
                Snapshots = new Dictionary<int, Texture2D>();
            }

            if (num != 0 || !Snapshots.ContainsKey(num) || Snapshots[num] == null)
            {
                CacheShaderValues();
                element.SetUnlock(card);
                SnapshotTexture snapshotTexture = Snapshot.RenderToTexture(width, height, element.gameObject, 1f, 1f, -10f, 10f, element.transform.localPosition);
                ResetShaderValues();
                Snapshots[num] = snapshotTexture.Snapshot;
            }

            return Snapshots[num];
        }

        public static Texture2D GetItemSnapshot(GameObject prefab)
        {
            int instanceID = prefab.GetInstanceID();
            if (Snapshots == null)
            {
                Snapshots = new Dictionary<int, Texture2D>();
            }

            if (!Snapshots.ContainsKey(instanceID) || Snapshots[instanceID] == null)
            {
                CacheShaderValues();
                Quaternion rotation = Quaternion.LookRotation(new Vector3(1f, -1f, 1f), new Vector3(0f, 1f, 1f));
                SnapshotTexture snapshotTexture = Snapshot.RenderPrefabToTexture(512, 512, prefab, rotation, 0.5f, 0.5f);
                ResetShaderValues();
                Snapshots[instanceID] = snapshotTexture.Snapshot;
            }

            return Snapshots[instanceID];
        }

        public static Texture2D GetApplianceSnapshot(GameObject prefab)
        {
            int instanceID = prefab.GetInstanceID();
            if (Snapshots == null)
            {
                Snapshots = new Dictionary<int, Texture2D>();
            }

            if (!Snapshots.ContainsKey(instanceID) || Snapshots[instanceID] == null)
            {
                CacheShaderValues();
                Quaternion rotation = Quaternion.LookRotation(new Vector3(1f, -1f, 1f), new Vector3(0f, 1f, 1f));
                SnapshotTexture snapshotTexture = Snapshot.RenderPrefabToTexture(512, 512, prefab, rotation, 0.5f, 0.5f, -10f, 10f, 0.5f, -0.25f * new Vector3(0f, 1f, 1f));
                ResetShaderValues();
                Snapshots[instanceID] = snapshotTexture.Snapshot;
            }

            return Snapshots[instanceID];
        }

        public static Texture2D GetFoodSnapshot(GameObject prefab, ItemView.ViewData data)
        {
            GameObject gameObject = Object.Instantiate(prefab);
            ItemView component = gameObject.GetComponent<ItemView>();
            component.UpdateData(data);
            CacheShaderValues();
            Quaternion rotation = Quaternion.LookRotation(new Vector3(0f, -0.5f, 0.5f), Vector3.up);
            SnapshotTexture snapshotTexture = Snapshot.RenderPrefabToTexture(128, 128, gameObject, rotation, 0.5f, 0.5f);
            ResetShaderValues();
            Object.Destroy(gameObject);
            return snapshotTexture.Snapshot;
        }

        public static Texture2D GetLayoutSnapshot(SiteView prefab, LayoutBlueprint blueprint)
        {
            int iD = blueprint.ID;
            if (_CachedImages.TryGetValue(iD, out var value) && value != null)
            {
                return value;
            }

            SiteView siteView = Object.Instantiate(prefab);
            siteView.UpdateData(new SiteView.ViewData
            {
                Floorplan = blueprint
            });
            CacheShaderValues();
            Quaternion rotation = Quaternion.LookRotation(new Vector3(1f, 0f, 0f), Vector3.up);
            SnapshotTexture snapshotTexture = Snapshot.RenderPrefabToTexture(128, 128, siteView.gameObject, rotation, 1.5f, 1.5f);
            ResetShaderValues();
            Object.Destroy(siteView.GameObject);
            if (_CachedImages.Count > CacheMaxSize)
            {
                _CachedImages.Clear();
            }

            _CachedImages[iD] = snapshotTexture.Snapshot;
            return snapshotTexture.Snapshot;
        }
    }
}
