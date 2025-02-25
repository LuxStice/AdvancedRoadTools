﻿using System.Collections.Generic;
using AdvancedRoadTools.Tools;
using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Input;
using Game.Modding;
using Game.Settings;

namespace AdvancedRoadTools;

[FileLocation(nameof(AdvancedRoadTools))]
[SettingsUIGroupOrder(kToggleGroup)]
[SettingsUIShowGroupName(kToggleGroup)]
[SettingsUIMouseAction(AdvancedRoadToolsMod.kInvertZoningActionName, ActionType.Button,
    usages: ["Zone Controller Tool"])]
public class Setting : ModSetting
{
    public const string kSection = "Main";

    public const string kToggleGroup = "Zone Controller Tool";
    public const string kInvertZoningAction = "InvertZoning";

    public Setting(IMod mod) : base(mod)
    {
    }

    [SettingsUISection(kSection, kToggleGroup)]
    public bool RemoveZonedCells { get; set; } = true;

    [SettingsUISection(kSection, kToggleGroup)]
    //[SettingsUIDisableByCondition(typeof(Setting), nameof(IfRemoveZonedCells))]
    public bool RemoveOccupiedCells { get; set; } = true;
    
    [SettingsUIMouseBinding(BindingMouse.Right, kInvertZoningAction)]
    [SettingsUISection(kSection, kToggleGroup)]
    public ProxyBinding InvertZoning { get; set; }

    public override void SetDefaults()
    {
        RemoveOccupiedCells = true;
        RemoveZonedCells = true;
        InvertZoning = new ProxyBinding{};
    }

    private bool IfRemoveZonedCells() => !RemoveZonedCells;
}

public class LocaleEN : IDictionarySource
{
    private readonly Setting m_Setting;

    public LocaleEN(Setting setting)
    {
        m_Setting = setting;
    }

    public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors,
        Dictionary<string, int> indexCounts)
    {
        return new Dictionary<string, string>
        {
            { m_Setting.GetSettingsLocaleID(), "Advanced Road Tools" },
            { m_Setting.GetOptionTabLocaleID(Setting.kSection), "Main" },

            { m_Setting.GetOptionGroupLocaleID(Setting.kToggleGroup), "Zone Controller Tool Options" },

            { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RemoveZonedCells)), "Prevent zoned cells from being removed" },
            { m_Setting.GetOptionDescLocaleID(nameof(Setting.RemoveZonedCells)), "Prevent zoned cells from being overriden during preview and set phase of Zone Controller Tool." +
                "\nSet this to true if you're having problem with losing your zoning configuration when using the tool." +
                "\nDefault: true" },
            { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RemoveOccupiedCells)), "Prevent occupied cells from being removed" },
            { m_Setting.GetOptionDescLocaleID(nameof(Setting.RemoveOccupiedCells)), "Prevent occupied cells from being overriden during preview and set phase of Zone Controller Tool." +
                "\nSet this to true if you're having problem with buildings becoming vacant and/or abandoned when using the tool." +
                "\nDefault: true" },
            { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InvertZoning)), "Invert Zoning Mouse Button" },
            { m_Setting.GetOptionDescLocaleID(nameof(Setting.InvertZoning)), "Inverts the current zoning configuration with a mouse action." },
            { $"Assets.NAME[{ZoningControllerToolSystem.ToolID}]","Zone Controller" },
            { $"Assets.DESCRIPTION[{ZoningControllerToolSystem.ToolID}]", "Tool to control how the zoning of a road behaves.\nChoose between zoning on both sides, only on the left or right, or no zoning for that road.\nBy default, right-click inverts the zoning configuration."},
            
            { $"Assets.NAME[{AdvancedParallelToolSystem.ToolID}]","Parallel Tool" },
            { $"Assets.DESCRIPTION[{AdvancedParallelToolSystem.ToolID}]", "WIP"}
        };
    }

    public void Unload()
    {
    }
}