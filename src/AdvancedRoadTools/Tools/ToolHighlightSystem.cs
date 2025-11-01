using Game;
using Game.Common;
using Game.Net;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace AdvancedRoadTools.Tools
{
    public partial class ToolHighlightSystem : GameSystemBase
    {
        private ComponentLookup<Edge> edgeLookup;
        private ComponentLookup<Highlighted> highlightedLookup;
        private EntityCommandBuffer ecb;
        private ToolOutputBarrier toolOutputBarrier;

        private NativeList<Entity> toHighlightEntities;
        private NativeList<Entity> toUnhighlightEntities;

        private EntityQuery highlightedQuery;

        protected override void OnCreate()
        {
            toolOutputBarrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>();

            edgeLookup = GetComponentLookup<Edge>(true);
            highlightedLookup = GetComponentLookup<Highlighted>(true);

            toHighlightEntities = new NativeList<Entity>(Allocator.Persistent);
            toUnhighlightEntities = new NativeList<Entity>(Allocator.Persistent);
        
            highlightedQuery = GetEntityQuery(ComponentType.ReadOnly<Highlighted>());
        }

        protected override void OnUpdate()
        {
            ecb = toolOutputBarrier.CreateCommandBuffer();
            edgeLookup.Update(this);
            highlightedLookup.Update(this);

            if (!toHighlightEntities.IsEmpty)
            {
                var job = new HighlightJob
                {
                    Entities = toHighlightEntities.AsReadOnly(),
                    HighlightedLookup = highlightedLookup,
                    EdgeLookup = edgeLookup,
                    ECB = ecb.AsParallelWriter()
                }.Schedule(toHighlightEntities.Length, 32, Dependency);
            
                Dependency = JobHandle.CombineDependencies(Dependency, job);
            }
        
            if (!toUnhighlightEntities.IsEmpty)
            {
                var job = new UnhighlightJob
                {
                    Entities = toUnhighlightEntities.AsReadOnly(),
                    HighlightedLookup = highlightedLookup,
                    EdgeLookup = edgeLookup,
                    ECB = ecb.AsParallelWriter()
                }.Schedule(toUnhighlightEntities.Length, 32, Dependency);
            
                Dependency = JobHandle.CombineDependencies(Dependency, job);
            }

            toolOutputBarrier.AddJobHandleForProducer(Dependency);
            Dependency.Complete();

            toHighlightEntities.Clear();
            toUnhighlightEntities.Clear();
        }

        public void ToggleHighlight(Entity entity)
        {
            if (highlightedLookup.HasComponent(entity))
                toUnhighlightEntities.Add(entity);
            else
                toHighlightEntities.Add(entity);
        }

        public void HighlightEntity(Entity entity, bool value)
        {
            if (value)
            {
                if (!toHighlightEntities.Contains(entity))
                    toHighlightEntities.Add(entity);
            }
            else
            {
                if (!toUnhighlightEntities.Contains(entity))
                    toUnhighlightEntities.Add(entity);
            }
        }

        private struct HighlightJob : IJobParallelFor
        {
            [ReadOnly] public ComponentLookup<Edge> EdgeLookup;
            public NativeArray<Entity>.ReadOnly Entities;
            [ReadOnly] public ComponentLookup<Highlighted> HighlightedLookup;
            public EntityCommandBuffer.ParallelWriter ECB;

            public void Execute(int index)
            {
                var entity = Entities[index];
                if (entity == Entity.Null) Mod.log.Error($"Trying to set highlights on a null entity!");

                if (!HighlightedLookup.HasComponent(entity))
                {
                    ECB.AddComponent<Highlighted>(index, entity);
                    ECB.AddComponent<BatchesUpdated>(index, entity);
                    Mod.log.Debug($"\tHighlighted {entity}");
                }
                
                if (EdgeLookup.TryGetComponent(entity, out var edge))
                {
                    ECB.AddComponent<Updated>(index, edge.m_Start);
                    ECB.AddComponent<Updated>(index, edge.m_End);
                }  
            }
        }

        private struct UnhighlightJob : IJobParallelFor
        {
            [ReadOnly] public ComponentLookup<Edge> EdgeLookup;
            public NativeArray<Entity>.ReadOnly Entities;
            [ReadOnly] public ComponentLookup<Highlighted> HighlightedLookup;
            public EntityCommandBuffer.ParallelWriter ECB;

            public void Execute(int index)
            {
                var entity = Entities[index];
                if (entity == Entity.Null) Mod.log.Error($"Trying to set highlights on a null entity!");

                if (HighlightedLookup.HasComponent(entity))
                {
                    ECB.RemoveComponent<Highlighted>(index, entity);
                    ECB.AddComponent<BatchesUpdated>(index, entity);
                    Mod.log.Debug($"\tUnhighlighted {entity}");
                }
                
                if (EdgeLookup.TryGetComponent(entity, out var edge))
                {
                    ECB.AddComponent<Updated>(index, edge.m_Start);
                    ECB.AddComponent<Updated>(index, edge.m_End);
                }
            }
        }
    }
}