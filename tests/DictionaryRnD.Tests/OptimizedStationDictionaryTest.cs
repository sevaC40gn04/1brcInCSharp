using System;
using TestEntry.Common;

namespace DictionaryRnD.Tests;

public class OptimizedStationDictionaryTest
{
    static string lenTestDataPath = @".\data\MeasurementsAllLens.txt";
    static byte[] lenTestDataBuffer;
    static OptimizedStationDictionaryTest()
    {
        using FileStream fs = File.OpenRead(lenTestDataPath);
        using BinaryReader br = new BinaryReader(fs);
        var fileSize = Convert.ToInt32(new FileInfo(lenTestDataPath).Length);
        lenTestDataBuffer = new byte[fileSize];
        br.BaseStream.Seek(0, SeekOrigin.Begin);
        var fileReadBufferCount = br.Read(lenTestDataBuffer, 0, fileSize);

        allLensTestCollection = new OptimizedStationDictionary(maxSupportedLen);
        allLensTestKey = allLensTestCollection.GenerateKey(lenTestDataBuffer);

        allLensNameConversionTestCollection = new OptimizedStationDictionary(maxSupportedLen);
        allLensNameConversionKey = allLensNameConversionTestCollection.GenerateKey(lenTestDataBuffer);
    }

    static int maxSupportedLen = OptimizedStationDictionary.SupportedMaxLen;

    [Fact]
    public void ExampleHowToUse()
    {
        var optDictionary = new OptimizedStationDictionary(maxSupportedLen);

        var key = optDictionary.GenerateKey(lenTestDataBuffer);
        key.StartIndex = 0x64;
        key.EndIndex = 0x6d;

        ref var stationData = ref optDictionary.AddGet(key);
        stationData.TempCount++;
        stationData.TempSum = 670;
        stationData.MaxTemp = 670;
        stationData.MinTemp = -34;

        stationData = ref optDictionary.AddGet(key);
        stationData.TempCount++;
        stationData.TempSum = 1108;

        stationData = ref optDictionary.AddGet(key);
        stationData.TempCount++;

        Assert.True(stationData.TempCount == 3);
        Assert.True(stationData.TempSum == 1108);
        Assert.True(stationData.MaxTemp == 670);
        Assert.True(stationData.MinTemp == -34);
    }

    static OptimizedStationDictionary allLensTestCollection;
    static OptStationKey allLensTestKey;
    [Theory]
    [InlineData(0x0, 0x0, 1)]
    [InlineData(0x7, 0x8, 2)]
    [InlineData(0x0f, 0x11, 3)]
    [InlineData(0x18, 0x1b, 4)]
    [InlineData(0x22, 0x26, 5)]
    [InlineData(0x2d, 0x32, 6)]
    [InlineData(0x3a, 0x40, 7)]
    [InlineData(0x47, 0x4e, 8)]
    [InlineData(0x55, 0x5d, 9)]
    [InlineData(0x64, 0x6d, 10)]
    [InlineData(0x74, 0x7e, 11)]
    [InlineData(0x85, 0x90, 12)]
    [InlineData(0x97, 0xa3, 13)]
    [InlineData(0xaa, 0xb7, 14)]
    [InlineData(0xbe, 0xcc, 15)]
    [InlineData(0xd3, 0xe2, 16)]
    [InlineData(0xe9, 0xf9, 17)]
    [InlineData(0x100, 0x111, 18)]
    [InlineData(0x118, 0x12a, 19)]
    [InlineData(0x131, 0x144, 20)]
    [InlineData(0x14b, 0x15f, 21)]
    [InlineData(0x166, 0x17b, 22)]
    [InlineData(0x182, 0x198, 23)]
    [InlineData(0x19f, 0x1b6, 24)]
    [InlineData(0x1bc, 0x1d4, 25)]
    [InlineData(0x1da, 0x1f3, 26)]
    [InlineData(0x1fa, 0x214, 27)]
    [InlineData(0x21b, 0x236, 28)]
    public void TestAllSupportedNameLens(int startIndex, int endIndex, int cityNameLen)
    {
        allLensTestKey.StartIndex = startIndex;
        allLensTestKey.EndIndex = endIndex;

        ref var stationData = ref allLensTestCollection.AddGet(allLensTestKey);
        stationData.TempCount++;
        stationData.TempSum = 670;
        stationData.MaxTemp = 680;
        stationData.MinTemp = -34;

        stationData = ref allLensTestCollection.AddGet(allLensTestKey);
        Assert.True(stationData.TempCount == 1);
        Assert.True(stationData.TempSum == 670);
        Assert.True(stationData.MaxTemp == 680);
        Assert.True(stationData.MinTemp == -34);
        Assert.True(stationData.NameLen == cityNameLen);
    }

    static OptimizedStationDictionary allLensNameConversionTestCollection;
    static OptStationKey allLensNameConversionKey;
    static Dictionary<string, int> expectedCityNames = new();
    [Theory]
    [InlineData(0x0, 0x0, 1, "J")]
    [InlineData(0x7, 0x8, 2, "Jo")]
    [InlineData(0x0f, 0x11, 3, "Wau")]
    [InlineData(0x18, 0x1b, 4, "Aden")]
    [InlineData(0x22, 0x26, 5, "Accra")]
    [InlineData(0x2d, 0x32, 6, "Anadyr")]
    [InlineData(0x3a, 0x40, 7, "Calgary")]
    [InlineData(0x47, 0x4e, 8, "Brussels")]
    [InlineData(0x55, 0x5d, 9, "Hanga Roa")]
    [InlineData(0x64, 0x6d, 10, "Bratislava")]
    [InlineData(0x74, 0x7e, 11, "Panama City")]
    [InlineData(0x85, 0x90, 12, "Philadelphia")]
    [InlineData(0x97, 0xa3, 13, "Dar es Salaam")]
    [InlineData(0xaa, 0xb7, 14, "Salt Lake City")]
    [InlineData(0xbe, 0xcc, 15, "Luxembourg City")]
    [InlineData(0xd3, 0xe2, 16, "Ho Chi Minh City")]
    [InlineData(0xe9, 0xf9, 17, "Ho Chi Minh City2")]
    [InlineData(0x100, 0x111, 18, "City of San Marino")]
    [InlineData(0x118, 0x12a, 19, "City of San Marino1")]
    [InlineData(0x131, 0x144, 20, "City of San Marino12")]
    [InlineData(0x14b, 0x15f, 21, "City of San Marino123")]
    [InlineData(0x166, 0x17b, 22, "City of San Marino1234")]
    [InlineData(0x182, 0x198, 23, "City of San Marino12345")]
    [InlineData(0x19f, 0x1b6, 24, "Petropavlovsk-Kamchatsky")]
    [InlineData(0x1bc, 0x1d4, 25, "Petropavlovsk-Kamchatsky1")]
    [InlineData(0x1da, 0x1f3, 26, "Las Palmas de Gran Canaria")]
    [InlineData(0x1fa, 0x214, 27, "Las Palmas de Gran Canaria1")]
    [InlineData(0x21b, 0x236, 28, "Las Palmas de Gran Canaria12")]
    public void TestAllNameLensConversion(int startIndex, int endIndex, int cityNameLen, string expectedCityName)
    {
        expectedCityNames.Add(expectedCityName, cityNameLen);

        allLensNameConversionKey.StartIndex = startIndex;
        allLensNameConversionKey.EndIndex = endIndex;

        ref var stationData = ref allLensNameConversionTestCollection.AddGet(allLensNameConversionKey);
        stationData.TempCount++;
        stationData.TempSum = 670;
        stationData.MaxTemp = 680;
        stationData.MinTemp = -34;

        var convertedDictionary = allLensNameConversionTestCollection.GetSortedCalculatedList();
        if (convertedDictionary.Count == 28)
        {
            foreach (var cityStationData in convertedDictionary) 
            {
                Assert.True(expectedCityNames.ContainsKey(cityStationData.Name));
            }
        }
    }
}