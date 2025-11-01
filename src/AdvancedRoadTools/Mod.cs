using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using AdvancedRoadTools.Tools;
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
    public class Mod : IMod
    {
        public static ILog log = LogManager.GetLogger($"{nameof(AdvancedRoadTools)}")
            .SetShowsErrorsInUI(false);
        public const string ModID = "AdvancedRoadTools";
        public static string ModDirectory;

        public static Setting Setting;
        public const string kInvertZoningActionName = "InvertZoning";
        public const string Name = "Advanced Road Tools";
        public static ProxyAction InvertZoningAction;
        public static string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString(3);

        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Debug($"{nameof(Mod)}.{MethodBase.GetCurrentMethod()?.Name}");

            RegisterPrefab();

            if (!GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
            {
                throw new NullReferenceException($"The mod executable could not be found");
            }
            
            Setting = new Setting(this);
            Setting.RegisterInOptionsUI();
        
            AddSources();
            Setting.RegisterKeyBindings();

            InvertZoningAction = Setting.GetAction(kInvertZoningActionName);

            AssetDatabase.global.LoadSettings(nameof(AdvancedRoadTools), Setting, new Setting(this));


            updateSystem.UpdateAt<ZoningControllerToolSystem>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAt<ToolHighlightSystem>(SystemUpdatePhase.ToolUpdate);

            updateSystem.UpdateAt<SyncCreatedRoadsSystem>(SystemUpdatePhase.Modification4);

            updateSystem.UpdateAt<SyncBlockSystem>(SystemUpdatePhase.Modification4B);

            updateSystem.UpdateAt<ZoningControllerToolUISystem>(SystemUpdatePhase.UIUpdate);

            GameManager.instance.onGamePreload += CreateTools;
        }

        private void AddSources()
        {
            log.Info($"Loading locales");


            
            var langPath = Path.Combine(AssemblyDirectory, "lang");

            if (!Directory.Exists(langPath))
            {
                log.Error($"lang folder not found under mod's directory.");
                return;
            }

            foreach (var path in Directory.GetFiles(langPath, "*.json"))
            {
                var localeID = Path.GetFileNameWithoutExtension(path);
                var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path));

                var locale = new Locale(localeID, Setting);
                locale.Entries = dict;

                GameManager.instance.localizationManager.AddSource(localeID, locale);
                log.Info($"\tLoaded locale {localeID}.json");
            }
            log.Info($"Finished loading locales");
        }

        private void CreateTools(Purpose purpose, GameMode mode)
        {
            ToolsHelper.InstantiateTools();
            GameManager.instance.onGamePreload -= CreateTools;
        }

        private void RegisterPrefab()
        {
            //World world = World;
            //PrefabSystem prefabSystem = world.GetOrCreateSystem<PrefabSystem>();
            var prefab = ScriptableObject.CreateInstance<ServicePrefab>();
            var uiObject = ScriptableObject.CreateInstance<UIObject>();


            //prefabSystem.AddComponentData(prefab, new UIObjectData{});
        }


        public void OnDispose()
        {
            log.Debug($"{nameof(Mod)}.{MethodBase.GetCurrentMethod()?.Name}");
            if (Setting != null)
            {
                Setting.UnregisterInOptionsUI();
                Setting = null;
            }
        }
    }
}