using UnityEngine;

namespace NeonHorde
{
    /// <summary>Smoothed follow + arena clamp + screen shake.</summary>
    public sealed class CameraFollow : MonoBehaviour
    {
        public Transform target;
        [SerializeField] float smoothing = Balance.CameraFollowSmoothing;

        Camera _cam;

        void Awake()
        {
            _cam = GetComponent<Camera>();
            if (_cam != null) _cam.orthographicSize = Balance.CameraOrthoSize;
        }

        void LateUpdate()
        {
            if (target == null) return;

            float t = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
            Vector3 p = transform.position;
            float baseX = Mathf.Lerp(p.x, target.position.x, t);
            float baseY = Mathf.Lerp(p.y, target.position.y, t);

            var rm = RunManager.Instance;
            if (rm != null && rm.State != null && rm.State.map != null && _cam != null)
            {
                float halfH = _cam.orthographicSize;
                float halfW = halfH * _cam.aspect;
                var map = rm.State.map;
                baseX = Mathf.Clamp(baseX, map.arenaMin.x + halfW, map.arenaMax.x - halfW);
                baseY = Mathf.Clamp(baseY, map.arenaMin.y + halfH, map.arenaMax.y - halfH);
                if (map.arenaMax.x - map.arenaMin.x < halfW * 2f) baseX = (map.arenaMin.x + map.arenaMax.x) * 0.5f;
                if (map.arenaMax.y - map.arenaMin.y < halfH * 2f) baseY = (map.arenaMin.y + map.arenaMax.y) * 0.5f;
            }

            Vector2 shake = ScreenShake.Tick(Time.deltaTime);
            transform.position = new Vector3(baseX + shake.x, baseY + shake.y, p.z);
        }
    }
}
