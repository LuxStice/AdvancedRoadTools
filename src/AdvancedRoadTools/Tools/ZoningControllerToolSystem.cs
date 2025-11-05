using AdvancedRoadTools.Components;
using Colossal.Serialization.Entities;
using Game;
using Game.Audio;
using Game.Common;
using Game.Input;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using Game.Zones;
using JetBrains.Annotations;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using PlacementFlags = Game.Net.PlacementFlags;

namespace AdvancedRoadTools.Tools
{
    [BurstCompile]
    public partial class ZoningControllerToolSystem : ToolBaseSystem, IARTTool
    {
        private ToolOutputBarrier toolOutputBarrier;
        private ZoningControllerToolUISystem zoningControllerToolUISystem;
        private ToolHighlightSystem toolHighlightSystem;

        private IProxyAction invertZoningAction;

        public const string ToolID = "Zone Controller Tool";
        public override string toolID => ToolID;

        private ComponentLookup<AdvancedRoad> advancedRoadLookup;
        private BufferLookup<SubBlock> subBlockLookup;
        private int2 Depths => zoningControllerToolUISystem.ToolDepths;
        [PublicAPI]
        private ZoningMode ZoningMode => zoningControllerToolUISystem.ToolZoningMode;
        private EntityQuery tempZoningQuery;
        private EntityQuery soundbankQuery;
        private PrefabBase toolPrefab;

        private NativeList<Entity> selectedEntities;

        [BurstCompile]
        protected override void OnCreate()
        {
            Mod.log.Debug($"{nameof(ZoningControllerToolSystem)}.{System.Reflection.MethodBase.GetCurrentMethod()?.Name}");
            base.OnCreate();
            toolOutputBarrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>();
            zoningControllerToolUISystem = World.GetOrCreateSystemManaged<ZoningControllerToolUISystem>();
            toolHighlightSystem = World.GetOrCreateSystemManaged<ToolHighlightSystem>();

            invertZoningAction = Mod.InvertZoningAction;

            tempZoningQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<TempZoning>()
                .Build(this);

            soundbankQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ToolUXSoundSettingsData>()
                .Build(this);

            advancedRoadLookup = GetComponentLookup<AdvancedRoad>(true);
            subBlockLookup = GetBufferLookup<SubBlock>(true);

            selectedEntities = new NativeList<Entity>(Allocator.Persistent);

            var definition = new ToolDefinition(typeof(ZoningControllerToolSystem), toolID, 59, new ToolDefinition.UI
            {
                ImagePath = ToolDefinition.UI.PathPrefix + "ToolsIcon.png",
            })
            {
                PlacementFlags = PlacementFlags.UndergroundUpgrade
            };

            ToolsHelper.RegisterTool(definition);
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            Mod.log.Debug($"{nameof(ZoningControllerToolSystem)}.{System.Reflection.MethodBase.GetCurrentMethod()?.Name}");
            base.OnGameLoadingComplete(purpose, mode);

            m_ToolSystem.tools.Remove(this);
            m_ToolSystem.tools.Insert(5, this);
        }

        /// <inheritdoc/>
        protected override void OnStartRunning()
        {
            Mod.log.Debug($"{nameof(ZoningControllerToolSystem)}.{System.Reflection.MethodBase.GetCurrentMethod()?.Name}");
            base.OnStartRunning();
            applyAction.shouldBeEnabled = true;
            invertZoningAction.shouldBeEnabled = true;
            requireZones = true;
            requireNet = Layer.Road;
            allowUnderground = true;
        }

        /// Cleans up actions or whatever else you want to happen when your tool becomes inactive.
        protected override void OnStopRunning()
        {
            Mod.log.Debug($"{nameof(ZoningControllerToolSystem)}.{System.Reflection.MethodBase.GetCurrentMethod()?.Name}");
            base.OnStartRunning();
            applyAction.shouldBeEnabled = false;
            invertZoningAction.shouldBeEnabled = false;
            requireZones = false;
            requireNet = Layer.None;
            allowUnderground = false;
        }

        public enum Mode
        {
            None,
            Select,
            Apply,
            Cancel,
            Preview
        }

        private Mode mode;
        private Entity previewEntity;

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps = Dependency;
            advancedRoadLookup.Update(this);
            subBlockLookup.Update(this);

            var ecb = toolOutputBarrier.CreateCommandBuffer();
            bool hasHit = GetRaycastResult(out Entity e, out RaycastHit hit);
            var soundbank = soundbankQuery.GetSingleton<ToolUXSoundSettingsData>();

            if (cancelAction.WasPressedThisFrame()) mode = Mode.Cancel;
            else if (applyAction.WasPressedThisFrame() || applyAction.IsPressed()) mode = Mode.Select;
            else if (applyAction.WasReleasedThisFrame() && hasHit) mode = Mode.Apply;
            else if (applyAction.WasReleasedThisFrame() && !hasHit) mode = Mode.Cancel;
            else mode = Mode.Preview;

            switch (mode)
            {
                case Mode.Preview:
                    if (previewEntity != e)
                    {
                        if (previewEntity != Entity.Null)
                        {
                            toolHighlightSystem.HighlightEntity(previewEntity, false);
                            selectedEntities.Clear();
                            previewEntity = Entity.Null;
                        }

                        if (hasHit)
                        {
                            toolHighlightSystem.HighlightEntity(e, true);
                            selectedEntities.Add(e);
                            previewEntity = e;
                        }
                    }
                    break;
                case Mode.Select when hasHit:
                    if (!selectedEntities.Contains(e))
                    {
                        selectedEntities.Add(e);
                        toolHighlightSystem.HighlightEntity(e, true);

                        AudioManager.instance.PlayUISound(soundbank.m_SelectEntitySound);
                    }

                    break;
                case Mode.Apply:
                    var setAdvancedRoadJob = new SetAdvancedRoadJob
                    {
                        TempZoningLookup = GetComponentLookup<TempZoning>(true),
                        Entities = selectedEntities.AsArray().AsReadOnly(),
                        ECB = ecb
                    }.Schedule(inputDeps);
                    inputDeps = JobHandle.CombineDependencies(inputDeps, setAdvancedRoadJob);

                    foreach (var selectedEntity in selectedEntities)
                    {
                        toolHighlightSystem.HighlightEntity(selectedEntity, false);
                    }
                    selectedEntities.Clear();

                    AudioManager.instance.PlayUISound(soundbank.m_NetBuildSound);
                    break;
                case Mode.Cancel:
                    foreach (var selectedEntity in selectedEntities)
                    {
                        toolHighlightSystem.HighlightEntity(selectedEntity, false);
                    }

                    AudioManager.instance.PlayUISound(soundbank.m_NetCancelSound);
                    selectedEntities.Clear();
                    break;
                default:

                    break;
            }

            if (invertZoningAction.WasPressedThisFrame())
            {
                zoningControllerToolUISystem.InvertZoningMode();
            }

            var syncTempJob = new SyncTempJob
            {
                ECB = toolOutputBarrier.CreateCommandBuffer().AsParallelWriter(),
                TempZoningLookup = GetComponentLookup<TempZoning>(true),
                SelectedEntities = selectedEntities.AsArray().AsReadOnly(),
                Depths = Depths
            }.Schedule(selectedEntities.Length, 32, inputDeps);

            inputDeps = JobHandle.CombineDependencies(inputDeps, syncTempJob);


            var tempZoningEntities = tempZoningQuery.ToEntityArray(Allocator.TempJob);;

            var cleanupTempJob = new CleanupTempJob
            {
                ECB = toolOutputBarrier.CreateCommandBuffer().AsParallelWriter(),
                SelectedEntities = selectedEntities.AsArray().AsReadOnly(),
                Entities = tempZoningEntities.AsReadOnly()
            }.Schedule(tempZoningEntities.Length, 32, inputDeps);
            inputDeps = JobHandle.CombineDependencies(inputDeps, cleanupTempJob);

            toolOutputBarrier.AddJobHandleForProducer(inputDeps);
            return inputDeps;
        }

        private bool ShouldHighlight(Entity entity)
        {
            if (advancedRoadLookup.TryGetComponent(entity, out var data))
            {
                return math.any(Depths != data.Depths);
            }
            else
            {
                return math.any(Depths != new int2(6));
            }
        }

        private new bool GetRaycastResult(out Entity entity, out RaycastHit hit)
        {
            if (!base.GetRaycastResult(out entity, out hit)) return false;

            var hasAdvancedRoad = advancedRoadLookup.TryGetComponent(entity, out var data);
            var hasSubBlock = subBlockLookup.TryGetBuffer(entity, out _);

            if (!hasSubBlock)
            {
                entity = Entity.Null;
                return false;
            }

            switch (hasAdvancedRoad)
            {
                case true
                    when math.any(Depths != data.Depths)
                    : //if it has advanced data and depths are different from current depths
                case false
                    when math.any(Depths != new int2(6))
                    : //if it doesn't have advanced data and depths are the game's defaults
                    return true;
            }

            entity = Entity.Null;
            return false;
        }

        public void SetPrefab(PrefabBase prefab) => toolPrefab = prefab;

        public override PrefabBase GetPrefab()
        {
            return toolPrefab;
        }

        /// <inheritdoc/>
        public override bool TrySetPrefab(PrefabBase prefab)
        {
            if (prefab.name != toolID)
                return false;

            Mod.log.Debug($"{toolID}:Selected");
            toolPrefab = prefab;
            return true;
        }

        public override void InitializeRaycast()
        {
            base.InitializeRaycast();

            m_ToolRaycastSystem.typeMask = TypeMask.Net;
            m_ToolRaycastSystem.netLayerMask = Layer.Road;
        }

        public void SetToolEnabled(bool isEnabled)
        {
            if (isEnabled && m_ToolSystem.activeTool != this)
            {
                m_ToolSystem.ActivatePrefabTool(GetPrefab());
            }
            else if (!isEnabled && m_ToolSystem.activeTool == this)
            {
                m_ToolSystem.ActivatePrefabTool(null);
            }
        }

        /// <summary>
        /// Updates the highlighted entities and sets them up to preview what the changes will be like
        /// </summary>
        public struct SyncTempJob : IJobParallelFor
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            public ComponentLookup<TempZoning> TempZoningLookup;
            public NativeArray<Entity>.ReadOnly SelectedEntities;
            public int2 Depths;

            public void Execute(int index)
            {
                var entity = SelectedEntities[index];

                if (TempZoningLookup.TryGetComponent(entity, out var data) && math.any(data.Depths != Depths))
                {
                    ECB.SetComponent(index, entity, new TempZoning
                    {
                        Depths = Depths
                    });
                }
                else
                {
                    ECB.AddComponent(index, entity, new TempZoning
                    {
                        Depths = Depths
                    });
                }
                ECB.AddComponent<Updated>(index, entity);
            }
        }

        public struct CleanupTempJob : IJobParallelFor
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            public NativeArray<Entity>.ReadOnly SelectedEntities;
            public NativeArray<Entity>.ReadOnly Entities;

            public void Execute(int index)
            {
                var entity = Entities[index];

                if (SelectedEntities.Contains(entity)) return;

                ECB.RemoveComponent<TempZoning>(index, entity);
                ECB.AddComponent<Updated>(index, entity);
            }
        }

        /// <summary>
        /// Sets the previewed zoning
        /// </summary>
        public struct SetAdvancedRoadJob : IJob
        {
            public NativeArray<Entity>.ReadOnly Entities;
            public ComponentLookup<TempZoning> TempZoningLookup;
            public EntityCommandBuffer ECB;

            public void Execute()
            {
                Mod.log.Debug($"[{nameof(SetAdvancedRoadJob)}.Execute()]");

                foreach (var entity in Entities)
                {
                    if (!TempZoningLookup.TryGetComponent(entity, out var tempZoning)) return;

                    var newDepth = tempZoning.Depths;

                    ECB.RemoveComponent<TempZoning>(entity);
                    ECB.AddComponent(entity,
                        new AdvancedRoad { Depths = newDepth });
                }
            }
        }
    }
}