using System;
using UnityEngine;
using Track;

namespace BattlestarLabs.TransferTable
{
    /// <summary>
    /// Spawns the official Railroader bridge track on the moving bridge deck using
    /// Railroader's native TrackObjectBuilder, ensuring authentic game rails, ties,
    /// roadbed timbers, and wheel colliders.
    /// Also maintains the 3rd rail / center car-pusher guide channel for close-quarters car moves.
    /// </summary>
    [DisallowMultipleComponent]
    public class TransferTableVisuals : MonoBehaviour
    {
        public TransferTableController controller;

        private GameObject _railsRoot;
        private GameObject _pusherGuideRoot;

        private void Awake()
        {
            if (controller == null)
                controller = GetComponent<TransferTableController>();
        }

        private void Start()
        {
            TryBuildTrack();
        }

        private void Update()
        {
            // If rails have not been built yet (e.g. bridge segment wasn't ready at Start), retry
            if (_railsRoot == null)
            {
                TryBuildTrack();
            }
        }

        public void BuildVisualRails()
        {
            TryBuildTrack();
        }

        /// <summary>
        /// Ensures the rail mesh GameObjects are active and their renderers are enabled.
        /// Called by TransferTableGraphAdapter after every lock/unlock to prevent car flicker.
        /// </summary>
        public void EnsureRailsActive()
        {
            if (_railsRoot == null) return;

            _railsRoot.SetActive(true);

            // Re-enable every MeshRenderer in the hierarchy (the game may disable them internally)
            foreach (var mr in _railsRoot.GetComponentsInChildren<MeshRenderer>(true))
                mr.enabled = true;
        }

        public bool TryBuildTrack()
        {
            if (controller == null) return false;
            controller.FindBridgeTransformIfNeeded();

            Transform bridgeTr = controller.bridgeTransform != null ? controller.bridgeTransform : transform;

            var adapter = GetComponent<TransferTableGraphAdapter>();
            TrackSegment bridgeSeg = adapter != null ? adapter.BridgeSegment : null;
            if (bridgeSeg == null)
            {
                Debug.LogWarning($"[{controller.tableId}] TryBuildTrack: BridgeSegment not ready yet, will retry.");
                return false;
            }

            var tom = TrackObjectManager.Instance;
            if (tom == null || tom.profile == null)
            {
                Debug.LogWarning($"[{controller.tableId}] TryBuildTrack: TrackObjectManager or profile not ready.");
                return false;
            }

            // Destroy any previous visual rail root before rebuilding
            if (_railsRoot != null)
                DestroyImmediate(_railsRoot);

            try
            {
                // TrackObjectBuilder's parent arg is the GameObject it will attach the mesh to.
                // We pass bridgeTr.gameObject so the rails live on the bridge deck.
                var builder = new TrackObjectBuilder(tom.profile, bridgeTr.gameObject, null, HideFlags.None);

                // CreateSegmentObject requires a SegmentProxy wrapper, NOT the raw TrackSegment.
                var proxy = new SegmentProxy(bridgeSeg);
                _railsRoot = builder.CreateSegmentObject(proxy);

                if (_railsRoot != null)
                {
                    // Force active — builder may create it disabled
                    _railsRoot.SetActive(true);

                    // Apply the track material to ALL mesh renderers in the hierarchy
                    if (tom.profile.trackMaterial != null)
                    {
                        foreach (var mr in _railsRoot.GetComponentsInChildren<MeshRenderer>(true))
                        {
                            mr.material = tom.profile.trackMaterial;
                            mr.enabled = true;
                        }
                    }

                    // Ensure everything is on the Rail layer so the game renders it correctly
                    int trackLayer = LayerMask.NameToLayer("Rail");
                    if (trackLayer >= 0)
                    {
                        _railsRoot.layer = trackLayer;
                        foreach (var tr in _railsRoot.GetComponentsInChildren<Transform>(true))
                            tr.gameObject.layer = trackLayer;
                    }

                    Debug.Log($"[{controller.tableId}] Bridge rail mesh built successfully on '{bridgeTr.name}'.");
                }
                else
                {
                    Debug.LogError($"[{controller.tableId}] TrackObjectBuilder.CreateSegmentObject returned null – bridge rails not created.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{controller.tableId}] Exception while building bridge rail mesh: {ex}");
                return false;
            }

            // Build the 3rd rail / center car-pusher cable guide channel (pinned for future use)
            BuildCenterPusherGuide(bridgeTr);

            return true;
        }

        private void BuildCenterPusherGuide(Transform bridgeTr)
        {
            if (_pusherGuideRoot != null)
                DestroyImmediate(_pusherGuideRoot);

            _pusherGuideRoot = new GameObject("PusherMuleGuide");
            _pusherGuideRoot.transform.SetParent(bridgeTr, false);
            _pusherGuideRoot.transform.localPosition = Vector3.zero;
            _pusherGuideRoot.transform.localRotation = Quaternion.identity;

            float length = controller.definition.bridgeLength;
            float railY = controller.definition.railHeightOffset;

            // Centerline guide marker / 3rd rail channel for future car pusher/puller moves
            LineRenderer lr = _pusherGuideRoot.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.positionCount = 2;
            lr.startWidth = 0.10f;
            lr.endWidth = 0.10f;
            lr.startColor = new Color(0.95f, 0.8f, 0.15f, 0.95f);
            lr.endColor = new Color(0.95f, 0.8f, 0.15f, 0.95f);

            var tom = TrackObjectManager.Instance;
            if (tom?.profile?.trackMaterial != null)
                lr.material = tom.profile.trackMaterial;

            lr.SetPositions(new Vector3[]
            {
                new Vector3(0f, railY + 0.18f,  length * 0.5f),
                new Vector3(0f, railY + 0.18f, -length * 0.5f)
            });
        }

        private void OnDestroy()
        {
            if (_railsRoot != null) DestroyImmediate(_railsRoot);
            if (_pusherGuideRoot != null) DestroyImmediate(_pusherGuideRoot);
        }
    }
}
