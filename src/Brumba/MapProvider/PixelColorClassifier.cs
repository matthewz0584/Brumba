using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using xColor = Microsoft.Xna.Framework.Color;
using xPoint = Microsoft.Xna.Framework.Point;
using xVector3 = Microsoft.Xna.Framework.Vector3;

namespace Brumba.MapProvider
{
    public interface IPixelColorClassifier
    {
        IDictionary<xColor, List<xPoint>> Classify(Bitmap bitmap, IEnumerable<xColor> classColors);
    }

    public class PixelColorClassifier : IPixelColorClassifier
    {
        public IDictionary<xColor, List<xPoint>> Classify(Bitmap bitmap, IEnumerable<xColor> classColors)
        {
            var colorsPixels = classColors.ToDictionary(ot => ot, ot => new List<xPoint>());
            for (var i = 0; i < bitmap.Height; i++)
                for (var j = 0; j < bitmap.Width; j++)
					colorsPixels[GetColorClass(ColorToXColor(bitmap.GetPixel(j, i)), classColors)].Add(new xPoint(j, bitmap.Height - i - 1));
            return colorsPixels;
        }

        public xColor GetColorClass(xColor color, IEnumerable<xColor> classColors)
        {
            return classColors
                .Select(cc => new { ClassColor = cc, DistToClass = (cc.ToVector3() - color.ToVector3()).Length() })
                .OrderBy(classColorDist => classColorDist.DistToClass).ThenBy(classColorDist => classColorDist.ClassColor.ToString())
                .First().ClassColor;
        }

        static xColor ColorToXColor(Color color)
        {
            return new xColor(color.R, color.G, color.B);
        }
    }
}