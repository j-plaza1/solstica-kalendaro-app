using Android.App;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using AndroidX.Core.View;
using Microsoft.Maui.Platform;

namespace SolsticaKalendaro.App;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        PaintSystemBars();
    }

    /// <summary>
    /// The theme can change while the app is running, and this activity says it handles that
    /// itself (<c>ConfigChanges.UiMode</c>), so the bars are painted again here.
    /// </summary>
    public override void OnConfigurationChanged(Configuration newConfig)
    {
        base.OnConfigurationChanged(newConfig);
        PaintSystemBars();
    }

    /// <summary>
    /// The status bar and the navigation bar, in the paper of the theme in force, with dark
    /// icons on the light one and light icons on the dark. MAUI has no cross-platform way to
    /// say this, which is why it is here rather than in the shared code.
    /// </summary>
    private void PaintSystemBars()
    {
        if (Window is not { } window) return;

        var paper = Palette.Colour("Paper").ToPlatform();

#pragma warning disable CA1422 // SetStatusBarColor: the replacement draws behind the bars instead,
        window.SetStatusBarColor(paper);          // which is a layout change, not a colour one.
        window.SetNavigationBarColor(paper);
#pragma warning restore CA1422

        if (WindowCompat.GetInsetsController(window, window.DecorView) is { } bars)
        {
            bool light = Palette.Wanted != AppTheme.Dark;
            bars.AppearanceLightStatusBars = light;
            bars.AppearanceLightNavigationBars = light;
        }
    }
}
