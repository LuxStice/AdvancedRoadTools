using System;
using System.Collections.Generic;
using System.IO;
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
    public class AdvancedRoadToolsMod : IMod
    {
        public static ILog log = LogManager.GetLogger($"{nameof(AdvancedRoadTools)}")
            .SetShowsErrorsInUI(false);
        public const string ModID = "AdvancedRoadTools";
        public static string ModDirectory;

        public static Setting Setting;
        public const string kInvertZoningActionName = "InvertZoning";
        public static ProxyAction InvertZoningAction;
        
        public void OnLoad(UpdateSystem updateSystem)
        {
            AdvancedRoadToolsMod.log.Debug($"{nameof(AdvancedRoadToolsMod)}.{System.Reflection.MethodBase.GetCurrentMethod()?.Name}");

            RegisterPrefab();

            if (!GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
            {
                throw new NullReferenceException($"The mod executable could not be found");
            }
            ModDirectory = Path.GetDirectoryName(asset.path);
            
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
            AdvancedRoadToolsMod.log.Info($"Loading locales");
            
            var langPath = Path.Combine(ModDirectory, "lang");

            if (!Directory.Exists(langPath))
            {
                AdvancedRoadToolsMod.log.Error($"lang folder not found under mod's directory.");
                return;
            }

            foreach (var path in Directory.GetFiles(langPath, "*.json"))
            {
                var localeID = Path.GetFileNameWithoutExtension(path);
                var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path));

                var locale = new Locale(localeID, Setting);
                locale.Entries = dict;

                GameManager.instance.localizationManager.AddSource(localeID, locale);
                AdvancedRoadToolsMod.log.Info($"\tLoaded locale {localeID}.json");
            }
            AdvancedRoadToolsMod.log.Info($"Finished loading locales");
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
            AdvancedRoadToolsMod.log.Debug($"{nameof(AdvancedRoadToolsMod)}.{System.Reflection.MethodBase.GetCurrentMethod()?.Name}");
            if (Setting != null)
            {
                Setting.UnregisterInOptionsUI();
                Setting = null;
            }
        }
    }
}