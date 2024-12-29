using ImGuiNET;

namespace Automaton.Features;

[Tweak(debug: true)]
internal class PacketFirewall : Tweak
{
    public override string Name => "Packet Firewall";
    public override string Description => "Selectively enable sending and receiving server packets.";

    private readonly Memory.PacketDispatcher PacketDispatcher = new();
    public override void Enable()
    {
        PacketDispatcher.PacketDispatcher_OnReceivePacketHook.Enable();
        PacketDispatcher.PacketDispatcher_OnSendPacketHook.Enable();
    }

    public override void Disable()
    {
        PacketDispatcher.PacketDispatcher_OnReceivePacketHook.Disable();
        PacketDispatcher.PacketDispatcher_OnSendPacketHook.Disable();
    }

    private int packet;
    private bool sending;
    public override void DrawConfig()
    {
        ImGui.Checkbox("Sending?", ref sending);
        if (ImGui.InputInt("Packet #", ref packet, 1, 10, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            if (sending)
                PacketDispatcher.DisallowedSentPackets.Add((uint)packet);
            else
                PacketDispatcher.DisallowedReceivedPackets.Add((uint)packet);
        }

        if (ImGui.Button("Block all sending"))
            PacketDispatcher.DisallowedSentPackets.AddRange(Enumerable.Range(0, 10000).Select(x => (uint)x));
        ImGui.SameLine();
        if (ImGui.Button("Block all receiving"))
            PacketDispatcher.DisallowedReceivedPackets.AddRange(Enumerable.Range(0, 10000).Select(x => (uint)x));

        if (ImGui.Button("Clear"))
        {
            PacketDispatcher.DisallowedSentPackets.Clear();
            PacketDispatcher.DisallowedReceivedPackets.Clear();
        }

        ImGui.TextUnformatted($"Blocked Sending Packets: {string.Join(", ", PacketDispatcher.DisallowedSentPackets)}");
        ImGui.TextUnformatted($"Blocked Receiving Packets: {string.Join(", ", PacketDispatcher.DisallowedReceivedPackets)}");
    }
}
