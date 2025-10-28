using System;

namespace AdvancedRoadTools
{
    [Flags]
    public enum ZoningMode
    {
        None = 0,
        Right = 1,
        Left = 2,
        Both = Right | Left
    }
}