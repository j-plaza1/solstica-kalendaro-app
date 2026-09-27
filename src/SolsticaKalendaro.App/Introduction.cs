namespace SolsticaKalendaro.App;

/// <summary>
/// Whether the reader has met the introduction. Shown once, on the first launch; after that it
/// is there to be asked for, from the menu and from About, and never again unasked.
/// </summary>
public static class Introduction
{
    private const string Preference = "introduction-seen";

    public static bool Seen => Preferences.Get(Preference, false);

    /// <summary>Skipping and finishing say the same thing: it has been offered.</summary>
    public static void MarkSeen() => Preferences.Set(Preference, true);
}
