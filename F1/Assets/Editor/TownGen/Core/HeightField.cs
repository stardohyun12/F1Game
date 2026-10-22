using System;

namespace PitCrew.TownGen
{
    /// <summary>땅 로컬 좌표(m, 원점 남서, +z 북)의 연속 고도 m. 두 앞단이 같은 고도맵을 공유한다(§3).</summary>
    public interface IHeightField
    {
        double HeightAt(double x, double z);
    }

    public sealed class FlatHeightField : IHeightField
    {
        readonly double h;
        public FlatHeightField(double h = 0) { this.h = h; }
        public double HeightAt(double x, double z) => h;
    }

    public sealed class FuncHeightField : IHeightField
    {
        readonly Func<double, double, double> f;
        public FuncHeightField(Func<double, double, double> f) { this.f = f; }
        public double HeightAt(double x, double z) => f(x, z);
    }

    /// <summary>
    /// 규칙 격자 고도(예: DEM 자른 것을 로컬 m 로 옮긴 것). 쌍선형 보간, 범위 밖은 가장자리 값.
    /// values[row * width + col], row 0 = 남쪽(z 작은 쪽). originX/Z = (0,0) 샘플의 로컬 위치.
    /// </summary>
    public sealed class GridHeightField : IHeightField
    {
        readonly float[] values;
        readonly int width, height;
        readonly double spacingX, spacingZ, originX, originZ;

        public GridHeightField(float[] values, int width, int height, double spacingX, double spacingZ, double originX = 0, double originZ = 0)
        {
            if (values == null || values.Length != width * height) throw new ArgumentException("values 길이 ≠ width×height");
            this.values = values; this.width = width; this.height = height;
            this.spacingX = spacingX; this.spacingZ = spacingZ; this.originX = originX; this.originZ = originZ;
        }

        public double HeightAt(double x, double z)
        {
            double fx = Clamp((x - originX) / spacingX, 0, width - 1);
            double fz = Clamp((z - originZ) / spacingZ, 0, height - 1);
            int x0 = (int)Math.Floor(fx), z0 = (int)Math.Floor(fz);
            int x1 = Math.Min(x0 + 1, width - 1), z1 = Math.Min(z0 + 1, height - 1);
            double tx = fx - x0, tz = fz - z0;
            double a = values[z0 * width + x0] * (1 - tx) + values[z0 * width + x1] * tx;
            double b = values[z1 * width + x0] * (1 - tx) + values[z1 * width + x1] * tx;
            return a * (1 - tz) + b * tz;
        }

        static double Clamp(double v, double lo, double hi) => v < lo ? lo : (v > hi ? hi : v);
    }
}
