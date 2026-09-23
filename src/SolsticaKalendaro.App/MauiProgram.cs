using Microsoft.Extensions.Logging;

namespace SolsticaKalendaro.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				// One alias per weight: FontAttributes only knows Bold, so a page that wants
				// SemiBold or Medium has to name the face itself.
				fonts.AddFont("Spectral-Regular.ttf", "Spectral");
				fonts.AddFont("Spectral-SemiBold.ttf", "SpectralSemiBold");
				fonts.AddFont("IBMPlexSans-Regular.ttf", "Plex");
				fonts.AddFont("IBMPlexSans-Medium.ttf", "PlexMedium");
				fonts.AddFont("IBMPlexSans-SemiBold.ttf", "PlexSemiBold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
