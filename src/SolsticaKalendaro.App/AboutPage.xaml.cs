using SolsticaKalendaro.App.Resources.Strings;
using SolsticaKalendaro.Core;

namespace SolsticaKalendaro.App;

/// <summary>
/// What this app is and where it came from. Installed from an APK rather than from a store, it
/// is the only thing that can say so: which version it is, whether that version is a
/// preliminary one, where newer ones appear, where the source is, and which version of the
/// proposal it implements.
///
/// The signing certificate's fingerprint is deliberately not here. An app that prints its own
/// fingerprint verifies nothing — whatever signed it will print whatever it likes — and the
/// check has to happen before installing, against the fingerprint in the README.
/// </summary>
public partial class AboutPage : ContentPage
{
    public AboutPage()
    {
        InitializeComponent();

        SemanticProperties.SetHint(BackButton, AppStrings.HintBack);

        TitleLabel.Text = AppStrings.MenuAbout;
        TaglineLabel.Text = AppStrings.AboutTagline;
        AuthorLabel.Text = AppStrings.AboutAuthor;

        AppHeading.Text = AppStrings.SectionApp.ToUpper(Language.Culture);
        ArticleHeading.Text = AppStrings.SectionArticle.ToUpper(Language.Culture);
        LicencesHeading.Text = AppStrings.SectionLicences.ToUpper(Language.Culture);

        VersionLabel.Text = Version();
        ReleasesButton.Text = AppStrings.NewVersions;
        SourceButton.Text = AppStrings.SourceCode;
        IntroButton.Text = AppStrings.IntroAgain;

        ArticleLabel.Text = string.Format(Language.Culture, AppStrings.ArticleVersion,
                                          SolsticaCalendar.SpecificationVersion);
        ArticleButton.Text = AppStrings.ReadArticle;

        LicencesLabel.Text = AppStrings.Licences;
    }

    /// <summary>
    /// What the app was built as. A preliminary release says so: its third component is zero,
    /// which is the release convention's own way of saying "not for daily use yet".
    /// </summary>
    private static string Version()
    {
        string version = AppInfo.Current.VersionString;
        string said = string.Format(Language.Culture, AppStrings.AppVersion, version);

        return ReleaseVersion.IsPreview(version)
            ? string.Format(Language.Culture, AppStrings.PreviewSuffix, said)
            : said;
    }

    private void OnBackClicked(object? sender, EventArgs e) => Navigation.PopAsync();

    private void OnNewVersions(object? sender, EventArgs e) => Links.Open(Links.Releases);

    private void OnSourceCode(object? sender, EventArgs e) => Links.Open(Links.Source);

    private void OnSeeIntroduction(object? sender, EventArgs e) =>
        Navigation.PushModalAsync(new IntroPage(firstTime: false));

    private void OnReadArticle(object? sender, EventArgs e) =>
        Links.Open(SolsticaCalendar.SpecificationUrl);
}
