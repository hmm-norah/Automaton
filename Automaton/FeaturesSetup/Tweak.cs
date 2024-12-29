using Automaton.IPC;
using AutoRetainerAPI;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using ECommons.Automation.NeoTaskManager;
using System.Reflection;

namespace Automaton.FeaturesSetup;

public abstract partial class Tweak : ITweak
{
    // https://github.com/Haselnussbomber/HaselTweaks
    public Tweak()
    {
        CachedType = GetType();
        InternalName = CachedType.Name;
        IncompatibilityWarnings = CachedType.GetCustomAttributes<IncompatibilityWarningAttribute>().ToArray();
        Requirements = CachedType.GetCustomAttributes<RequirementAttribute>().ToArray();
        Outdated = CachedType.GetCustomAttribute<TweakAttribute>()?.Outdated ?? false;
        Disabled = CachedType.GetCustomAttribute<TweakAttribute>()?.Disabled ?? false;
        IsDebug = CachedType.GetCustomAttribute<TweakAttribute>()?.Debug ?? false;

        TaskManager = new();

        if (Requirements.Any(r => r.InternalName == AutoRetainerIPC.Name))
            AutoRetainer = new(Name);

        try
        {
            Svc.Hook.InitializeFromAttributes(this);
        }
        catch (SignatureException ex)
        {
            Error(ex, "SignatureException, flagging as outdated");
            Outdated = true;
            LastInternalException = ex;
            return;
        }

        try
        {
            SetupVTableHooks(); // before SetupAddressHooks!
        }
        catch (Exception ex)
        {
            Error(ex, "Unexpected error during SetupVTableHooks");
            LastInternalException = ex;
            return;
        }

        try
        {
            SetupAddressHooks();
        }
        catch (Exception ex)
        {
            Error(ex, "Unexpected error during SetupAddressHooks");
            LastInternalException = ex;
            return;
        }

        Ready = true;
    }

    public Type CachedType { get; init; }
    public string InternalName { get; init; }
    public IncompatibilityWarningAttribute[] IncompatibilityWarnings { get; init; }
    public RequirementAttribute[] Requirements { get; init; }

    public abstract string Name { get; }
    public abstract string Description { get; }
    public bool IsDebug { get; }

    public bool Outdated { get; protected set; }
    public bool Ready { get; protected set; }
    public bool Enabled { get; protected set; }
    public bool Disabled { get; protected set; }

    protected TaskManager TaskManager = null!;
    protected AutoRetainerApi AutoRetainer = null!;

    public virtual void SetupAddressHooks() { }
    public virtual void SetupVTableHooks() { }

    public virtual void Enable() { }
    public virtual void Disable() { }
    public virtual void Dispose() { }
    public virtual void DrawConfig() { }
    public virtual void OnConfigChange(string fieldName) { }
}

public abstract partial class Tweak // Internal
{
    private bool Disposed { get; set; }
    internal Exception? LastInternalException { get; set; }

    protected IEnumerable<PropertyInfo> Hooks => CachedType
        .GetProperties(BindingFlags.NonPublic | BindingFlags.Instance)
        .Where(prop =>
            prop.PropertyType.IsGenericType &&
            prop.PropertyType.GetGenericTypeDefinition() == typeof(Hook<>)
        );

    protected void CallHooks(string methodName)
    {
        foreach (var property in Hooks)
        {
            var hook = property.GetValue(this);
            if (hook == null) continue;

            typeof(Hook<>)
                .MakeGenericType(property.PropertyType.GetGenericArguments().First())
                .GetMethod(methodName)?
                .Invoke(hook, null);
        }
    }

    internal virtual void EnableInternal()
    {
        if (!Ready || Outdated || Disabled) return;
        if (Requirements.Any(r => !r.IsLoaded))
        {
            ModuleMessage("Feature not enabled due to missing dependencies. Please install them then re-enable this feature.");
            return;
        }

        try
        {
            EnableCommands();
        }
        catch (Exception ex)
        {
            Error(ex, "Unexpected error during Enable (Commands)");
            LastInternalException = ex;
        }

        try
        {
            CallHooks("Enable");
        }
        catch (Exception ex)
        {
            Error(ex, "Unexpected error during Enable (Hooks)");
            LastInternalException = ex;
            return;
        }

        try
        {
            Enable();
        }
        catch (Exception ex)
        {
            Error(ex, "Unexpected error during Enable");
            LastInternalException = ex;
            return;
        }

        LastInternalException = null;
        Enabled = true;
    }

    internal virtual void DisableInternal(bool isDisposing = false)
    {
        if (!Enabled) return;

        try
        {
            DisableCommands();
        }
        catch (Exception ex)
        {
            Error(ex, "Unexpected error during Disable (Commands)");
            LastInternalException = ex;
        }

        if (!isDisposing)
        {
            try
            {
                CallHooks("Disable");
            }
            catch (Exception ex)
            {
                Error(ex, "Unexpected error during Disable (Hooks)");
                LastInternalException = ex;
            }
        }

        try
        {
            Disable();
        }
        catch (Exception ex)
        {
            Error(ex, "Unexpected error during Disable");
            LastInternalException = ex;
        }

        Enabled = false;
    }

    internal virtual void DisposeInternal()
    {
        if (Disposed)
            return;

        DisableInternal(true);

        try
        {
            CallHooks("Dispose");
        }
        catch (Exception ex)
        {
            Error(ex, "Unexpected error during Dispose (Hooks)");
            LastInternalException = ex;
        }

        try
        {
            Dispose();
        }
        catch (Exception ex)
        {
            Error(ex, "Unexpected error during Dispose");
            LastInternalException = ex;
        }

        Ready = false;
        Disposed = true;
    }

    internal virtual void OnConfigChangeInternal(string fieldName)
    {
        try
        {
            OnConfigChange(fieldName);
        }
        catch (Exception ex)
        {
            Error(ex, "Unexpected error during OnConfigChange");
            LastInternalException = ex;
            return;
        }
    }

    protected virtual void EnableCommands() { }
    protected virtual void DisableCommands() { }
}

public abstract partial class Tweak // Logging
{
    public void Log(string messageTemplate, params object[] values)
        => Information(messageTemplate, values);

    public void Log(Exception exception, string messageTemplate, params object[] values)
        => Information(exception, messageTemplate, values);

    public void Verbose(string messageTemplate, params object[] values)
        => Svc.Log.Verbose($"[{InternalName}] {messageTemplate}", values);

    public void Verbose(Exception exception, string messageTemplate, params object[] values)
        => Svc.Log.Verbose(exception, $"[{InternalName}] {messageTemplate}", values);

    public void Debug(string messageTemplate, params object[] values)
        => Svc.Log.Debug($"[{InternalName}] {messageTemplate}", values);

    public void Debug(Exception exception, string messageTemplate, params object[] values)
        => Svc.Log.Debug(exception, $"[{InternalName}] {messageTemplate}", values);

    public void Information(string messageTemplate, params object[] values)
        => Svc.Log.Information($"[{InternalName}] {messageTemplate}", values);

    public void Information(Exception exception, string messageTemplate, params object[] values)
        => Svc.Log.Information(exception, $"[{InternalName}] {messageTemplate}", values);

    public void Warning(string messageTemplate, params object[] values)
        => Svc.Log.Warning($"[{InternalName}] {messageTemplate}", values);

    public void Warning(Exception exception, string messageTemplate, params object[] values)
        => Svc.Log.Warning(exception, $"[{InternalName}] {messageTemplate}", values);

    public void Error(string messageTemplate, params object[] values)
        => Svc.Log.Error($"[{InternalName}] {messageTemplate}", values);

    public void Error(Exception exception, string messageTemplate, params object[] values)
        => Svc.Log.Error(exception, $"[{InternalName}] {messageTemplate}", values);

    public void Fatal(string messageTemplate, params object[] values)
        => Svc.Log.Fatal($"[{InternalName}] {messageTemplate}", values);

    public void Fatal(Exception exception, string messageTemplate, params object[] values)
        => Svc.Log.Fatal(exception, $"[{InternalName}] {messageTemplate}", values);

    public void ModuleMessage(SeString messageTemplate) => ModuleMessage(messageTemplate.TextValue);
    public void ModuleMessage(string messageTemplate)
    {
        var message = new XivChatEntry
        {
            Message = new SeStringBuilder()
                .AddUiForeground($"[{Name}] ", 62)
                .Append(messageTemplate)
                .Build()
        };

        Svc.Chat.Print(message);
    }
}
