using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattlestarLabs.TransferTable
{
    /// <summary>
    /// Handles placement, repositioning, rotation, and bulldozing of transfer tables.
    /// Prevents tables from being permanently stuck where first spawned.
    /// </summary>
    [DisallowMultipleComponent]
    public class TransferTablePlacement : MonoBehaviour
    {
        public static List<TransferTablePlacement> AllInstances = new List<TransferTablePlacement>();
        public static TransferTablePlacement ActiveInstance { get; private set; }

        public TransferTableController controller;
        public TransferTableGraphAdapter adapter;

        public bool IsPlaced { get; private set; } = false;
        public bool IsPlacementMode { get; private set; } = false;

        public static readonly float[] RotationSteps = new float[] { 1f, 5f, 10f, 15f, 45f, 90f };
        private int _rotationStepIndex = 3; // Defaults to 15 degrees

        private float _currentRotationAngle = 0f;
        private float _heightOffset = 0f;

        private void Awake()
        {
            if (controller == null) controller = GetComponent<TransferTableController>();
            if (adapter == null) adapter = GetComponent<TransferTableGraphAdapter>();
        }

        private void OnEnable()
        {
            if (!AllInstances.Contains(this))
                AllInstances.Add(this);

            if (ActiveInstance == null)
                ActiveInstance = this;
        }

        private void OnDisable()
        {
            AllInstances.Remove(this);
            if (ActiveInstance == this)
            {
                ActiveInstance = AllInstances.Count > 0 ? AllInstances[0] : null;
            }
        }

        public void StartPlacement(bool isNewSpawn)
        {
            IsPlacementMode = true;
            IsPlaced = false;
            ActiveInstance = this;

            // If repositioning an existing table, disconnect its graph nodes first
            if (!isNewSpawn && adapter != null)
            {
                adapter.CleanUpGraph();
            }

            _currentRotationAngle = transform.eulerAngles.y;
            Debug.Log($"[{controller?.tableId}] Entered Placement Mode. Left-Click: Place, R: Rotate 15°, PgUp/PgDn: Elevation, Esc: Cancel.");
        }

        public void ConfirmPlacement()
        {
            IsPlacementMode = false;
            IsPlaced = true;
            ActiveInstance = this;

            // Initialize track graph and visuals
            if (adapter != null)
            {
                adapter.RegeneratePitNodes();
                adapter.InitializeBridge();
            }

            var visuals = GetComponent<TransferTableVisuals>();
            if (visuals != null)
            {
                visuals.BuildVisualRails();
            }

            Debug.Log($"[{controller?.tableId}] Placement confirmed at {transform.position}, rotation {_currentRotationAngle:F1}°");
        }

        public void CancelPlacement()
        {
            IsPlacementMode = false;
            if (!IsPlaced)
            {
                // New spawn cancelled -> destroy
                Destroy(gameObject);
            }
            else
            {
                // Reposition cancelled -> restore nodes
                if (adapter != null)
                {
                    adapter.RegeneratePitNodes();
                    adapter.InitializeBridge();
                }
            }
        }

        public void Bulldoze()
        {
            Debug.Log($"[{controller?.tableId}] Bulldozing transfer table...");
            if (adapter != null)
            {
                adapter.CleanUpGraph();
            }
            Destroy(gameObject);
        }

        private static int _lastTabFrame = -1;

        public static void CycleActiveTable()
        {
            if (Time.frameCount == _lastTabFrame) return;
            _lastTabFrame = Time.frameCount;

            AllInstances.RemoveAll(inst => inst == null);
            if (AllInstances.Count == 0)
            {
                ActiveInstance = null;
                return;
            }

            int idx = AllInstances.IndexOf(ActiveInstance);
            idx = (idx + 1) % AllInstances.Count;
            ActiveInstance = AllInstances[idx];
            Debug.Log($"[TransferTable] Active table cycled to: {ActiveInstance.controller?.tableId} ({idx + 1}/{AllInstances.Count})");
        }

        private void Update()
        {
            // Tab key cycling is globally frame-guarded so all instances can check without double-stepping
            if (Input.GetKeyDown(KeyCode.Tab) && AllInstances.Count > 1)
            {
                CycleActiveTable();
            }

            if (IsPlacementMode)
            {
                UpdatePlacementFollow();
                HandlePlacementInput();
            }
            else
            {
                if (ActiveInstance == this)
                {
                    // P to reposition
                    if (IsPlaced && Input.GetKeyDown(KeyCode.P))
                    {
                        StartPlacement(false);
                    }
                    // Delete / Backspace to bulldoze
                    else if (IsPlaced && (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace)))
                    {
                        Bulldoze();
                    }
                }
            }
        }

        private void UpdatePlacementFollow()
        {
            if (Camera.main == null) return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 500f))
            {
                Vector3 targetPos = hit.point + Vector3.up * _heightOffset;
                transform.position = targetPos;
                transform.rotation = Quaternion.Euler(0f, _currentRotationAngle, 0f);
            }
        }

        private void HandlePlacementInput()
        {
            // T cycles through rotation steps: 1, 5, 10, 15, 45, 90
            if (Input.GetKeyDown(KeyCode.T))
            {
                _rotationStepIndex = (_rotationStepIndex + 1) % RotationSteps.Length;
                Debug.Log($"[{controller?.tableId}] Rotation step set to: {RotationSteps[_rotationStepIndex]}°");
            }

            // R: Clockwise, Shift+R: Counterclockwise
            if (Input.GetKeyDown(KeyCode.R))
            {
                float step = RotationSteps[_rotationStepIndex];
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    _currentRotationAngle -= step;
                }
                else
                {
                    _currentRotationAngle += step;
                }

                if (_currentRotationAngle < 0f) _currentRotationAngle += 360f;
                _currentRotationAngle %= 360f;
            }

            // Height adjustments
            if (Input.GetKey(KeyCode.PageUp))
            {
                _heightOffset += 0.5f * Time.deltaTime;
            }
            else if (Input.GetKey(KeyCode.PageDown))
            {
                _heightOffset -= 0.5f * Time.deltaTime;
            }

            // Left-click to place
            if (Input.GetMouseButtonDown(0))
            {
                ConfirmPlacement();
            }
            // Right-click or Escape to cancel
            else if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelPlacement();
            }
        }

        // Draggable window state
        private static Rect _windowRect = new Rect(20, 80, 520, 165);
        private static bool _isCollapsed = false;

        private void OnGUI()
        {
            if (ActiveInstance != this) return;

            string title = IsPlacementMode ? $"[PLACING] {controller?.tableId} (Drag Header)" : $"Transfer Table: {controller?.tableId} (Drag Header)";
            _windowRect = GUI.Window(98765, _windowRect, DrawWindowContents, title);
            _windowRect.x = Mathf.Clamp(_windowRect.x, 0, Mathf.Max(0, Screen.width - _windowRect.width));
            _windowRect.y = Mathf.Clamp(_windowRect.y, 0, Mathf.Max(0, Screen.height - _windowRect.height));
        }

        private void DrawWindowContents(int windowID)
        {
            // Top 25px is the draggable header
            GUI.DragWindow(new Rect(0, 0, 10000, 25));

            GUILayout.BeginHorizontal();
            if (AllInstances.Count > 1)
            {
                int currentNum = AllInstances.IndexOf(this) + 1;
                if (GUILayout.Button($"Table {currentNum}/{AllInstances.Count} (Tab)", GUILayout.Width(130), GUILayout.Height(22)))
                {
                    CycleActiveTable();
                }
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(_isCollapsed ? "▼ Expand" : "▲ Collapse", GUILayout.Width(85), GUILayout.Height(22)))
            {
                _isCollapsed = !_isCollapsed;
            }
            GUILayout.EndHorizontal();

            if (_isCollapsed)
            {
                string shortState = controller != null ? (controller.State == TransferTableController.MovementState.Locked ? $"Stall {controller.CurrentStallIndex + 1}" : "Moving") : "Ready";
                GUILayout.Label($"<b>{controller?.tableId}</b>: {shortState} | Tab: Cycle | P: Move");
                GUI.DragWindow();
                return;
            }

            if (IsPlacementMode)
            {
                float step = RotationSteps[_rotationStepIndex];
                GUILayout.Label($"Angle: <b>{_currentRotationAngle:F0}°</b> (Step: <b>{step}°</b>)");
                GUILayout.Label("Left-Click: Confirm | Esc: Cancel | PgUp/PgDn: Elevation");
                GUILayout.Label("R: Rotate CW | Shift+R: Rotate CCW | T: Step [1, 5, 10, 15, 45, 90]");
            }
            else
            {
                string stateStr = controller != null ? (controller.State == TransferTableController.MovementState.Locked ? $"Locked at Stall {controller.CurrentStallIndex + 1}" : "Moving...") : "Ready";
                GUILayout.Label($"State: <color=yellow>{stateStr}</color>");

                // Clickable Stall Buttons
                if (controller != null && controller.definition != null)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Go to:", GUILayout.Width(45));
                    for (int s = 0; s < controller.definition.totalStalls; s++)
                    {
                        bool isCurrent = controller.CurrentStallIndex == s && controller.State == TransferTableController.MovementState.Locked;
                        string btnText = isCurrent ? $"[{s + 1}]" : $"{s + 1}";
                        if (GUILayout.Button(btnText, GUILayout.Width(35), GUILayout.Height(24)))
                        {
                            controller.MoveToStall(s);
                        }
                    }
                    if (GUILayout.Button("◄", GUILayout.Width(30), GUILayout.Height(24)))
                    {
                        controller.StepLeft();
                    }
                    if (GUILayout.Button("►", GUILayout.Width(30), GUILayout.Height(24)))
                    {
                        controller.StepRight();
                    }
                    GUILayout.EndHorizontal();
                }

                GUILayout.Label("Keypad 1-5: Stalls | Keypad +/-: Step | P: Move | Del: Bulldoze");
            }

            GUI.DragWindow();
        }
    }
}
