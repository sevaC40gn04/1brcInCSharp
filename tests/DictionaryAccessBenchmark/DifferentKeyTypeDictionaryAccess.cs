using BenchmarkDotNet.Attributes;
using DictionaryRnD;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DictionaryAccessBenchmark;

public class DifferentKeyTypeDictionaryAccess
{
    [Params(1, 10)]
    public int AccessTimes;

    public const int DictionaryCapacity = 400;

    private const int minStrLen = 4;
    private const int maxStrLen = 12;

    private int[]? generatedIntKey;
    private string[]? generatedStringKey;
    private string[]? generatedStringKeyVarLen;
    private string[]? generatedCityNameKeyVarLen;
    private string[]? generatedCityNameKeyVarLenCustomHash;

    Random Random = new Random();

    [GlobalSetup]
    public void IterationInit()
    {
        generatedIntKey = new int[DictionaryCapacity];
        generatedStringKey = new string[DictionaryCapacity];
        generatedStringKeyVarLen = new string[DictionaryCapacity];
        generatedCityNameKeyVarLen = new string[DictionaryCapacity];

        intKeyDictionary = new();
        strKeyDictionary = new();
        strKeyDictionaryVarLen = new();
        cityNameDictionaryVarLen = new();

        var dummyDataBlock = new DataBlock { SomeIntField = 0 };

        for (int i = 1; i < DictionaryCapacity; i++)
        {
            intKeyDictionary.Add(i, dummyDataBlock);
            generatedIntKey[i] = i;
        }

        for (int i = 1; i < DictionaryCapacity; i++)
        {
            var keyLen = Random.Next(minStrLen, maxStrLen);
            var key = i.ToString($"X{keyLen}");
            strKeyDictionaryVarLen.Add(key, dummyDataBlock);
            generatedStringKeyVarLen[i] = key;
        }

        for (int i = 1; i < DictionaryCapacity; i++)
        {
            var key = $"{i:x8}";
            strKeyDictionary.Add(key, dummyDataBlock);
            generatedStringKey[i] = key;
        }

        for (int i = 1; i < DictionaryCapacity; i++)
        {
            while (true)
            {
                var key = Utils.GenerateRandomCityName(Random.Next(minStrLen, maxStrLen));
                if (cityNameDictionaryVarLen.ContainsKey(key)) continue;
                cityNameDictionaryVarLen.Add(key, dummyDataBlock);
                generatedCityNameKeyVarLen[i] = key;
                break;
            }
        }
    }

    Dictionary<int, DataBlock>? intKeyDictionary;
    Dictionary<string, DataBlock>? strKeyDictionary;
    Dictionary<string, DataBlock>? strKeyDictionaryVarLen;
    Dictionary<string, DataBlock>? cityNameDictionaryVarLen;

    [Benchmark(Baseline = true)]
    public void AccessToIntKeyDictionary()
    {
        for (int i = 0; i < AccessTimes; i++)
        {
            var keyIndex = Random.Next(1, DictionaryCapacity);

            var dataBlock = intKeyDictionary![generatedIntKey![keyIndex]];
            dataBlock.SomeIntField++;
        }
    }

    [Benchmark]
    public void AccessToIntDeltaKeyDictionary()
    {
        for (int i = 0; i < AccessTimes; i++)
        {
            var keyIndex = Random.Next(1, DictionaryCapacity);

            intKeyDictionary!.TryGetValue(generatedIntKey![keyIndex] + DictionaryCapacity, out var dataBlock);
        }
    }

    [Benchmark]
    public void AccessToStrKeyDictionary()
    {
        for (int i = 0; i < AccessTimes; i++)
        {
            var keyIndex = Random.Next(1, DictionaryCapacity);

            var dataBlock = strKeyDictionary![generatedStringKey![keyIndex]];
            dataBlock.SomeIntField++;
        }
    }

    [Benchmark]
    public void AccessToStrKeyDictionaryVarLen()
    {
        for (int i = 0; i < AccessTimes; i++)
        {
            var keyIndex = Random.Next(1, DictionaryCapacity);

            var dataBlock = strKeyDictionaryVarLen![generatedStringKeyVarLen![keyIndex]];
            dataBlock.SomeIntField++;
        }
    }

    [Benchmark]
    public void AccessToCityNameDictionaryVarLen()
    {
        for (int i = 0; i < AccessTimes; i++)
        {
            var keyIndex = Random.Next(1, DictionaryCapacity);

            var dataBlock = cityNameDictionaryVarLen![generatedCityNameKeyVarLen![keyIndex]];
            dataBlock.SomeIntField++;
        }
    }

    [Benchmark]
    public void AccessToStrKeyDictionaryVarLenMisses()
    {
        for (int i = 0; i < AccessTimes; i++)
        {
            var keyIndex = Random.Next(1, DictionaryCapacity);

            strKeyDictionaryVarLen!.TryGetValue(generatedStringKeyVarLen![keyIndex] + "0", out var dataBlock);
        }
    }

    [Benchmark]
    public void AccessToCityNameDictionaryVarLenMisses()
    {
        for (int i = 0; i < AccessTimes; i++)
        {
            var keyIndex = Random.Next(1, DictionaryCapacity);

            cityNameDictionaryVarLen!.TryGetValue(generatedCityNameKeyVarLen![keyIndex] + "0", out var dataBlock);
        }
    }
}

public class DataBlock
{
    public int SomeIntField;
}
