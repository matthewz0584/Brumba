using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Point = Microsoft.Xna.Framework.Point;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace Brumba.MapProvider
{
    public interface IPixelColorClassifier
    {
        IDictionary<Color, List<Point>> Classify(Bitmap bitmap, IEnumerable<Color> classColors);
    }

    public class PixelColorClassifier : IPixelColorClassifier
    {
        public IDictionary<Color, List<Point>> Classify(Bitmap bitmap, IEnumerable<Color> classColors)
        {
            var colorsPixels = classColors.ToDictionary(ot => ot, ot => new List<Point>());
            for (var i = 0; i < bitmap.Height; i++)
                for (var j = 0; j < bitmap.Width; j++)
                    colorsPixels[GetColorClass(bitmap.GetPixel(j, i), classColors)].Add(new Point(j, i));
            return colorsPixels;
        }

        public Color GetColorClass(Color color, IEnumerable<Color> classColors)
        {
            return classColors
                .Select(cc => new { ClassColor = cc, DistToClass = (ColorToVector(cc) - ColorToVector(color)).Length() })
                .OrderBy(classColorDist => classColorDist.DistToClass).ThenBy(classColorDist => classColorDist.ClassColor.ToArgb())
                .First().ClassColor;
        }

        static Vector3 ColorToVector(Color color)
        {
            return new Vector3(color.R, color.G, color.B);
        }
    }
}