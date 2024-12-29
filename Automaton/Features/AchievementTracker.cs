using Automaton.UI;
using Dalamud.Interface.Components;
using ECommons.ImGuiMethods;
using ECommons.SimpleGui;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using ImGuiNET;

namespace Automaton.Features;

public class AchievementTrackerConfiguration
{
    public List<AchievementTracker.Achv> Achievements = [];
    public Vector4 BarColour = Vector4.One;
    public int UpdateFrequency = 60;
    public bool AutoRemoveCompleted = false;
}

[Tweak]
public unsafe class AchievementTracker : Tweak<AchievementTrackerConfiguration>
{
    public override string Name => "Achievement Tracker";
    public override string Description => $"Adds an achievement tracker. Open menu with /atracker.";

    public override void DrawConfig()
    {
        ImGuiX.DrawSection("Configuration");
        var edited = false;
        ImGuiEx.TextV("Bar Colour: ");
        ImGui.SameLine();
        var newColor = ImGuiComponents.ColorPickerWithPalette(1, "##color", Config.BarColour, ImGuiColorEditFlags.NoAlpha);

        edited |= !Config.BarColour.Equals(newColor);

        if (edited)
        {
            Config.BarColour = newColor;
        }

        ImGui.SliderInt("Update Frequency (s)", ref Config.UpdateFrequency, 60, 600, "%d", ImGuiSliderFlags.AlwaysClamp);
        ImGui.Checkbox("Auto Remove Completed", ref Config.AutoRemoveCompleted);
    }

    public class Achv
    {
        public uint ID;
        public required string Name;
        public uint CurrentProgress;
        public uint MaxProgress;
        public string Description = string.Empty;
        public byte Points = 0;
        public bool Completed => CurrentProgress != default && CurrentProgress >= MaxProgress;
    }

    private readonly Memory.AchievementProgress AchievementProgress = new();
    public override void Enable()
    {
        AchievementProgress.ReceiveAchievementProgressHook.Enable();
        Events.AchievementProgressUpdate += OnAchievementProgressUpdate;
        EzConfigGui.WindowSystem.AddWindow(new AchievementTrackerUI(this));
    }

    public override void Disable()
    {
        AchievementProgress.ReceiveAchievementProgressHook.Disable();
        Events.AchievementProgressUpdate -= OnAchievementProgressUpdate;
        EzConfigGui.RemoveWindow<AchievementTrackerUI>();
    }

    [CommandHandler("/atracker", "Toggle the Achievement Tracker window")]
    private void OnCommand(string command, string arguments) => EzConfigGui.GetWindow<AchievementTrackerUI>()!.IsOpen ^= true;

    private void OnAchievementProgressUpdate(uint id, uint current, uint max)
    {
        foreach (var achv in Config.Achievements)
        {
            if (achv.ID == id)
            {
                achv.CurrentProgress = current;
                achv.MaxProgress = max;
            }
        }
    }

    public void RequestUpdate(uint id = 0)
    {
        if (id == 0)
            Config.Achievements.Where(a => !a.Completed).ToList().ForEach(achv => Achievement.Instance()->RequestAchievementProgress(achv.ID));
        else
            Achievement.Instance()->RequestAchievementProgress(id);
    }
}
