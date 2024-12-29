namespace Automaton.FeaturesSetup.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class TweakAttribute(bool debug = false, bool outdated = false, bool disabled = false) : Attribute
{
    public bool Debug = debug;
    public bool Outdated = outdated;
    public bool Disabled = disabled;
}
