using System;
using AdvancedRoadTools.Components;
using Colossal.Mathematics;
using Game;
using Game.Common;
using Game.Rendering;
using Game.Simulation;
using Game.Zones;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace AdvancedRoadTools
{
    public partial class RenderZoneControllerToolSystem : GameSystemBase
    {
        private EntityQuery UpdatedBlocksQuery;

        private ModificationBarrier4B modificationBarrier;
        private OverlayRenderSystem overlaySystem;
        private TerrainSystem terrainSystem;

        private EntityQuery tempZoningQuery;


        protected override void OnCreate()
        {
            base.OnCreate();

            tempZoningQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<TempZoning>()
                .Build(this);

            modificationBarrier = World.GetOrCreateSystemManaged<ModificationBarrier4B>();
            overlaySystem = World.GetOrCreateSystemManaged<OverlayRenderSystem>();
            terrainSystem = World.GetOrCreateSystemManaged<TerrainSystem>();

            RequireAnyForUpdate(tempZoningQuery);
        }

        protected override void OnUpdate()
        {
            var ecb = modificationBarrier.CreateCommandBuffer();

            var tempZoningEntities = tempZoningQuery.ToEntityArray(Allocator.TempJob);
            var drawOverlayJob = new DrawAffectedBlocksJob
            {
                TransformLookup = GetComponentLookup<Game.Objects.Transform>(true),
                Entities = tempZoningEntities.AsReadOnly(),
                BlockLookup = GetComponentLookup<Block>(true),
                overlayBuffer = overlaySystem.GetBuffer(out var inputDeps),
                heightData = terrainSystem.GetHeightData(),

            }.Schedule(tempZoningEntities.Length, 32, inputDeps);
            inputDeps = JobHandle.CombineDependencies(inputDeps, drawOverlayJob);

            modificationBarrier.AddJobHandleForProducer(Dependency);
        }

        public struct DrawAffectedBlocksJob : IJobParallelFor
        {
            [ReadOnly] public ComponentLookup<Game.Objects.Transform> TransformLookup;
            [ReadOnly] public NativeArray<Entity>.ReadOnly Entities;
            [ReadOnly] public ComponentLookup<Block> BlockLookup;
            [ReadOnly] public OverlayRenderSystem.Buffer overlayBuffer;
            [ReadOnly] public TerrainHeightData heightData;


            public void Execute(int index)
            {
                var entity = Entities[index];
                BlockLookup.TryGetComponent(entity, out var block);
                var transform = TransformLookup[entity];
                var forward = math.forward(transform.m_Rotation);
                var right = math.right() * forward;

                float3 a, b, c, d;
                a= transform.m_Position;
                b = transform.m_Position + (block.m_Size.x * forward * ZoneUtils.CELL_SIZE);
                c = transform.m_Position + (block.m_Size.x * forward * ZoneUtils.CELL_SIZE) + (block.m_Size.y * right * ZoneUtils.CELL_SIZE);
                d = transform.m_Position + (block.m_Size.y * right * ZoneUtils.CELL_SIZE);

                var lineColor = UnityEngine.Color.red;

                overlayBuffer.DrawLine(lineColor, lineColor, 0.0f, OverlayRenderSystem.StyleFlags.Projected, new Line3.Segment(a,b), .5f, new float2(0f, 1f));
                overlayBuffer.DrawLine(lineColor, lineColor, 0.0f, OverlayRenderSystem.StyleFlags.Projected, new Line3.Segment(b,c), .5f, new float2(0f, 1f));
                overlayBuffer.DrawLine(lineColor, lineColor, 0.0f, OverlayRenderSystem.StyleFlags.Projected, new Line3.Segment(c,d), .5f, new float2(0f, 1f));
                overlayBuffer.DrawLine(lineColor, lineColor, 0.0f, OverlayRenderSystem.StyleFlags.Projected, new Line3.Segment(d,a), .5f, new float2(0f, 1f));
            }
        }
    }
}