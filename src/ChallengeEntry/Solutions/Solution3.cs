using DissectedDictionary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TestEntry.Common;

namespace ChallengeEntry.Solutions;

internal class Solution3
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
            foreach (var cityDataStruct in Contexts[i].Cities)
            {
                if (!resultDictionary.TryGetValue(cityDataStruct.Key, out var cityData))
                {
                    cityData = new CityStationData();
                    resultDictionary.Add(cityDataStruct.Key, cityData);
                }
                cityData.TempCount += cityDataStruct.Value.TempCount;
                cityData.TempSum += cityDataStruct.Value.TempSum;
                if (cityData.MaxTemp < cityDataStruct.Value.MaxTemp) cityData.MaxTemp = cityDataStruct.Value.MaxTemp;
                if (cityData.MinTemp > cityDataStruct.Value.MinTemp) cityData.MinTemp = cityDataStruct.Value.MinTemp;
            }
            Contexts[0].CollisionCount += Contexts[i].CollisionCount;
        }

        var sortedNames = resultDictionary.Keys.OrderBy(x => x);

        timings.Add((Stopwatch.GetTimestamp(), tsNotch.FileProcessed));

        var lastTiming = timings[timings.Count - 1];
        var firstTiming = timings[0];
        var testRunDuration = Stopwatch.GetElapsedTime(firstTiming.ts, lastTiming.ts);

        Console.WriteLine($"threadCount={ThreadCount} readBufferSize={bufferSizeFileData} dur={testRunDuration} collisionCount={Contexts[0].CollisionCount}");
        Console.WriteLine();

        foreach (var name in sortedNames)
        {
            var result = resultDictionary[name];
            Console.Write($"{name}={result.MinTemp/10M:F1}/{(result.TempSum/result.TempCount)/10M:F1}/{result.MaxTemp/10M:F1},");
        }

        testRuns.Add((bufferSizeFileData, ThreadCount, testRunDuration));

        Console.WriteLine();
        Console.ReadLine();
    }

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

                                var cityNameLen = cityEndIndex - cityStartIndex + 1;
                                var cityName = Encoding.UTF8.GetString(buffer, cityStartIndex, cityNameLen);
                                if (!cities.TryGetValue(cityName, out var cityStationData))
                                {
                                    cityStationData = new CityStationData();
                                    cities.Add(cityName, cityStationData);
                                }
                                threadContext.CollisionCount += cities.LastHashCollisionCount;

                                if (cityStationData.MaxTemp < parsedTemp) cityStationData.MaxTemp = parsedTemp;
                                if (cityStationData.MinTemp > parsedTemp) cityStationData.MinTemp = parsedTemp;
                                cityStationData.TempCount++;
                                cityStationData.TempSum += parsedTemp;

                                #endregion Process City

                                curToken = StringToken.city;
                                byteToProcessIndex++;
                                cityStartIndex = byteToProcessIndex;

                                tempSign = 0;

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

    class ThreadProcessingDataContext
    {
        public int ThreadIndex = 0;
        public byte[] StartBrokenStringBuffer = new byte[threadBrokenStringsBufferSize];
        public byte[] EndBrokenStringBuffer = new byte[threadBrokenStringsBufferSize];

        public ThreadProcessingDataContext? NextThreadContext;

        public DissectedDictionary<string, CityStationData> Cities = new ();
        public int CollisionCount = 0;
    }

    class CityStationData
    {
        public int MinTemp = int.MaxValue;
        public int MaxTemp = int.MinValue;
        public int TempCount = 0;
        public long TempSum = 0;
    }
}
