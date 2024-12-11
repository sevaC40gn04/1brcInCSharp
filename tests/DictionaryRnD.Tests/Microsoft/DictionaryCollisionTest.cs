using DissectedDictionary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestEntry.Common;
using Xunit.Abstractions;

namespace DictionaryRnD.Tests.Microsoft;

public class DictionaryCollisionTest
{
    static List<int> intKeys = new();
    static List<int> intRandKeys = new();
    static List<string> strKeys = new();

    static DissectedDictionary<int, int> intKeyDictionary = new(testListCount);
    static DissectedDictionary<int, int> intRandKeyDictionary = new(testListCount);
    static DissectedDictionary<string, int> strKeyDictionary = new(testListCount);

    const int testListCount = 420;

    static string lenTestDataPath = @".\data\MeasurementsAllLens.txt";
    static byte[] lenTestDataBuffer;

    static OptimizedStationDictionary byteKeyDictionary;
    static OptStationKey optStationKey;

    static DictionaryCollisionTest()
    {
        for (int i = 0; i < testListCount; i++)
        {
            intKeys.Add(i);
            intKeyDictionary[i] = i;
        }

        for (int i = 0; i < testListCount; i++)
        {
            var rand = new Random();
            var randIntKey = rand.Next(1, int.MaxValue);

            intRandKeys.Add(randIntKey);
            intRandKeyDictionary[i] = randIntKey;
        }

        for (int i = 0; i < testListCount; i++)
        {
            var strKey = $"{i}str{i}";
            strKeys.Add(strKey);
            strKeyDictionary[strKey] = i;
        }

        using FileStream fs = File.OpenRead(lenTestDataPath);
        using BinaryReader br = new BinaryReader(fs);
        var fileSize = Convert.ToInt32(new FileInfo(lenTestDataPath).Length);
        lenTestDataBuffer = new byte[fileSize];
        br.BaseStream.Seek(0, SeekOrigin.Begin);
        var fileReadBufferCount = br.Read(lenTestDataBuffer, 0, fileSize);

        byteKeyDictionary = new OptimizedStationDictionary(OptimizedStationDictionary.SupportedMaxLen);
        optStationKey = byteKeyDictionary.GenerateKey(lenTestDataBuffer);
    }

    private ITestOutputHelper OutputHelper { get; }
    public DictionaryCollisionTest(ITestOutputHelper outputHelper)
    {
        OutputHelper = outputHelper;
    }

    private const int GetAccessCount = 1000000;

    [Fact]
    public void IntKeyCollisionsCountTest()
    {
        int collisionCount = 0;

        Random random = new Random();

        for (int i = 0; i < GetAccessCount; i++)
        {
            var keyIndexToAccess = random.Next(0, testListCount - 1);
            var idToAccess = intKeys[keyIndexToAccess];

            var accessOpValue = intKeyDictionary[idToAccess];
            collisionCount = collisionCount + intKeyDictionary.LastHashCollisionCount;
        }
        OutputHelper.WriteLine($"int key dictionary, getAccess.count={GetAccessCount} collision.count={collisionCount}");
    }

    [Fact]
    public void IntRandKeyCollisionsCountTest()
    {
        int collisionCount = 0;

        Random random = new Random();

        for (int i = 0; i < GetAccessCount; i++)
        {
            var keyIndexToAccess = random.Next(0, testListCount - 1);
            var idToAccess = intRandKeys[keyIndexToAccess];

            var accessOpValue = intRandKeyDictionary[idToAccess];
            collisionCount = collisionCount + intRandKeyDictionary.LastHashCollisionCount;
        }
        OutputHelper.WriteLine($"int Random key dictionary, getAccess.count={GetAccessCount} collision.count={collisionCount}");
    }

    [Fact]
    public void StrKeyCollisionsCountTest()
    {
        int collisionCount = 0;

        Random random = new Random();

        for (int i = 0; i < GetAccessCount; i++)
        {
            var idToAccess = random.Next(0, testListCount - 1);
            var keyToAccess = $"{i}str{i}";
            var accessOpValue = strKeyDictionary[keyToAccess];
            collisionCount = collisionCount + strKeyDictionary.LastHashCollisionCount;
        }

        OutputHelper.WriteLine($"str key dictionary, getAccess.count={GetAccessCount} collision.count={collisionCount}");
    }
}
