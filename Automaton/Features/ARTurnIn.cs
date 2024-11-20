using Automaton.IPC;
using ECommons.Automation.NeoTaskManager;
using ECommons.ImGuiMethods;
using ImGuiNET;

namespace Automaton.Features;

public class ARTurnInConfiguration
{
    [IntConfig(DefaultValue = 50, Min = 0, Max = 140)]
    public int InventoryFreeSlotThreshold = 50;

    public List<ulong> ExcludedCharacters = [];
}

[Tweak, Requirement(NavmeshIPC.Name, NavmeshIPC.Repo), Requirement(AutoRetainerIPC.Name, AutoRetainerIPC.Repo), Requirement(DeliverooIPC.Name, DeliverooIPC.Repo), Requirement(LifestreamIPC.Name, LifestreamIPC.Repo)]
internal class ARTurnIn : Tweak<ARTurnInConfiguration>
{
    public override string Name => "AutoRetainer x Deliveroo";
    public override string Description => "On CharacterPostProcess, automatically go to your grand company and turn in your gear when inventory is below a certain threshold.";

    public override void Enable()
    {
        AutoRetainer.OnCharacterPostprocessStep += CheckCharacter;
        AutoRetainer.OnCharacterReadyToPostProcess += TurnIn;
    }

    public override void Disable()
    {
        AutoRetainer.OnCharacterPostprocessStep -= CheckCharacter;
        AutoRetainer.OnCharacterReadyToPostProcess -= TurnIn;
    }

    public override void DrawConfig()
    {
        base.DrawConfig();

        if (!Config.ExcludedCharacters.Contains(Svc.ClientState.LocalContentId))
        {
            if (ImGui.Button("Exclude Current Character"))
                Config.ExcludedCharacters.Add(Svc.ClientState.LocalContentId);
        }
        else
        {
            if (ImGui.Button("Remove Character Exclusion"))
                Config.ExcludedCharacters.Remove(Svc.ClientState.LocalContentId);
        }

        ImGuiX.DrawSection("Debug");

        ImGui.TextUnformatted($"LS:{P.Lifestream.IsBusy()} AR:{P.AutoRetainer.IsBusy()} D:{P.Deliveroo.IsTurnInRunning()} N:{P.Navmesh.IsRunning()}");
        if (Player.Available)
            ImGui.TextUnformatted($"o:{PlayerEx.Occupied} m:{Player.IsMoving} c:{PlayerEx.IsCasting} l:{PlayerEx.AnimationLock}");

        if (ImGui.Button("FinishCharacterPostProcess"))
            AutoRetainer.FinishCharacterPostProcess();

        if (ImGui.Button("TurnIn (All Tasks Combined)"))
            TurnIn();

        if (ImGui.Button($"GoToGC"))
            TaskManager.Enqueue(GoToGC);

        if (ImGui.Button($"TurnInGear"))
            TaskManager.Enqueue(Deliveroo);

        if (ImGui.Button($"GoHome"))
            TaskManager.Enqueue(GoHome);

        if (TaskManager.Tasks.Count > 0)
        {
            ImGuiX.DrawSection("Tasks");
            if (ImGui.Button($"Kill Tasks : {TaskManager.NumQueuedTasks}"))
                TaskManager.Abort();
            ImGuiBB.Text($"[color=#33E6E6]{TaskManager.CurrentTask?.Name}:[/color] [color=#FFFFFF]{TaskManager.CurrentTask?.Function.Method.Name}[/color]");
            foreach (var task in TaskManager.Tasks)
            {
                ImGui.Indent();
                ImGuiBB.Text($"[color=#33E6E6]{task.Name}:[/color] [color=#FFFFFF]{task.Function.Method.Name}[/color]");
                ImGui.Unindent();
            }
        }
    }

    private void CheckCharacter()
    {
        if (Config.ExcludedCharacters.Any(x => x == Svc.ClientState.LocalContentId))
            Svc.Log.Info("Skipping post process turn in for character: character excluded.");
        else
        {
            if (!P.UsingARPostProcess && P.AutoRetainer.GetInventoryFreeSlotCount() <= Config.InventoryFreeSlotThreshold)
            {
                P.UsingARPostProcess = true;
                AutoRetainer.RequestCharacterPostprocess();
            }
            else
                Svc.Log.Info("Skipping post process turn in for character: inventory above threshold.");
        }
    }

    private void TurnIn()
    {
        TaskManager.Enqueue(GoToGC, configuration: LSConfig);
        TaskManager.EnqueueDelay(1000);
        TaskManager.Enqueue(Deliveroo, configuration: DConfig);
        TaskManager.EnqueueDelay(1000);
        TaskManager.Enqueue(GoHome, configuration: LSConfig);
        TaskManager.EnqueueDelay(1000);
        TaskManager.Enqueue(AutoRetainer.FinishCharacterPostProcess);
        TaskManager.Enqueue(() => P.UsingARPostProcess = false);
    }

    // bless lifestream for doing literally all the annoying work for me already
    private void GoToGC() => TaskManager.InsertMulti([new(() => P.Lifestream.ExecuteCommand("gc")), new(() => P.Lifestream.IsBusy()), new(() => !P.Lifestream.IsBusy(), LSConfig)]);
    private void Deliveroo() => TaskManager.InsertMulti([new(() => Svc.Commands.ProcessCommand("/deliveroo enable")), new(() => P.Deliveroo.IsTurnInRunning()), new(() => !P.Deliveroo.IsTurnInRunning(), DConfig)]);
    private void GoHome() => TaskManager.InsertMulti([new(() => P.Lifestream.ExecuteCommand("auto")), new(() => P.Lifestream.IsBusy()), new(() => !P.Lifestream.IsBusy(), LSConfig)]);

    private TaskManagerConfiguration LSConfig => new(timeLimitMS: 2 * 60 * 1000);
    private TaskManagerConfiguration DConfig => new(timeLimitMS: 10 * 60 * 1000, abortOnTimeout: false);
}
