using System;
using AdvancedRoadTools.Components;
using Game;
using Game.Common;
using Game.Zones;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace AdvancedRoadTools
{
    public partial class SyncBlockSystem : GameSystemBase
    {
        private EntityQuery UpdatedBlocksQuery;

        private ModificationBarrier4B _modificationBarrier;


        protected override void OnCreate()
        {
            base.OnCreate();

            UpdatedBlocksQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAllRW<Block, ValidArea>()
                .WithAll<Owner, Updated>()
                .Build(this);

            _modificationBarrier = World.GetOrCreateSystemManaged<ModificationBarrier4B>();
        }

        protected override void OnUpdate()
        {
            if (UpdatedBlocksQuery.IsEmpty) return;
        
            //AdvancedRoadToolsMod.log.Debug(
            //    $"[{nameof(SyncBlockSystem)}] Synchronizing Blocks and Valid Areas of {UpdatedBlocksQuery.CalculateEntityCount()} blocks.");

            var ecb = _modificationBarrier.CreateCommandBuffer();

            var updatedBlocks = UpdatedBlocksQuery.ToEntityArray(Allocator.TempJob);

            var syncBlockJob = new SyncBlockJob
            {
                ECB = ecb.AsParallelWriter(),
                Entities = updatedBlocks.AsReadOnly(),
                BlockLookup = GetComponentLookup<Block>(true),
                ValidAreaLookup = GetComponentLookup<ValidArea>(true),
                OwnerLookup = GetComponentLookup<Owner>(true),
                CellLookup = GetBufferLookup<Cell>(true),
                AdvancedRoadLookup = GetComponentLookup<AdvancedRoad>(true),
                TempZoningLookup = GetComponentLookup<TempZoning>(true),
            }.Schedule(UpdatedBlocksQuery.CalculateEntityCount(), 32, Dependency);
            updatedBlocks.Dispose(syncBlockJob);
        
            Dependency = JobHandle.CombineDependencies(Dependency, syncBlockJob);

            _modificationBarrier.AddJobHandleForProducer(Dependency);
        }

        public struct SyncBlockJob : IJobParallelFor
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            public NativeArray<Entity>.ReadOnly Entities;
            [ReadOnly] public ComponentLookup<Block> BlockLookup;
            [ReadOnly] public ComponentLookup<ValidArea> ValidAreaLookup;
            [ReadOnly] public BufferLookup<Cell> CellLookup;
            [ReadOnly] public ComponentLookup<Owner> OwnerLookup;
            [ReadOnly] public ComponentLookup<AdvancedRoad> AdvancedRoadLookup;
            [ReadOnly] public ComponentLookup<TempZoning> TempZoningLookup;

            public void Execute(int index)
            {
                var blockEntity = Entities[index];
                var block = BlockLookup[blockEntity];
                var validArea = ValidAreaLookup[blockEntity];

                if (!OwnerLookup.TryGetComponent(blockEntity, out var owner))
                    throw new NullReferenceException($"Block {blockEntity} has no owner assigned.");

                var roadEntity = owner.m_Owner;

                var left = (math.dot(1, block.m_Direction) < 0);

                int depth;
                if (TempZoningLookup.TryGetComponent(roadEntity, out var tempZoning))
                    depth = left ? tempZoning.Depths.x : tempZoning.Depths.y;
                else if (AdvancedRoadLookup.TryGetComponent(roadEntity, out var data))
                    depth = left ? data.Depths.x : data.Depths.y;
                else
                    return;

                if (AdvancedRoadToolsMod.Setting.RemoveOccupiedCells && IsAnyCellOccupied(CellLookup[blockEntity], block, validArea))
                    return;
                if (AdvancedRoadToolsMod.Setting.RemoveZonedCells && IsAnyCellZoned(CellLookup[blockEntity], block, validArea))
                    return;

                block.m_Size.y = depth;
                ECB.SetComponent(index, blockEntity, block);
                validArea.m_Area.w = depth;
                ECB.SetComponent(index, blockEntity, validArea);
            }

            private bool IsAnyCellOccupied(in DynamicBuffer<Cell> cells, in Block block, in ValidArea validArea)
            {
                if (validArea.m_Area.y * validArea.m_Area.w == 0)
                {
                    return false;
                }

                for (int z = validArea.m_Area.z; z < validArea.m_Area.w; z++)
                {
                    for (int x = validArea.m_Area.x; x < validArea.m_Area.y; x++)
                    {
                        int index = z * block.m_Size.x + x;
                        Cell cell = cells[index];

                        if ((cell.m_State & CellFlags.Occupied) != 0)
                            return true;
                    }
                }

                return false;
            }
        
            private bool IsAnyCellZoned(in DynamicBuffer<Cell> cells, in Block block, in ValidArea validArea)
            {
                if (validArea.m_Area.y * validArea.m_Area.w == 0)
                {
                    return false;
                }

                for (int z = validArea.m_Area.z; z < validArea.m_Area.w; z++)
                {
                    for (int x = validArea.m_Area.x; x < validArea.m_Area.y; x++)
                    {
                        int index = z * block.m_Size.x + x;
                        Cell cell = cells[index];
                    
                        if (cell.m_Zone.m_Index != ZoneType.None.m_Index)
                            return true;
                    }
                }

                return false;
            }
        }
    }
}