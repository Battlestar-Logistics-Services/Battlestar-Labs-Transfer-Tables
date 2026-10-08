using System.IO;
using System.Reflection;
using HarmonyLib;
using Track;
using UnityModManagerNet;
using UnityEngine;

namespace BattlestarLabs.TransferTable
{
    public static class Main
    {
        public static UnityModManager.ModEntry ModEntry { get; private set; }
        public static bool Enabled { get; private set; }
        private static AssetBundle _tableBundle;
        private static int _spawnCounter = 0;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModEntry = modEntry;
            modEntry.OnToggle = OnToggle;
            modEntry.OnUpdate = OnUpdate;

            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            modEntry.Logger.Log("[Battlestar Labs] Transfer Table Mod (Native Edition) Loaded Successfully!");
            return true;
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            Enabled = value;
            return true;
        }

        private static void OnUpdate(UnityModManager.ModEntry modEntry, float deltaTime)
        {
            if (!Enabled) return;

            // Press Numpad * to spawn a Battlestar Labs Transfer Table!
            if (Input.GetKeyDown(KeyCode.KeypadDivide))
            {
                SpawnNativeTransferTable();
            }
        }

        public static GameObject SpawnNativeTransferTable()
        {
            if (Camera.main == null)
            {
                ModEntry.Logger.Log("[Battlestar Labs] Cannot spawn: Camera not found!");
                return null;
            }

            // Raycast down to find exact ground height
            Vector3 aimPos = Camera.main.transform.position + Camera.main.transform.forward * 20f;
            Vector3 rayStart = new Vector3(aimPos.x, aimPos.y + 100f, aimPos.z);
            float groundY = aimPos.y;

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 300f))
            {
                groundY = hit.point.y;
            }

            Vector3 spawnPos = new Vector3(aimPos.x, groundY, aimPos.z);
            Quaternion spawnRot = Quaternion.Euler(0f, Camera.main.transform.eulerAngles.y, 0f);

            _spawnCounter++;
            string id = $"BST_TransferTable_{_spawnCounter:D2}";
            return CreateTransferTableInstance(id, spawnPos, spawnRot);
        }

        public static GameObject CreateTransferTableInstance(string id, Vector3 position, Quaternion rotation)
        {
            ModEntry.Logger.Log($"[Battlestar Labs] Spawning native Transfer Table: {id} at {position}");

            // 1. Root GameObject parented to Railroader's master Graph.Shared!
            GameObject root = new GameObject(id);
            if (Graph.Shared != null)
            {
                root.transform.SetParent(Graph.Shared.transform);
            }
            root.transform.position = position;
            root.transform.rotation = rotation;

            // 2. Load the AssetBundle if not already loaded
            if (_tableBundle == null)
            {
                string bundlePath = Path.Combine(ModEntry.Path, "SCAssetPacks", "battlestarlabs_transfertables", "Bundle");
                if (File.Exists(bundlePath))
                {
                    _tableBundle = AssetBundle.LoadFromFile(bundlePath);
                    ModEntry.Logger.Log("[Battlestar Labs] AssetBundle loaded successfully!");
                }
                else
                {
                    ModEntry.Logger.Log($"[Battlestar Labs] ERROR: AssetBundle not found at {bundlePath}");
                }
            }

            // 3. Instantiate the 3D model
            Transform bridgeRoot = null;
            if (_tableBundle != null)
            {
                GameObject prefab = null;
                // Try direct load or load the first GameObject asset found in bundle
                string[] allAssets = _tableBundle.GetAllAssetNames();
                foreach (string name in allAssets)
                {
                    ModEntry.Logger.Log($"[Battlestar Labs] Asset in bundle: {name}");
                    if (name.ToLower().EndsWith(".prefab"))
                    {
                        prefab = _tableBundle.LoadAsset<GameObject>(name);
                        break;
                    }
                }

                if (prefab != null)
                {
                    GameObject visualInstance = Object.Instantiate(prefab, root.transform);
                    visualInstance.name = "VisualModel";
                    visualInstance.transform.localPosition = Vector3.zero;
                    visualInstance.transform.localRotation = Quaternion.identity;

                    if (visualInstance != null)
                    {
                        foreach (Transform t in visualInstance.GetComponentsInChildren<Transform>(true))
                        {
                            if (t != visualInstance.transform && (t.name.ToLower().Contains("bridge") || t.name.Equals("Bridge_Root", System.StringComparison.OrdinalIgnoreCase)))
                            {
                                bridgeRoot = t;
                                break;
                            }
                        }
                    }
                    ModEntry.Logger.Log($"[Battlestar Labs] Instantiated 3D model: {visualInstance?.name}, Bridge Transform: {(bridgeRoot != null ? bridgeRoot.name : "NOT FOUND")}");
                }
                else
                {
                    ModEntry.Logger.Log("[Battlestar Labs] ERROR: No prefab could be loaded from bundle!");
                }
            }

            // 4. Controller
            var controller = root.AddComponent<TransferTableController>();
            controller.tableId = id;
            controller.bridgeTransform = bridgeRoot;

            // 5. Adapter
            var adapter = root.AddComponent<TransferTableGraphAdapter>();
            adapter.controller = controller;

            // 6. Visuals (procedural rails on bridge deck)
            var visuals = root.AddComponent<TransferTableVisuals>();
            visuals.controller = controller;

            // 7. Input
            var input = root.AddComponent<TransferTableInput>();
            input.controller = controller;
            input.adapter = adapter;

            // 8. Placement & Repositioning
            var placement = root.AddComponent<TransferTablePlacement>();
            placement.controller = controller;
            placement.adapter = adapter;

            // Enter placement mode for interactive positioning
            placement.StartPlacement(true);

            ModEntry.Logger.Log($"[Battlestar Labs] Table {id} spawned in Placement Mode!");
            return root;
        }
    }
}