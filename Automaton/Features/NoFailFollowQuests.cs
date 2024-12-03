namespace Automaton.Features;

[Tweak]
internal class NoFailFollowQuests : Tweak
{
    public override string Name => "No Fail Follow Quests";
    public override string Description => "Prevents being seen during follow quests (you can still be too far away).";

    private readonly Memory.FollowQuestRecastCheck FollowQuestSequence = new();
    public override void Enable() => FollowQuestSequence.RecastHook.Enable();
    public override void Disable() => FollowQuestSequence.RecastHook.Disable();
}
