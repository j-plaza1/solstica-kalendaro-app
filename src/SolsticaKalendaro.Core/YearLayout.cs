using System.Collections.Concurrent;

namespace SolsticaKalendaro.Core;

/// <summary>
/// The sixteen blocks of a Solstica Kalendaro year, in the order in which they occur.
/// <see cref="Supertago"/> is the exception: it is a one-day extra-weekly insertion at a
/// Sunday|Monday seam, and which seam that is depends on the validity period (section 9.3).
/// </summary>
public enum PeriodKind
{
    Unua = 1,
    Dua,
    Tria,
    EkvinoksoI,
    Kvara,
    Kvina,
    Sesa,
    Jarmezo,
    Sepa,
    Oka,
    Naua,
    EkvinoksoII,
    Deka,
    DekUnua,
    DekDua,
    Jarfino,

    /// <summary>Intercalated day of leap years. Not part of the linear sequence.</summary>
    Supertago = 100
}

public static class PeriodKindExtensions
{
    public static bool IsMonth(this PeriodKind p) => p is
        PeriodKind.Unua or PeriodKind.Dua or PeriodKind.Tria or
        PeriodKind.Kvara or PeriodKind.Kvina or PeriodKind.Sesa or
        PeriodKind.Sepa or PeriodKind.Oka or PeriodKind.Naua or
        PeriodKind.Deka or PeriodKind.DekUnua or PeriodKind.DekDua;

    public static bool IsTransitionBlock(this PeriodKind p) => p is
        PeriodKind.EkvinoksoI or PeriodKind.Jarmezo or PeriodKind.EkvinoksoII;

    /// <summary>Extra-weekly days stand outside the seven-day cycle and have no weekday.</summary>
    public static bool IsExtraWeekly(this PeriodKind p) =>
        p is PeriodKind.Jarfino or PeriodKind.Supertago;

    /// <summary>Canonical Esperanto name.</summary>
    public static string Name(this PeriodKind p) => p switch
    {
        PeriodKind.EkvinoksoI => "Ekvinokso I",
        PeriodKind.EkvinoksoII => "Ekvinokso II",
        PeriodKind.DekUnua => "Dek-unua",
        PeriodKind.DekDua => "Dek-dua",
        PeriodKind.Naua => "Na\u016da",
        _ => p.ToString()
    };
}

/// <summary>
/// Widths of the three transition blocks. They always total 28 days; what changes across
/// validity periods is how those 28 are arranged. Only one structural reform occurs in the
/// whole tabulated window (section 9.4): 7/14/7 gives way to 7/7/14 in the year 3324.
/// </summary>
public sealed record BlockWidths(int EkvinoksoI, int Jarmezo, int EkvinoksoII)
{
    public const int Total = 28;

    /// <summary>The arrangement of the current period. Jarmezo carries the wide block.</summary>
    public static readonly BlockWidths SevenFourteenSeven = new(7, 14, 7);

    /// <summary>From the year 3324. Ekvinokso II carries the wide block instead.</summary>
    public static readonly BlockWidths SevenSevenFourteen = new(7, 7, 14);

    public int Width(PeriodKind p) => p switch
    {
        PeriodKind.EkvinoksoI => EkvinoksoI,
        PeriodKind.Jarmezo => Jarmezo,
        PeriodKind.EkvinoksoII => EkvinoksoII,
        _ => throw new ArgumentOutOfRangeException(nameof(p), "Not a transition block.")
    };

    public bool IsWellFormed =>
        EkvinoksoI > 0 && Jarmezo > 0 && EkvinoksoII > 0
        && EkvinoksoI % 7 == 0 && Jarmezo % 7 == 0 && EkvinoksoII % 7 == 0
        && EkvinoksoI + Jarmezo + EkvinoksoII == Total;

    public override string ToString() => $"{EkvinoksoI}/{Jarmezo}/{EkvinoksoII}";
}

/// <summary>
/// Start ordinal and length of every block, for a given transition-block arrangement.
/// Ordinals are common-year ordinals: the Supertago is not accounted for here.
/// </summary>
public sealed class YearLayout
{
    private static readonly ConcurrentDictionary<BlockWidths, YearLayout> Cache = new();

    private readonly int[] _start = new int[16];
    private readonly int[] _length = new int[16];

    public BlockWidths Blocks { get; }

    /// <summary>Blocks in order of occurrence, excluding the Supertago.</summary>
    public static readonly PeriodKind[] Sequence =
    [
        PeriodKind.Unua, PeriodKind.Dua, PeriodKind.Tria,
        PeriodKind.EkvinoksoI,
        PeriodKind.Kvara, PeriodKind.Kvina, PeriodKind.Sesa,
        PeriodKind.Jarmezo,
        PeriodKind.Sepa, PeriodKind.Oka, PeriodKind.Naua,
        PeriodKind.EkvinoksoII,
        PeriodKind.Deka, PeriodKind.DekUnua, PeriodKind.DekDua,
        PeriodKind.Jarfino
    ];

    private YearLayout(BlockWidths blocks)
    {
        if (!blocks.IsWellFormed)
            throw new ArgumentException($"Transition blocks {blocks} must be positive multiples of 7 summing to 28.", nameof(blocks));

        Blocks = blocks;
        int ordinal = 1;
        for (int i = 0; i < Sequence.Length; i++)
        {
            var p = Sequence[i];
            int len = p.IsMonth() ? 28 : p == PeriodKind.Jarfino ? 1 : blocks.Width(p);
            _start[i] = ordinal;
            _length[i] = len;
            ordinal += len;
        }
        // 336 months + 28 transition + 1 Jarfino
        System.Diagnostics.Debug.Assert(ordinal - 1 == 365);
    }

    public static YearLayout For(BlockWidths blocks) => Cache.GetOrAdd(blocks, b => new YearLayout(b));

    private static int Index(PeriodKind p) => Array.IndexOf(Sequence, p) is var i && i >= 0
        ? i
        : throw new ArgumentOutOfRangeException(nameof(p), $"{p} has no fixed place in the year.");

    /// <summary>Common-year ordinal of day 1 of the block.</summary>
    public int Start(PeriodKind p) => _start[Index(p)];

    public int Length(PeriodKind p) => _length[Index(p)];

    /// <summary>Common-year ordinal of a block day. Does not account for the Supertago.</summary>
    public int CommonOrdinal(PeriodKind p, int day)
    {
        int i = Index(p);
        if (day < 1 || day > _length[i])
            throw new ArgumentOutOfRangeException(nameof(day), $"Day {day} is outside {p.Name()} (1..{_length[i]}).");
        return _start[i] + day - 1;
    }

    /// <summary>Inverse of <see cref="CommonOrdinal"/>.</summary>
    public (PeriodKind Period, int Day) FromCommonOrdinal(int ordinal)
    {
        if (ordinal is < 1 or > 365)
            throw new ArgumentOutOfRangeException(nameof(ordinal));
        for (int i = Sequence.Length - 1; i >= 0; i--)
            if (ordinal >= _start[i])
                return (Sequence[i], ordinal - _start[i] + 1);
        throw new InvalidOperationException("Unreachable.");
    }
}
