//#define colorEnabled

using NAudio.Wave;
using OpenCvSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace AsciiPlayer
{
    internal class Program
    {
        private const int GWL_STYLE = -16;
        private const int WS_MAXIMIZEBOX = 0x10000;
        private const int WS_MINIMIZEBOX = 0x20000;

        private static Stream consoleStream = null;
        private static IntPtr consoleHandle = IntPtr.Zero;

        private static void SetPosition(int i) => SetConsoleCursorPosition(consoleHandle, i);

        private static void Write(string str) => Write(Encoding.UTF8.GetBytes(str));

        private static void Write(byte[] buffer) => consoleStream.Write(buffer, 0, buffer.Length);

        private static void Write(byte b) => consoleStream.WriteByte(b);

        private static void ResetColor() => Write("\u001b[0m");

        private static string Pastel(string text, int r, int g, int b) => "\u001b[38;2;" + r + ";" + g + ";" + b + "m" + text;

        private static string SetPosition(int row, int collum) => "\u001b[" + row + ";" + collum + "H";

        [StructLayout(LayoutKind.Sequential)]
        public struct CONSOLE_SCREEN_BUFFER_INFO
        {
            public COORD dwSize;
            public COORD dwCursorPosition;
            public short wAttributes;
            public SMALL_RECT srWindow;
            public COORD dwMaximumWindowSize;
        }

        [StructLayout(LayoutKind.Sequential)] public struct COORD { public short X; public short Y; }

        [StructLayout(LayoutKind.Sequential)] public struct SMALL_RECT { public short Left; public short Top; public short Right; public short Bottom; }

        [StructLayout(LayoutKind.Sequential)] public struct CONSOLE_SCREEN_BUFFER_INFOEX { public uint cbSize; public COORD dwSize; public COORD dwCursorPosition; public ushort wAttributes; public SMALL_RECT srWindow; public COORD dwMaximumWindowSize; public ushort wPopupAttributes; public bool bFullscreenSupported; [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] public uint[] ColorTable; public uint ulInformationalMask; }
        [StructLayout(LayoutKind.Sequential)] public struct CONSOLE_FONT_INFOEX { public uint cbSize; public uint nFont; public COORD dwFontSize; public int FontFamily; public int FontWeight; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string FaceName; }

        [DllImport("kernel32.dll", SetLastError = true)] public static extern bool GetConsoleScreenBufferInfoEx(IntPtr hConsoleOutput, ref CONSOLE_SCREEN_BUFFER_INFOEX ConsoleScreenBufferInfoEx);

        [DllImport("kernel32.dll", SetLastError = true)] public static extern bool SetConsoleScreenBufferInfoEx(IntPtr hConsoleOutput, ref CONSOLE_SCREEN_BUFFER_INFOEX ConsoleScreenBufferInfoEx);

        [DllImport("user32.dll", SetLastError = true)] public static extern bool ShowScrollBar(IntPtr hWnd, int wBar, bool bShow);

        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetCurrentConsoleFontEx(IntPtr consoleOutput, bool maximumWindow, ref CONSOLE_FONT_INFOEX consoleCurrentFontEx);

        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GetConsoleScreenBufferInfo(IntPtr hConsoleOutput, out CONSOLE_SCREEN_BUFFER_INFO lpConsoleScreenBufferInfo);

        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetConsoleScreenBufferSize(IntPtr hConsoleOutput, COORD dwSize);

        [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")] private static extern bool SetConsoleTextAttribute(IntPtr hConsoleOutput, int wAttributes);

        [DllImport("kernel32.dll")] private static extern bool SetConsoleCursorPosition(IntPtr hConsoleOutput, int coord);

        [DllImport("kernel32.dll")] private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")] private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")] public static extern int DeleteMenu(IntPtr hMenu, int nPosition, int wFlags);

        [DllImport("user32.dll")] private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("kernel32.dll", ExactSpelling = true)] private static extern IntPtr GetConsoleWindow();

        public static class ConsoleHelper
        {
            private const int FixedWidthTrueType = 54; private const int StandardOutputHandle = -11; [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)] internal static extern bool SetCurrentConsoleFontEx(IntPtr hConsoleOutput, bool MaximumWindow, ref FontInfo ConsoleCurrentFontEx); [return: MarshalAs(UnmanagedType.Bool)][DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)] internal static extern bool GetCurrentConsoleFontEx(IntPtr hConsoleOutput, bool MaximumWindow, ref FontInfo ConsoleCurrentFontEx); private static readonly IntPtr ConsoleOutputHandle = GetStdHandle(StandardOutputHandle); [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] public struct FontInfo { internal int cbSize; internal int FontIndex; internal short FontWidth; public short FontSize; public int FontFamily; public int FontWeight; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string FontName; }

            public static FontInfo[] SetCurrentFont(string font, short fontSize = 0)
            {
                FontInfo before = new FontInfo { cbSize = Marshal.SizeOf<FontInfo>() }; if (GetCurrentConsoleFontEx(ConsoleOutputHandle, false, ref before)) { FontInfo set = new FontInfo { cbSize = Marshal.SizeOf<FontInfo>(), FontIndex = 0, FontFamily = FixedWidthTrueType, FontName = font, FontWeight = 400, FontSize = fontSize > 0 ? fontSize : before.FontSize }; if (!SetCurrentConsoleFontEx(ConsoleOutputHandle, false, ref set)) { throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()); } FontInfo after = new FontInfo { cbSize = Marshal.SizeOf<FontInfo>() }; GetCurrentConsoleFontEx(ConsoleOutputHandle, false, ref after); return new[] { before, set, after }; } else { var er = Marshal.GetLastWin32Error(); Console.WriteLine("Get error " + er); throw new System.ComponentModel.Win32Exception(er); }
            }
        }

        private enum ColorDifferenceMode
        {
            GlobalBrightNess,
            ColorDifferenceChange
        }
        private static void Main(string[] arg)
        {
            string videoPath = arg.Length == 0 ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads\\Bad Apple!!.mp4") : arg[0];

            Console.Title = Path.GetFileNameWithoutExtension(videoPath);

            using (Process currentProcess = Process.GetCurrentProcess())
            {
                currentProcess.PriorityBoostEnabled = true;
                currentProcess.PriorityClass = ProcessPriorityClass.RealTime;
            }

            consoleHandle = GetStdHandle(-11);

            ConsoleHelper.SetCurrentFont("Consolas", 1);

            consoleStream = Console.OpenStandardOutput();

            if (!(GetConsoleMode(consoleHandle, out var outConsoleMode) && SetConsoleMode(consoleHandle, outConsoleMode | 0x0001 | 0x0004))) throw new Exception("Error setting console colors comptible");

            Console.OutputEncoding = Encoding.ASCII;

            //GetConsoleScreenBufferInfo(consoleHandle, out var scrBufferInfo);
            //SetConsoleScreenBufferSize(consoleHandle, new COORD { X = scrBufferInfo.dwSize.X, Y = (short)(scrBufferInfo.srWindow.Bottom - scrBufferInfo.srWindow.Top + 1) });

            Console.BufferWidth = Console.WindowWidth;
            Console.BufferHeight = Console.WindowHeight;

            IntPtr consoleWindowHandle = GetConsoleWindow();

            SetWindowLong(consoleWindowHandle, GWL_STYLE, GetWindowLong(consoleWindowHandle, GWL_STYLE) & ~WS_MAXIMIZEBOX & ~WS_MINIMIZEBOX);

            Write(Pastel("", 255, 255, 255));

            //char[] charSet = "$@B%8&WM#*oahkbdpqwmZO0QLCJUYXzcvunxrjft/\\|()1{}[]?-_+~<>i!lI;:,\"^`'.  ";

            const int maxBrightness = 256 * 3;

            char[] charSet = " .,:;i1tfLCOG08@#".ToCharArray();

            const int colorLessMinCharSetLengh = 1;
            const int minColorChangeNeeded = 64 * 2;

            const int fpsDivisor = 1;

            const int withDivisor = 2, heightDivisor = withDivisor * 2;

            const int audioBufferLengh = 1000;

            VideoCapture capture = new VideoCapture();

            int blankBrightNess = ((maxBrightness) / charSet.Length) * colorLessMinCharSetLengh;

            int timeBetweenFrames = 0;

            while (true)
            {
                MediaFoundationReader reader = new MediaFoundationReader(videoPath);

                BufferedWaveProvider bufferedWaveProvider = new BufferedWaveProvider(reader.WaveFormat);
                bufferedWaveProvider.BufferDuration = TimeSpan.FromMilliseconds(audioBufferLengh * 4);
                bufferedWaveProvider.DiscardOnBufferOverflow = true;
                bufferedWaveProvider.ReadFully = true;

                using (WaveOut player = new WaveOut())
                {
                    player.Init(bufferedWaveProvider);

                    //player.DesiredLatency = timeBetweenFrames;
                    player.Play();

                    capture.Open(videoPath, VideoCaptureAPIs.ANY);

                    int videoFps = (int)Math.Round(capture.Fps) / fpsDivisor;

                    //int timeBetweenFrames = (int)((1000 / capture.Fps) / 1.08);

                    if (timeBetweenFrames == 0)
                    {
                        timeBetweenFrames = (int)(1000 / (capture.Fps / fpsDivisor));

#if colorEnabled
                        timeBetweenFrames -= timeBetweenFrames / 16;
#endif
                    }

                    int frameWidth = (int)capture.Get(VideoCaptureProperties.FrameWidth);
                    int frameHeight = (int)capture.Get(VideoCaptureProperties.FrameHeight);

                    bool sucess = false;

                    while (!sucess)
                    {
                        try
                        {
                            Console.SetWindowSize((frameWidth / withDivisor) + 2, (frameHeight / heightDivisor) + 2);

                            sucess = true;
                        }
                        catch
                        {
                            Write("Please set console smallet with control + minus");
                            Thread.Sleep(200);
                        }
                    }

                    Mat img = new Mat();

                    Console.WriteLine("Begin extracting frames from video file..");

                    Stopwatch sw = new Stopwatch();
                    Stopwatch timeIntegrity = new Stopwatch();

                    StringBuilder sb = new StringBuilder();

                    int currentFrame = videoFps;

                    int currentSecond = 0, lastColor = 0;

                    timeIntegrity.Restart();

                    int lasSleepTime = 0;

                    long difference = 0;

                    int[] rowIndexes = new int[(frameHeight / heightDivisor) + 1];

                    while (capture.IsOpened())
                    {
                        for (int i = 0; i < fpsDivisor; i++) capture.Read(img);

                        if (img.Empty()) break;

                        if (currentFrame >= videoFps)
                        {
                            if (currentSecond > 0) difference = (timeIntegrity.ElapsedMilliseconds / currentSecond) - 1000;

                            if (timeIntegrity.ElapsedMilliseconds / 1000 < currentSecond)
                            {
                                int timeToWait = (currentSecond * 1000) - (int)timeIntegrity.ElapsedMilliseconds - 1;

                                if (timeToWait > timeBetweenFrames) timeBetweenFrames++;

                                lasSleepTime = timeToWait;

                                Thread.Sleep(timeToWait);
                            }
                            else// if (difference > timeBetweenFrames)
                            {
                                if (timeBetweenFrames > 0) timeBetweenFrames--;
                            }

                            WriteAudio(bufferedWaveProvider, reader, currentSecond == 0 ? 2 : 1);

                            currentFrame = 0;
                            currentSecond++;
                        }

                        for (int y = 0; y < img.Height; y += heightDivisor)
                        {
                            for (int x = 0; x < img.Width; x += withDivisor)
                            {
                                Vec3b pixelValue = img.At<Vec3b>(y, x);

                                int brightness = pixelValue.Item0 + pixelValue.Item1 + pixelValue.Item2;
                                char predictedChar = charSet[(brightness * charSet.Length) / maxBrightness];

#if colorEnabled
                                if (brightness > blankBrightNess && Math.Abs(brightness - lastColor) > minColorChangeNeeded) //Dont change the color if its too similar to the current one
                                {
                                    sb.Append(Pastel(predictedChar.ToString(), pixelValue.Item2, pixelValue.Item1, pixelValue.Item0));

                                    lastColor = brightness;
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

                        int currentRowIndex = 0;

#if colorEnabled
                        string frame = sb.ToString();
#else
string frame = string.Join("\n", sb.ToString().Split('\n').Select(line =>
                        {
                            string trimed = line.TrimEnd();

                            int trimedLengh = RemoveAnsi(trimed).Length;

                            int diff = rowIndexes[currentRowIndex] - trimedLengh;
                            rowIndexes[currentRowIndex] = trimedLengh;

                            currentRowIndex++;

                            if (diff <= 0) return trimed;

                            return trimed + new string(' ',diff);
                        }));
#endif


                        sb.Clear();

                        Write(frame);

                        SetPosition(0);

                        currentFrame++;

                        Console.Title = "CPF:" + frame.Length + " MS:" + sw.ElapsedMilliseconds + " TBF:" + timeBetweenFrames + " LST: " + lasSleepTime + " D:" + difference;

                        if (sw.ElapsedMilliseconds < timeBetweenFrames) Thread.Sleep(timeBetweenFrames - (int)sw.ElapsedMilliseconds);

                        sw.Restart();
                    }
                }
            }
        }

        public static void WriteAudio(BufferedWaveProvider buffer, MediaFoundationReader reader, int secconds)
        {
            byte[] audioBuffer = new byte[reader.WaveFormat.AverageBytesPerSecond * secconds];
            int bytesRead = reader.Read(audioBuffer, 0, audioBuffer.Length);
            buffer.AddSamples(audioBuffer, 0, bytesRead);
        }

        public static string RemoveAnsi(string txt)
        {
            StringBuilder sb = new StringBuilder();

            bool insideCmd = false;

            foreach(char c in txt)
            {
                if (c == '\u001b') insideCmd = true;
                if (insideCmd && c == 'm') insideCmd = false;

                if (!insideCmd) sb.Append(c);
            }

            return sb.ToString();
        }
        /*public static int RoundColor(int color, int divisor)
        {
            int remainder = (color & (divisor - 1));

            return (remainder >= divisor >> 1 ? Math.Min(color + divisor - remainder, 255) : color - remainder);
        }*/

        /*public static bool IsWhiteOrGray(byte r,byte g,byte b)
        {
            return r == g && g == b || Math.Abs(r - g) <= 2 && Math.Abs(g - b) <= 2 && Math.Abs(r - b) <= 2;
        }*/
    }
}