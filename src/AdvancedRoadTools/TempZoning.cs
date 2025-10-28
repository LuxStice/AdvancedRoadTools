using Unity.Entities;
using Unity.Mathematics;

namespace AdvancedRoadTools
{
    public struct TempZoning : IComponentData
    {
        public int2 Depths;
    }
}