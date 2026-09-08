using UnityEngine;
using UnityEngine.UI;

namespace LostRealms
{
    /// Game state + HUD: hearts, coins, gems, stage banner, death/respawn flow, level-complete screen.
    /// UI is built at runtime so the scene stays a minimal bootstrap.
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public int Health => _health;
        private int _health = GameConfig.MaxHealth, _coins, _gems, _kills;
        private Text _hearts, _counters, _banner, _overlayTitle, _overlaySub;
        private GameObject _overlay;
        private CameraFollow _camera;

        private void Awake()
        {
            Instance = this;
            BuildHud();
        }

        private void Start()
        {
            _camera = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        }

        // ---------------------------------------------------------------- events

        internal void OnLevelLoaded(LevelSchema level)
        {
            if (_banner != null)
            {
                _banner.text = $"STAGE {level.id} — {level.stageName.ToUpper()}";
                Invoke(nameof(HideBanner), 3.5f);
            }
        }

        private void HideBanner() { if (_banner != null) _banner.text = ""; }

        internal void OnPickup(bool gem)
        {
            if (gem) _gems++; else _coins++;
            UpdateHud();
        }

        internal void OnEnemyKilled()
        {
            _kills++;
        }

        internal void OnPlayerHurt(int amount)
        {
            _health -= amount;
            UpdateHud();
            if (_camera != null) _camera.Shake(4.4f, 0.075f);
        }

        internal void OnPlayerDied()
        {
            if (_banner != null) _banner.text = "FALLEN...";
            if (_camera != null) _camera.Shake(6f, 0.2f);
        }

        internal void OnRespawn()
        {
            _health = Mathf.Max(1, GameConfig.MaxHealth - 1);
            UpdateHud();
            if (_banner != null) _banner.text = "";
        }

        internal void LevelComplete()
        {
            if (_overlay.activeSelf) return;
            _overlay.SetActive(true);
            _overlayTitle.text = "MOSSLIGHT TRAIL CLEAR";
            _overlaySub.text = $"Coins {_coins}   Gems {_gems}   Foes {_kills}\nUnity port v0.1 — placeholder Aster mesh";
            Time.timeScale = 0f;
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        // ---------------------------------------------------------------- HUD

        private void UpdateHud()
        {
            if (_hearts != null) _hearts.text = new string('\u2665', Mathf.Max(0, _health));
            if (_counters != null) _counters.text = $"COINS {_coins}   GEMS {_gems}";
        }

        private void BuildHud()
        {
            var canvasGo = new GameObject("HudCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _hearts = MakeText(canvasGo.transform, "Hearts", new Vector2(0f, 1f), new Vector2(24f, -20f), TextAnchor.UpperLeft, 44, new Color(1f, 0.42f, 0.46f));
            _counters = MakeText(canvasGo.transform, "Counters", new Vector2(1f, 1f), new Vector2(-24f, -20f), TextAnchor.UpperRight, 40, new Color(1f, 0.86f, 0.45f));
            _banner = MakeText(canvasGo.transform, "Banner", new Vector2(0.5f, 1f), new Vector2(0f, -16f), TextAnchor.UpperCenter, 46, new Color(0.74f, 0.95f, 0.63f));

            _overlay = new GameObject("CompleteOverlay", typeof(Image));
            _overlay.transform.SetParent(canvasGo.transform, false);
            var dim = _overlay.GetComponent<Image>();
            dim.color = new Color(0.04f, 0.06f, 0.1f, 0.86f);
            dim.raycastTarget = true;
            var or = _overlay.GetComponent<RectTransform>();
            or.anchorMin = Vector2.zero; or.anchorMax = Vector2.one;
            or.offsetMin = or.offsetMax = Vector2.zero;
            _overlay.SetActive(false);

            _overlayTitle = MakeText(_overlay.transform, "Title", new Vector2(0.5f, 0.62f), Vector2.zero, TextAnchor.MiddleCenter, 72, new Color(0.86f, 0.98f, 0.8f));
            _overlaySub = MakeText(_overlay.transform, "Sub", new Vector2(0.5f, 0.44f), Vector2.zero, TextAnchor.MiddleCenter, 40, Color.white);

            var btnGo = new GameObject("AgainBtn", typeof(Image), typeof(Button));
            btnGo.transform.SetParent(_overlay.transform, false);
            var btn = btnGo.GetComponent<Button>();
            btn.onClick.AddListener(RestartLevel);
            btn.targetGraphic = btnGo.GetComponent<Image>();
            btnGo.GetComponent<Image>().color = new Color(0.32f, 0.56f, 0.36f, 0.9f);
            var br = btnGo.GetComponent<RectTransform>();
            br.anchorMin = br.anchorMax = new Vector2(0.5f, 0.28f);
            br.sizeDelta = new Vector2(420f, 96f);
            var btnLabel = MakeText(btnGo.transform, "Label", new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter, 40, Color.white);
            btnLabel.text = "PLAY AGAIN";

            UpdateHud();
        }

        private static Text MakeText(Transform parent, string name, Vector2 anchor, Vector2 offset, TextAnchor align, int size, Color color)
        {
            var go = new GameObject(name, typeof(Text));
            go.transform.SetParent(parent, false);
            var txt = go.GetComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = size;
            txt.alignment = align;
            txt.color = color;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = anchor;
            r.anchoredPosition = offset;
            r.sizeDelta = new Vector2(800f, 60f);
            return txt;
        }
    }
}
