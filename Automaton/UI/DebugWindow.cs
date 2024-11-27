using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ECommons.Automation;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace Automaton.UI;

internal class DebugWindow : Window
{
    public DebugWindow() : base($"{Name} - Debug {VersionString}###{nameof(DebugWindow)}")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public static void Dispose() { }

    public override bool DrawConditions() => Player.Available && C.ShowDebug;

    private Enums.ExecuteCommandFlag flag;
    private Enums.ExecuteCommandComplexFlag flag2;
    private int[] ecParams = new int[4];
    private int[] eccParams = new int[4];
    private readonly Memory.ExecuteCommands executeCommands = new();

    private unsafe List<Pointer<InventoryItem>> Inventory
    {
        get
        {
            List<Pointer<InventoryItem>> items = [];
            foreach (var inv in Utilities.Inventory.Equippable)
            {
                var cont = InventoryManager.Instance()->GetInventoryContainer(inv);
                for (var i = 0; i < cont->Size; ++i)
                    if (cont->GetInventorySlot(i)->ItemId != 0)
                        items.Add(cont->GetInventorySlot(i));
            }
            return items;
        }
    }
    private unsafe List<Pointer<InventoryItem>> FilteredItems => Inventory.Where(x => GetRow<Item>(x.Value->ItemId)?.Name.ExtractText().ToLowerInvariant().Contains(searchFilter.ToLowerInvariant()) ?? false).ToList();
    private string searchFilter = "";
    public override unsafe void Draw()
    {
        using var tabs = ImRaii.TabBar("tabs");
        if (!tabs) return;
        using (var tabExecuteCommand = ImRaii.TabItem("ExecuteCommand"))
        {
            if (tabExecuteCommand)
            {
                ImGuiX.Enum("ExecuteCommand", ref flag);
                ImGui.InputInt("p1", ref ecParams[0]);
                ImGui.InputInt("p2", ref ecParams[1]);
                ImGui.InputInt("p3", ref ecParams[2]);
                ImGui.InputInt("p4", ref ecParams[3]);
                if (ImGui.Button("exeucte"))
                    executeCommands.ExecuteCommand(flag, ecParams[0], ecParams[1], ecParams[2], ecParams[3]);

                using var id = ImRaii.PushId("complex");
                ImGuiX.Enum("ExecuteCommandComplex", ref flag2);
                ImGui.InputInt("p1", ref eccParams[0]);
                ImGui.InputInt("p2", ref eccParams[1]);
                ImGui.InputInt("p3", ref eccParams[2]);
                ImGui.InputInt("p4", ref eccParams[3]);
                if (ImGui.Button("exeucte"))
                    executeCommands.ExecuteCommandComplexLocation(flag2, Player.Position, eccParams[0], eccParams[1], eccParams[2], eccParams[3]);

            }
        }
        using (var tabMiscTools = ImRaii.TabItem("Misc Tools"))
        {
            if (tabMiscTools)
            {
                List<string> cantSpend = [];
                if (ImGui.Button("Spend Nuts"))
                {
                    if (TryGetAddonByName<AtkUnitBase>("ShopExchangeCurrency", out var addon))
                    {
                        const uint nuts = 26533;
                        var nutsAmt = InventoryManager.Instance()->GetInventoryItemCount(nuts);
                        var nutsCost = 25;
                        var freeslots = InventoryManager.Instance()->GetEmptySlotsInBag() + Utilities.Inventory.GetEmptySlots([InventoryType.ArmoryRings]);
                        uint tobuy = (uint)Math.Min(nutsAmt / nutsCost, freeslots);
                        Svc.Log.Info($"{InventoryManager.Instance()->GetEmptySlotsInBag()} {Utilities.Inventory.GetEmptySlots([InventoryType.ArmoryRings])} {nutsAmt} {nutsAmt / nutsCost} {tobuy}");
                        Callback.Fire(addon, true, 0, 49, tobuy);
                    }
                    else
                        cantSpend.Add("ShopExchangeCurrency not open");
                }
                if (ImGui.IsItemHovered()) ImGui.SetTooltip($"Buys the most amount of {GetRow<Item>(34922)?.Name}");
                cantSpend.ForEach(x => ImGuiEx.Text((uint)Colors.Red, x));
            }
        }
        using (var tabPlayerEx = ImRaii.TabItem($"{nameof(PlayerEx)}"))
        {
            if (tabPlayerEx)
            {
                var pi = typeof(PlayerEx).GetProperties();
                foreach (var p in pi)
                {
                    try
                    {
                        ImGui.TextColored(new Vector4(0.2f, 0.6f, 0.4f, 1), $"{p.Name}: ");
                        ImGui.SameLine();
                        ImGui.TextDisabled($"{p.GetValue(typeof(PlayerEx))}");
                    }
                    catch (Exception e)
                    {
                        ImGui.TextColored(new Vector4(1, 0, 0, 1), $"[ERROR] {e.Message}");
                    }
                }
            }
        }
        using (var tabInventorySearch = ImRaii.TabItem("Inventory"))
        {
            if (tabInventorySearch)
            {
                ImGui.InputText("Filter", ref searchFilter, 256);
                DrawInventory();
            }
        }
    }

    private unsafe void DrawInventory()
    {
        foreach (var item in FilteredItems)
        {
            var data = GetRow<Item>(item.Value->ItemId)!;
            ImGui.TextUnformatted($"[{item.Value->ItemId}] {item.Value->Container} {item.Value->Slot} {data.Value.Name}");
        }
    }
}
