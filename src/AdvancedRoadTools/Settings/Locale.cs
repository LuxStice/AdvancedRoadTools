using System.Collections.Generic;
using System.Linq;
using AdvancedRoadTools.Tools;
using Colossal;

namespace AdvancedRoadTools
{
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

        public override string ToString() =>
            $"[ART.Locale] {LocaleID}; Entries: {(Entries is null ? "null" : $"{Entries.Count}")}";

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
                    {setting.GetSettingsLocaleID(), Mod.Name},

                    // Main Tab
                    {setting.GetOptionTabLocaleID(nameof(Setting.MainTab)), "Main"},
                    {
                        setting.GetOptionGroupLocaleID(nameof(Setting.ZoneControllerGroup)),
                        "Zone Controller Tool Options"
                    },
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
                    {setting.GetOptionLabelLocaleID(nameof(Setting.InvertZoningBinding)), "Invert Zoning Mouse Button"},
                    {
                        setting.GetOptionDescLocaleID(nameof(Setting.InvertZoningBinding)),
                        "Inverts the current zoning configuration with a mouse action."
                    },

                    {setting.GetOptionTabLocaleID(nameof(Setting.AboutTab)), "About Me"},
                    {setting.GetOptionGroupLocaleID(nameof(Setting.ModInfoGroup)), "Mod Info"},
                    {setting.GetOptionLabelLocaleID(nameof(Setting.ModName)), "Mod Name"},
                    {setting.GetOptionLabelLocaleID(nameof(Setting.Version)), "Version"},

                    {$"Assets.NAME[{ZoningControllerToolSystem.ToolID}]", "Zone Controller"},
                    {
                        $"Assets.DESCRIPTION[{ZoningControllerToolSystem.ToolID}]",
                        "Tool to control how the zoning of a road behaves.\nChoose between zoning on both sides, only on the left or right, or no zoning for that road.\nBy default, right-click inverts the zoning configuration."
                    }
                };
        }

        public void Unload()
        {
        }


        /// <summary>
        /// Gets a tooltip title locale key.
        /// </summary>
        /// <param name="key">Inside brackets part of key.</param>
        /// <returns>Locale Key string for tooltip title.</returns>
        public static string TooltipTitleKey(string key)
        {
            return $"{Mod.ModID}.TOOLTIP_TITLE[{key}]";
        }

        /// <summary>
        /// Gets a tooltip description locale key.
        /// </summary>
        /// <param name="key">Inside brackets part of key.</param>
        /// <returns>Locale key string for tooltip description.</returns>
        public static string TooltipDescriptionKey(string key)
        {
            return $"{Mod.ModID}.TOOLTIP_DESCRIPTION[{key}]";
        }

        private string SectionLabel(string key)
        {
            return $"{Mod.ModID}.SECTION_TITLE[{key}]";
        }


        private string TextLabel(string key)
        {
            return $"{Mod.ModID}.TEXT_LABEL[{key}]";
        }
    }
}