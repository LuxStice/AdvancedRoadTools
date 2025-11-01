using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Serialization.Entities;
using Game;
using Game.Net;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Tools;
using HarmonyLib;
using Unity.Entities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AdvancedRoadTools.Tools
{
    public static class ToolsHelper
    {
        public static List<ToolDefinition> ToolDefinitions { get; set; } = new();

        private static Dictionary<ToolDefinition, Tuple<PrefabBase, UIObject>> toolsLookup = new();
        public static bool Initialized { get; private set; }

        public static bool HasTool(ToolDefinition tool) => ToolDefinitions.Contains(tool);
        public static bool HasTool(string toolId) => ToolDefinitions.Exists(t => t.ToolID == toolId);

        private static World world;
        private static PrefabSystem prefabSystem;
        private static PrefabBase originalPrefab;
        private static List<PrefabBase> prefabs => Traverse.Create(prefabSystem).Field<List<PrefabBase>>("m_Prefabs").Value;

        private static UIObject originalUiObject;


        public static void RegisterTool(ToolDefinition toolDefinition)
        {
            if (HasTool(toolDefinition))
            {
                Mod.log.Error($"Tool \"{toolDefinition.ToolID}\" already registered");
                return;
            }

            Mod.log.Info($"Registering tool \"{toolDefinition.ToolID}\" with system \"{toolDefinition.Type.Name}\"");
            ToolDefinitions.Add(toolDefinition);
        }

        public static void Initialize(bool force = false)
        {
            if (Initialized && !force)
            {
                Mod.log.Info($"Trying to initialize ToolsHelper but it is already running.");
                return;
            }

            ToolDefinitions = new(8);
            toolsLookup = new(8);
            Initialized = true;
        }

        public static void InstantiateTools()
        {
            if (Initialized) return;

            Mod.log.Info($"Creating tools UI. {ToolDefinitions.Count} registered tools");

            world = Traverse.Create(GameManager.instance).Field<World>("m_World").Value;
            prefabSystem = world.GetExistingSystemManaged<PrefabSystem>();

            originalPrefab = prefabs.FirstOrDefault(p => p.name == "Wide Sidewalk");
            if (originalPrefab is null)
            {
                Mod.log.Error($"Could not find Wide Sidewalk Prefab");
                return;
            }

            originalUiObject = originalPrefab?.GetComponent<UIObject>();
            if (originalUiObject is null)
            {
                Mod.log.Error($"Could not find Wide Sidewalk UI Object");
                return;
            }

            // Getting the original Grass Prefab's PlaceableNetData
            // var originalUpgradePrefabData = prefabSystem.GetComponentData<PlaceableNetData>(originalPrefab);

            foreach (var definition in ToolDefinitions)
            {
                try
                {
                    var toolPrefab = Object.Instantiate(originalPrefab);

                    toolPrefab.name = definition.ToolID;

                    toolPrefab.Remove<UIObject>();
                    toolPrefab.Remove<Unlockable>();
                    toolPrefab.Remove<NetSubObjects>();

                    var uiObject = ScriptableObject.CreateInstance<UIObject>();
                    uiObject.m_Icon = definition.ui.ImagePath;
                    uiObject.name = definition.ToolID;
                    uiObject.m_IsDebugObject = originalUiObject.m_IsDebugObject;
                    uiObject.m_Priority = definition.Priority;
                    uiObject.m_Group = originalUiObject.m_Group;
                    uiObject.active = originalUiObject.active;
                    toolPrefab.AddComponentFrom(uiObject);

                    var netUpgrade = ScriptableObject.CreateInstance<NetUpgrade>();
                    toolPrefab.AddComponentFrom(netUpgrade);

                    var tool = world.GetOrCreateSystemManaged(definition.Type) as ToolBaseSystem;

                    if (!tool.TrySetPrefab(toolPrefab))
                    {
                        Mod.log.Error($"Failed to set up tool prefab for type \"{definition.Type}\"");
                        continue;
                    }

                    if (!prefabSystem.AddPrefab(toolPrefab))
                    {
                        Mod.log.Error($"Tool \"{definition.ToolID}\" could not be added to \"{nameof(PrefabSystem)}\"");
                        continue;
                    }

                    toolsLookup.Add(definition, new Tuple<PrefabBase, UIObject>(toolPrefab, uiObject));
                    Mod.log.Info($"\tTool \"{definition.ToolID}\" was successfully created");
                }
                catch (Exception e)
                {
                    Mod.log.Error($"\tTool \"{definition.ToolID}\" could not be created: {e}");
                }

                GameManager.instance.onGameLoadingComplete += SetupUpTools;
            }
        }

        private static void SetupUpTools(Purpose purpose, GameMode mode)
        {
            Mod.log.Info($"Setting up tools. {toolsLookup.Count} registered tools");
        
            foreach (var kvp in toolsLookup)
            {
                var toolDefinition = kvp.Key;
                var prefab = kvp.Value.Item1;
                var uiObject = kvp.Value.Item2;
                try
                {
                    var placeableNetData = prefabSystem.GetComponentData<PlaceableNetData>(originalPrefab);
                    placeableNetData.m_SetUpgradeFlags = toolDefinition.SetFlags;
                    placeableNetData.m_UnsetUpgradeFlags = toolDefinition.UnsetFlags;
                    placeableNetData.m_PlacementFlags = toolDefinition.PlacementFlags;
                
                    if(toolDefinition.Underground)
                        placeableNetData.m_PlacementFlags |= PlacementFlags.UndergroundUpgrade;

                    prefabSystem.AddComponentData(prefab, placeableNetData);
                }
                catch (Exception e)
                {
                    Mod.log.Error($"\tCould not setup tool {toolDefinition.ToolID}: {e}");
                }
            }
        }
    }
}