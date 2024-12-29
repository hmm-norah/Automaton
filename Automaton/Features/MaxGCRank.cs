namespace Automaton.Features;

[Tweak]
internal class MaxGCRank : Tweak
{
    public override string Name => "Enforce Expert Delivery";
    public override string Description => "Automatically maxes your GC rank to force the expert delivery window to show. Does not bypass anything else rank-restricted.";

    private readonly Memory.GrandCompanyRank GrandCompanyRank = new();
    public override void Enable() => GrandCompanyRank.GCRankHook.Enable();
    public override void Disable() => GrandCompanyRank.GCRankHook.Disable();
}
