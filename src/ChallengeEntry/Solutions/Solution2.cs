using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TestEntry.Common;

namespace TestEntry.Solutions;

internal class Solution2
{
    public static string DataPath = @"";

    static int oneMegabyte = 1024 * 1024;

    public static int ThreadCount = 1;
    public static int ReadBufferInMb
    {
        set { bufferSizeFileData = value * oneMegabyte; }
    }

    static int bufferSizeFileData = 8 * oneMegabyte;
    static int bufferSizePreviousFrameData = 40;

    static int cityBufferSize = 32;

    enum StringToken
    {
        unknown = 0,
        city = 1,
        temp = 2,
    }

    enum tsNotch
    {
        Start = 0,
        FrameProcessed = 1,
        FileProcessed = 2,
    }
    static List<(long ts, tsNotch tsType)> timings = new();

    static List<(int mBytes, int threadCount, TimeSpan duration)> testRuns = new();

    static int threadBrokenStringsBufferSize = 40;
    static Dictionary<int, ThreadProcessingDataContext> Contexts = new();

    public static async Task Main(string[] args)
    {
        Console.WriteLine("Start processing measurements");
        Console.WriteLine($"threadCount={ThreadCount} readBufferSize={bufferSizeFileData}");
        Console.WriteLine();

        ThreadProcessingDataContext? threadCtx = null;
        for (int i = ThreadCount - 1; i >= 0; i--)
        {
            threadCtx = new ThreadProcessingDataContext { ThreadIndex = i, NextThreadContext = threadCtx };
            Contexts.Add(i, threadCtx);
        }

        timings.Clear();
        timings.Add((Stopwatch.GetTimestamp(), tsNotch.Start));

        List<Task> tasks = new();

        for (int i = 0; i < ThreadCount; i++)
        {
            var indexToUse = i;
            var initialToken = indexToUse == 0 ? StringToken.city : StringToken.unknown;
            tasks.Add(Task.Run(() => ReadAndProcessFileChunk(indexToUse, initialToken, Contexts[indexToUse])));
        }

        await Task.WhenAll(tasks);
        var resultDictionary = Contexts[0].Cities;

        for (int i = 1; i < ThreadCount; i++)
        {
            resultDictionary.AddDictionary(Contexts[i].Cities);
        }

        timings.Add((Stopwatch.GetTimestamp(), tsNotch.FileProcessed));

        var lastTiming = timings[timings.Count - 1];
        var firstTiming = timings[0];
        var testRunDuration = Stopwatch.GetElapsedTime(firstTiming.ts, lastTiming.ts);

        testRuns.Add((bufferSizeFileData, ThreadCount, testRunDuration));

        Console.WriteLine($"threadCount={ThreadCount} readBufferSize={bufferSizeFileData} dur={testRunDuration}");
        Console.WriteLine();

        var resultForReport = resultDictionary.GetSortedCalculatedList();
        Console.Write('{');
        for(int i=0;i<resultForReport.Count;i++)
        {
            var result = resultForReport[i];
            var commaPlaceHolder = i == resultForReport.Count - 1 ? string.Empty : Comma;
            Console.Write($"{result.Name}={result.MinTemp:F1}/{result.MeanTemp:F1}/{result.MaxTemp:F1}{commaPlaceHolder}");
        }
        Console.Write('}');

        Console.ReadLine();
    }

    private const string Comma = ",";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static unsafe void ReadAndProcessFileChunk(int threadIndex, StringToken initialTokenType, ThreadProcessingDataContext threadContext)
    {
        var cities = threadContext.Cities;
        var lastThread = threadContext.NextThreadContext == null;
        var extraLoopToStitchBuffers = false;

        using (FileStream fs = File.OpenRead(DataPath))
        {
            using (BinaryReader br = new BinaryReader(fs))
            {
                var curToken = initialTokenType;

                var fileSize = new FileInfo(DataPath).Length;
                var usedFileSize = (fileSize / ThreadCount) + 1;
                br.BaseStream.Seek(threadIndex * usedFileSize, SeekOrigin.Begin);

                long readCursor = 0;
                byte[] buffer = new byte[bufferSizeFileData + bufferSizePreviousFrameData];
                var cityKey = cities.GenerateKey(buffer);

                byte* bufferPtr;
                fixed (byte* ptr = buffer)
                {
                    bufferPtr = ptr;
                }

                int notProcessedBytesLen = 0;

                byte byteToProcess = 0;
                int byteToProcessIndex = 0;

                threadContext.StartBrokenStringBuffer[0] = 0;
                int fileReadBufferCount = 1;
                int readBufferCount = 0;

            stitchingExtraLoop:

                while ((readCursor < usedFileSize && fileReadBufferCount != 0) || extraLoopToStitchBuffers)
                {
                    var cityStartIndex = 0;
                    var cityEndIndex = 0;

                    sbyte tempSign = 0;
                    var tempStartIndex = 0;
                    var decimalPointIndex = 0;

                    if (extraLoopToStitchBuffers)
                    {
                        extraLoopToStitchBuffers = false;
                        goto processStitchedBuffers;
                    }

                    int bytesToRead = (int)Math.Min(bufferSizeFileData, usedFileSize - readCursor);

                    fileReadBufferCount = br.Read(buffer, 0 + notProcessedBytesLen, bytesToRead);
                    readBufferCount = fileReadBufferCount + notProcessedBytesLen;

                    byteToProcessIndex = 0;

                    if (curToken == StringToken.unknown)
                    {
                        var brokenStringBuffer = threadContext.StartBrokenStringBuffer;
                        byte brokenStringLen = 0;

                        while (byteToProcessIndex < readBufferCount)
                        {
                            //byteToProcess = buffer[byteToProcessIndex];
                            byteToProcess = *((byte*)(bufferPtr + byteToProcessIndex));
                            if (byteToProcess == 0x0A)
                            {
                                byteToProcessIndex++;

                                curToken = StringToken.city;
                                cityStartIndex = byteToProcessIndex;

                                Array.Copy(buffer, 0, brokenStringBuffer, 1, brokenStringLen);
                                brokenStringBuffer[0] = brokenStringLen;

                                break;
                            }

                            brokenStringLen++;
                            byteToProcessIndex++;
                        }
                    }

                processStitchedBuffers:
                    var readBufferCount8offset = readBufferCount - 8;
                    while (byteToProcessIndex < readBufferCount)
                    {
                        byteToProcess = *((byte*)(bufferPtr + byteToProcessIndex));

                        switch (byteToProcess)
                        {
                            case 0x0A:
                                #region Parse Temp

                                uint* pTempUint32;
                                byte* decimalPointPtr = bufferPtr + decimalPointIndex;

                                short parsedTemp = 0;
                                int tempLen = (short)(decimalPointIndex - tempStartIndex + 1);
                                *((byte*)(decimalPointPtr)) = *((byte*)(decimalPointPtr + 1));
                                switch (tempLen)
                                {
                                    case 2:
                                        pTempUint32 = (uint*)(bufferPtr + tempStartIndex);
                                        *(pTempUint32) = *(pTempUint32) - 0x00003030;
                                        parsedTemp = *((byte*)(decimalPointPtr--));
                                        parsedTemp = (short)((byte)(*((byte*)(decimalPointPtr)) * 0x0A) + parsedTemp);

                                        break;
                                    case 3:
                                        pTempUint32 = (uint*)(bufferPtr + tempStartIndex - 1);
                                        *(pTempUint32) = *(pTempUint32) - 0x30303000;
                                        parsedTemp = *((byte*)(decimalPointPtr));
                                        decimalPointPtr--;
                                        parsedTemp = (short)((byte)(*((byte*)(decimalPointPtr)) * 0x0A) + parsedTemp);
                                        decimalPointPtr--;
                                        parsedTemp = (short)((short)(*((byte*)(decimalPointPtr)) * 0x0A * 0x0A) + parsedTemp);

                                        break;
                                }
                                if (tempSign == -1)
                                    parsedTemp *= -1;

                                #endregion Parse Temp

                                #region Process City

                                cityKey.StartIndex = cityStartIndex;
                                cityKey.EndIndex = cityEndIndex;

                                ref var cityData = ref cities.AddGet(cityKey);

                                if (cityData.MaxTemp < parsedTemp) cityData.MaxTemp = parsedTemp;
                                if (cityData.MinTemp > parsedTemp) cityData.MinTemp = parsedTemp;
                                cityData.TempCount++;
                                cityData.TempSum += parsedTemp;

                                #endregion Process City

                                curToken = StringToken.city;

                                byteToProcessIndex++;
                                cityStartIndex = byteToProcessIndex;
                                tempSign = 0;

                                if (byteToProcessIndex < readBufferCount8offset)
                                {
                                    var next8bytes = *((ulong*)(bufferPtr + byteToProcessIndex)) ^ 0x3b3b3b3b3b3b3b3b;
                                    var has3b = (next8bytes - 0x0101010101010101) & ~next8bytes & 0x8080808080808080;
                                    if (has3b == 0)
                                    {
                                        byteToProcessIndex += 8;

                                        if (byteToProcessIndex >= readBufferCount8offset)
                                        {
                                            continue;
                                        }
                                        next8bytes = *((ulong*)(bufferPtr + byteToProcessIndex)) ^ 0x3b3b3b3b3b3b3b3b;
                                        has3b = (next8bytes - 0x0101010101010101) & ~next8bytes & 0x8080808080808080;

                                        // Third read seems degrades performance
                                        // (maybe due to an extra condition below
                                        // when statistically third read works only for 10-15% of cases)

                                        //if (has3b == 0)
                                        //{
                                        //    byteToProcessIndex += 8;

                                        //    if (byteToProcessIndex >= readBufferCount8offset)
                                        //    {
                                        //        continue;
                                        //    }
                                        //    next8bytes = *((ulong*)(bufferPtr + byteToProcessIndex)) ^ 0x3b3b3b3b3b3b3b3b;
                                        //    has3b = (next8bytes - 0x0101010101010101) & ~next8bytes & 0x8080808080808080;
                                        //}
                                    }
                                    var zeroPlace = System.Runtime.Intrinsics.X86.Lzcnt.X64.LeadingZeroCount(has3b);
                                    var indexOffset = map2bufferIndexHopOffset[zeroPlace];
                                    byteToProcessIndex += indexOffset;
                                }

                                continue;
                            case 0x3B: // ;
                                if (curToken == StringToken.city)
                                {
                                    cityEndIndex = byteToProcessIndex - 1;
                                    curToken = StringToken.temp;
                                    byteToProcessIndex++;
                                    tempStartIndex = byteToProcessIndex;
                                    continue;
                                }
                                else
                                {
                                    // partial data between threads
                                }

                                break;
                            default:
                                if (curToken == StringToken.temp)
                                {
                                    switch (byteToProcess)
                                    {
                                        case 0x2D: // -
                                            tempSign = -1;
                                            byteToProcessIndex++;
                                            tempStartIndex = byteToProcessIndex;
                                            continue;
                                        case 0x2E: // .
                                            decimalPointIndex = byteToProcessIndex;
                                            break;
                                    }
                                }

                                break;
                        }

                        byteToProcessIndex++;
                    }

                    if (byteToProcess != 0x0A)
                    {
                        notProcessedBytesLen = readBufferCount - cityStartIndex;
                        Array.Copy(buffer, cityStartIndex, buffer, 0, notProcessedBytesLen);
                    }
                    else
                    {
                        notProcessedBytesLen = 0;
                    }
                    curToken = StringToken.city;

                    readCursor += fileReadBufferCount;
                }

                if (byteToProcess != 0x0A && !lastThread && !extraLoopToStitchBuffers)
                {
                    byteToProcessIndex--;
                    var brokenStringBuffer = threadContext.EndBrokenStringBuffer;
                    byte brokenStringLen = 0;

                    while (byteToProcessIndex > 0)
                    {
                        byteToProcess = *((byte*)(bufferPtr + byteToProcessIndex));
                        if (byteToProcess == 0x0A)
                        {
                            byteToProcessIndex++;

                            Array.Copy(buffer, byteToProcessIndex, brokenStringBuffer, 1, brokenStringLen);
                            brokenStringBuffer[0] = brokenStringLen;

                            break;
                        }

                        brokenStringLen++;
                        byteToProcessIndex--;
                    }

                    if (brokenStringLen > 0)
                    {
                        Array.Copy(brokenStringBuffer, 1, buffer, 0, brokenStringLen);
                    }
                    var nextCtxStartBrokenBuffer = threadContext!.NextThreadContext!.StartBrokenStringBuffer;
                    var nextCtxStartBrokenBufferLen = nextCtxStartBrokenBuffer[0];
                    if (nextCtxStartBrokenBufferLen > 0)
                    {
                        Array.Copy(nextCtxStartBrokenBuffer, 1, buffer, brokenStringLen, nextCtxStartBrokenBufferLen);
                    }

                    byteToProcessIndex = 0;
                    readBufferCount = brokenStringLen + nextCtxStartBrokenBufferLen;
                    buffer[readBufferCount] = 0x0A;
                    readBufferCount++;
                    extraLoopToStitchBuffers = true;
                    goto stitchingExtraLoop;
                }
                else
                {
                    threadContext.EndBrokenStringBuffer[0] = 0;
                }
            }
        }
    }

    static int[] map2bufferIndexHopOffset = new int[]
    {
        7, 0, 0, 0, 0, 0, 0, 0,
        6, 0, 0, 0, 0, 0, 0, 0,
        5, 0, 0, 0, 0, 0, 0, 0,
        4, 0, 0, 0, 0, 0, 0, 0,
        3, 0, 0, 0, 0, 0, 0, 0,
        2, 0, 0, 0, 0, 0, 0, 0,
        1, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,

        8,
    };

    class ThreadProcessingDataContext
    {
        public int ThreadIndex = 0;
        public byte[] StartBrokenStringBuffer = new byte[threadBrokenStringsBufferSize];
        public byte[] EndBrokenStringBuffer = new byte[threadBrokenStringsBufferSize];

        public ThreadProcessingDataContext? NextThreadContext;

        public OptimizedStationDictionary Cities = new OptimizedStationDictionary(32);
    }
}
