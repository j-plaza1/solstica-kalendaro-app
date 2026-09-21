namespace SolsticaKalendaro.App;

/// <summary>Picks one of the three row templates. The outline's shape decides, not a flag.</summary>
public sealed class RowTemplateSelector : DataTemplateSelector
{
    public DataTemplate? Section { get; set; }
    public DataTemplate? Week { get; set; }
    public DataTemplate? Band { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container) => item switch
    {
        SectionView => Section!,
        WeekView => Week!,
        BandView => Band!,
        _ => throw new ArgumentOutOfRangeException(nameof(item), $"No template for {item.GetType().Name}.")
    };
}
