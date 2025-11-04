using Colossal.IO.AssetDatabase;
using Game.Input;
using Game.Modding;
using Game.Settings;

namespace AdvancedRoadTools
{
    [FileLocation(nameof(AdvancedRoadTools))]
    [SettingsUIGroupOrder(ZoneControllerGroup, ModInfoGroup)]
    [SettingsUIShowGroupName(ZoneControllerGroup, ModInfoGroup)]
    [SettingsUIMouseAction(Mod.InvertZoningActionName, ActionType.Button, "Zone Controller Tool")]
    public class Setting : ModSetting
    {
        public const string MainTab = "MainTab";
        public const string ZoneControllerGroup = "ZoneControllerToolGroup";

        public const string KeybindingsTab = "KeybindingsTab";


        public const string AboutTab = "AboutTab";
        public const string ModInfoGroup = "ModInfoGroup";

        public Setting(IMod mod) : base(mod)
        {
        }

        // These are inversed since pre 1.0 version. Changing it will break translations so i'll hold on into it until really needed
        /// <summary>
        /// Prevent zoned cells from being removed. If disabled, <see cref="RemoveOccupiedCells"/> will be uninteractable as well.
        /// </summary>
        [SettingsUISection(MainTab, ZoneControllerGroup)]
        public bool RemoveZonedCells { get; set; } = false;

        /// <summary>
        /// Prevent occupied cells from being removed.
        /// </summary>
        [SettingsUISection(MainTab, ZoneControllerGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IfNotRemoveZonedCells))]
        public bool RemoveOccupiedCells { get; set; } = true;

        [SettingsUIMouseBinding(BindingMouse.Right, nameof(InvertZoningBinding))]
        [SettingsUISection(KeybindingsTab, ZoneControllerGroup)]
        public ProxyBinding InvertZoningBinding { get; set; }

        [SettingsUISection(AboutTab, ModInfoGroup)] public string ModName => Mod.Name;

        [SettingsUISection(AboutTab, ModInfoGroup)] public string Version => Mod.Version;

        public override void SetDefaults()
        {
            RemoveOccupiedCells = true;
            RemoveZonedCells = true;
            InvertZoningBinding = new ProxyBinding { };
        }

        private bool IfNotRemoveZonedCells() => !RemoveZonedCells;
    }
}