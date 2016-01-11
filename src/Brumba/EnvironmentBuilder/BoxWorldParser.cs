using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Brumba.MapProvider;
using Brumba.Simulation.Common;
using Brumba.Utils;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.Simulation.Physics;
using rVector3 = Microsoft.Robotics.PhysicalModel.Vector3;
using rPose = Microsoft.Robotics.PhysicalModel.Pose;
using xPoint = Microsoft.Xna.Framework.Point;
using xVector2 = Microsoft.Xna.Framework.Vector2;

namespace Brumba.Simulation.EnvironmentBuilder
{
    public class BoxWorldParser
    {
        private readonly BoxWorldParserSettings _settings;
        private readonly IPixelBlockGlue _pixelGlue;
        private readonly IPixelColorClassifier _pixelClassifier;

        public BoxWorldParser(BoxWorldParserSettings settings, IPixelBlockGlue pixelGlue, IPixelColorClassifier pixelClassifier)
        {
            _settings = settings;
            _pixelGlue = pixelGlue;
            _pixelClassifier = pixelClassifier;
        }

        public IEnumerable<SingleShapeEntity> ParseBoxes(Bitmap bitmap)
        {
            return _pixelClassifier.
                Classify(bitmap, 
                    _settings.BoxTypes.Select(bt => bt.ColorOnMapImage).Concat(new[] {_settings.FloorType.ColorOnMapImage})).
                Where(pair => pair.Key != _settings.FloorType.ColorOnMapImage).
                SelectMany(pair => 
                    ParseTypeBoxes(_settings.BoxTypes.Single(bt => bt.ColorOnMapImage == pair.Key), pair.Value));
        }

        public IEnumerable<SingleShapeEntity> ParseTypeBoxes(BoxType type, IEnumerable<xPoint> pixels)
        {
            return _pixelGlue.GluePixelBlocks(pixels).Select((pixelBlock, i) =>
            {
                var center = SimToMapTransformations.MapToSim((pixelBlock.LeftTop.ToVec() + pixelBlock.Size.ToVec() / 2) * 
                    _settings.GridCellSize, (float)type.Height / 2);
                var dimensions = SimToMapTransformations.MapToSim(pixelBlock.Size.ToVec() * _settings.GridCellSize, (float)type.Height);
				return new SingleShapeEntity(new BoxShape(
					new BoxShapeProperties((float)type.Mass, new rPose(), dimensions)
					{
						DiffuseColor = TypeConversion.FromXNA(type.ColorOnMapImage.ToVector4()),
						TextureFileName = type.TextureFileName
					}),
					center)
				{
					State = { Name = string.Format("{0} {1}", type.ColorOnMapImage, i), Flags = EntitySimulationModifiers.Kinematic }
				};
            });
        }
    }
}