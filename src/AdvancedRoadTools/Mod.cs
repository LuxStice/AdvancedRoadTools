using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AdvancedRoadTools.Tools;
using Colossal;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Colossal.Serialization.Entities;
using Game;
using Game.Input;
using Game.Modding;
using Game.Prefabs;
using Game.SceneFlow;
using Newtonsoft.Json;
using UnityEngine;

namespace AdvancedRoadTools
{
    /// <inheritdoc />
    public class Mod : IMod
    {
        /// <summary>
        /// Mod's logger
        /// </summary>
        public static ILog log = LogManager.GetLogger($"{nameof(AdvancedRoadTools)}")
            .SetShowsErrorsInUI(false);
        /// <summary>
        /// Mod's ID
        /// </summary>
        public const string ModID = "AdvancedRoadTools";
        /// <summary>
        /// Mod's Directory
        /// </summary>
        public static string ModDirectory;

        /// <summary>
        /// Mod's settings instance
        /// </summary>
        public static Setting Setting;
        /// <summary>
        /// Global name for the Invert Zoning action.
        /// </summary>
        public const string InvertZoningActionName = "InvertZoningBinding";
        /// <summary>
        /// Mod's Name
        /// </summary>
        public const string Name = ModAssemblyInfo.Title;

        /// <summary>
        /// Invert Zoning <see cref="ProxyAction"/> instance.
        ///
        /// </summary>
        public static ProxyAction InvertZoningAction;

        /// <summary>
        /// Assembly version.
        /// </summary>
        public const string Version = ModAssemblyInfo.Version;

        /// <summary>
        /// On mod load by Colossal Framework.
        /// </summary>
        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info($"Version {Version}");
            log.Debug($"{nameof(Mod)}.{MethodBase.GetCurrentMethod()?.Name}");

            if (!GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
            {
                throw new NullReferenceException($"The mod executable could not be found");
            }

            ModDirectory = Path.GetDirectoryName(asset.path);

            // Creating and defining the mod's settings.
            Setting = new Setting(this);
            Setting.RegisterInOptionsUI();

            Setting.RegisterKeyBindings();
            InvertZoningAction = Setting.GetAction(nameof(Setting.InvertZoningBinding));

            LoadAndAddLocalizationSources();
            AssetDatabase.global.LoadSettings(nameof(AdvancedRoadTools), Setting, new Setting(this));

            RegisterSystems(updateSystem);

            GameManager.instance.onGamePreload += CreateTools;

#if DEBUG && EXPORT_LOCALIZATION
            log.Info($"{nameof(Mod)}.{nameof(OnLoad)} Exporting localization");
            var localeDict = new Locale("en-US", Setting).ReadEntries(new List<IDictionaryEntryError>(), new Dictionary<string, int>()).ToDictionary(pair => pair.Key, pair => pair.Value);
            var str = JsonConvert.SerializeObject(localeDict, Formatting.Indented);
            try
            {
                if (ModDirectory is null) throw new NullReferenceException();
                var path = Path.Combine(ModDirectory, "export.json");
                File.WriteAllText(path, str);
                log.Info($"Localization exported to {path}");
            }
            catch
            {
                log.Error($"Couldn't export localization to export.json");
            }
#endif
        }

        /// <summary>
        /// On mod unload by Colossal Framework.
        /// </summary>
        public void OnDispose()
        {
            log.Debug($"{nameof(Mod)}.{MethodBase.GetCurrentMethod()?.Name}");
            if (Setting != null)
            {
                Setting.UnregisterInOptionsUI();
                Setting = null;
            }
        }

        private void RegisterSystems(UpdateSystem updateSystem)
        {
            updateSystem.UpdateAt<ZoningControllerToolSystem>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAt<ToolHighlightSystem>(SystemUpdatePhase.ToolUpdate);

            updateSystem.UpdateAt<SyncCreatedRoadsSystem>(SystemUpdatePhase.Modification4);

            updateSystem.UpdateAt<SyncBlockSystem>(SystemUpdatePhase.Modification4B);

            updateSystem.UpdateAt<ZoningControllerToolUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<RenderZoneControllerToolSystem>(SystemUpdatePhase.Rendering);
        }

        private void LoadAndAddLocalizationSources()
        {
            log.Info($"Loading locales");

            var langPath = Path.Combine(ModDirectory, "lang");

            if (!Directory.Exists(langPath))
            {
                log.Error($"lang folder not found under mod's directory.");
                return;
            }

            int localeCount = 0;
            foreach (var path in Directory.GetFiles(langPath, "*.json"))
            {
                var localeID = Path.GetFileNameWithoutExtension(path);
                var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path));

                var locale = new Locale(localeID, Setting);
                locale.Entries = dict;

                GameManager.instance.localizationManager.AddSource(localeID, locale);
                localeCount++;
            }
            log.Info($"Finished loading {localeCount} locales");
        }

        private void CreateTools(Purpose purpose, GameMode mode)
        {
            ToolsHelper.InstantiateTools();
            GameManager.instance.onGamePreload -= CreateTools;
        }
    }
}