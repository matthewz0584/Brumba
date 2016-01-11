using System;
using System.Diagnostics.Contracts;
using System.Drawing;

namespace Brumba.Utils
{
    public class CrossCountryGenerator
    {
        static readonly Random _rGen = new Random(234);

        public static Bitmap Generate(int size, float unevenness)
        {
            Contract.Requires(size > 0);
            Contract.Requires(unevenness > 0);
            Contract.Ensures(Contract.Result<Bitmap>() != null);

            return GenerateBitmap(GenerateHeightData(size, unevenness));
        }

        static Bitmap GenerateBitmap(float[,] heightData)
        {
            Contract.Requires(heightData != null);
            Contract.Ensures(Contract.Result<Bitmap>() != null);
            Contract.Ensures(Contract.Result<Bitmap>().Size.Width == heightData.GetLength(0));
            Contract.Ensures(Contract.Result<Bitmap>().Size.Height == heightData.GetLength(1));

            var bitmap = new Bitmap(heightData.GetLength(0), heightData.GetLength(1));
            for (int row = 0; row < heightData.GetLength(0); ++row)
                for (int col = 0; col < heightData.GetLength(1); ++col)
                {
                    //Terrain generator uses bitmap values of red (0 - 255) as height values in scale from -12.7 to 12.7m
                    //So I need to translate values by +12.7 and scale by 10
                    var redValue = (byte)((heightData[row, col] + 12.7) * 10);
                    if (redValue < 0)
                        redValue = 0;
                    else if (redValue > 255)
                        redValue = 255;
                    bitmap.SetPixel(row, col, Color.FromArgb(redValue, redValue, redValue));
                }
            return bitmap;
        }

        static float[,] GenerateHeightData(int size, float unevenness)
        {
            Contract.Requires(size > 0);
            Contract.Requires(unevenness > 0);
            Contract.Ensures(Contract.Result<float[,]>() != null);
            Contract.Ensures(Contract.Result<float[,]>().GetLength(0) == size);
            Contract.Ensures(Contract.Result<float[,]>().GetLength(1) == size);

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

        static float CalculateOffset(float unevenness)
        {
            Contract.Requires(unevenness > 0);
            Contract.Ensures(Contract.Result<float>() >= -unevenness / 2);
            Contract.Ensures(Contract.Result<float>() <= unevenness / 2);

            return ((float)_rGen.NextDouble() - 0.5f) * unevenness;
        }

        //static float CalculateBase(int row, int col, float[,] heightData)
        //{
        //    if (row == 0 && col == 0)
        //        return 0;
        //    if (row == 0)
        //        return heightData[0, col - 1];
        //    if (col == 0)
        //        return heightData[row - 1, 0];
        //    return (heightData[row - 1, col] + heightData[row, col - 1]) / 2;
        //    //return heightData[row, col - 1];
        //}
    }
}
