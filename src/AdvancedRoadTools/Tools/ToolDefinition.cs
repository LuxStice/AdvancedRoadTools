using System;
using System.Collections.Generic;
using Game.Net;
using Game.Prefabs;

namespace AdvancedRoadTools.Tools
{
    public struct ToolDefinition : IEquatable<ToolDefinition>
    {
        public Type Type;
        public string ToolID;
        public int Priority;
        public bool Underground;
        public UI ui;
        public PlacementFlags PlacementFlags;
        public CompositionFlags SetFlags;
        public CompositionFlags UnsetFlags;
        public IEnumerable<NetPieceRequirements> SetState;
        public IEnumerable<NetPieceRequirements> UnsetState;

        public ToolDefinition(Type toolSystemType,string toolId, UI ui) : this(toolSystemType, toolId, 60, ui)
        {
        
        }

        public ToolDefinition(Type toolSystemType, string toolId, int priority = 60, UI ui = default)
        {
            Type = toolSystemType;
            ToolID = toolId;
            Priority = priority;
            this.ui = ui;
            PlacementFlags = default(PlacementFlags);
            SetFlags = default;
            UnsetFlags = default;
            Underground = false;
            SetState = null;
            UnsetState = null;
        }

        public struct UI
        {
            public const string PathPrefix = "coui://ui-mods/images/";
            public const string ImageFormat = ".svg";
            public string ImagePath;

            public UI(string imagePath)
            {
                ImagePath = imagePath;
            }
        }


        public bool Equals(ToolDefinition other)
        {
            return ToolID == other.ToolID;
        }

        public override bool Equals(object obj)
        {
            return obj is ToolDefinition other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (ToolID != null ? ToolID.GetHashCode() : 0);
        }
    }
}