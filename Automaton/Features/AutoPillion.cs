using Lumina.Excel.Sheets;

namespace Automaton.Features;

[Tweak]
public class AutoPillion : Tweak
{
    public override string Name => "Auto Pillion";
    public override string Description => "Automatically hop in to other peoples' mounts when you are near them.";

    public override void Enable() => Svc.Framework.Update += OnUpdate;
    public override void Disable() => Svc.Framework.Update -= OnUpdate;

    private unsafe void OnUpdate(IFramework framework)
    {
        if (!Player.Available || PlayerEx.Occupied || Svc.Condition[ConditionFlag.Mounted])
        {
            if (TaskManager.Tasks.Count > 0)
                TaskManager.Abort();
            return;
        }

        // TODO: add a check if there are any seats left to get into
        var target = Svc.Party.FirstOrDefault(o => o?.ObjectId != Player.Object.GameObjectId && o?.GameObject?.YalmDistanceX < 3 && GetRow<Mount>(o.GameObject.Character()->Mount.MountId)!.Value.ExtraSeats > 0, null);
        if (target != null && target.GameObject != null && P.Memory.RidePillion != null)
        {
            TaskManager.Enqueue(() => Svc.Log.Debug("Detected mounted party member with extra seats, mounting..."));
            TaskManager.Enqueue(() => P.Memory.RidePillion(target.GameObject.BattleChara(), 10));
            TaskManager.Enqueue(() => Svc.Condition[ConditionFlag.Mounted]);
        }
    }
}
