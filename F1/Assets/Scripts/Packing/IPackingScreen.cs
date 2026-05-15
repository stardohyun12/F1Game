using System.Collections.Generic;

namespace PitCrew.Packing
{
    public interface IPackingScreen
    {
        void Show(OrderJob job);
        /// <summary>채널 8 정정 — 다중 주문 (각 주문 1개 = 큐브 1개).</summary>
        void Show(List<OrderJob> orders);
        void Hide();
        bool IsVisible { get; }
    }
}
