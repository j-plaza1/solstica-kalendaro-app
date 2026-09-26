using Microsoft.Extensions.DependencyInjection;

namespace SolsticaKalendaro.App;

public partial class App : Application
{
	public App()
	{
		// Before any page is built, so that every string it asks for is already the right one.
		Language.Apply();
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new NavigationPage(new MainPage()));
	}
}