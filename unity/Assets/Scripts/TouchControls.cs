using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LostRealms
{
    /// Single input hub: keyboard for desktop testing, on-screen touch controls for the phone.
    /// PlayerController reads TouchControls.State each frame.
    public class TouchControls : MonoBehaviour
    {
        public struct InputState
        {
            public float moveX;          // -1..1
            public bool jumpPressed;     // consumed on read
            public bool jumpHeld;
            public bool attackPressed;   // consumed on read
        }

        public static InputState State
        {
            get
            {
                var s = _state;
                _state.jumpPressed = false;
                _state.attackPressed = false;
                return s;
            }
        }

        private static InputState _state;
        private static int _movePointerId = int.MinValue;
        private Vector2 _moveOrigin;
        private RectTransform _stickBase, _stickKnob;

        private void Awake() { BuildUi(); }

        private void Update()
        {
            // ---- keyboard (editor / desktop)
            float kb = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) kb -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) kb += 1f;
            if (kb != 0f) _state.moveX = kb;
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W)) _state.jumpPressed = true;
            if (Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.K)) _state.attackPressed = true;
            _state.jumpHeld = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W) || _touchJumpHeld;

            // ---- touch look-left / look-right via screen edge swipes is unnecessary; stick only.
            if (kb == 0f && _movePointerId == int.MinValue)
            {
                // decay toward neutral so releasing the stick stops the hero
                _state.moveX = Mathf.MoveTowards(_state.moveX, 0f, Time.deltaTime * 8f);
            }
        }

        private bool _touchJumpHeld;

        // ---------------------------------------------------------------- UI

        private void BuildUi()
        {
            var canvasGo = new GameObject("TouchCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                es.transform.SetParent(canvasGo.transform, false);
            }

            // ---- left: virtual stick capture zone (invisible) + visible stick
            var zoneGo = new GameObject("MoveZone", typeof(Image), typeof(MoveZone));
            zoneGo.transform.SetParent(canvasGo.transform, false);
            var zoneImg = zoneGo.GetComponent<Image>();
            zoneImg.color = new Color(1f, 1f, 1f, 0.005f);   // raycast target, invisible
            var zoneRect = zoneGo.GetComponent<RectTransform>();
            zoneRect.anchorMin = new Vector2(0f, 0f);
            zoneRect.anchorMax = new Vector2(0.45f, 0.5f);
            zoneRect.offsetMin = Vector2.zero;
            zoneRect.offsetMax = Vector2.zero;
            zoneGo.GetComponent<MoveZone>().Owner = this;

            _stickBase = MakeCircle(canvasGo.transform, "StickBase", new Vector2(0.13f, 0.16f), 150f, new Color(1f, 1f, 1f, 0.10f));
            _stickKnob = MakeCircle(canvasGo.transform, "StickKnob", new Vector2(0.13f, 0.16f), 66f, new Color(1f, 1f, 1f, 0.35f));
            _stickBase.gameObject.SetActive(false);
            _stickKnob.gameObject.SetActive(false);

            // ---- right: JUMP + ATK buttons
            var jump = MakeButton(canvasGo.transform, "BtnJump", new Vector2(0.885f, 0.16f), 190f, "JUMP");
            var atk = MakeButton(canvasGo.transform, "BtnAtk", new Vector2(0.755f, 0.26f), 160f, "ATK");
            jump.GetComponent<TouchButton>().Owner = this;
            atk.GetComponent<TouchButton>().Owner = this;
            jump.GetComponent<TouchButton>().IsJump = true;
        }

        private static RectTransform MakeCircle(Transform parent, string name, Vector2 anchor, float size, Color color)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            var sprite = CircleSprite();
            if (sprite != null) img.sprite = sprite;
            img.color = color;
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = anchor;
            r.sizeDelta = Vector2.one * size;
            r.anchoredPosition = Vector2.zero;
            return r;
        }

        private static Sprite CircleSprite()
        {
            // built-in UISprite is a rounded rect — good enough for pads
            return Resources.GetBuiltinResource<Sprite>("UISprite.psd");
        }

        private static RectTransform MakeButton(Transform parent, string name, Vector2 anchor, float size, string label)
        {
            var go = new GameObject(name, typeof(Image), typeof(TouchButton));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            var sprite = CircleSprite();
            if (sprite != null) { img.sprite = sprite; img.type = Image.Type.Sliced; }
            img.color = new Color(1f, 1f, 1f, 0.16f);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = anchor;
            r.sizeDelta = new Vector2(size, size * 0.62f);
            r.anchoredPosition = Vector2.zero;

            var txtGo = new GameObject("Label", typeof(Text));
            txtGo.transform.SetParent(go.transform, false);
            var txt = txtGo.GetComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text = label;
            txt.fontSize = 34;
            txt.color = new Color(1f, 1f, 1f, 0.85f);
            txt.alignment = TextAnchor.MiddleCenter;
            txt.raycastTarget = false;
            var tr = txtGo.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
            tr.offsetMin = tr.offsetMax = Vector2.zero;
            return r;
        }

        // ---------------------------------------------------------------- stick handling

        internal void MoveBegin(Vector2 screenPos, int pointerId)
        {
            _movePointerId = pointerId;
            _moveOrigin = screenPos;
            _stickBase.gameObject.SetActive(true);
            _stickKnob.gameObject.SetActive(true);
            PositionStick(screenPos);
        }

        internal void MoveDrag(Vector2 screenPos)
        {
            if (_movePointerId == int.MinValue) return;
            Vector2 delta = screenPos - _moveOrigin;
            float maxRadius = 130f;
            Vector2 clamped = Vector2.ClampMagnitude(delta, maxRadius);
            _state.moveX = Mathf.Clamp(delta.x / (maxRadius * 0.45f), -1f, 1f);
            _stickKnob.anchoredPosition = _stickBase.anchoredPosition + clamped;
        }

        internal void MoveEnd()
        {
            _movePointerId = int.MinValue;
            _state.moveX = 0f;
            _stickBase.gameObject.SetActive(false);
            _stickKnob.gameObject.SetActive(false);
        }

        internal void ButtonPress(bool isJump)
        {
            if (isJump) { _state.jumpPressed = true; _touchJumpHeld = true; }
            else _state.attackPressed = true;
        }

        internal void ButtonRelease(bool isJump)
        {
            if (isJump) _touchJumpHeld = false;
        }

        private void PositionStick(Vector2 screenPos)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _stickBase.parent.GetComponent<RectTransform>(), screenPos, null, out var local);
            _stickBase.anchoredPosition = local;
            _stickKnob.anchoredPosition = local;
        }

        // ---------------------------------------------------------------- pointer handlers

        private class MoveZone : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
        {
            public TouchControls Owner;
            public void OnPointerDown(PointerEventData d) => Owner?.MoveBegin(d.position, d.pointerId);
            public void OnDrag(PointerEventData d) => Owner?.MoveDrag(d.position);
            public void OnPointerUp(PointerEventData d) => Owner?.MoveEnd();
        }

        private class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
        {
            public TouchControls Owner;
            public bool IsJump;
            public void OnPointerDown(PointerEventData d) => Owner?.ButtonPress(IsJump);
            public void OnPointerUp(PointerEventData d) => Owner?.ButtonRelease(IsJump);
        }
    }
}
