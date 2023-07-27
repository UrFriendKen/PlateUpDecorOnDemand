using Kitchen;
using UnityEngine;

namespace KitchenDecorOnDemand.Utils
{
    public static class SnapshotUtils
    {
        private static int _SnapshotLayer;

        private static LayerMask _SnapshotLayerMask;

        private static Camera _SnapshotCamera;

        [HideInInspector]
        public static int SnapshotLayer
        {
            get
            {
                if (_SnapshotLayer == 0)
                {
                    _SnapshotLayer = LayerMask.NameToLayer("Snapshot Camera");
                }

                return _SnapshotLayer;
            }
        }

        [HideInInspector]
        private static int SnapshotLayerMask
        {
            get
            {
                if ((int)_SnapshotLayerMask == 0)
                {
                    _SnapshotLayerMask = LayerMask.GetMask("Snapshot Camera");
                }

                return _SnapshotLayerMask;
            }
        }

        [HideInInspector]
        private static Camera SnapshotCamera
        {
            get
            {
                if (_SnapshotCamera == null)
                {
                    GameObject gameObject = GameObject.Find("Snapshot Camera");
                    if ((bool)gameObject)
                    {
                        _SnapshotCamera = gameObject.GetComponent<Camera>();
                    }
                    else
                    {
                        GameObject gameObject2 = new GameObject("Snapshot Camera", typeof(Camera));
                        _SnapshotCamera = gameObject2.GetComponent<Camera>();
                        _SnapshotCamera.cullingMask = SnapshotLayerMask;
                        _SnapshotCamera.enabled = false;
                        _SnapshotCamera.orthographic = true;
                        _SnapshotCamera.clearFlags = CameraClearFlags.Depth;
                        _SnapshotCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                    }
                }

                return _SnapshotCamera;
            }
        }

        public enum RelativePosition
        {
            Above,
            Forward
        }

        public static SnapshotTexture RenderPrefabToTextureForward(int pixel_width, int pixel_height, GameObject prefab, Quaternion rotation, float target_width, float target_height, float near = -10f, float far = 10f, float scale = 1f, Vector3 position = default(Vector3))
        {
            GameObject gameObject = Object.Instantiate(prefab);
            gameObject.transform.rotation = rotation;
            gameObject.transform.localScale = Vector3.one * scale;
            SnapshotTexture result = RenderToTexture(pixel_width, pixel_height, gameObject, target_width, target_height, near, far, position, RelativePosition.Forward);
            Object.Destroy(gameObject);
            return result;
        }

        public static SnapshotTexture RenderPrefabToTexture(int pixel_width, int pixel_height, GameObject prefab, Quaternion rotation, float target_width, float target_height, float near = -10f, float far = 10f, float scale = 1f, Vector3 position = default(Vector3))
        {
            GameObject gameObject = Object.Instantiate(prefab);
            gameObject.transform.rotation = rotation;
            gameObject.transform.localScale = Vector3.one * scale;
            SnapshotTexture result = RenderToTexture(pixel_width, pixel_height, gameObject, target_width, target_height, near, far, position, RelativePosition.Above);
            Object.Destroy(gameObject);
            return result;
        }

        public static SnapshotTexture RenderToTexture(int pixel_width, int pixel_height, GameObject target, float target_width, float target_height, float near = -10f, float far = 10f, Vector3 offset = default(Vector3), RelativePosition fromDirection = RelativePosition.Above)
        {
            Camera camera;
            switch (fromDirection)
            {
                case RelativePosition.Forward:
                    camera = SetupSnapshotForward(pixel_width, pixel_height, target.transform.position - offset, target_width, target_height, near, far);
                    break;
                case RelativePosition.Above:
                default:
                    camera = SetupSnapshotAbove(pixel_width, pixel_height, target.transform.position - offset, target_width, target_height, near, far);
                    break;
            }
            RenderTexture snapshot = TakeSnapshot(camera, target, pixel_width, pixel_height);
            Texture2D texture = SaveSnapshot(snapshot);
            return new SnapshotTexture(texture, target_width, target_height);
        }

        private static RenderTexture TakeSnapshot(Camera camera, GameObject target, int pixel_width, int pixel_height)
        {
            RenderTexture temporary = RenderTexture.GetTemporary(pixel_width, pixel_height, 0, RenderTextureFormat.ARGB32);
            RenderTexture targetTexture = camera.targetTexture;
            int layer = target.layer;
            camera.targetTexture = temporary;
            SetLayer(target, SnapshotLayer);
            camera.Render();
            camera.targetTexture = targetTexture;
            SetLayer(target, layer);
            return temporary;
        }

        private static void SetLayer(GameObject obj, int layer)
        {
            Transform[] componentsInChildren = obj.GetComponentsInChildren<Transform>(includeInactive: true);
            foreach (Transform transform in componentsInChildren)
            {
                transform.gameObject.layer = layer;
            }
        }

        private static Texture2D SaveSnapshot(RenderTexture snapshot)
        {
            int width = snapshot.width;
            int height = snapshot.height;
            RenderTexture active = RenderTexture.active;
            RenderTexture.active = snapshot;
            Texture2D texture2D = new Texture2D(width, height);
            texture2D.anisoLevel = 2;
            texture2D.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            texture2D.Apply(updateMipmaps: false);
            RenderTexture.ReleaseTemporary(snapshot);
            RenderTexture.active = active;
            return texture2D;
        }

        private static Camera SetupSnapshotAbove(int pixel_width, int pixel_height, Vector3 origin, float target_width, float target_height, float near = -10f, float far = 10f)
        {
            return SetupSnapshotCamera(pixel_width, pixel_height, origin, -Vector3.up, Vector3.forward, target_width, target_height, near, far);
        }

        private static Camera SetupSnapshotForward(int pixel_width, int pixel_height, Vector3 origin, float target_width, float target_height, float near = -10f, float far = 10f)
        {
            return SetupSnapshotCamera(pixel_width, pixel_height, origin, Vector3.forward, Vector3.up, target_width, target_height, near, far);
        }

        private static Camera SetupSnapshotCamera(int pixel_width, int pixel_height, Vector3 origin, Vector3 cam_forward, Vector3 cam_up, float target_width, float target_height, float near = -10f, float far = 10f)
        {
            Camera snapshotCamera = SnapshotCamera;
            snapshotCamera.transform.position = origin - cam_forward;
            snapshotCamera.transform.rotation = Quaternion.LookRotation(cam_forward, cam_up);
            snapshotCamera.aspect = (float)pixel_width / (float)pixel_height;
            snapshotCamera.orthographicSize = target_height;
            snapshotCamera.nearClipPlane = near;
            snapshotCamera.farClipPlane = far;
            return snapshotCamera;
        }

        public static void Blur(RenderTexture source, Material blurMaterial)
        {
            RenderTexture temporary = RenderTexture.GetTemporary(source.width, source.height);
            Graphics.Blit(source, temporary, blurMaterial, 0);
            Graphics.Blit(temporary, source, blurMaterial, 1);
            RenderTexture.ReleaseTemporary(temporary);
        }
    }
}
