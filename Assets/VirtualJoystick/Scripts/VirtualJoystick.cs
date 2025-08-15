using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.Linq;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
#endif

namespace Terresquall
{

    [Serializable]
    [RequireComponent(typeof(Image), typeof(RectTransform))]
    public partial class VirtualJoystick : MonoBehaviour
    {

        [Tooltip("The unique ID for this joystick. Needs to be unique.")]
        public int ID;
        [Tooltip("The component that the user will drag around for joystick input.")]
        public Image controlStick;

        [Header("Debug")]
        [Tooltip("Prints to the console the control stick's direction within the joystick.")]
        public bool consolePrintAxis = false;

        public enum InputMode { oldInputManager, newInputSystem };
        public static InputMode inputMode;

        public static InputMode GetInputMode()
        {
#if ENABLE_INPUT_SYSTEM
#if ENABLE_LEGACY_INPUT_MANAGER
            return InputMode.oldInputManager;
#else
            EnhancedTouchSupport.Enable();
            return InputMode.newInputSystem;
#endif
#else
            return InputMode.oldInputManager;
#endif
        }

        [Header("Settings")]
        [Tooltip("Disables the joystick if not on a mobile platform.")]
        public bool onlyOnMobile = true;
        [Tooltip("Colour of the control stick while it is being dragged.")]
        public Color dragColor = new Color(0.9f, 0.9f, 0.9f, 1f);

        [Tooltip("How responsive the control stick is to dragging.")]
        public float sensitivity = 2f;
        [Tooltip("How far you can drag the control stick away from the joystick's centre.")]
        [Range(0, 2)] public float radius = 0.7f;
        [Tooltip("How far must you drag the control stick from the joystick's centre before it registers input")]
        [Range(0, 1)] public float deadzone = 0.3f;

        [Tooltip("Joystick automatically snaps to the edge when outside the deadzone.")]
        public bool edgeSnap;
        [Tooltip("Number of directions of the joystick. \nKeep at 0 for a free joystick. \nWorks best with multiples of 4")]
        [Range(0, 20)] public int directions = 0;

        [Tooltip("Use this to adjust the angle that the directions are pointed towards.")]
        public float angleOffset = 0;

        [Header("Recenter / Visibility")]
        [Tooltip("Start with joystick hidden; it appears the first time the screen is touched or clicked.")]
        public bool startHidden = true; // NEW
        [Tooltip("If true, any touch/click not on the joystick will move it to that position and start dragging.")]
        public bool recenterAnywhere = true; // NEW

        [Tooltip("Snaps the joystick to wherever the finger is within a certain boundary.")]
        public bool snapsToTouch = false;
        public Rect boundaries;

        // Private variables.
        internal Vector2 desiredPosition, axis, origin, lastAxis;
        internal Color originalColor;
        [HideInInspector] public int currentPointerId = -2;

        internal static readonly Dictionary<int, VirtualJoystick> instances = new Dictionary<int, VirtualJoystick>();

        public const string VERSION = "1.1.6";  // bumped
        public const string DATE = "8 June 2025";

        Vector2Int lastScreen;
        protected Canvas rootCanvas;
        public Canvas GetRootCanvas()
        {
            Canvas[] all = GetComponentsInParent<Canvas>();
            if (all.Length > 0) return all[all.Length - 1];
            return null;
        }

        void OnValidate()
        {
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null) return;

            // Only assign a default if boundaries haven't been customized yet
            if (snapsToTouch && boundaries.width == 0 && boundaries.height == 0)
            {
                boundaries.width = rectTransform.rect.width + 250;
                boundaries.height = rectTransform.rect.height + 250;
                boundaries.x = transform.position.x - (boundaries.width / 2f);
                boundaries.y = transform.position.y - (boundaries.height / 2f);
            }
        }

        // Get an existing instance of a joystick.
        public static VirtualJoystick GetInstance(int id = 0)
        {
            if (!instances.ContainsKey(id))
            {
                if (id == 0)
                {
                    if (instances.Count > 0)
                    {
                        id = instances.Keys.First();
                        Debug.LogWarning($"You are reading Joystick input without specifying an ID, so joystick ID {id} is being used instead.");
                    }
                    else
                    {
                        Debug.LogError("There are no Virtual Joysticks in the Scene!");
                        return null;
                    }
                }
                else
                {
                    Debug.LogError($"Virtual Joystick ID '{id}' does not exist!");
                    return null;
                }
            }
            return instances[id];
        }

        public static int CountActiveInstances()
        {
            int count = 0;
            foreach (KeyValuePair<int, VirtualJoystick> j in instances)
                if (j.Value.isActiveAndEnabled) count++;
            return count;
        }

        public Vector2 GetAxisDelta() { return GetAxis() - lastAxis; }
        public static Vector2 GetAxisDelta(int id = 0)
        {
            if (instances.Count <= 0)
            {
                Debug.LogWarning("No instances of joysticks found on the Scene.");
                return Vector2.zero;
            }
            return GetInstance(id).GetAxisDelta();
        }

        public Vector2 GetAxis() { return axis; }

        public float GetAxis(string axe)
        {
            switch (axe.ToLower())
            {
                case "horizontal": case "h": case "x": return axis.x;
                case "vertical": case "v": case "y": return axis.y;
            }
            return 0;
        }

        public static float GetAxis(string axe, int id = 0)
        {
            if (instances.Count <= 0)
            {
                Debug.LogWarning("No instances of joysticks found on the Scene.");
                return 0;
            }
            return GetInstance(id).GetAxis(axe);
        }

        public static Vector2 GetAxis(int id = 0)
        {
            if (instances.Count <= 0)
            {
                Debug.LogWarning("No active instance of Virtual Joystick found on the Scene.");
                return Vector2.zero;
            }
            return GetInstance(id).axis;
        }

        public Vector2 GetAxisRaw()
        {
            return new Vector2(
                Mathf.Abs(axis.x) < deadzone || Mathf.Approximately(axis.x, 0) ? 0 : Mathf.Sign(axis.x),
                Mathf.Abs(axis.y) < deadzone || Mathf.Approximately(axis.y, 0) ? 0 : Mathf.Sign(axis.y)
            );
        }

        public float GetAxisRaw(string axe)
        {
            float f = GetAxis(axe);
            if (Mathf.Abs(f) < deadzone || Mathf.Approximately(f, 0)) return 0;
            return Mathf.Sign(f);
        }

        public static float GetAxisRaw(string axe, int id = 0)
        {
            if (instances.Count <= 0)
            {
                Debug.LogWarning("No active instance of Virtual Joystick found on the Scene.");
                return 0;
            }
            return GetInstance(id).GetAxisRaw(axe);
        }

        public static Vector2 GetAxisRaw(int id = 0)
        {
            if (instances.Count <= 0)
            {
                Debug.LogWarning("No instances of joysticks found on the Scene.");
                return Vector2.zero;
            }
            return GetInstance(id).GetAxisRaw();
        }

        public float GetRadius()
        {
            RectTransform t = transform as RectTransform;

            if (t)
            {
                if (rootCanvas != null)
                {
                    if (rootCanvas.renderMode == RenderMode.WorldSpace || rootCanvas.renderMode == RenderMode.ScreenSpaceCamera)
                    {
                        float canvasScaleFactor = rootCanvas.scaleFactor;
                        float adjustedRadius = radius * canvasScaleFactor;
                        return adjustedRadius;
                    }
                    else
                    {
                        return radius * t.rect.width * 0.5f;
                    }
                }
            }
            return radius;
        }

        // —————————————————————————————————————
        // Pointer events (we invoke these ourselves)
        // —————————————————————————————————————
        public void OnPointerDown(PointerEventData data)
        {
            currentPointerId = data.pointerId;
            SetPosition(data.position);
            controlStick.color = dragColor;
        }

        public void OnPointerUp(PointerEventData data)
        {
            desiredPosition = transform.position;
            controlStick.color = originalColor;
            currentPointerId = -2;
        }

        // —————————————————————————————————————
        // Helpers (visibility / positioning)
        // —————————————————————————————————————
        void ShowIfHidden()
        { // NEW
            if (!controlStick.enabled)
            {
                controlStick.enabled = true;
                var bg = GetComponent<Image>();
                if (bg) bg.enabled = true;
            }
        }

        void HideNow()
        { // NEW
            if (controlStick) controlStick.enabled = false;
            var bg = GetComponent<Image>();
            if (bg) bg.enabled = false;
        }

        protected void SetPosition(Vector2 screenPosition)
        {
            Vector2 position;

            if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                position = screenPosition;
            }
            else
            {
                Vector3 worldPoint;
                if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    rootCanvas.transform as RectTransform,
                    screenPosition,
                    rootCanvas.worldCamera,
                    out worldPoint
                ))
                {
                    position = worldPoint;
                }
                else
                {
                    position = screenPosition;
                }
            }

            Vector2 diff = position - (Vector2)transform.position;
            float r = GetRadius();
            bool snapToEdge = edgeSnap && (diff.magnitude / r) > deadzone;

            if (directions <= 0)
            {
                desiredPosition = snapToEdge
                    ? (Vector2)transform.position + diff.normalized * r
                    : (Vector2)transform.position + Vector2.ClampMagnitude(diff, r);
            }
            else
            {
                Vector2 snapDirection = SnapDirection(diff.normalized, directions, ((360f / directions) + angleOffset) * Mathf.Deg2Rad);
                desiredPosition = snapToEdge
                    ? (Vector2)transform.position + snapDirection * r
                    : (Vector2)transform.position + Vector2.ClampMagnitude(snapDirection * diff.magnitude, r);
            }
        }

        private Vector2 SnapDirection(Vector2 vector, int directions, float symmetryAngle)
        {
            Vector2 symmetryLine = new Vector2(Mathf.Cos(symmetryAngle), Mathf.Sin(symmetryAngle));
            float angle = Vector2.SignedAngle(symmetryLine, vector);
            angle /= 180f / directions;
            angle = (angle >= 0f) ? Mathf.Floor(angle) : Mathf.Ceil(angle);
            if ((int)Mathf.Abs(angle) % 2 == 1) angle += (angle >= 0f) ? 1 : -1;
            angle *= 180f / directions;
            angle *= Mathf.Deg2Rad;
            Vector2 result = new Vector2(Mathf.Cos(angle + symmetryAngle), Mathf.Sin(angle + symmetryAngle));
            result *= vector.magnitude;
            return result;
        }

        void Reset()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Image img = transform.GetChild(i).GetComponent<Image>();
                if (img) { controlStick = img; break; }
            }
        }

        public Rect GetBounds()
        {
            if (!snapsToTouch) return new Rect(0, 0, 0, 0);
            return new Rect(boundaries.x, boundaries.y, boundaries.width, boundaries.height);
        }

        void OnEnable()
        {
            if (!Application.isMobilePlatform && onlyOnMobile)
            {
                gameObject.SetActive(false);
                Debug.Log($"Your Virtual Joystick \"{name}\" is disabled because Only On Mobile is checked, and you are not on a mobile platform or mobile emualation.", gameObject);
                return;
            }

            rootCanvas = GetRootCanvas();
            if (!rootCanvas)
            {
                Debug.LogError($"Your Virtual Joystick \"{name})\" is not attached to a Canvas, so it won't work. It has been disabled.", gameObject);
                enabled = false;
            }

            origin = desiredPosition = transform.position;
            StartCoroutine(Activate());
            originalColor = controlStick.color;

            lastScreen = new Vector2Int(Screen.width, Screen.height);

            if (!instances.ContainsKey(ID))
                instances.Add(ID, this);
            else
                Debug.LogWarning("You have multiple Virtual Joysticks with the same ID on the Scene! You may not be able to retrieve input from some of them.", this);

            // NEW: start hidden if requested
            if (startHidden) HideNow();
        }

        IEnumerator Activate()
        {
            yield return new WaitForEndOfFrame();
            origin = desiredPosition = transform.position;
        }

        void OnDisable()
        {
            if (instances.ContainsKey(ID))
                instances.Remove(ID);
            else
                Debug.LogWarning("Unable to remove disabled joystick from the global Virtual Joystick list. You may have changed the ID of your joystick on runtime.", this);
        }

        void Update()
        {
            PositionUpdate();
            CheckForDrag();

            lastAxis = axis;

            controlStick.transform.position = Vector2.MoveTowards(controlStick.transform.position, desiredPosition, sensitivity);

            axis = (controlStick.transform.position - transform.position) / GetRadius();
            if (axis.magnitude < deadzone) axis = Vector2.zero;

            if (axis.sqrMagnitude > 0)
            {
                string output = string.Format("Virtual Joystick ({0}): {1}", name, axis);
                if (consolePrintAxis) Debug.Log(output);
            }
        }

        // Return whether this pointer is over the joystick (and starts dragging it if so)
        bool CheckForInteraction(Vector2 position, int pointerId = -1)
        { // CHANGED: returns bool
            PointerEventData data = new PointerEventData(null);
            data.position = position;
            data.pointerId = pointerId;

            List<RaycastResult> results = new List<RaycastResult>();
            GraphicRaycaster raycaster = rootCanvas.GetComponent<GraphicRaycaster>();
            raycaster.Raycast(data, results);

            foreach (RaycastResult result in results)
            {
                if (IsGameObjectOrChild(result.gameObject, gameObject))
                {
                    OnPointerDown(data);
                    return true; // hit joystick
                }
            }
            return false; // not over joystick
        }

        void CheckForDrag()
        {
            if (lastScreen.x != Screen.width || lastScreen.y != Screen.height)
            {
                lastScreen = new Vector2Int(Screen.width, Screen.height);
                OnEnable();
            }

            if (currentPointerId > -2)
            {
                if (currentPointerId > -1)
                {
                    for (int i = 0; i < GetTouchCount(); i++)
                    {
                        Touch t = GetTouch(i);
                        if (t.fingerId == currentPointerId)
                        {
                            SetPosition(t.position);
                            break;
                        }
                    }
                }
                else
                {
                    SetPosition(GetMousePosition());
                }
            }
        }

        void PositionUpdate()
        {
            int touchCount = GetTouchCount();

            // — Touch flow —
            if (touchCount > 0)
            {
                for (int i = 0; i < touchCount; i++)
                {
                    Touch t = GetTouch(i);
                    switch (t.phase)
                    {
                        case Touch.Phase.Began:
                            ShowIfHidden(); // NEW: reveal when first used

                            // Try to start drag if the touch is on the joystick
                            bool hitJoy = CheckForInteraction(t.position, t.fingerId);

                            // If not on joystick, optionally recenter anywhere (or within bounds if using snapsToTouch)
                            if (!hitJoy)
                            {
                                if (recenterAnywhere || (snapsToTouch && GetBounds().Contains(t.position)))
                                {
                                    Uproot(t.position, t.fingerId, true); // force recenter
                                }
                            }
                            break;

                        case Touch.Phase.Ended:
                        case Touch.Phase.Canceled:
                            if (currentPointerId == t.fingerId)
                                OnPointerUp(new PointerEventData(null));
                            break;
                    }
                }

                // — Mouse flow —
            }
            else if (GetMouseButtonDown(0))
            {
                ShowIfHidden(); // NEW

                Vector2 mousePos = GetMousePosition();

                bool hitJoy = CheckForInteraction(mousePos, -1);
                if (!hitJoy)
                {
                    if (recenterAnywhere || (snapsToTouch && GetBounds().Contains(mousePos)))
                    {
                        Uproot(mousePos, -1, true); // force recenter
                    }
                }
            }

            if (GetMouseButtonUp(0) && currentPointerId == -1)
            {
                OnPointerUp(new PointerEventData(null));
            }
        }

        // Recenter joystick to newPos and start drag.
        public void Uproot(Vector2 newPos, int newPointerId = -1, bool force = false)
        { // CHANGED: added 'force'
            // If not forcing and the move is tiny, ignore (legacy behavior)
            if (!force && Vector2.Distance(transform.position, newPos) < GetRadius()) return;

            Vector2 position;
            if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                position = newPos;
            }
            else
            {
                Vector3 worldPoint;
                if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    rootCanvas.transform as RectTransform,
                    newPos,
                    rootCanvas.worldCamera,
                    out worldPoint
                ))
                {
                    position = worldPoint;
                }
                else
                {
                    position = newPos;
                }
            }

            transform.position = position;
            desiredPosition = position;

            PointerEventData data = new PointerEventData(EventSystem.current)
            {
                position = newPos,
                pointerId = newPointerId
            };
            OnPointerDown(data);
        }

        bool IsGameObjectOrChild(GameObject hitObject, GameObject target)
        {
            if (hitObject == target) return true;
            foreach (Transform child in target.transform)
                if (IsGameObjectOrChild(hitObject, child.gameObject)) return true;
            return false;
        }

        static Vector2 GetMousePosition()
        {
#if ENABLE_INPUT_SYSTEM
            switch (GetInputMode())
            {
                case InputMode.newInputSystem:
                    return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            }
#endif
            return Input.mousePosition;
        }

        static bool GetMouseButton(int buttonId)
        {
#if ENABLE_INPUT_SYSTEM
            switch (GetInputMode())
            {
                case InputMode.newInputSystem:
                    if (Mouse.current != null)
                    {
                        switch (buttonId)
                        {
                            case 0: return Mouse.current.leftButton.isPressed;
                            case 1: return Mouse.current.rightButton.isPressed;
                            case 2: return Mouse.current.middleButton.isPressed;
                        }
                    }
                    return false;
            }
#endif
            return Input.GetMouseButton(buttonId);
        }

        static bool GetMouseButtonDown(int buttonId)
        {
#if ENABLE_INPUT_SYSTEM
            switch (GetInputMode())
            {
                case InputMode.newInputSystem:
                    if (Mouse.current != null)
                    {
                        switch (buttonId)
                        {
                            case 0: return Mouse.current.leftButton.wasPressedThisFrame;
                            case 1: return Mouse.current.rightButton.wasPressedThisFrame;
                            case 2: return Mouse.current.middleButton.wasPressedThisFrame;
                        }
                    }
                    return false;
            }
#endif
            return Input.GetMouseButtonDown(buttonId);
        }

        static bool GetMouseButtonUp(int buttonId)
        {
#if ENABLE_INPUT_SYSTEM
            switch (GetInputMode())
            {
                case InputMode.newInputSystem:
                    if (Mouse.current != null)
                    {
                        switch (buttonId)
                        {
                            case 0: return Mouse.current.leftButton.wasReleasedThisFrame;
                            case 1: return Mouse.current.rightButton.wasReleasedThisFrame;
                            case 2: return Mouse.current.middleButton.wasReleasedThisFrame;
                        }
                    }
                    return false;
            }
#endif
            return Input.GetMouseButtonUp(buttonId);
        }

        public class Touch
        {
            public Vector2 position;
            public int fingerId = -1;
            public enum Phase { None, Began, Moved, Stationary, Ended, Canceled }
            public Phase phase = Phase.None;
        }

        static Touch GetTouch(int touchId)
        {
#if ENABLE_INPUT_SYSTEM
            switch (GetInputMode())
            {
                case InputMode.newInputSystem:
                    UnityEngine.InputSystem.EnhancedTouch.Touch nt = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[touchId];
                    return new Touch
                    {
                        position = nt.screenPosition,
                        fingerId = nt.finger.index,
                        phase = (Touch.Phase)Enum.Parse(typeof(Touch.Phase), nt.phase.ToString())
                    };
            }
#endif
            UnityEngine.Touch t = Input.GetTouch(touchId);
            return new Touch
            {
                position = t.position,
                fingerId = t.fingerId,
                phase = (Touch.Phase)Enum.Parse(typeof(Touch.Phase), t.phase.ToString())
            };
        }

        static int GetTouchCount()
        {
#if ENABLE_INPUT_SYSTEM
            switch (GetInputMode())
            {
                case InputMode.newInputSystem:
                    return UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count;
            }
#endif
            return Input.touchCount;
        }
    }
}
