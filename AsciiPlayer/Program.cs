#define colorEnabled

using NAudio.Wave;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
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

        private static void WriteConsole(string str) => WriteConsole(Encoding.UTF8.GetBytes(str));

        private static void WriteConsole(ref string str) => WriteConsole(Encoding.UTF8.GetBytes(str));

        private static void WriteConsole(byte[] buffer) => consoleStream.Write(buffer, 0, buffer.Length);

        private static void WriteConsole(byte b) => consoleStream.WriteByte(b);

        private static void SetSize(int x, int y) => WriteConsole("\u001b[8;" + y + ";" + x + "t");

        private static void ResetColor() => WriteConsole("\u001b[0m");

        //private static string Pastel(string text, int r, int g, int b) => "\u001b[38;2;" + r + ";" + g + ";" + b + "m" + text;
        public static string Pastel(char c, int r, int g, int b) => "\u001b[38;2;" + r + ";" + g + ";" + b + "m" + c;

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

        [DllImport("kernel32.dll", SetLastError = true)] public static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")] private static extern bool SetConsoleTextAttribute(IntPtr hConsoleOutput, int wAttributes);

        [DllImport("kernel32.dll")] private static extern bool SetConsoleCursorPosition(IntPtr hConsoleOutput, int coord);

        [DllImport("kernel32.dll")] private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")] private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")] public static extern int DeleteMenu(IntPtr hMenu, int nPosition, int wFlags);

        [DllImport("user32.dll")] private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("kernel32.dll", ExactSpelling = true)] private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll", SetLastError = true)] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        private enum ColorDifferenceMode
        {
            GlobalBrightNess,
            ColorDifferenceChange
        }

        private static Dictionary<string, string> argsSplited = new Dictionary<string, string>();

        private static bool ParseBool(string text, bool defaultValue = default)
        {
            string trimed = text.Trim();

            if (char.ToLower(trimed[0]) == 't' || trimed == "1") return true;
            else if (char.ToLower(trimed[0]) == 'f' || trimed == "0") return false;

            return bool.TryParse(text, out bool res) ? res : defaultValue;
        }

        private static bool GetArgBool(string argName, bool defaultValue) => argsSplited.ContainsKey(argName) ? ParseBool(argsSplited[argName], defaultValue) : defaultValue;

        private static string GetArgString(string argName, string defaultValue) => argsSplited.ContainsKey(argName) ? argsSplited[argName] : defaultValue;

        private static int GetArgInt(string argName, int defaultValue) => argsSplited.ContainsKey(argName) ? int.TryParse(argsSplited[argName], out int parsed) ? parsed : defaultValue : defaultValue;

        private static void Main(string[] args)
        {
            foreach (string argument in args)
            {
                int index = argument.IndexOf('=');

                if (index == -1) continue;

                string value = argument.Substring(index + 1);

                argsSplited.Add(argument.Substring(0, index).ToLower(), (value[0] == '\"' && value[value.Length - 1] == '\"' ? value.Substring(1, value.Length - 2) : value));
            }

            foreach (DictionaryEntry element in Environment.GetEnvironmentVariables())
            {
                if (argsSplited.ContainsKey((string)element.Key)) continue;

                argsSplited.Add((string)element.Key, (string)element.Value);
            }

            string videoPath = args.Length == 0 ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads\\PABLO.mp4") : args[0];

            Console.Title = Path.GetFileNameWithoutExtension(videoPath);

            using (Process currentProcess = Process.GetCurrentProcess())
            {
                currentProcess.PriorityBoostEnabled = true;
                currentProcess.PriorityClass = ProcessPriorityClass.RealTime;
            }

            consoleHandle = GetStdHandle(-11);

            ConsoleHelper.SetCurrentFont("Consolas", 1);
            Console.CursorVisible = false;

            consoleStream = Console.OpenStandardOutput();

            if (!(GetConsoleMode(consoleHandle, out var outConsoleMode) && SetConsoleMode(consoleHandle, outConsoleMode | 0x0001 | 0x0004))) throw new Exception("Error setting console colors comptible");

            Console.OutputEncoding = Encoding.ASCII;

            //GetConsoleScreenBufferInfo(consoleHandle, out var scrBufferInfo);
            //SetConsoleScreenBufferSize(consoleHandle, new COORD { X = scrBufferInfo.dwSize.X, Y = (short)(scrBufferInfo.srWindow.Bottom - scrBufferInfo.srWindow.Top + 1) });

            /*ImgToAscii.Compile(
                 ((Bitmap)Image.FromFile("C:\\Users\\Mrgaton\\Downloads\\istockphoto-670137214-612x612.jpg")).ToMat(), "El_gato_con_gafas");

            Environment.Exit(1);*/

#if colorEnabled
            IntPtr consoleWindowHandle = GetConsoleWindow();

            SetWindowLong(consoleWindowHandle, GWL_STYLE, GetWindowLong(consoleWindowHandle, GWL_STYLE) & ~WS_MAXIMIZEBOX & ~WS_MINIMIZEBOX);

            WriteConsole(Pastel(' ', 255, 255, 255));
#endif

            //char[] charSet = "$@B%8&WM#*oahkbdpqwmZO0QLCJUYXzcvunxrjft/\\|()1{}[]?-_+~<>i!lI;:,\"^`'.  ";

            bool loopVideo = GetArgBool("loop", true);

            const int maxBrightness = 256 * 3;

            char[] charSet = " .,:;i1tfLCOG08@#".ToCharArray();

            const int colorLessMinCharSetLengh = 1;

            int fpsDivisor = GetArgInt("fpsdivisor", 2);

            int withDivisor = GetArgInt("sizedivisor", 2), heightDivisor = withDivisor * 2;

            const int audioBufferLengh = 1000;

            VideoCapture capture = new VideoCapture();

#if colorEnabled
            int minColorChangeNeeded = GetArgInt("mincolorchange", 64);

            int blankBrightNess = ((maxBrightness) / charSet.Length) * colorLessMinCharSetLengh;
#endif

            int timeBetweenFrames = 0;

            bool firstTime = true;

            while (true)
            {
                MediaFoundationReader reader = new MediaFoundationReader(videoPath);

                BufferedWaveProvider bufferedWaveProvider = new BufferedWaveProvider(reader.WaveFormat);
                bufferedWaveProvider.BufferDuration = TimeSpan.FromMilliseconds(audioBufferLengh * 4);
                bufferedWaveProvider.DiscardOnBufferOverflow = true;
                bufferedWaveProvider.ReadFully = true;

                WaveOutEvent player = new WaveOutEvent();

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

                while (firstTime && !sucess)
                {
                    try
                    {
                        Console.SetWindowSize((frameWidth / withDivisor) + 1, (frameHeight / heightDivisor) + 1);

                        sucess = true;
                    }
                    catch
                    {
                        WriteConsole("Please set console smallet with (control + minus)\n");

                        Thread.Sleep(100);
                    }
                }
                firstTime = false;

                Console.BufferWidth = Console.WindowWidth;
                Console.BufferHeight = Console.WindowHeight;

                Mat img = new Mat();

                Console.WriteLine("Begin extracting frames from video file..");

                Stopwatch sw = new Stopwatch();
                Stopwatch timeIntegrity = new Stopwatch();

                StringBuilder sb = new StringBuilder();

                int currentFrame = videoFps;

                int currentSecond = 0;

                byte lastR = 0, lastG = 0, lastB = 0;

                timeIntegrity.Restart();

                int lasSleepTime = 0;

                long difference = 0;

                int[] rowIndexes = new int[(frameHeight / heightDivisor) + 4];

                capture.Read(img);

                while (capture.IsOpened())
                {
                    for (int i = 0; i < fpsDivisor - 1; i++) capture.Grab();

                    capture.Read(img);

                    if (img.Empty()) break;

                    if (currentFrame >= videoFps)
                    {
                        if (currentSecond > 0) difference = (timeIntegrity.ElapsedMilliseconds / currentSecond) - 1000;

                        if (timeIntegrity.ElapsedMilliseconds / 1000 < currentSecond)
                        {
                            int timeToWait = (currentSecond * 1000) - (int)timeIntegrity.ElapsedMilliseconds - 1;
                            if (timeToWait > timeBetweenFrames) timeBetweenFrames++;

                            Thread.Sleep(lasSleepTime = timeToWait);
                        }
                        else// if (difference > timeBetweenFrames)
                        {
                            if (timeBetweenFrames > 0) timeBetweenFrames--;
                        }

                        WriteAudio(bufferedWaveProvider, reader, currentSecond == 0 ? 2 : 1);

                        currentFrame = 0;
                        currentSecond++;
                    }

                    for (int y = 0; y < frameHeight; y += heightDivisor)
                    {
                        if (y > frameHeight) break;

                        for (int x = 0; x < frameWidth; x += withDivisor)
                        {
                            if (x > frameWidth) break;

                            Vec3b pixelValue = img.At<Vec3b>(y, x);

                            int brightness = pixelValue.Item0 + pixelValue.Item1 + pixelValue.Item2;

                            char predictedChar = charSet[(brightness * charSet.Length) / maxBrightness];

                            //MaximizeBrightness(ref pixelValue.Item2, ref pixelValue.Item1, ref pixelValue.Item0);
                            //QuantinizePixel(ref pixelValue.Item2, ref pixelValue.Item1, ref pixelValue.Item0);
#if colorEnabled
                            int colorDiff = Math.Abs(pixelValue.Item2 - lastR) + Math.Abs(pixelValue.Item1 - lastG) + Math.Abs(pixelValue.Item0 - lastB);

                            if (brightness > blankBrightNess && colorDiff > minColorChangeNeeded) //Dont change the color if its too similar to the current one
                            {
                                sb.Append(Pastel(predictedChar, pixelValue.Item2, pixelValue.Item1, pixelValue.Item0));

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

                    int currentRowIndex = 0;

#if colorEnabled
                    string frame = sb.ToString();
#else
                    //int lengh = sb.ToString().Split('\n').Length;

                    string frame = string.Join("\n", sb.ToString().Split('\n').Select(line =>
                    {
                        string trimed = line.TrimEnd();

                        int trimedLengh = trimed.Length; //RemoveAnsi(trimed).Length;

                        int diff = rowIndexes[currentRowIndex] - trimedLengh;

                        rowIndexes[currentRowIndex++] = trimedLengh;

                        if (diff <= 0) return trimed;

                        return trimed + new string(' ', diff);
                    }));
#endif

                    sb.Clear();

                    WriteConsole(ref frame);

                    SetPosition(0);

                    currentFrame++;

                    Console.Title = "Git:Mrgaton/AsciiPlayer CPF:" + frame.Length + " MS:" + sw.ElapsedMilliseconds + " TBF:" + timeBetweenFrames + " LST: " + lasSleepTime + " D:" + difference;

                    if (sw.ElapsedMilliseconds < timeBetweenFrames) Thread.Sleep(timeBetweenFrames - (int)sw.ElapsedMilliseconds);

                    sw.Restart();
                }

                if (!loopVideo) break;

                Thread.Sleep(750);
            }
        }

#if colorEnabled
        private const byte quantinizeValue = 16;

        public static void QuantinizePixel(ref byte R, ref byte G, ref byte B)
        {
            R = (byte)((R / quantinizeValue) * quantinizeValue);
            G = (byte)((G / quantinizeValue) * quantinizeValue);
            B = (byte)((B / quantinizeValue) * quantinizeValue);
        }

        public static void MaximizeBrightness(ref byte R, ref byte G, ref byte B)
        {
            byte maxOriginal = Math.Max(R, Math.Max(G, B));

            if (maxOriginal > 0)
            {
                double factor = 255.0 / maxOriginal;

                R = (byte)(R * factor);
                G = (byte)(G * factor);
                B = (byte)(B * factor);
            }
        }

#endif

        public static void WriteAudio(BufferedWaveProvider buffer, MediaFoundationReader reader, int secconds)
        {
            byte[] audioBuffer = new byte[reader.WaveFormat.AverageBytesPerSecond * secconds];
            int bytesRead = reader.Read(audioBuffer, 0, audioBuffer.Length);
            buffer.AddSamples(audioBuffer, 0, bytesRead);
        }

        /*public static string RemoveAnsi(string txt)
        {
            StringBuilder sb = new StringBuilder();

            bool insideCmd = false;

            foreach (char c in txt)
            {
                if (c == '\u001b') insideCmd = true;
                if (insideCmd && c == 'm') insideCmd = false;

                if (!insideCmd) sb.Append(c);
            }

            return sb.ToString();
        }*/

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