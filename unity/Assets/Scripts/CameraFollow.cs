using UnityEngine;

namespace LostRealms
{
    /// Orthographic side-scroller camera with horizontal look-ahead, vertical damping and shake.
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        private Camera _cam;
        private float _baseY;
        private float _shakeTime, _shakeStrength;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            _cam.orthographic = true;
            _cam.orthographicSize = 3.2f;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color32(0x16, 0x1E, 0x2C, 255);
            _baseY = transform.position.y;
        }

        private void LateUpdate()
        {
            if (target == null && PlayerController.Instance != null) target = PlayerController.Instance.transform;
            if (target == null) return;

            float dt = Time.deltaTime;
            float lookahead = Mathf.Sign(PlayerController.Instance.Velocity.x) * Mathf.Min(Mathf.Abs(PlayerController.Instance.Velocity.x) * 0.25f, 0.9f);
            float targetX = target.position.x + lookahead;
            float halfW = _cam.orthographicSize * _cam.aspect;
            targetX = Mathf.Clamp(targetX, halfW, GameConfig.LevelEndX - halfW + 0.5f);

            float targetY = target.position.y + 1.2f;
            targetY = Mathf.Clamp(Mathf.Lerp(_baseY, targetY, 0.6f), _baseY - 1.6f, _baseY + 6f);

            var pos = transform.position;
            pos.x = Mathf.Lerp(pos.x, targetX, dt * 6f);
            pos.y = Mathf.Lerp(pos.y, targetY, dt * 3.5f);
            transform.position = pos;

            if (_shakeTime > 0f)
            {
                _shakeTime -= dt;
                var shake = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * _shakeStrength * 0.05f;
                transform.position += shake;
            }
        }

        public void Shake(float strength, float time)
        {
            _shakeStrength = Mathf.Max(_shakeStrength, strength);
            _shakeTime = Mathf.Max(_shakeTime, time);
        }
    }
}
