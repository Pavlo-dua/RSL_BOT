namespace RSLBot.Core.Extensions
{
    using System.Drawing;

    public static class RectangleEx
    {
        public static Point ToPoint(this Rectangle rec)
        {
            return new Point((rec.X + rec.Right) / 2, (rec.Y + rec.Bottom) / 2);
        }

    }
}
