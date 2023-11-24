using NAudio.Wave;
using OpenCvSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace AsciiPlayer
{
    internal class Program
    {
        private static Stream consoleStream = null;
        private static IntPtr consoleHandle = IntPtr.Zero;

        private static void SetPosition(int i) => SetConsoleCursorPosition(consoleHandle, i);

        private static void Write(string str) => Write(Encoding.UTF8.GetBytes(str));

        private static void Write(byte[] buffer) => consoleStream.Write(buffer, 0, buffer.Length);

        private static void Write(byte b) => consoleStream.WriteByte(b);

        private static void ResetColor() => Write("\u001b[0m");

        private static string Pastel(string text, byte r, byte g, byte b) => "\u001b[38;2;" + r + ";" + g + ";" + b + "m" + text;

        [StructLayout(LayoutKind.Sequential)] public struct COORD { public short X; public short Y; }

        [StructLayout(LayoutKind.Sequential)] public struct SMALL_RECT { public short Left; public short Top; public short Right; public short Bottom; }

        [StructLayout(LayoutKind.Sequential)] public struct CONSOLE_SCREEN_BUFFER_INFOEX { public uint cbSize; public COORD dwSize; public COORD dwCursorPosition; public ushort wAttributes; public SMALL_RECT srWindow; public COORD dwMaximumWindowSize; public ushort wPopupAttributes; public bool bFullscreenSupported; [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] public uint[] ColorTable; public uint ulInformationalMask; }

        [DllImport("kernel32.dll", SetLastError = true)] public static extern bool GetConsoleScreenBufferInfoEx(IntPtr hConsoleOutput, ref CONSOLE_SCREEN_BUFFER_INFOEX ConsoleScreenBufferInfoEx);

        [DllImport("kernel32.dll", SetLastError = true)] public static extern bool SetConsoleScreenBufferInfoEx(IntPtr hConsoleOutput, ref CONSOLE_SCREEN_BUFFER_INFOEX ConsoleScreenBufferInfoEx);

        [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] public static extern bool ShowScrollBar(IntPtr hWnd, int wBar, bool bShow);

        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetCurrentConsoleFontEx(IntPtr consoleOutput, bool maximumWindow, ref CONSOLE_FONT_INFOEX consoleCurrentFontEx);

        [StructLayout(LayoutKind.Sequential)] public struct CONSOLE_FONT_INFOEX { public uint cbSize; public uint nFont; public COORD dwFontSize; public int FontFamily; public int FontWeight; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string FaceName; }

        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GetConsoleScreenBufferInfo(IntPtr hConsoleOutput, out CONSOLE_SCREEN_BUFFER_INFO lpConsoleScreenBufferInfo);

        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetConsoleScreenBufferSize(IntPtr hConsoleOutput, COORD dwSize);

        [StructLayout(LayoutKind.Sequential)]
        public struct CONSOLE_SCREEN_BUFFER_INFO
        {
            public COORD dwSize;
            public COORD dwCursorPosition;
            public short wAttributes;
            public SMALL_RECT srWindow;
            public COORD dwMaximumWindowSize;
        }

        [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")] private static extern bool SetConsoleTextAttribute(IntPtr hConsoleOutput, int wAttributes);

        [DllImport("kernel32.dll")] private static extern bool SetConsoleCursorPosition(IntPtr hConsoleOutput, int coord);

        [DllImport("kernel32.dll")] private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")] private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int GWL_STYLE = -16;
        private const int WS_MAXIMIZEBOX = 0x10000;
        private const int WS_MINIMIZEBOX = 0x20000;

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

        private static void Main(string[] arg)
        {
            string videoPath = arg.Length == 0 ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads\\Cry_About_It.mp4") : arg[0];

            Console.Title = Path.GetFileNameWithoutExtension(videoPath);

            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;

            consoleHandle = GetStdHandle(-11);

            ConsoleHelper.SetCurrentFont("Consolas", 1);
            consoleStream = Console.OpenStandardOutput();

            if (!(GetConsoleMode(consoleHandle, out var outConsoleMode) && SetConsoleMode(consoleHandle, outConsoleMode | 0x0001 | 0x0004))) throw new Exception("Error setting console colors comptible");

            GetConsoleScreenBufferInfo(consoleHandle, out var scrBufferInfo);
            SetConsoleScreenBufferSize(consoleHandle, new COORD { X = scrBufferInfo.dwSize.X, Y = (short)(scrBufferInfo.srWindow.Bottom - scrBufferInfo.srWindow.Top + 1) });

            IntPtr consoleWindowHandle = GetConsoleWindow();

            SetWindowLong(consoleWindowHandle, GWL_STYLE, GetWindowLong(consoleWindowHandle, GWL_STYLE) & ~WS_MAXIMIZEBOX & ~WS_MINIMIZEBOX);

            //char[] charSet = "$@B%8&WM#*oahkbdpqwmZO0QLCJUYXzcvunxrjft/\\|()1{}[]?-_+~<>i!lI;:,\"^`'.  ";

            char[] charSet = " .,:;i1tfLCG08@#".ToCharArray();

            const int withDivisor = 4;
            const int heightDivisor = withDivisor * 2;

            const int fpsDivisor = 3;

            int timeBetweenFrames = 0;

            while (true)
            {
                MediaFoundationReader reader = new MediaFoundationReader(videoPath);

                BufferedWaveProvider bufferedWaveProvider = new BufferedWaveProvider(reader.WaveFormat);
                bufferedWaveProvider.BufferDuration = TimeSpan.FromMilliseconds(1000);
                bufferedWaveProvider.DiscardOnBufferOverflow = true;

                WaveOut player = new WaveOut();
                player.Init(bufferedWaveProvider);

                player.Play();

                VideoCapture capture = new VideoCapture(videoPath);

                int videoFps = (int)capture.Fps / fpsDivisor;

                //int timeBetweenFrames = (int)((1000 / capture.Fps) / 1.08);

                if (timeBetweenFrames == 0)
                {
                    timeBetweenFrames = (int)(1000 / (capture.Fps / fpsDivisor));

                    timeBetweenFrames -= timeBetweenFrames / 8;
                }

                int frameWidth = (int)capture.Get(VideoCaptureProperties.FrameWidth);
                int frameHeight = (int)capture.Get(VideoCaptureProperties.FrameHeight);

                try
                {
                    Console.SetWindowSize((frameWidth / withDivisor) + 1, (frameHeight / heightDivisor) + 2);
                }
                catch { }

                Mat img = new Mat();

                Console.WriteLine("Begin extracting frames from video file..");

                Stopwatch sw = new Stopwatch();
                Stopwatch timeIntegrity = new Stopwatch();

                StringBuilder sb = new StringBuilder();

                int maxBrightness = 256 * 3;

                int currentFrame = videoFps;

                int currentSecond = 0, lastColor = 0;

                timeIntegrity.Restart();

                while (capture.IsOpened())
                {
                    for (int i = 0; i < fpsDivisor; i++) capture.Read(img);

                    if (img.Empty()) break;

                    if (currentFrame >= videoFps)
                    {
                        currentFrame = 0;

                        if (timeIntegrity.ElapsedMilliseconds / 1000 < currentSecond)
                        {
                            timeBetweenFrames++;

                            Thread.Sleep((currentSecond * 1000) - (int)timeIntegrity.ElapsedMilliseconds);
                        }
                        else timeBetweenFrames--;

                        byte[] buffer = new byte[reader.WaveFormat.AverageBytesPerSecond];
                        int bytesRead = reader.Read(buffer, 0, buffer.Length);
                        bufferedWaveProvider.AddSamples(buffer, 0, bytesRead);

                        currentSecond++;
                    }

                    for (int y = 0; y < img.Height; y += heightDivisor)
                    {
                        for (int x = 0; x < img.Width; x += withDivisor)
                        {
                            Vec3b pixelValue = img.At<Vec3b>(y, x);

                            int brightness = (pixelValue.Item0 + pixelValue.Item1 + pixelValue.Item2);

                            //sb.Append(charSet[(brightness * charSet.Length) / maxBrightness]);

                            if (Math.Abs(brightness - lastColor) > 64)
                            {
                                sb.Append(Pastel(charSet[(brightness * charSet.Length) / maxBrightness].ToString(), pixelValue.Item2, pixelValue.Item1, pixelValue.Item0));

                                lastColor = brightness;
                            }
                            else
                            {
                                sb.Append(charSet[(brightness * charSet.Length) / maxBrightness]);
                            }
                        }

                        sb.Append('\n');
                    }

                    Write(sb.ToString());
                    SetPosition(0);

                    currentFrame++;

                    Console.Title = "CPF:" + sb.Length + " MS:" + sw.ElapsedMilliseconds + " TBF:" + timeBetweenFrames;

                    sb.Clear();

                    if (sw.ElapsedMilliseconds < timeBetweenFrames) Thread.Sleep(timeBetweenFrames - (int)sw.ElapsedMilliseconds);

                    sw.Restart();
                }
            }
        }
    }
}