using Automaton.Configuration;
using Automaton.IPC;
using Automaton.UI;
using AutoRetainerAPI;
using Dalamud.Plugin;
using ECommons;
using ECommons.Automation.LegacyTaskManager;
using ECommons.Configuration;
using ECommons.SimpleGui;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;

namespace Automaton;

public class Plugin : IDalamudPlugin
{
    public static string Name => "CBT";
    public static string VersionString => $"v{P.GetType().Assembly.GetName().Version?.Major}.{P.GetType().Assembly.GetName().Version?.Minor}";
    private const string Command = "/cbt";
    private const string LegacyCommand = "/automaton";

    internal static Plugin P = null!;
    private readonly Config Config;
    public static Config C => P.Config;

    public static readonly HashSet<Tweak> Tweaks = [];
    internal TaskManager TaskManager;
    internal AddonObserver AddonObserver;

    internal Provider Provider;
    internal NavmeshIPC Navmesh;
    internal AutoRetainerApi AutoRetainerAPI;
    internal LifestreamIPC Lifestream;
    internal DeliverooIPC Deliveroo;
    internal AutoRetainerIPC AutoRetainer;
    internal bool UsingARPostProcess;

    internal Memory Memory = null!;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        P = this;
        ECommonsMain.Init(pluginInterface, P, ECommons.Module.DalamudReflector, ECommons.Module.ObjectFunctions);

        EzConfig.DefaultSerializationFactory = new YamlFactory();
        Config = EzConfig.Init<Config>();

        IMigration[] migrations = [new V3()];
        foreach (var migration in migrations)
        {
            if (Config.Version < migration.Version)
            {
                Svc.Log.Info($"Migrating from config version {Config.Version} to {migration.Version}");
                migration.Migrate(ref Config);
                Config.Version = migration.Version;
            }
        }

        Svc.Framework.Update += EventWatcher;

        EzCmd.Add(Command, OnCommand, $"Opens the {Name} menu");
        EzCmd.Add(LegacyCommand, OnCommand);
        EzConfigGui.Init(new HaselWindow().Draw, nameOverride: $"{Name} {VersionString}");
        EzConfigGui.WindowSystem.AddWindow(new DebugWindow());
        try
        {
            Memory = new();
        }
        catch (Exception ex)
        {
            Svc.Log.Error(ex, "Failed to initialize Memory");
        }

        AddonObserver = new();
        TaskManager = new();
        Provider = new();
        Navmesh = new();
        AutoRetainerAPI = new();
        Lifestream = new();
        Deliveroo = new();
        AutoRetainer = new();

        Svc.Framework.RunOnFrameworkThread(InitializeTweaks);
        C.EnabledTweaks.CollectionChanged += OnChange;
    }

    private bool inpvp = false;
    private void EventWatcher(IFramework framework)
    {
        if (PlayerEx.InPvP)
        {
            if (!inpvp)
            {
                inpvp = true;
                Events.OnEnteredPvPInstance();
            }
        }
        else
            inpvp = false;
    }

    public static void OnChange(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var t in Tweaks)
        {
            if (C.EnabledTweaks.Contains(t.InternalName) && !t.Enabled)
                TryExecute(t.EnableInternal);
            else if (!C.EnabledTweaks.Contains(t.InternalName) && t.Enabled || t.Enabled && t.IsDebug && !C.ShowDebug)
                t.DisableInternal();
            EzConfig.Save();
        }
    }

    public void Dispose()
    {
        foreach (var tweak in Tweaks)
        {
            Svc.Log.Debug($"Disposing {tweak.InternalName}");
            TryExecute(tweak.DisposeInternal);
        }
        Svc.Framework.Update -= EventWatcher;
        C.EnabledTweaks.CollectionChanged -= OnChange;
        AddonObserver.Dispose();
        Memory?.Dispose();
        ECommonsMain.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        if (args.StartsWith('d'))
            EzConfigGui.GetWindow<DebugWindow>()!.Toggle();
        else
            EzConfigGui.Window.Toggle();
    }

    private void InitializeTweaks()
    {
        foreach (var tweakType in GetType().Assembly.GetTypes().Where(type => type.Namespace == "Automaton.Features" && type.GetCustomAttribute<TweakAttribute>() != null))
        {
            Svc.Log.Verbose($"Initializing {tweakType.Name}");
            try
            {
                Tweaks.Add((Tweak)Activator.CreateInstance(tweakType)!);
            }
            catch (Exception ex)
            {
                Svc.Log.Error($"Failed to initialize {tweakType.Name}", ex);
            }
        }

        foreach (var tweak in Tweaks)
        {
            if (!Config.EnabledTweaks.Contains(tweak.InternalName))
                continue;

            if (Config.EnabledTweaks.Contains(tweak.InternalName) && tweak.IsDebug && !Config.ShowDebug)
                Config.EnabledTweaks.Remove(tweak.InternalName);

            TryExecute(tweak.EnableInternal);
        }
    }
}

