using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.STD;
using Lumina.Excel.Sheets;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace Automaton.Utilities;
internal static partial class Extensions
{
    public static uint Reverse(this uint value)
        => ((value & 0x000000FFu) << 24) | ((value & 0x0000FF00u) << 8) |
            ((value & 0x00FF0000u) >> 8) | ((value & 0xFF000000u) >> 24);

    public static int RoundOff(this int i, int sliderIncrement) => (int)Math.Round(i / Convert.ToDouble(sliderIncrement)) * sliderIncrement;
    public static float RoundOff(this float i, float sliderIncrement) => (float)Math.Round(i / sliderIncrement) * sliderIncrement;

    public static Vector2 Vec2(this int i) => new(i);
    public static Vector2 ToVec2(this Point p) => new(p.X, p.Y);
    public static Point ToPoint(this Vector2 v) => new((int)Math.Round(v.X), (int)Math.Round(v.Y));
    public static Vector3 ToVector3(this (float X, float Y, float Z) t) => new(t.X, t.Y, t.Z);
    public static bool TryParseVector3(this string input, out Vector3 output)
    {
        output = Vector3.Zero;
        var pattern = @"(-?\d+(\.\d+)?),(-?\d+(\.\d+)?),(-?\d+(\.\d+)?)";
        var match = Regex.Match(input, pattern);
        if (match.Success)
        {
            var x = float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            var y = float.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
            var z = float.Parse(match.Groups[5].Value, CultureInfo.InvariantCulture);
            output = new Vector3(x, y, z);
            return true;
        }
        return false;
    }

    public static uint ToHex(this uint i) => uint.Parse(i.ToString("X"), NumberStyles.HexNumber);

    public static int Ms(this int i) => i * 1000;

    public static string ToTitleCase(this string s) => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s.ToLower());
    public static string GetLast(this string source, int tail_length) => tail_length >= source.Length ? source : source[^tail_length..];
    public static string SplitWords(this string source) => SplitWords().Replace(source, " ").Trim();
    public static string FilterNonAlphanumeric(this string input) => FilterNonAlphanumeric().Replace(input, string.Empty);
    //public static unsafe string AtkValueStringToString(this byte* atkString) => MemoryHelper.ReadSeStringNullTerminated(new nint(atkString)).ToString();

    public static List<T> ToList<T>(this StdVector<T> stdVector) where T : unmanaged
    {
        var list = new List<T>();
        var size = stdVector.LongCount;

        unsafe
        {
            T* current = stdVector.First;
            for (var i = 0; i < size; i++)
            {
                list.Add(current[i]);
            }
        }

        return list;
    }

    [GeneratedRegex("(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])")]
    // smart word split for things in pascal case while also handling acronyms/initialisms
    private static partial Regex SplitWords();

    [GeneratedRegex("[^\\p{L}\\p{N}]")]
    private static partial Regex FilterNonAlphanumeric();

    //public static IEnumerable<T> Enumerate<T>(this Span<T> span)
    //{
    //    foreach (var index in Enumerable.Range(0, span.Length))
    //        yield return span[index];
    //}
    //public static IEnumerable<T> Enumerate<T>(this ReadOnlySpan<T> span)
    //{
    //    foreach (var index in Enumerable.Range(0, span.Length))
    //        yield return span[index];
    //}

    public static unsafe string ValueString(this AtkValue v) => v.Type switch
    {
        ValueType.Int => $"{v.Int}",
        ValueType.String => Marshal.PtrToStringUTF8(new IntPtr(v.String)) ?? string.Empty,
        ValueType.UInt => $"{v.UInt}",
        ValueType.Bool => $"{v.Byte != 0}",
        ValueType.Float => $"{v.Float}",
        ValueType.Vector => "[Vector]",
        ValueType.ManagedString => Marshal.PtrToStringUTF8(new IntPtr(v.String))?.TrimEnd('\0') ?? string.Empty,
        ValueType.ManagedVector => "[Managed Vector]",
        _ => $"Unknown Type: {v.Type}"
    };

    public static void AddRange(this SeStringBuilder builder, IEnumerable<Payload> payloads)
    {
        foreach (var payload in payloads) { builder.Add(payload); }
    }
    public static void AddSeString(this SeStringBuilder builder, SeString str) => builder.AddRange(str.Payloads);

    internal record IngredientExtension()
    {
        public required Item Item { get; init; }
        public required int Amount { get; init; }
    }

    public static IEnumerable<IngredientExtension> Ingredients(this Recipe recipe)
    {
        var output = new List<IngredientExtension>();
        for (var i = 0; i < recipe.Ingredient.Count; i++)
        {
            try
            {
                var item = recipe.Ingredient[i].Value;
                var amount = recipe.AmountIngredient[i];

                output.Add(new IngredientExtension() { Item = item, Amount = amount });
            }
            catch { }
        }

        return output;
    }
}

