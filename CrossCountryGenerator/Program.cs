using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace CrossCountryGenerator
{
    class Program
    {
        public const int SIZE = 257;
        //Vehicle length = 0.35, wheel diameter = 0.1
        //If vehicle length = 1.0 (distance between samples by default), then wheel diameter = 0.3
        public const float WHEEL_DIAMETER = 0.3f;

        private static Random m_rGen = new Random(234);
        
        static void Main(string[] args)
        {
            GenerateBitmap(GenerateHeightData()).Save("crosscountry_02.bmp");
        }

        private static Bitmap GenerateBitmap(float[,] heightData)
        {
            var bitmap = new Bitmap(SIZE, SIZE);
            for (int row = 0; row < SIZE; ++row)
                for (int col = 0; col < SIZE; ++col)
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

        private static float[,] GenerateHeightData()
        {
            var heightData = new float[SIZE, SIZE];
            for (int row = 0; row < SIZE; ++row)
                for (int col = 0; col < SIZE; ++col)
                    heightData[row, col] = /*CalculateBase(row, col, heightData)*/ + CalculateOffset();
            return heightData;
        }

        private static float CalculateOffset()
        {            
            return ((float)m_rGen.NextDouble() - 0.5f) * WHEEL_DIAMETER;
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
