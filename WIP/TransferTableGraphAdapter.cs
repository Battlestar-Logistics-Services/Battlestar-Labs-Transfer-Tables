using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core;
using HarmonyLib;
using Model;
using Track;
using UnityEngine;

namespace BattlestarLabs.TransferTable
{
    [RequireComponent(typeof(TransferTableController))]
    public class TransferTableGraphAdapter : Turntable
    {
        public TransferTableController controller;

        [System.Serializable]
        public struct StallNodes
        {
            public TrackNode frontNode; // +Z (Entry)
            public TrackNode backNode;  // -Z (Exit)
        }

        [SerializeField]
        private List<StallNodes> _stalls = new List<StallNodes>();
        public IReadOnlyList<StallNodes> Stalls => _stalls;

        private TrackSegment _bridgeSegment;
        private TrackNode _freeNodeA;
        private TrackNode _freeNodeB;
        private Graph _graphRef;

        public TrackSegment BridgeSegment => _bridgeSegment;
        public Graph.PositionRotation GetBridgePositionRotation(Location loc)
{
    Transform bridgeTr =
        controller.bridgeTransform != null
            ? controller.bridgeTransform
            : transform;

    float halfLength = controller.definition.bridgeLength * 0.5f;
    float railY = controller.definition.railHeightOffset;

    Vector3 centerWorld =
        bridgeTr.TransformPoint(new Vector3(0f, railY, 0f));

    Vector3 centerGraph =
        TrackGraph.transform.InverseTransformPoint(centerWorld);

    Quaternion bridgeRotationGraph =
        Quaternion.Inverse(TrackGraph.transform.rotation)
        * bridgeTr.rotation;

    bool fromA = loc.end == TrackSegment.End.A;

    Quaternion rotation =
        bridgeRotationGraph
        * Quaternion.Euler(0f, fromA ? 180f : 0f, 0f);

    Vector3 position =
        centerGraph
        + rotation * Vector3.forward
        * (loc.distance - halfLength);

    if (!_diagnosticPoseLogged)
    {
        _diagnosticPoseLogged = true;

        Debug.LogWarning(
            $"[{controller?.tableId}] DIAGNOSTIC: " +
            $"GetBridgePositionRotation reached. " +
            $"locDistance={loc.distance:F3}, " +
            $"locEnd={loc.end}, " +
            $"bridgeWorld={bridgeTr.position}, " +
            $"centerGraph={centerGraph}, " +
            $"resultPosition={position}, " +
            $"resultRotation={rotation.eulerAngles}");
    }

    return new Graph.PositionRotation(position, rotation);
}



        private Graph TrackGraph
        {
            get
            {
                if (_graphRef == null)
                    _graphRef = Graph.Shared != null ? Graph.Shared : GetComponentInParent<Graph>();
                return _graphRef;
            }
        }

        private void Awake()
        {
            if (controller == null)
                controller = GetComponent<TransferTableController>();
        }

        private void OnEnable()
        {
            if (controller != null)
            {
                controller.OnStallLocked += HandleLocked;
                controller.OnTableUnlocked += HandleUnlocked;
                controller.OnMovementUpdated += HandleMovementUpdated;
            }
        }

        private void OnDisable()
        {
            if (controller != null)
            {
                controller.OnStallLocked -= HandleLocked;
                controller.OnTableUnlocked -= HandleUnlocked;
                controller.OnMovementUpdated -= HandleMovementUpdated;
            }
        }

        public void InitializeBridge()
        {
            Debug.Log(
                $"[TransferTable Diagnostic] Car assembly: " +
                $"{typeof(Car).Assembly.FullName}");

            if (TrackGraph == null)
            {
                Debug.LogError($"[{controller?.tableId}] Cannot initialize bridge: TrackGraph not found!");
                return;
            }

            if (_bridgeSegment != null)
                return;

            float halfLength = controller.definition.bridgeLength * 0.5f;
            float railY = controller.definition.railHeightOffset;

            Transform parentTransform = controller.bridgeTransform != null ? controller.bridgeTransform : transform;

            // 1. Create Free Nodes
            _freeNodeA = CreateFreeNode("FreeA", new Vector3(0f, railY, halfLength), parentTransform);
            _freeNodeB = CreateFreeNode("FreeB", new Vector3(0f, railY, -halfLength), parentTransform);

            // 2. Create Bridge TrackSegment
            GameObject segGo = new GameObject($"{controller.tableId}_BridgeSegment", typeof(TrackSegment));
            segGo.transform.SetParent(parentTransform, false);
            segGo.transform.localPosition = new Vector3(0f, railY, 0f);
            segGo.transform.localRotation = Quaternion.identity;

            _bridgeSegment = segGo.GetComponent<TrackSegment>();
            _bridgeSegment.id = $"{controller.tableId}-Bridge";
            _bridgeSegment.style = TrackSegment.Style.Bridge;
            _bridgeSegment.turntable = this;
            _bridgeSegment.speedLimit = 15;
            _bridgeSegment.groupId = null; // null ensures Graph.AddSegment adds and enables the segment!

            // Initially connect to free nodes
            _bridgeSegment.a = _freeNodeA;
            _bridgeSegment.b = _freeNodeB;

            TrackGraph.AddSegment(_bridgeSegment, true);
            _bridgeSegment.InvalidateCurve();

            // Sync base Turntable private fields via reflection so base game systems see this as a turntable
            SyncBaseTurntableFields();

            Debug.Log($"[{controller.tableId}] Bridge initialized successfully. Initial segment length: {Vector3.Distance(_freeNodeA.transform.position, _freeNodeB.transform.position):F2}m");

            // Lock to initial stall if starting in locked state
            if (controller.State == TransferTableController.MovementState.Locked)
            {
                LinkBridgeToStall(controller.CurrentStallIndex);
            }
        }

        private TrackNode CreateFreeNode(string suffix, Vector3 localPos, Transform parentTransform)
        {
            GameObject go = new GameObject($"{controller.tableId}_{suffix}", typeof(TrackNode));
            go.transform.SetParent(parentTransform, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;

            TrackNode node = go.GetComponent<TrackNode>();
            node.id = $"{controller.tableId}-{suffix}";
            node.turntable = this;

            TrackGraph.AddNode(node);
            return node;
        }

        private bool _diagnosticPoseLogged;

        public void RegeneratePitNodes()
        {
            if (TrackGraph == null)
            {
                Debug.LogError($"[{controller?.tableId}] Cannot generate pit nodes: TrackGraph not found!");
                return;
            }

            // Clean up existing pit nodes
            foreach (var stall in _stalls)
            {
                if (stall.frontNode != null) DestroyImmediate(stall.frontNode.gameObject);
                if (stall.backNode != null) DestroyImmediate(stall.backNode.gameObject);
            }
            _stalls.Clear();

            var def = controller.definition;
            float halfLength = def.bridgeLength * 0.5f;
            float railY = def.railHeightOffset;

            for (int i = 0; i < def.totalStalls; i++)
            {
                float xOffset = def.GetOffsetForStall(i);

                Vector3 localFront = new Vector3(xOffset, railY, halfLength);
                Vector3 localBack = new Vector3(xOffset, railY, -halfLength);

                Vector3 worldFront = transform.TransformPoint(localFront);
                Vector3 worldBack = transform.TransformPoint(localBack);

                Quaternion frontRot = transform.rotation;
                Quaternion backRot = transform.rotation * Quaternion.Euler(0f, 180f, 0f);

                TrackNode fNode = CreatePitNode($"{controller.tableId}_Stall_{i}_Front", worldFront, frontRot);
                TrackNode bNode = CreatePitNode($"{controller.tableId}_Stall_{i}_Back", worldBack, backRot);

                _stalls.Add(new StallNodes { frontNode = fNode, backNode = bNode });
            }

            Debug.Log($"[{controller.tableId}] Generated {_stalls.Count} stall node pairs.");
            SyncBaseTurntableFields();
        }

        private TrackNode CreatePitNode(string name, Vector3 worldPos, Quaternion worldRot)
        {
            GameObject go = new GameObject(name, typeof(TrackNode));
            go.transform.SetParent(TrackGraph != null ? TrackGraph.transform : transform);
            go.transform.position = worldPos;
            go.transform.rotation = worldRot;

            TrackNode node = go.GetComponent<TrackNode>();
            node.id = name;
            node.turntable = this;
            TrackGraph.AddNode(node);
            return node;
        }

        private void HandleMovementUpdated(float currentOffset)
        {
            if (_bridgeSegment == null || _freeNodeA == null || _freeNodeB == null)
                return;

            // Free nodes track the bridge deck in real-time as it slides
            float halfLength = controller.definition.bridgeLength * 0.5f;
            float railY = controller.definition.railHeightOffset;

            Transform bridgeTr = controller.bridgeTransform != null ? controller.bridgeTransform : transform;
            _freeNodeA.transform.position = bridgeTr.TransformPoint(new Vector3(0f, railY, halfLength));
            _freeNodeB.transform.position = bridgeTr.TransformPoint(new Vector3(0f, railY, -halfLength));
            _freeNodeA.transform.rotation = transform.rotation;
            _freeNodeB.transform.rotation = transform.rotation;

            _bridgeSegment.InvalidateCurve();
        }

        private void HandleLocked(int stallIndex)
        {
            LinkBridgeToStall(stallIndex);
            ForceCarVisibilityOnBridge();
        }

        private void HandleUnlocked()
{
            _diagnosticPoseLogged = false;

            Debug.LogWarning(
            $"[{controller?.tableId}] DIAGNOSTIC: HandleUnlocked reached.");

            if (_bridgeSegment != null && _freeNodeA != null && _freeNodeB != null)
            {
                // Connect bridge segment to moving free nodes
                _bridgeSegment.a = _freeNodeA;
                _bridgeSegment.b = _freeNodeB;
                _bridgeSegment.InvalidateCurve();
            }

            SetStopIndexProperty(null);
            ForceCarVisibilityOnBridge();
            Debug.Log($"[{controller?.tableId}] Unlocked: Bridge segment restored to free nodes.");
        }

        private void ForceCarVisibilityOnBridge()
        {
            if (_bridgeSegment == null) return;

            try
            {
                var tc = TrainController.Shared;
                if (tc == null) return;

                // _carSegmentCache field confirmed in TrainController
                var cacheField = typeof(TrainController).GetField("_carSegmentCache",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (cacheField == null)
                {
                    Debug.LogWarning($"[{controller?.tableId}] _carSegmentCache field not found on TrainController.");
                    return;
                }

                var cache = cacheField.GetValue(tc);
                if (cache == null) return;

                var enumMethod = cache.GetType().GetMethod("EnumerateCarIdsOnSegment",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (enumMethod == null)
                {
                    Debug.LogWarning($"[{controller?.tableId}] EnumerateCarIdsOnSegment not found on CarSegmentCache.");
                    return;
                }

                var carIds = enumMethod.Invoke(cache, new object[] { _bridgeSegment.id })
                    as System.Collections.IEnumerable;
                if (carIds == null) return;

                var tryGetMethod = typeof(TrainController).GetMethod("TryGetCarForId",
                    BindingFlags.Public | BindingFlags.Instance);

                foreach (var idObj in carIds)
                {
                    if (idObj == null) continue;
                    string carId = idObj.ToString();

                    Car car = null;
                    bool found = false;

                    if (tryGetMethod != null)
                    {
                        var args = new object[] { carId, null };
                        found = (bool)tryGetMethod.Invoke(tc, args);
                        car = args[1] as Car;
                    }

                    if (found && car != null)
                    {
                        // Car.SetVisible(bool) is a confirmed public method
                        car.SetVisible(true);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{controller?.tableId}] ForceCarVisibilityOnBridge: {ex.Message}");
            }
        }

        public void LinkBridgeToStall(int stallIndex)
        {
            if (_bridgeSegment == null)
            {
                InitializeBridge();
            }

            if (_stalls == null || _stalls.Count == 0)
            {
                RegeneratePitNodes();
            }

            if (stallIndex >= 0 && stallIndex < _stalls.Count)
            {
                var targetStall = _stalls[stallIndex];
                if (targetStall.frontNode != null && targetStall.backNode != null)
                {
                    _bridgeSegment.a = targetStall.frontNode;
                    _bridgeSegment.b = targetStall.backNode;
                    _bridgeSegment.InvalidateCurve();

                    SetStopIndexProperty(stallIndex);
                    Debug.Log($"[{controller.tableId}] Mag-lock engaged: Segment connected to Stall {stallIndex} (Front: {_bridgeSegment.a.name}, Back: {_bridgeSegment.b.name})");
                }
            }
        }

        private void SetStopIndexProperty(int? index)
        {
            var prop = typeof(Turntable).GetProperty("StopIndex");
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(this, index, null);
            }
            else
            {
                var field = typeof(Turntable).GetField("<StopIndex>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null) field.SetValue(this, index);
            }
        }

        private void SyncBaseTurntableFields()
        {
            // Sync _segment, _freeA, _freeB on base Turntable
            SetBaseField("_segment", _bridgeSegment);
            SetBaseField("_freeA", _freeNodeA);
            SetBaseField("_freeB", _freeNodeB);
            SetBaseField("bridgeGroupId", controller?.tableId);

            // Populate base nodes list with pit stall nodes
            var baseNodesField = typeof(Turntable).GetField("nodes", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (baseNodesField != null)
            {
                var list = new List<TrackNode>();
                foreach (var stall in _stalls)
                {
                    if (stall.frontNode != null) list.Add(stall.frontNode);
                    if (stall.backNode != null) list.Add(stall.backNode);
                }
                baseNodesField.SetValue(this, list);
            }
        }

        private void SetBaseField(string fieldName, object value)
        {
            var f = typeof(Turntable).GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null && value != null)
            {
                f.SetValue(this, value);
            }
        }

        public void CleanUpGraph()
        {
            if (_bridgeSegment != null)
            {
                DestroyImmediate(_bridgeSegment.gameObject);
                _bridgeSegment = null;
            }
            if (_freeNodeA != null)
            {
                DestroyImmediate(_freeNodeA.gameObject);
                _freeNodeA = null;
            }
            if (_freeNodeB != null)
            {
                DestroyImmediate(_freeNodeB.gameObject);
                _freeNodeB = null;
            }
            foreach (var stall in _stalls)
            {
                if (stall.frontNode != null) DestroyImmediate(stall.frontNode.gameObject);
                if (stall.backNode != null) DestroyImmediate(stall.backNode.gameObject);
            }
            _stalls.Clear();
        }

        private void OnDestroy()
        {
            CleanUpGraph();
        }

        // Inform Railroader route graph: bridge is reachable from adjacent tracks when locked
        public new bool SegmentsReachableFrom(TrackSegment segment, TrackSegment.End end, out TrackSegment other)
        {
            if (controller.State == TransferTableController.MovementState.Locked && _bridgeSegment != null)
            {
                TrackNode targetNode = (end == TrackSegment.End.A) ? _bridgeSegment.a : _bridgeSegment.b;
                if (targetNode != null && TrackGraph != null)
                {
                    foreach (TrackSegment seg in TrackGraph.SegmentsConnectedTo(targetNode))
                    {
                        if (seg != _bridgeSegment)
                        {
                            other = seg;
                            return true;
                        }
                    }
                }
            }
            other = null;
            return false;
        }

        // Inform Railroader route graph: pit nodes are not dead ends when locked
        public new bool NodeIsDeadEnd(TrackNode node, out Vector3 direction)
        {
            if (_bridgeSegment == null || controller.State != TransferTableController.MovementState.Locked)
            {
                direction = Vector3.zero;
                return true;
            }

            if (node == _freeNodeA || node == _freeNodeB)
            {
                direction = Vector3.zero;
                return true;
            }

            if (node == _bridgeSegment.a || node == _bridgeSegment.b)
            {
                direction = node.transform.rotation * Vector3.forward;
                return !SegmentsReachableFrom(_bridgeSegment, _bridgeSegment.EndForNode(node), out _);
            }

            direction = Vector3.zero;
            return true;
        }

        public bool IsCarStraddlingGap(out Car blockingCar)
        {
            blockingCar = null;

            if (_bridgeSegment == null || _bridgeSegment.a == null || _bridgeSegment.b == null)
            {
                return false;
            }

            try
            {
                TrainController tc = TrainController.Shared;
                if (tc != null)
                {
                    if (tc.CarWheelBoundsOver(_bridgeSegment.a, out blockingCar)) return true;
                    if (tc.CarWheelBoundsOver(_bridgeSegment.b, out blockingCar)) return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{controller?.tableId}] Note during gap safety check: {ex.Message}");
            }

            blockingCar = null;
            return false;
        }
    }

    [HarmonyPatch(typeof(Turntable), nameof(Turntable.GetPositionRotation))]
    public static class TurntableGetPositionRotationPatch
    {
        public static bool Prefix(
            Turntable __instance,
            Location loc,
            ref Graph.PositionRotation __result)
        {
            if (__instance is TransferTableGraphAdapter adapter &&
                adapter.BridgeSegment != null)
            {
                adapter.BridgeSegment.GetPositionRotationAtDistance(
                    loc.distance,
                    loc.end,
                    PositionAccuracy.Standard,
                    out Vector3 pos,
                    out Quaternion rot);

                __result = new Graph.PositionRotation(pos, rot);
                

                return false;
            }

            return true;
        }
    }


    [HarmonyPatch(typeof(Turntable), "UpdateSegmentIndex")]
    public static class TurntableUpdateSegmentIndexPatch
    {
        public static bool Prefix(Turntable __instance)
        {
            // return false = skip original; only suppress for our adapter
            return !(__instance is TransferTableGraphAdapter);
        }
    }
    
}

