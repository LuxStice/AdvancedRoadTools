using System.Collections.Generic;
using System.Linq;
using AdvancedRoadTools.Tools;
using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Input;
using Game.Modding;
using Game.Settings;

namespace AdvancedRoadTools
{
    [FileLocation(nameof(AdvancedRoadTools))]
    [SettingsUIGroupOrder(kToggleGroup)]
    [SettingsUIShowGroupName(kToggleGroup)]
    [SettingsUIMouseAction(AdvancedRoadToolsMod.kInvertZoningActionName, ActionType.Button, "Zone Controller Tool")]
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

    public class Locale : IDictionarySource
    {
        public readonly string LocaleID;
        private readonly Setting setting;
        public Dictionary<string, string> Entries;

        public Locale(string localeID, Setting setting)
        {
            LocaleID = localeID;
            this.setting = setting;
        }

        public override string ToString() => $"[ART.Locale] {LocaleID}; Entries: {(Entries is null ? "null" : $"{Entries.Count}")}";

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors,
            Dictionary<string, int> indexCounts)
        {
            if (Entries is not null && Entries.Count > 0)
            {
                return Entries.ToDictionary(pair => pair.Key, pair => pair.Value);
            }
            else 
                return new Dictionary<string, string>
                {
                    { setting.GetSettingsLocaleID(), "Advanced Road Tools" },
                    { setting.GetOptionTabLocaleID(Setting.kSection), "Main" },

                    { setting.GetOptionGroupLocaleID(Setting.kToggleGroup), "Zone Controller Tool Options" },

                    {
                        setting.GetOptionLabelLocaleID(nameof(Setting.RemoveZonedCells)),
                        "Prevent zoned cells from being removed"
                    },
                    {
                        setting.GetOptionDescLocaleID(nameof(Setting.RemoveZonedCells)),
                        "Prevent zoned cells from being overriden during preview and set phase of Zone Controller Tool." +
                        "\nSet this to true if you're having problem with losing your zoning configuration when using the tool." +
                        "\nDefault: true"
                    },
                    {
                        setting.GetOptionLabelLocaleID(nameof(Setting.RemoveOccupiedCells)),
                        "Prevent occupied cells from being removed"
                    },
                    {
                        setting.GetOptionDescLocaleID(nameof(Setting.RemoveOccupiedCells)),
                        "Prevent occupied cells from being overriden during preview and set phase of Zone Controller Tool." +
                        "\nSet this to true if you're having problem with buildings becoming vacant and/or abandoned when using the tool." +
                        "\nDefault: true"
                    },
                    { setting.GetOptionLabelLocaleID(nameof(Setting.InvertZoning)), "Invert Zoning Mouse Button" },
                    {
                        setting.GetOptionDescLocaleID(nameof(Setting.InvertZoning)),
                        "Inverts the current zoning configuration with a mouse action."
                    },

                    { $"Assets.NAME[{ZoningControllerToolSystem.ToolID}]", "Zone Controller" },
                    {
                        $"Assets.DESCRIPTION[{ZoningControllerToolSystem.ToolID}]",
                        "Tool to control how the zoning of a road behaves.\nChoose between zoning on both sides, only on the left or right, or no zoning for that road.\nBy default, right-click inverts the zoning configuration."
                    }
                };
        }

        public void Unload()
        {
        }
    }
}