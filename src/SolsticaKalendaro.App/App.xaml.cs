using Microsoft.Extensions.DependencyInjection;

namespace SolsticaKalendaro.App;

public partial class App : Application
{
	public App()
	{
		// Before any page is built, so that every string it asks for is already the right one.
		Language.Apply();
		InitializeComponent();

		// And before any row is drawn, so that the grid's heights and sizes are already the
		// ones the reader's font setting asks for, in the colours of the theme in force.
		TextScale.Apply(Resources);
		Palette.Apply(Resources);

		RequestedThemeChanged += OnThemeChanged;
	}

	/// <summary>
	/// The reader has moved the system between light and dark. The XAML follows by itself — the
	/// palette's colours are asked for dynamically — but the views built in code read their
	/// colours once, so the pages are built again, keeping the reader where they were.
	/// </summary>
	private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e)
	{
		if (!Palette.HasChanged) return;

		Palette.Apply(Resources);
		Rebuild.WhereTheReaderStands();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new NavigationPage(new MainPage()));
	}
}