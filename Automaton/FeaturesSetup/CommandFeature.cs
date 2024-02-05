using Automaton.FeaturesSetup;
using Dalamud.Game.Command;
using ECommons.DalamudServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Automaton.Features;

public abstract partial class CommandFeature : Feature
{
    public abstract string Command { get; set; }
    public virtual string[] Alias => Array.Empty<string>();
    public virtual string HelpMessage => $"[{Automaton.Name} {Name}]";
    public virtual bool ShowInHelp => false;
    public virtual List<string> Parameters => new();
    public override FeatureType FeatureType => FeatureType.Commands;

    protected abstract void OnCommand(List<string> args);

    protected virtual void OnCommandInternal(string _, string args)
    {
        args = args.ToLower();
        OnCommand(args.Split(' ').ToList());
    }

    private readonly List<string> registeredCommands = [];

    public override void Enable()
    {
        if (Svc.Commands.Commands.ContainsKey(Command))
        {
            Svc.Log.Error($"Command '{Command}' is already registered.");
        }
        else
        {
            Svc.Commands.AddHandler(Command, new CommandInfo(OnCommandInternal)
            {
                HelpMessage = HelpMessage,
                ShowInHelp = ShowInHelp
            });

            registeredCommands.Add(Command);
        }

        foreach (var a in Alias)
        {
            if (!Svc.Commands.Commands.ContainsKey(a))
            {
                Svc.Commands.AddHandler(a, new CommandInfo(OnCommandInternal)
                {
                    HelpMessage = HelpMessage,
                    ShowInHelp = false
                });
                registeredCommands.Add(a);
            }
        }
    }

    public override void Disable()
    {
        foreach (var c in registeredCommands)
        {
            Svc.Commands.RemoveHandler(c);
        }
        registeredCommands.Clear();
        base.Disable();
    }

    public static List<string> GetArgumentList(string args) => ArgumentRegex().Matches(args)
    .Select(m =>
    {
        return m.Value.StartsWith('"') && m.Value.EndsWith('"') ? m.Value[1..^1] : m.Value;
    })
    .ToList();

    [GeneratedRegex("[\\\"].+?[\\\"]|[^ ]+")]
    private static partial Regex ArgumentRegex();
}
