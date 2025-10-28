using System;
using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace AdvancedRoadTools.Components
{
    public struct AdvancedRoad : IComponentData, IEquatable<AdvancedRoad>, ISerializable
    {
        public int2 Depths
        {
            get => new (depthLeft, depthRight);
            set
            {
                depthLeft = value.x;
                depthRight = value.y;
            }
        }
        public int depthLeft;
        public int depthRight;

        public bool Equals(AdvancedRoad other)
        {
            return other.depthLeft == depthLeft && other.depthRight == depthRight;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(depthLeft);
            writer.Write(depthRight);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out depthLeft);
            reader.Read(out depthRight);
        }
    }
}