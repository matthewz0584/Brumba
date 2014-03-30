using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Brumba.MapProvider;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.Simulation.Physics;
using Point = Microsoft.Xna.Framework.Point;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace Brumba.Simulation.EnvironmentBuilder
{
    public class BoxWorldBuilder
    {
        private readonly BoxWorldBuilderSettings _settings;
        private readonly IPixelBlockGlue _pixelGlue;
        private readonly IPixelColorClassifier _pixelClassifier;

        public BoxWorldBuilder(BoxWorldBuilderSettings settings, IPixelBlockGlue pixelGlue, IPixelColorClassifier pixelClassifier)
        {
            _settings = settings;
            _pixelGlue = pixelGlue;
            _pixelClassifier = pixelClassifier;
        }

        public IEnumerable<SingleShapeEntity> CreateBoxes(Bitmap bitmap)
        {
            return _pixelClassifier.
                Classify(bitmap, 
                    _settings.BoxTypes.Select(bt => bt.ColorOnMapImage).Concat(new[] {_settings.FloorType.ColorOnMapImage})).
                Where(pair => pair.Key != _settings.FloorType.ColorOnMapImage).
                SelectMany(pair => 
                    CreateTypeBoxes(_settings.BoxTypes.Single(bt => bt.ColorOnMapImage == pair.Key), pair.Value));
        }

        public IEnumerable<SingleShapeEntity> CreateTypeBoxes(BoxType type, IEnumerable<Point> pixels)
        {
            return _pixelGlue.GluePixelBlocks(pixels).Select((pixelBlock, i) =>
            {
                var center =
                    (new Vector3(pixelBlock.LeftTop.X, 0, pixelBlock.LeftTop.Y) +
                     new Vector3(pixelBlock.Width / 2.0f, (float)type.Height / 2 / _settings.GridCellSize, pixelBlock.Height / 2.0f)) * _settings.GridCellSize;
                var dimensions =
                    new Vector3(pixelBlock.Width, (float)type.Height / _settings.GridCellSize, pixelBlock.Height) * _settings.GridCellSize;
                return new SingleShapeEntity(
                    new BoxShape(new BoxShapeProperties((float)type.Mass, new Pose(), TypeConversion.FromXNA(dimensions)) { TextureFileName = type.TextureFileName }),
                    TypeConversion.FromXNA(center))
                {
                    State = { Name = string.Format("{0} {1}", type.ColorOnMapImage, i) }
                };
            });
        }
    }
}