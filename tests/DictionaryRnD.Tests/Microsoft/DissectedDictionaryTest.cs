using DissectedDictionary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DictionaryRnD.Tests.Microsoft;

public class DissectedDictionaryTest
{
    static List<int> intKeys = new();
    static List<string> strKeys = new();

    [Fact]
    public void DemonstrateIntKey()
    {
        var msImplementation = new DissectedDictionary<int, int>();

        msImplementation[1] = 2;

        var value = msImplementation.TryGetValue(1, out var key);

        var lastHashCollisionCount = msImplementation.LastHashCollisionCount;
        var lastHashCode = msImplementation.LastHashCode;

        msImplementation[2] = 3;

        lastHashCollisionCount = msImplementation.LastHashCollisionCount;
        lastHashCode = msImplementation.LastHashCode;

        msImplementation[3] = 4;
        msImplementation[4] = 4;
        msImplementation[5] = 4;
        msImplementation[6] = 4;

        msImplementation[2] = 3;

        msImplementation[7] = 4;

        lastHashCollisionCount = msImplementation.LastHashCollisionCount;
        lastHashCode = msImplementation.LastHashCode;
    }

    [Fact]
    public void DemonstrateStrKey()
    {
        var msImplementation = new DissectedDictionary<string, int>();

        msImplementation["1"] = 2;

        var lastHashCollisionCount = msImplementation.LastHashCollisionCount;
        var lastHashCode = msImplementation.LastHashCode;

        msImplementation["2"] = 3;

        lastHashCollisionCount = msImplementation.LastHashCollisionCount;
        lastHashCode = msImplementation.LastHashCode;

        msImplementation["3"] = 4;
        msImplementation["4"] = 4;
        msImplementation["5"] = 4;
        msImplementation["6"] = 4;

        msImplementation["2"] = 3;

        lastHashCollisionCount = msImplementation.LastHashCollisionCount;
        lastHashCode = msImplementation.LastHashCode;

        msImplementation["2"] = 3;

        lastHashCollisionCount = msImplementation.LastHashCollisionCount;
        lastHashCode = msImplementation.LastHashCode;

        msImplementation["24"] = 3;

        lastHashCollisionCount = msImplementation.LastHashCollisionCount;
        lastHashCode = msImplementation.LastHashCode;

        msImplementation["22"] = 3;

        lastHashCollisionCount = msImplementation.LastHashCollisionCount;
        lastHashCode = msImplementation.LastHashCode;
    }
}
