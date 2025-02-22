﻿using AdvancedRoadTools.Logging;
using AdvancedRoadTools.Tools;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Input;
using Game.Modding;
using Game.Prefabs;
using Game.SceneFlow;
using UnityEngine;

namespace AdvancedRoadTools;

public class AdvancedRoadToolsMod : IMod
{
    public const string ModID = "AdvancedRoadTools";
    
    
    public static Setting m_Setting;
    public const string kInvertZoningActionName = "InvertZoning";
    public static ProxyAction m_InvertZoningAction;
    
    public void OnLoad(UpdateSystem updateSystem)
    {
        log.Info(nameof(OnLoad));

        RegisterPrefab();

        if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
            log.Info($"Current mod asset at {asset.path}");

        m_Setting = new Setting(this);
        m_Setting.RegisterInOptionsUI();
        GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(m_Setting));
        m_Setting.RegisterKeyBindings();

        m_InvertZoningAction = m_Setting.GetAction(kInvertZoningActionName);

        AssetDatabase.global.LoadSettings(nameof(AdvancedRoadTools), m_Setting, new Setting(this));

        
        updateSystem.UpdateAt<AdvancedParallelToolSystem>(SystemUpdatePhase.ToolUpdate);
        updateSystem.UpdateAt<ZoningControllerToolSystem>(SystemUpdatePhase.ToolUpdate);
        updateSystem.UpdateAt<ToolHighlightSystem>(SystemUpdatePhase.ToolUpdate);
        
        updateSystem.UpdateAt<SyncCreatedRoadsSystem>(SystemUpdatePhase.Modification4);
        updateSystem.UpdateAt<SyncBlockSystem>(SystemUpdatePhase.Modification4B);
        updateSystem.UpdateAt<ZoningControllerToolUISystem>(SystemUpdatePhase.UIUpdate);
        
        GameManager.instance.onGameLoadingComplete += CreateTools;
    }

    private void CreateTools(Purpose purpose, GameMode mode)
    {
        ToolsHelper.CreateToolsUI();
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
        log.Info(nameof(OnDispose));
        if (m_Setting != null)
        {
            m_Setting.UnregisterInOptionsUI();
            m_Setting = null;
        }
    }
}