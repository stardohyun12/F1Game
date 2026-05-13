namespace PitCrew.Packing
{
    public interface IPackingScreen
    {
        void Show(OrderJob job);
        void Hide();
        bool IsVisible { get; }
    }
}
