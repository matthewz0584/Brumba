using System;
using System.Drawing;

namespace Brumba.Simulation.SimulationTester
{
    public class CrossCountryGenerator
    {
        private static Random m_rGen = new Random(234);

        public static Bitmap Generate(int size, float unevenness)
        {
            return GenerateBitmap(GenerateHeightData(size, unevenness));
        }

        private static Bitmap GenerateBitmap(float[,] heightData)
        {
            var bitmap = new Bitmap(heightData.GetLength(0), heightData.GetLength(1));
            for (int row = 0; row < heightData.GetLength(0); ++row)
                for (int col = 0; col < heightData.GetLength(1); ++col)
                {
                    //Terrain generator uses bitmap values of red (0 - 255) as height values in scale from -12.7 to 12.7m
                    //So I need to translate values by +12.7 and scale by 10
                    byte redValue = (byte)((heightData[row, col] + 12.7) * 10);
                    if (redValue < 0)
                        redValue = 0;
                    else if (redValue > 255)
                        redValue = 255;
                    bitmap.SetPixel(row, col, Color.FromArgb(redValue, redValue, redValue));
                }
            return bitmap;
        }

        private static float[,] GenerateHeightData(int size, float unevenness)
        {
            var heightData = new float[size, size];
            for (int row = 0; row < size; ++row)
                for (int col = 0; col < size; ++col)
                    heightData[row, col] = /*CalculateBase(row, col, heightData) +*/ CalculateOffset(unevenness);

            var zeroPlatoSize = 2;
            for (int row = size / 2 - zeroPlatoSize; row <= size / 2 + zeroPlatoSize; ++row)
                for (int col = size / 2 - zeroPlatoSize; col <= size / 2 + zeroPlatoSize; ++col)
                    heightData[row, col] = 0;
            return heightData;
        }

        private static float CalculateOffset(float unevenness)
        {
            return ((float)m_rGen.NextDouble() - 0.5f) * unevenness;
        }

        private static float CalculateBase(int row, int col, float[,] heightData)
        {
            if (row == 0 && col == 0)
                return 0;
            if (row == 0)
                return heightData[0, col - 1];
            if (col == 0)
                return heightData[row - 1, 0];
            return (heightData[row - 1, col] + heightData[row, col - 1]) / 2;
            //return heightData[row, col - 1];
        }
    }
}
