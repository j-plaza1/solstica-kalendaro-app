using Microsoft.Maui.Controls.Shapes;
using SolsticaKalendaro.App.Resources.Strings;

namespace SolsticaKalendaro.App;

/// <summary>
/// What a button says when it cannot do what it offers. A button that is simply disabled asks
/// the reader to work out why on their own, and the answer — the calendar has not begun yet,
/// and begins on a particular day — is not one anybody can guess. So the buttons stay live and
/// this says why, where the reader asked.
///
/// A layer over the page rather than a page of its own: what it explains is on the screen
/// underneath, and reading the answer should not take that screen away. Tapping outside it, the
/// system's back button and the button in the corner all close it, and while it is open it is
/// the only thing that can be touched.
/// </summary>
public static class MessageOverlay
{
    /// <summary>
    /// The one message on screen, if there is one. One at a time: it is opened by a tap on a
    /// page that it then covers.
    /// </summary>
    private static (Grid Layer, Grid Root, ContentPage Page)? _open;

    public static bool IsOpen => _open is not null;

    /// <summary>
    /// Shows a message over a page. The page's own content is a Grid — every page that shows
    /// one has one — and the layer covers the whole of it.
    /// </summary>
    /// <param name="moreInfo">
    /// Whether to offer the introduction. It is offered where the answer is about the calendar
    /// itself rather than about what was typed.
    /// </param>
    public static void Show(ContentPage page, string message, bool moreInfo)
    {
        if (page.Content is not Grid root) return;

        Dismiss();

        var layer = Build(page, message, moreInfo);
        root.Add(layer);
        Grid.SetRowSpan(layer, Math.Max(root.RowDefinitions.Count, 1));
        Grid.SetColumnSpan(layer, Math.Max(root.ColumnDefinitions.Count, 1));

        _open = (layer, root, page);

        // A message belongs to the page under it. Leaving that page — back, or Go arriving
        // somewhere — takes the message with it, rather than leaving one that is no longer
        // on screen but still swallows the next press of the back button.
        page.Disappearing += OnPageLeft;
    }

    private static void OnPageLeft(object? sender, EventArgs e) => Dismiss();

    /// <summary>
    /// Closes the message, and says whether there was one. The pages call it from the system's
    /// back button, which should close the message before it leaves the page.
    /// </summary>
    public static bool Dismiss()
    {
        if (_open is not { } open) return false;

        open.Page.Disappearing -= OnPageLeft;
        open.Root.Remove(open.Layer);
        _open = null;
        return true;
    }

    private static Grid Build(ContentPage page, string message, bool moreInfo)
    {
        var layer = new Grid
        {
            // The page is still there to be read; what it cannot be is touched.
            BackgroundColor = Palette.Colour("Scrim").WithAlpha(0.4f)
        };
        layer.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => Dismiss()) });

        var card = new Border
        {
            Background = new SolidColorBrush(Palette.Colour("Paper")),
            Stroke = new SolidColorBrush(Palette.Colour("CardEdge")),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(14) },
            Padding = new Thickness(20),
            Margin = new Thickness(20),
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Center,
            Content = Card(page, message, moreInfo)
        };

        // A tap on the card is not a tap outside it. Without this it would reach the layer.
        card.GestureRecognizers.Add(new TapGestureRecognizer());

        layer.Add(card);
        return layer;
    }

    private static View Card(ContentPage page, string message, bool moreInfo)
    {
        var text = new Label
        {
            Text = message,
            FontFamily = "Plex",
            FontSize = 15,
            LineHeight = 1.4,
            TextColor = Palette.Colour("Ink")
        };

        // At the largest text sizes the sentence is taller than the screen, and a message that
        // cannot be read to the end says nothing.
        var scroller = new ScrollView { Content = text };

        // Side by side while they fit, and one above the other when the words have grown too
        // wide for that.
        var buttons = new FlexLayout
        {
            Direction = Microsoft.Maui.Layouts.FlexDirection.Row,
            Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap,
            JustifyContent = Microsoft.Maui.Layouts.FlexJustify.End,
            Margin = new Thickness(0, 10, 0, 0)
        };

        if (moreInfo)
            buttons.Add(Action(AppStrings.MoreInfo, Palette.Colour("Accent"), "PlexMedium", () =>
            {
                Dismiss();
                page.Navigation.PushModalAsync(new IntroPage(firstTime: false, screen: 0));
            }));

        buttons.Add(Action(AppStrings.IntroClose, Palette.Colour("Ink"), "PlexSemiBold", () => Dismiss()));

        var column = new VerticalStackLayout { Spacing = 0 };
        column.Add(scroller);
        column.Add(buttons);
        return column;
    }

    private static Button Action(string text, Color colour, string family, Action tapped)
    {
        var button = new Button
        {
            Text = text,
            LineBreakMode = LineBreakMode.WordWrap,
            FontFamily = family,
            FontSize = 14,
            TextColor = colour,
            BackgroundColor = Colors.Transparent,
            BorderWidth = 0,
            Padding = new Thickness(12, 10),
            MinimumHeightRequest = 44,
            MinimumWidthRequest = 44
        };

        button.Clicked += (_, _) => tapped();
        return button;
    }
}
