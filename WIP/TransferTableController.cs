using System;
using UnityEngine;

namespace BattlestarLabs.TransferTable
{
    public class TransferTableController : MonoBehaviour
    {
        public enum MovementState
        {
            Locked,
            Moving
        }

        [Header("Identity")]
        [Tooltip("Unique ID for this table instance (prefixes all child nodes and logs)")]
        public string tableId = "BST_TransferTable_01";

        [Header("Configuration")]
        public TransferTableDefinition definition = new TransferTableDefinition();

        [Header("Kinematics")]
        [Tooltip("Travel speed of the bridge deck in meters/sec (1.5 m/s is ~3.3 mph)")]
        public float travelSpeed = 1.5f;

        [Header("State (Read-Only)")]
        [SerializeField]
        private MovementState _state = MovementState.Locked;
        public MovementState State => _state;

        public int CurrentStallIndex { get; private set; }
        public int? TargetStallIndex { get; private set; }

        public float CurrentOffset { get; private set; }
        public float TargetOffset { get; private set; }

        [Header("Bridge Transform Reference")]
        [Tooltip("The moving bridge GameObject that slides laterally along local X")]
        public Transform bridgeTransform;

        // Events for other systems (GraphAdapter, SFX, UI) to listen to
        public event Action<int> OnStallLocked;
        public event Action OnTableUnlocked;
        public event Action<float> OnMovementUpdated;

        private float _initialBridgeY = 0f;
        private float _initialBridgeZ = 0f;

        private void Awake()
        {
            FindBridgeTransformIfNeeded();
        }

        public void FindBridgeTransformIfNeeded()
        {
            if (bridgeTransform != null)
            {
                _initialBridgeY = bridgeTransform.localPosition.y;
                _initialBridgeZ = bridgeTransform.localPosition.z;
                return;
            }

            // Recursive search across all children for Bridge_Root or any bridge transform
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child == this.transform) continue;
                if (child.name.Equals("Bridge_Root", StringComparison.OrdinalIgnoreCase) || child.name.ToLower().Contains("bridge"))
                {
                    bridgeTransform = child;
                    _initialBridgeY = bridgeTransform.localPosition.y;
                    _initialBridgeZ = bridgeTransform.localPosition.z;
                    Debug.Log($"[{tableId}] Successfully bound moving bridge transform: {bridgeTransform.name}");
                    break;
                }
            }

            if (bridgeTransform == null)
            {
                Debug.LogWarning($"[{tableId}] Warning: Moving bridge child not found in hierarchy! Creating empty anchor.");
                GameObject anchor = new GameObject("Bridge_Root");
                anchor.transform.SetParent(this.transform, false);
                bridgeTransform = anchor.transform;
            }
        }

        private void Start()
        {
            FindBridgeTransformIfNeeded();

            // Initialize at starting stall
            CurrentStallIndex = definition.defaultStallIndex;
            CurrentOffset = definition.GetOffsetForStall(CurrentStallIndex);
            TargetOffset = CurrentOffset;

            ApplyBridgePosition();

            _state = MovementState.Locked;
            OnStallLocked?.Invoke(CurrentStallIndex);
        }

       private void FixedUpdate()
{
    if (_state == MovementState.Moving)
    {
        UpdateMovement();
    }
}


        /// <summary>
        /// Command the table to go to a specific stall (0 to N-1).
        /// </summary>
        public bool MoveToStall(int targetIndex)
        {
            if (!definition.IsValidStall(targetIndex))
            {
                Debug.LogWarning($"[{tableId}] Invalid stall {targetIndex} requested.");
                return false;
            }

            if (targetIndex == CurrentStallIndex && _state == MovementState.Locked)
            {
                return true; // Already there
            }

            TargetStallIndex = targetIndex;
            TargetOffset = definition.GetOffsetForStall(targetIndex);

            // Unlock table and start sliding
            _state = MovementState.Moving;
            OnTableUnlocked?.Invoke();

            return true;
        }

        /// <summary>
        /// Manual jog one stall to the left (-)
        /// </summary>
        public void StepLeft()
        {
            int next = (_state == MovementState.Moving && TargetStallIndex.HasValue) 
                ? TargetStallIndex.Value - 1 
                : CurrentStallIndex - 1;

            if (definition.IsValidStall(next))
                MoveToStall(next);
        }

        /// <summary>
        /// Manual jog one stall to the right (+)
        /// </summary>
        public void StepRight()
        {
            int next = (_state == MovementState.Moving && TargetStallIndex.HasValue) 
                ? TargetStallIndex.Value + 1 
                : CurrentStallIndex + 1;

            if (definition.IsValidStall(next))
                MoveToStall(next);
        }

        private void UpdateMovement()
        {
            // Linear slide along X
            CurrentOffset = Mathf.MoveTowards(CurrentOffset, TargetOffset, travelSpeed * Time.deltaTime);
            ApplyBridgePosition();
            OnMovementUpdated?.Invoke(CurrentOffset);

            // Mag-Lock Check: within 3-inch tolerance window?
            if (Mathf.Abs(CurrentOffset - TargetOffset) <= definition.lockTolerance)
            {
                // Snap cleanly to target center
                CurrentOffset = TargetOffset;
                ApplyBridgePosition();

                CurrentStallIndex = TargetStallIndex.Value;
                TargetStallIndex = null;
                _state = MovementState.Locked;

                Debug.Log($"[{tableId}] Mag-lock engaged at Stall {CurrentStallIndex}!");
                OnStallLocked?.Invoke(CurrentStallIndex);
            }
        }

        private void ApplyBridgePosition()
        {
            if (bridgeTransform != null)
            {
                // Local X = lateral crossing, preserving initial model Y height and Z depth
                bridgeTransform.localPosition = new Vector3(CurrentOffset, _initialBridgeY, _initialBridgeZ);
            }
        }
    }
}