using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using xColor = Microsoft.Xna.Framework.Color;
using xPoint = Microsoft.Xna.Framework.Point;
using xVector3 = Microsoft.Xna.Framework.Vector3;
using DC = System.Diagnostics.Contracts;

namespace Brumba.MapProvider
{
    [DC.ContractClassAttribute(typeof(IPixelColorClassifierContract))]
    public interface IPixelColorClassifier
    {
        IDictionary<xColor, List<xPoint>> Classify(Bitmap bitmap, IEnumerable<xColor> classColors);
    }

    public class PixelColorClassifier : IPixelColorClassifier
    {
        public IDictionary<xColor, List<xPoint>> Classify(Bitmap bitmap, IEnumerable<xColor> classColors)
        {
            var colorsPixels = classColors.ToDictionary(cc => cc, cc => new List<xPoint>());
            for (var i = 0; i < bitmap.Height; i++)
                for (var j = 0; j < bitmap.Width; j++)
					colorsPixels[GetColorClass(ColorToXColor(bitmap.GetPixel(j, i)), classColors)].Add(new xPoint(j, bitmap.Height - i - 1));
            return colorsPixels;
        }

        public xColor GetColorClass(xColor color, IEnumerable<xColor> classColors)
        {
            DC.Contract.Requires(classColors != null);
            DC.Contract.Requires(classColors.Any());
            DC.Contract.Ensures(classColors.Contains(DC.Contract.Result<xColor>()));

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

    [DC.ContractClassForAttribute(typeof(IPixelColorClassifier))]
    abstract class IPixelColorClassifierContract : IPixelColorClassifier
    {
        public IDictionary<xColor, List<xPoint>> Classify(Bitmap bitmap, IEnumerable<xColor> classColors)
        {
            DC.Contract.Requires(bitmap != null);
            DC.Contract.Requires(classColors != null);
            DC.Contract.Requires(classColors.Any());
            DC.Contract.Ensures(DC.Contract.Result<IDictionary<xColor, List<xPoint>>>() != null);
            DC.Contract.Ensures(DC.Contract.Result<IDictionary<xColor, List<xPoint>>>().Keys.Count == classColors.Count());
            DC.Contract.Ensures(DC.Contract.Result<IDictionary<xColor, List<xPoint>>>().Values.Sum(lp => lp.Count) == bitmap.Width * bitmap.Height);
            return default(IDictionary<xColor, List<xPoint>>);
        }
    }
}