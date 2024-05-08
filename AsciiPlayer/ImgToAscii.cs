#define colorEnabled

using OpenCvSharp;
using System;
using System.IO;
using System.Text;

namespace AsciiPlayer
{
    internal class ImgToAscii
    {
        private const int withDivisor = 3;
        private const int heightDivisor = withDivisor * 2;

        public static void Compile(Mat data, string title)
        {
            byte lastR = 0, lastG = 0, lastB = 0;

            char[] charSet = " .,:;i1tfLCOG08@#".ToCharArray();

            const int maxBrightness = 256 * 3;
            const int colorLessMinCharSetLengh = 1;

            int minColorChangeNeeded = 1;
            int blankBrightNess = ((maxBrightness) / charSet.Length) * colorLessMinCharSetLengh;

            StringBuilder sb = new StringBuilder();

            sb.Append($"\u001b]0;" + title + "\u0007");
            sb.Append($"\u001b[8;{(data.Height / heightDivisor) + 2};{(data.Width / withDivisor) + 1}t");

            for (int y = 0; y < data.Height; y += heightDivisor)
            {
                if (y > data.Height) break;

                for (int x = 0; x < data.Width; x += withDivisor)
                {
                    if (x > data.Width) break;

                    Vec3b pixelValue = data.At<Vec3b>(y, x);

                    int brightness = pixelValue.Item0 + pixelValue.Item1 + pixelValue.Item2;

                    char predictedChar = charSet[(brightness * charSet.Length) / maxBrightness];

                    Program.MaximizeBrightness(ref pixelValue.Item2, ref pixelValue.Item1, ref pixelValue.Item0);
                    Program.QuantinizePixel(ref pixelValue.Item2, ref pixelValue.Item1, ref pixelValue.Item0);
#if colorEnabled
                    int colorDiff = Math.Abs(pixelValue.Item2 - lastR) + Math.Abs(pixelValue.Item1 - lastG) + Math.Abs(pixelValue.Item0 - lastB);

                    if (brightness > blankBrightNess && colorDiff > minColorChangeNeeded) //Dont change the color if its too similar to the current one
                    {
                        sb.Append(Program.Pastel(predictedChar, pixelValue.Item2, pixelValue.Item1, pixelValue.Item0));

                        lastR = pixelValue.Item2;
                        lastG = pixelValue.Item1;
                        lastB = pixelValue.Item0;
                    }
                    else
                    {
                        sb.Append(predictedChar);
                    }

#else
                    sb.Append(predictedChar);
#endif
                }

                sb.Append('\n');
            }

            File.WriteAllBytes("buffer.txt", Encoding.UTF8.GetBytes(sb.ToString()));
        }
    }
}