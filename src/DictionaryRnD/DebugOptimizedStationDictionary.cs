using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DictionaryRnD;

public class DebugOptimizedStationDictionary
{
    public struct Entry
    {
        public int NameLen = 0;
        public int TargetBucket;

        public ulong Hash_8_1;
        public ulong Hash_8_2;
        public ulong Hash_8_3;
        public ulong Hash_8_4;

        public uint Hash_4_1;
        public uint Hash_4_2;

        public uint SortName_4_1;
        public uint SortName_4_2;
        public uint SortName_4_3;
        public uint SortName_4_4;
        public uint SortName_4_5;
        public uint SortName_4_6;
        public uint SortName_4_7;
        public uint SortName_4_8;

        public uint hashCode;
        public int next;


        public int MinTemp = int.MaxValue;
        public int MaxTemp = int.MinValue;
        public int TempCount = 0;
        public long TempSum = 0;

        public Entry() { }
    }

    public int[][] buckets;
    public Entry[][] entries;
    public int[] counts;
    public int lenCapacity;

    public DebugOptimizedStationDictionary(int lenCapacity)
    {
        this.lenCapacity = lenCapacity;
        Initialize(lenCapacity);
    }

    public static int SupportedMinLen = 1;
    public static int SupportedMaxLen = 28;

    private void Initialize(int lenCapacity)
    {
        lenCapacity++;
        //        int size = HashHelpers.GetPrime(capacity);
        int size = (0xD0 - 0x41 << 3) + (0xD0 - 0x61 << 2) + (0xD0 - 0x61);

        buckets = new int[lenCapacity][];
        for (int i = 0; i < lenCapacity; i++)
        {
            buckets[i] = new int[size];
            for (int j = 0; j < size; j++) buckets[i][j] = -1;
        }

        entries = new Entry[lenCapacity][];
        for (int i = 0; i < lenCapacity; i++)
        {
            entries[i] = new Entry[size];
        }

        counts = new int[lenCapacity];
        for (int i = 0; i < lenCapacity; i++)
            counts[i] = 0;
    }

    public int LastCollisionCount;
    public unsafe ref Entry AddGet(DebugOptStationKey key)
    {
        var nameLen = key.EndIndex - key.StartIndex + 1;
        key.NameLen = nameLen;

        var processingHandler = len2HandlerMap[nameLen];
        key.CollisionCount = 0;
        ref var returnedEntry = ref processingHandler(key, ref counts[nameLen]);
        LastCollisionCount = key.CollisionCount;
        return ref returnedEntry;
    }

    public unsafe DebugOptStationKey GenerateKey(byte[] buffer)
    {
        byte* bytePtr;
        uint* pUint32;
        ulong* pUint64;

        fixed (byte* ptr = buffer)
        {
            bytePtr = ptr;
            pUint32 = (uint*)bytePtr;
            pUint64 = (ulong*)bytePtr;
        }

        var newKey =
            new DebugOptStationKey()
            {
                SourceArray = buffer,

                buckets = buckets,
                entries = entries,

                BytePtr = bytePtr,
                UintPtr = pUint32,
                UlongPtr = pUint64,
            };

        var nameSwapBuffer = newKey.NameSwapBuffer;
        fixed (byte* ptr = nameSwapBuffer)
        {
            pUint32 = (uint*)ptr;
            newKey.UintNameSwapBufferPtr = pUint32;
        }

        return newKey;
    }

    public unsafe List<DebugStationResult> GetSortedCalculatedList()
    {
        var totalCount = 0;
        for (int i = 0; i < counts.Length; i++)
        {
            totalCount += counts[i];
        }

        var tempResult = new List<(int lenBucket, int bucketIndex)>(totalCount);

        for (int i = 0; i < counts.Length; i++)
        {
            var scannedBucket = buckets[i];
            var bucketCount = counts[i];
            for (int j = 0; j < bucketCount; j++)
            {
                tempResult.Add((i, j));
            }
        }

        BucketComparer.entries = entries;
        tempResult.Sort(new BucketComparer());

        InitPointers();
        var stationResults = new List<DebugStationResult>(totalCount);

        foreach (var indexSet in tempResult)
        {
            ref var srcEntry = ref entries[indexSet.lenBucket][indexSet.bucketIndex];
            DebugStationResult aResultStation = new()
            {
                MaxTemp = srcEntry.MaxTemp / 10M,
                MinTemp = srcEntry.MinTemp / 10M,
                MeanTemp = srcEntry.TempSum / srcEntry.TempCount / 10M,
            };

            stationResults.Add(aResultStation);

            var copyNameHandler = entry2StationResultHandler[srcEntry.NameLen];
            copyNameHandler(ref srcEntry, aResultStation);
        }

        return stationResults;
    }

    delegate void CopyName(ref Entry srcEntry, DebugStationResult stationResult);

    private static Dictionary<int, CopyName> entry2StationResultHandler = new()
    {
        { 1, BuildNameFromHash_4_1 },
        { 2, BuildNameFromHash_4_1 },
        { 3, BuildNameFromHash_4_1 },
        { 4, BuildNameFromHash_4_1 },

        { 5, BuildNameFromHash_8_1 },
        { 6, BuildNameFromHash_8_1 },
        { 7, BuildNameFromHash_8_1 },
        { 8, BuildNameFromHash_8_1 },

        { 9, BuildNameFromHash_8_1_4_1 },
        { 10, BuildNameFromHash_8_1_4_1 },
        { 11, BuildNameFromHash_8_1_4_1 },
        { 12, BuildNameFromHash_8_1_4_1 },

        { 13, BuildNameFromHash_8_1_8_2 },
        { 14, BuildNameFromHash_8_1_8_2 },
        { 15, BuildNameFromHash_8_1_8_2 },
        { 16, BuildNameFromHash_8_1_8_2 },

        { 17, BuildNameFromHash_8_1_8_2_4_1 },
        { 18, BuildNameFromHash_8_1_8_2_4_1 },
        { 19, BuildNameFromHash_8_1_8_2_4_1 },
        { 20, BuildNameFromHash_8_1_8_2_4_1 },

        { 21, BuildNameFromHash_8_1_8_2_8_3 },
        { 22, BuildNameFromHash_8_1_8_2_8_3 },
        { 23, BuildNameFromHash_8_1_8_2_8_3 },
        { 24, BuildNameFromHash_8_1_8_2_8_3 },

        { 25, BuildNameFromHash_8_1_8_2_8_3_4_1 },
        { 26, BuildNameFromHash_8_1_8_2_8_3_4_1 },
        { 27, BuildNameFromHash_8_1_8_2_8_3_4_1 },
        { 28, BuildNameFromHash_8_1_8_2_8_3_4_1 },
    };

    private static byte[] convertToNameBuffer = new byte[32];
    private static unsafe byte* ctnbBytePtr;

    private static unsafe ulong* ctnbUlongPtr;
    private static unsafe ulong* ctnbUlong8Ptr;
    private static unsafe ulong* ctnbUlong16Ptr;

    private static unsafe uint* ctnbUintPtr;
    private static unsafe uint* ctnbUint8Ptr;
    private static unsafe uint* ctnbUint16Ptr;
    private static unsafe uint* ctnbUint24Ptr;

    private static unsafe void InitPointers()
    {
        fixed (byte* ptr = convertToNameBuffer)
        {
            ctnbBytePtr = ptr;

            ctnbUlongPtr = (ulong*)ctnbBytePtr;
            ctnbUlong8Ptr = (ulong*)(ctnbBytePtr + 8);
            ctnbUlong16Ptr = (ulong*)(ctnbBytePtr + 16);

            ctnbUintPtr = (uint*)ctnbBytePtr;
            ctnbUint8Ptr = (uint*)(ctnbBytePtr + 8);
            ctnbUint16Ptr = (uint*)(ctnbBytePtr + 16);
            ctnbUint24Ptr = (uint*)(ctnbBytePtr + 24);
        }
    }

    private static unsafe void BuildNameFromHash_4_1(ref Entry srcEntry, DebugStationResult stationResult)
    {
        *ctnbUintPtr = srcEntry.Hash_4_1;
        stationResult.Name = Encoding.UTF8.GetString(convertToNameBuffer, 0, srcEntry.NameLen);
    }
    private static unsafe void BuildNameFromHash_8_1(ref Entry srcEntry, DebugStationResult stationResult)
    {
        *ctnbUlongPtr = srcEntry.Hash_8_1;
        stationResult.Name = Encoding.UTF8.GetString(convertToNameBuffer, 0, srcEntry.NameLen);
    }
    private static unsafe void BuildNameFromHash_8_1_4_1(ref Entry srcEntry, DebugStationResult stationResult)
    {
        *ctnbUlongPtr = srcEntry.Hash_8_1;
        *ctnbUint8Ptr = srcEntry.Hash_4_1;
        stationResult.Name = Encoding.UTF8.GetString(convertToNameBuffer, 0, srcEntry.NameLen);
    }
    private static unsafe void BuildNameFromHash_8_1_8_2(ref Entry srcEntry, DebugStationResult stationResult)
    {
        *ctnbUlongPtr = srcEntry.Hash_8_1;
        *ctnbUlong8Ptr = srcEntry.Hash_8_2;
        stationResult.Name = Encoding.UTF8.GetString(convertToNameBuffer, 0, srcEntry.NameLen);
    }
    private static unsafe void BuildNameFromHash_8_1_8_2_4_1(ref Entry srcEntry, DebugStationResult stationResult)
    {
        *ctnbUlongPtr = srcEntry.Hash_8_1;
        *ctnbUlong8Ptr = srcEntry.Hash_8_2;
        *ctnbUint16Ptr = srcEntry.Hash_4_1;
        stationResult.Name = Encoding.UTF8.GetString(convertToNameBuffer, 0, srcEntry.NameLen);
    }
    private static unsafe void BuildNameFromHash_8_1_8_2_8_3(ref Entry srcEntry, DebugStationResult stationResult)
    {
        *ctnbUlongPtr = srcEntry.Hash_8_1;
        *ctnbUlong8Ptr = srcEntry.Hash_8_2;
        *ctnbUlong16Ptr = srcEntry.Hash_8_3;
        stationResult.Name = Encoding.UTF8.GetString(convertToNameBuffer, 0, srcEntry.NameLen);
    }
    private static unsafe void BuildNameFromHash_8_1_8_2_8_3_4_1(ref Entry srcEntry, DebugStationResult stationResult)
    {
        *ctnbUlongPtr = srcEntry.Hash_8_1;
        *ctnbUlong8Ptr = srcEntry.Hash_8_2;
        *ctnbUlong16Ptr = srcEntry.Hash_8_3;
        *ctnbUint24Ptr = srcEntry.Hash_4_1;
        stationResult.Name = Encoding.UTF8.GetString(convertToNameBuffer, 0, srcEntry.NameLen);
    }

    public class BucketComparer : IComparer<(int lenBucket, int bucketIndex)>
    {
        public static Entry[][] entries;

        public int Compare((int lenBucket, int bucketIndex) x, (int lenBucket, int bucketIndex) y)
        {
            ref var xEntry = ref entries[x.lenBucket][x.bucketIndex];
            ref var yEntry = ref entries[y.lenBucket][y.bucketIndex];

            if (x.lenBucket < 5)
            {
                return
                    xEntry.SortName_4_1 < yEntry.SortName_4_1 ?
                        -1 : xEntry.SortName_4_1 > yEntry.SortName_4_1 ? 1 : 0;
            }
            else if (x.lenBucket < 9)
            {
                if (y.lenBucket < 5)
                {
                    return
                        xEntry.SortName_4_1 < yEntry.SortName_4_1 ?
                            -1 : xEntry.SortName_4_1 > yEntry.SortName_4_1 ? 1 : 0;
                }
                else if (y.lenBucket < 9)
                {
                    if (xEntry.SortName_4_1 == yEntry.SortName_4_1)
                    {
                        return
                            xEntry.SortName_4_2 < yEntry.SortName_4_2 ?
                                -1 : xEntry.SortName_4_2 > yEntry.SortName_4_2 ? 1 : 0;
                    }
                    else
                    {
                        return
                            xEntry.SortName_4_1 < yEntry.SortName_4_1 ?
                                -1 : xEntry.SortName_4_1 > yEntry.SortName_4_1 ? 1 : 0;
                    }
                }
            }
            else if (x.lenBucket < 13)
            {
                if (y.lenBucket < 5)
                {
                    return
                        xEntry.SortName_4_1 < yEntry.SortName_4_1 ?
                            -1 : xEntry.SortName_4_1 > yEntry.SortName_4_1 ? 1 : 0;
                }
                else if (y.lenBucket < 9)
                {
                    if (xEntry.SortName_4_1 == yEntry.SortName_4_1)
                    {
                        return
                            xEntry.SortName_4_2 < yEntry.SortName_4_2 ?
                                -1 : xEntry.SortName_4_2 > yEntry.SortName_4_2 ? 1 : 0;
                    }
                    else
                    {
                        return
                            xEntry.SortName_4_1 < yEntry.SortName_4_1 ?
                                -1 : xEntry.SortName_4_1 > yEntry.SortName_4_1 ? 1 : 0;
                    }
                }
                else if (y.lenBucket < 13)
                {
                    if (xEntry.SortName_4_1 == yEntry.SortName_4_1)
                    {
                        return
                            xEntry.SortName_4_2 < yEntry.SortName_4_2 ?
                                -1 : xEntry.SortName_4_2 > yEntry.SortName_4_2 ? 1 : 0;
                    }
                    else if (xEntry.SortName_4_2 == yEntry.SortName_4_2)
                    {
                        return
                            xEntry.SortName_4_3 < yEntry.SortName_4_3 ?
                                -1 : xEntry.SortName_4_3 > yEntry.SortName_4_3 ? 1 : 0;
                    }
                }
            }
            return
                xEntry.SortName_4_1 < yEntry.SortName_4_1 ?
                    -1 : xEntry.SortName_4_1 > yEntry.SortName_4_1 ? 1 : 0;
        }
    }

    public void AddDictionary(DebugOptimizedStationDictionary srcDictionary)
    {
        var processingKey = new AddUpdateKey { };

        var srcLenCapacity = srcDictionary.lenCapacity;
        var srcCounts = srcDictionary.counts;
        var srcEntries = srcDictionary.entries;

        for (int stationLen = 1; stationLen < srcLenCapacity; stationLen++)
        {
            ref var srcLenCount = ref srcCounts[stationLen];
            var srcLenEntrySet = srcEntries[stationLen]!;

            var dstLenEntrySet = entries[stationLen]!;
            processingKey.buckets = buckets[stationLen]!;
            processingKey.entries = entries[stationLen]!;
            processingKey.NameLen = stationLen;
            processingKey.Count = ref counts[stationLen]!;

            for (int i = 0; i < srcLenCount; i++)
            {
                processingKey.SrcEntry = ref srcLenEntrySet[i];

                var processingHandler = len2AddUpdateHandler[stationLen];
                processingHandler(ref processingKey);
            }
        }
    }

    protected ref struct AddUpdateKey
    {
        public ref Entry SrcEntry;
        public int NameLen;

        public int[] buckets;
        public Entry[] entries;
        public ref int Count;
    }

    delegate void AddUpdateEntry(ref AddUpdateKey srcEntry);

    private static Dictionary<int, AddUpdateEntry> len2AddUpdateHandler = new()
    {
        { 3, Hash_4_1_Processing },
        { 4, Hash_4_1_Processing },

        { 5, Hash_8_1_Processing },
        { 6, Hash_8_1_Processing },
        { 7, Hash_8_1_Processing },
        { 8, Hash_8_1_Processing },

        { 9, Hash_8_1_4_1_Processing },
        { 10, Hash_8_1_4_1_Processing },
        { 11, Hash_8_1_4_1_Processing },
        { 12, Hash_8_1_4_1_Processing },

        { 13, Hash_8_1_8_2_Processing },
        { 14, Hash_8_1_8_2_Processing },
        { 15, Hash_8_1_8_2_Processing },
        { 16, Hash_8_1_8_2_Processing },

        { 17, Hash_8_1_8_2_4_1_Processing },
        { 18, Hash_8_1_8_2_4_1_Processing },
        { 24, Hash_8_1_8_2_8_3_Processing },
        { 26, Hash_8_1_8_2_8_3_4_1_Processing },
    };

    private static unsafe void Hash_4_1_Processing(ref AddUpdateKey key)
    {
        ref var srcEntry = ref key.SrcEntry;

        var nameLen = key.NameLen;
        var usedBucket = key.buckets;
        var usedEntries = key.entries;
        var targetBucket = srcEntry.TargetBucket;
        ref var count = ref key.Count;

        var hashCode = srcEntry.Hash_4_1;

        for (int i = usedBucket[targetBucket]; i >= 0; i = usedEntries[i].next)
        {
            var entry = usedEntries[i];
            if (entry.Hash_4_1 == hashCode)
            {
                ref var foundEntry = ref usedEntries[i];

                foundEntry.TempCount = foundEntry.TempCount + srcEntry.TempCount;
                foundEntry.TempSum = foundEntry.TempSum + srcEntry.TempSum;

                if (srcEntry.MaxTemp > foundEntry.MaxTemp) foundEntry.MaxTemp = srcEntry.MaxTemp;
                if (srcEntry.MinTemp < foundEntry.MinTemp) foundEntry.MinTemp = srcEntry.MinTemp;

                return;
            }
        }

        var index = count;
        count++;

        usedEntries[index].next = usedBucket[targetBucket];
        usedBucket[targetBucket] = index;

        ref var dstValue = ref usedEntries[index]!;
        dstValue.NameLen = key.NameLen;
        dstValue.Hash_4_1 = hashCode;
        dstValue.TargetBucket = srcEntry.TargetBucket;

        dstValue.MaxTemp = srcEntry.MaxTemp;
        dstValue.MinTemp = srcEntry.MinTemp;
        dstValue.TempCount = srcEntry.TempCount;
        dstValue.TempSum = srcEntry.TempSum;
    }

    private static unsafe void Hash_8_1_Processing(ref AddUpdateKey key)
    {
        ref var srcEntry = ref key.SrcEntry;

        var nameLen = key.NameLen;
        var usedBucket = key.buckets;
        var usedEntries = key.entries;
        var targetBucket = srcEntry.TargetBucket;
        ref var count = ref key.Count;

        var hashCode = srcEntry.Hash_8_1;

        for (int i = usedBucket[targetBucket]; i >= 0; i = usedEntries[i].next)
        {
            var entry = usedEntries[i];
            if (entry.Hash_8_1 == hashCode)
            {
                ref var foundEntry = ref usedEntries[i];

                foundEntry.TempCount = foundEntry.TempCount + srcEntry.TempCount;
                foundEntry.TempSum = foundEntry.TempSum + srcEntry.TempSum;

                if (srcEntry.MaxTemp > foundEntry.MaxTemp) foundEntry.MaxTemp = srcEntry.MaxTemp;
                if (srcEntry.MinTemp < foundEntry.MinTemp) foundEntry.MinTemp = srcEntry.MinTemp;

                return;
            }
        }

        var index = count;
        count++;

        usedEntries[index].next = usedBucket[targetBucket];
        usedBucket[targetBucket] = index;

        ref var dstValue = ref usedEntries[index]!;
        dstValue.NameLen = key.NameLen;
        dstValue.Hash_8_1 = hashCode;
        dstValue.TargetBucket = srcEntry.TargetBucket;

        dstValue.MaxTemp = srcEntry.MaxTemp;
        dstValue.MinTemp = srcEntry.MinTemp;
        dstValue.TempCount = srcEntry.TempCount;
        dstValue.TempSum = srcEntry.TempSum;
    }

    private static unsafe void Hash_8_1_4_1_Processing(ref AddUpdateKey key)
    {
        ref var srcEntry = ref key.SrcEntry;

        var nameLen = key.NameLen;
        var usedBucket = key.buckets;
        var usedEntries = key.entries;
        var targetBucket = srcEntry.TargetBucket;
        ref var count = ref key.Count;

        var hashCode = srcEntry.Hash_8_1;
        var hashCode2 = srcEntry.Hash_4_1;

        for (int i = usedBucket[targetBucket]; i >= 0; i = usedEntries[i].next)
        {
            var entry = usedEntries[i];
            if (entry.Hash_8_1 == hashCode && entry.Hash_4_1 == hashCode2)
            {
                ref var foundEntry = ref usedEntries[i];

                foundEntry.TempCount = foundEntry.TempCount + srcEntry.TempCount;
                foundEntry.TempSum = foundEntry.TempSum + srcEntry.TempSum;

                if (srcEntry.MaxTemp > foundEntry.MaxTemp) foundEntry.MaxTemp = srcEntry.MaxTemp;
                if (srcEntry.MinTemp < foundEntry.MinTemp) foundEntry.MinTemp = srcEntry.MinTemp;

                return;
            }
        }

        var index = count;
        count++;

        usedEntries[index].next = usedBucket[targetBucket];
        usedBucket[targetBucket] = index;

        ref var dstValue = ref usedEntries[index]!;
        dstValue.NameLen = key.NameLen;
        dstValue.Hash_8_1 = hashCode;
        dstValue.Hash_4_1 = hashCode2;
        dstValue.TargetBucket = srcEntry.TargetBucket;

        dstValue.MaxTemp = srcEntry.MaxTemp;
        dstValue.MinTemp = srcEntry.MinTemp;
        dstValue.TempCount = srcEntry.TempCount;
        dstValue.TempSum = srcEntry.TempSum;
    }

    private static unsafe void Hash_8_1_8_2_Processing(ref AddUpdateKey key)
    {
        ref var srcEntry = ref key.SrcEntry;

        var nameLen = key.NameLen;
        var usedBucket = key.buckets;
        var usedEntries = key.entries;
        var targetBucket = srcEntry.TargetBucket;
        ref var count = ref key.Count;

        var hashCode = srcEntry.Hash_8_1;
        var hashCode2 = srcEntry.Hash_8_2;

        for (int i = usedBucket[targetBucket]; i >= 0; i = usedEntries[i].next)
        {
            var entry = usedEntries[i];
            if (entry.Hash_8_1 == hashCode && entry.Hash_8_2 == hashCode2)
            {
                ref var foundEntry = ref usedEntries[i];

                foundEntry.TempCount = foundEntry.TempCount + srcEntry.TempCount;
                foundEntry.TempSum = foundEntry.TempSum + srcEntry.TempSum;

                if (srcEntry.MaxTemp > foundEntry.MaxTemp) foundEntry.MaxTemp = srcEntry.MaxTemp;
                if (srcEntry.MinTemp < foundEntry.MinTemp) foundEntry.MinTemp = srcEntry.MinTemp;

                return;
            }
        }

        var index = count;
        count++;

        usedEntries[index].next = usedBucket[targetBucket];
        usedBucket[targetBucket] = index;

        ref var dstValue = ref usedEntries[index]!;
        dstValue.NameLen = key.NameLen;
        dstValue.Hash_8_1 = hashCode;
        dstValue.Hash_8_2 = hashCode2;
        dstValue.TargetBucket = srcEntry.TargetBucket;

        dstValue.MaxTemp = srcEntry.MaxTemp;
        dstValue.MinTemp = srcEntry.MinTemp;
        dstValue.TempCount = srcEntry.TempCount;
        dstValue.TempSum = srcEntry.TempSum;
    }

    private static unsafe void Hash_8_1_8_2_4_1_Processing(ref AddUpdateKey key)
    {
        ref var srcEntry = ref key.SrcEntry;

        var nameLen = key.NameLen;
        var usedBucket = key.buckets;
        var usedEntries = key.entries;
        var targetBucket = srcEntry.TargetBucket;
        ref var count = ref key.Count;

        var hashCode = srcEntry.Hash_8_1;
        var hashCode2 = srcEntry.Hash_8_2;
        var hashCode3 = srcEntry.Hash_4_1;

        for (int i = usedBucket[targetBucket]; i >= 0; i = usedEntries[i].next)
        {
            var entry = usedEntries[i];
            if (entry.Hash_8_1 == hashCode && entry.Hash_8_2 == hashCode2 && entry.Hash_4_1 == hashCode3)
            {
                ref var foundEntry = ref usedEntries[i];

                foundEntry.TempCount = foundEntry.TempCount + srcEntry.TempCount;
                foundEntry.TempSum = foundEntry.TempSum + srcEntry.TempSum;

                if (srcEntry.MaxTemp > foundEntry.MaxTemp) foundEntry.MaxTemp = srcEntry.MaxTemp;
                if (srcEntry.MinTemp < foundEntry.MinTemp) foundEntry.MinTemp = srcEntry.MinTemp;

                return;
            }
        }

        var index = count;
        count++;

        usedEntries[index].next = usedBucket[targetBucket];
        usedBucket[targetBucket] = index;

        ref var dstValue = ref usedEntries[index]!;
        dstValue.NameLen = key.NameLen;
        dstValue.Hash_8_1 = hashCode;
        dstValue.Hash_8_2 = hashCode2;
        dstValue.Hash_4_1 = hashCode3;
        dstValue.TargetBucket = srcEntry.TargetBucket;

        dstValue.MaxTemp = srcEntry.MaxTemp;
        dstValue.MinTemp = srcEntry.MinTemp;
        dstValue.TempCount = srcEntry.TempCount;
        dstValue.TempSum = srcEntry.TempSum;
    }

    private static unsafe void Hash_8_1_8_2_8_3_Processing(ref AddUpdateKey key)
    {
        ref var srcEntry = ref key.SrcEntry;

        var nameLen = key.NameLen;
        var usedBucket = key.buckets;
        var usedEntries = key.entries;
        var targetBucket = srcEntry.TargetBucket;
        ref var count = ref key.Count;

        var hashCode = srcEntry.Hash_8_1;
        var hashCode2 = srcEntry.Hash_8_2;
        var hashCode3 = srcEntry.Hash_8_3;

        for (int i = usedBucket[targetBucket]; i >= 0; i = usedEntries[i].next)
        {
            var entry = usedEntries[i];
            if (entry.Hash_8_1 == hashCode && entry.Hash_8_2 == hashCode2 && entry.Hash_8_3 == hashCode3)
            {
                ref var foundEntry = ref usedEntries[i];

                foundEntry.TempCount = foundEntry.TempCount + srcEntry.TempCount;
                foundEntry.TempSum = foundEntry.TempSum + srcEntry.TempSum;

                if (srcEntry.MaxTemp > foundEntry.MaxTemp) foundEntry.MaxTemp = srcEntry.MaxTemp;
                if (srcEntry.MinTemp < foundEntry.MinTemp) foundEntry.MinTemp = srcEntry.MinTemp;

                return;
            }
        }

        var index = count;
        count++;

        usedEntries[index].next = usedBucket[targetBucket];
        usedBucket[targetBucket] = index;

        ref var dstValue = ref usedEntries[index]!;
        dstValue.NameLen = key.NameLen;
        dstValue.Hash_8_1 = hashCode;
        dstValue.Hash_8_2 = hashCode2;
        dstValue.Hash_8_3 = hashCode3;
        dstValue.TargetBucket = srcEntry.TargetBucket;

        dstValue.MaxTemp = srcEntry.MaxTemp;
        dstValue.MinTemp = srcEntry.MinTemp;
        dstValue.TempCount = srcEntry.TempCount;
        dstValue.TempSum = srcEntry.TempSum;
    }

    private static unsafe void Hash_8_1_8_2_8_3_4_1_Processing(ref AddUpdateKey key)
    {
        ref var srcEntry = ref key.SrcEntry;

        var nameLen = key.NameLen;
        var usedBucket = key.buckets;
        var usedEntries = key.entries;
        var targetBucket = srcEntry.TargetBucket;
        ref var count = ref key.Count;

        var hashCode = srcEntry.Hash_8_1;
        var hashCode2 = srcEntry.Hash_8_2;
        var hashCode3 = srcEntry.Hash_8_3;
        var hashCode4 = srcEntry.Hash_4_1;

        for (int i = usedBucket[targetBucket]; i >= 0; i = usedEntries[i].next)
        {
            var entry = usedEntries[i];
            if (entry.Hash_8_1 == hashCode && entry.Hash_8_2 == hashCode2 && entry.Hash_8_3 == hashCode3 && entry.Hash_4_1 == hashCode4)
            {
                ref var foundEntry = ref usedEntries[i];

                foundEntry.TempCount = foundEntry.TempCount + srcEntry.TempCount;
                foundEntry.TempSum = foundEntry.TempSum + srcEntry.TempSum;

                if (srcEntry.MaxTemp > foundEntry.MaxTemp) foundEntry.MaxTemp = srcEntry.MaxTemp;
                if (srcEntry.MinTemp < foundEntry.MinTemp) foundEntry.MinTemp = srcEntry.MinTemp;

                return;
            }
        }

        var index = count;
        count++;

        usedEntries[index].next = usedBucket[targetBucket];
        usedBucket[targetBucket] = index;

        ref var dstValue = ref usedEntries[index]!;
        dstValue.NameLen = key.NameLen;
        dstValue.Hash_8_1 = hashCode;
        dstValue.Hash_8_2 = hashCode2;
        dstValue.Hash_8_3 = hashCode3;
        dstValue.Hash_4_1 = hashCode4;
        dstValue.TargetBucket = srcEntry.TargetBucket;

        dstValue.MaxTemp = srcEntry.MaxTemp;
        dstValue.MinTemp = srcEntry.MinTemp;
        dstValue.TempCount = srcEntry.TempCount;
        dstValue.TempSum = srcEntry.TempSum;
    }

    delegate ref Entry SetHash(DebugOptStationKey key, ref int count);

    private static Dictionary<int, SetHash> len2HandlerMap = new()
    {
        { 1, ProcessLen1 },
        { 2, ProcessLen2 },
        { 3, ProcessLen3 },
        { 4, ProcessLen4 },
        { 5, ProcessLen5 },
        { 6, ProcessLen6 },
        { 7, ProcessLen7 },
        { 8, ProcessLen8 },
        { 9, ProcessLen9 },
        { 10, ProcessLen10 },
        { 11, ProcessLen11 },
        { 12, ProcessLen12 },
        { 13, ProcessLen13 },
        { 14, ProcessLen14 },
        { 15, ProcessLen15 },
        { 16, ProcessLen16 },
        { 17, ProcessLen17 },
        { 18, ProcessLen18 },
        { 19, ProcessLen19 },
        { 20, ProcessLen20 },
        { 21, ProcessLen21 },
        { 22, ProcessLen22 },
        { 23, ProcessLen23 },
        { 24, ProcessLen24 },
        { 25, ProcessLen25 },
        { 26, ProcessLen26 },
        { 27, ProcessLen27 },
        { 28, ProcessLen28 },
    };

    private static unsafe ref Entry ProcessLen1(DebugOptStationKey key, ref int count)
    {
        var nameLen = key.NameLen;
        var usedBucket = key.buckets[nameLen];
        var usedEntries = key.entries[nameLen];

        var buffer = key.SourceArray!;
        var startIndex = key.StartIndex;

        byte firstChar = (byte)(buffer[startIndex] - 0x41);
        var secondChar = (byte)(buffer[startIndex + 1] - 0x61);
        var thirdChar = (byte)(buffer[startIndex + 2] - 0x61);

        var targetBucket = (firstChar << 3) + (secondChar << 2) + thirdChar;

        byte* indexPtr = key.BytePtr + startIndex;
        uint* pUint32 = (uint*)indexPtr;
        key.Hash_4_1 = *pUint32;
        key.Hash_4_1 &= 0x000000FF;
        var hashCode = key.Hash_4_1;

        for (int i = usedBucket[targetBucket]; i >= 0; i = usedEntries[i].next)
        {
            var entry = usedEntries[i];
            if (entry.Hash_4_1 == hashCode)
            {
                return ref usedEntries[i];
            }
        }

        var index = count;
        count++;

        usedEntries[index].next = usedBucket[targetBucket];
        usedBucket[targetBucket] = index;

        ref var entryValue = ref usedEntries[index]!;
        entryValue.NameLen = key.NameLen;
        entryValue.Hash_4_1 = hashCode;
        entryValue.TargetBucket = targetBucket;

        #region sorting name
        var nameSwapBuffer = key.NameSwapBuffer;

        nameSwapBuffer[3] = buffer[startIndex];
        nameSwapBuffer[2] = 0;
        nameSwapBuffer[1] = 0;
        nameSwapBuffer[0] = 0;
        entryValue.SortName_4_1 = *key.UintNameSwapBufferPtr;
        #endregion sorting name

        return ref entryValue;
    }

    private static unsafe ref Entry ProcessLen2(DebugOptStationKey key, ref int count)
    {
        var nameLen = key.NameLen;
        var usedBucket = key.buckets[nameLen];
        var usedEntries = key.entries[nameLen];

        var buffer = key.SourceArray!;
        var startIndex = key.StartIndex;

        byte firstChar = (byte)(buffer[startIndex] - 0x41);
        var secondChar = (byte)(buffer[startIndex + 1] - 0x61);
        var thirdChar = (byte)(buffer[startIndex + 2] - 0x61);

        var targetBucket = (firstChar << 3) + (secondChar << 2) + thirdChar;

        byte* indexPtr = key.BytePtr + startIndex;
        uint* pUint32 = (uint*)indexPtr;
        key.Hash_4_1 = *pUint32;
        key.Hash_4_1 &= 0x0000FFFF;
        var hashCode = key.Hash_4_1;

        for (int i = usedBucket[targetBucket]; i >= 0; i = usedEntries[i].next)
        {
            var entry = usedEntries[i];
            if (entry.Hash_4_1 == hashCode)
            {
                return ref usedEntries[i];
            }
        }

        var index = count;
        count++;

        usedEntries[index].next = usedBucket[targetBucket];
        usedBucket[targetBucket] = index;

        ref var entryValue = ref usedEntries[index]!;
        entryValue.NameLen = key.NameLen;
        entryValue.Hash_4_1 = hashCode;
        entryValue.TargetBucket = targetBucket;

        #region sorting name
        var nameSwapBuffer = key.NameSwapBuffer;

        nameSwapBuffer[3] = buffer[startIndex];
        nameSwapBuffer[2] = buffer[startIndex + 1];
        nameSwapBuffer[1] = 0;
        nameSwapBuffer[0] = 0;
        entryValue.SortName_4_1 = *key.UintNameSwapBufferPtr;
        #endregion sorting name

        return ref entryValue;
    }

    private static unsafe ref Entry ProcessLen3(DebugOptStationKey key, ref int count)
    {
        var nameLen = key.NameLen;
        var usedBucket = key.buckets[nameLen];
        var usedEntries = key.entries[nameLen];

        var buffer = key.SourceArray!;
        var startIndex = key.StartIndex;

        byte firstChar = (byte)(buffer[startIndex] - 0x41);
        var secondChar = (byte)(buffer[startIndex + 1] - 0x61);
        var thirdChar = (byte)(buffer[startIndex + 2] - 0x61);

        var targetBucket = (firstChar << 3) + (secondChar << 2) + thirdChar;

        byte* indexPtr = key.BytePtr + startIndex;
        uint* pUint32 = (uint*)indexPtr;
        key.Hash_4_1 = *pUint32;
        key.Hash_4_1 &= 0x00FFFFFF;
        var hashCode = key.Hash_4_1;

        for (int i = usedBucket[targetBucket]; i >= 0; i = usedEntries[i].next)
        {
            var entry = usedEntries[i];
            if (entry.Hash_4_1 == hashCode)
            {
                return ref usedEntries[i];
            }
        }

        var index = count;
        count++;

        usedEntries[index].next = usedBucket[targetBucket];
        usedBucket[targetBucket] = index;

        ref var entryValue = ref usedEntries[index]!;
        entryValue.NameLen = key.NameLen;
        entryValue.Hash_4_1 = hashCode;
        entryValue.TargetBucket = targetBucket;

        #region sorting name
        var nameSwapBuffer = key.NameSwapBuffer;

        nameSwapBuffer[3] = buffer[startIndex];
        nameSwapBuffer[2] = buffer[startIndex + 1];
        nameSwapBuffer[1] = buffer[startIndex + 2];
        nameSwapBuffer[0] = 0;
        entryValue.SortName_4_1 = *key.UintNameSwapBufferPtr;
        #endregion sorting name

        return ref entryValue;
    }

    private static unsafe ref Entry ProcessLen4(DebugOptStationKey key, ref int count)
    {
        var nameLen = key.NameLen;
        var usedBucket = key.buckets[nameLen];
        var usedEntries = key.entries[nameLen];

        var buffer = key.SourceArray!;
        var startIndex = key.StartIndex;

        byte firstChar = (byte)(buffer[startIndex] - 0x41);
        var secondChar = (byte)(buffer[startIndex + 1] - 0x61);
        var thirdChar = (byte)(buffer[startIndex + 2] - 0x61);

        var targetBucket = (firstChar << 3) + (secondChar << 2) + thirdChar;

        byte* indexPtr = key.BytePtr + startIndex;
        uint* pUint32 = (uint*)indexPtr;
        key.Hash_4_1 = *pUint32;
        var hashCode = key.Hash_4_1;

        for (int i = usedBucket[targetBucket]; i >= 0; i = usedEntries[i].next)
        {
            var entry = usedEntries[i];
            if (entry.Hash_4_1 == hashCode)
            {
                return ref usedEntries[i];
            }
        }

        var index = count;
        count++;

        usedEntries[index].next = usedBucket[targetBucket];
        usedBucket[targetBucket] = index;

        ref var entryValue = ref usedEntries[index]!;
        entryValue.NameLen = key.NameLen;
        entryValue.Hash_4_1 = hashCode;
        entryValue.TargetBucket = targetBucket;

        #region sorting name
        var nameSwapBuffer = key.NameSwapBuffer;

        nameSwapBuffer[3] = buffer[startIndex];
        nameSwapBuffer[2] = buffer[startIndex + 1];
        nameSwapBuffer[1] = buffer[startIndex + 2];
        nameSwapBuffer[0] = buffer[startIndex + 3];
        entryValue.SortName_4_1 = *key.UintNameSwapBufferPtr;
        #endregion sorting name

        return ref entryValue;
    }

    private static unsafe ref Entry ProcessLen5(DebugOptStationKey key, ref int count)
    {
        var nameLen = key.NameLen;
        var usedBucket = key.buckets[nameLen];
        var usedEntries = key.entries[nameLen];

        var buffer = key.SourceArray!;
        var startIndex = key.StartIndex;

        byte firstChar = (byte)(buffer[startIndex] - 0x41);
        var secondChar = (byte)(buffer[startIndex + 1] - 0x61);
        var thirdChar = (byte)(buffer[startIndex + 2] - 0x61);

        var targetBucket = (firstChar << 3) + (secondChar << 2) + thirdChar;

        byte* indexPtr = key.BytePtr + startIndex;
        ulong* pUint64 = (ulong*)indexPtr;
        key.Hash_8_1 = *pUint64;
        key.Hash_8_1 &= 0x000000FFFFFFFFFF;
        var hashCode = key.Hash_8_1;

        for (int i = usedBucket[targetBucket]; i >= 0; i = usedEntries[i].next)
        {
            var entry = usedEntries[i];
            if (entry.Hash_8_1 == hashCode)
            {
                return ref usedEntries[i];
            }
        }

        var index = count;
        count++;

        usedEntries[index].next = usedBucket[targetBucket];
        usedBucket[targetBucket] = index;

        ref var entryValue = ref usedEntries[index]!;
        entryValue.NameLen = key.NameLen;
        entryValue.Hash_8_1 = hashCode;
        entryValue.TargetBucket = targetBucket;

        #region sorting name
        var nameSwapBuffer = key.NameSwapBuffer;

        nameSwapBuffer[3] = buffer[startIndex];
        nameSwapBuffer[2] = buffer[startIndex + 1];
        nameSwapBuffer[1] = buffer[startIndex + 2];
        nameSwapBuffer[0] = buffer[startIndex + 3];
        entryValue.SortName_4_1 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 4];
        nameSwapBuffer[2] = 0;
        nameSwapBuffer[1] = 0;
        nameSwapBuffer[0] = 0;
        entryValue.SortName_4_2 = *key.UintNameSwapBufferPtr;
        #endregion sorting name

        return ref entryValue;
    }

    private static unsafe ref Entry ProcessLen6(DebugOptStationKey key, ref int count)
    {
        var nameLen = key.NameLen;
        var usedBucket = key.buckets[nameLen];
        var usedEntries = key.entries[nameLen];

        var buffer = key.SourceArray!;
        var startIndex = key.StartIndex;

        byte firstChar = (byte)(buffer[startIndex] - 0x41);
        var secondChar = (byte)(buffer[startIndex + 1] - 0x61);
        var thirdChar = (byte)(buffer[startIndex + 2] - 0x61);

        var targetBucket = (firstChar << 3) + (secondChar << 2) + thirdChar;

        byte* indexPtr = key.BytePtr + startIndex;
        ulong* pUint64 = (ulong*)indexPtr;
        key.Hash_8_1 = *pUint64;
        key.Hash_8_1 &= 0x0000FFFFFFFFFFFF;
        var hashCode = key.Hash_8_1;

        for (int i = usedBucket[targetBucket]; i >= 0; i = usedEntries[i].next)
        {
            var entry = usedEntries[i];
            if (entry.Hash_8_1 == hashCode)
            {
                return ref usedEntries[i];
            }
        }

        var index = count;
        count++;

        usedEntries[index].next = usedBucket[targetBucket];
        usedBucket[targetBucket] = index;

        ref var entryValue = ref usedEntries[index]!;
        entryValue.NameLen = key.NameLen;
        entryValue.Hash_8_1 = hashCode;
        entryValue.TargetBucket = targetBucket;

        #region sorting name
        var nameSwapBuffer = key.NameSwapBuffer;

        nameSwapBuffer[3] = buffer[startIndex];
        nameSwapBuffer[2] = buffer[startIndex + 1];
        nameSwapBuffer[1] = buffer[startIndex + 2];
        nameSwapBuffer[0] = buffer[startIndex + 3];
        entryValue.SortName_4_1 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 4];
        nameSwapBuffer[2] = buffer[startIndex + 5];
        nameSwapBuffer[1] = 0;
        nameSwapBuffer[0] = 0;
        entryValue.SortName_4_2 = *key.UintNameSwapBufferPtr;
        #endregion sorting name

        return ref entryValue;
    }

    public static unsafe ref Entry ProcessLen7(DebugOptStationKey key, ref int count)
    {
        var nameLen = key.NameLen;
        var usedBucket = key.buckets[nameLen];
        var usedEntries = key.entries[nameLen];

        var buffer = key.SourceArray!;
        var startIndex = key.StartIndex;

        byte firstChar = (byte)(buffer[startIndex] - 0x41);
        var secondChar = (byte)(buffer[startIndex + 1] - 0x61);
        var thirdChar = (byte)(buffer[startIndex + 2] - 0x61);

        var targetBucket = (firstChar << 3) + (secondChar << 2) + thirdChar;

        byte* indexPtr = key.BytePtr + startIndex;
        ulong* pUint64 = (ulong*)indexPtr;
        key.Hash_8_1 = *pUint64;
        key.Hash_8_1 &= 0x00FFFFFFFFFFFFFF;
        var hashCode = key.Hash_8_1;

        for (int i = usedBucket[targetBucket]; i >= 0; i = usedEntries[i].next)
        {
            var entry = usedEntries[i];
            if (entry.Hash_8_1 == hashCode)
            {
                return ref usedEntries[i];
            }
        }

        var index = count;
        count++;

        usedEntries[index].next = usedBucket[targetBucket];
        usedBucket[targetBucket] = index;

        ref var entryValue = ref usedEntries[index]!;
        entryValue.NameLen = key.NameLen;
        entryValue.Hash_8_1 = hashCode;
        entryValue.TargetBucket = targetBucket;

        #region sorting name
        var nameSwapBuffer = key.NameSwapBuffer;

        nameSwapBuffer[3] = buffer[startIndex];
        nameSwapBuffer[2] = buffer[startIndex + 1];
        nameSwapBuffer[1] = buffer[startIndex + 2];
        nameSwapBuffer[0] = buffer[startIndex + 3];
        entryValue.SortName_4_1 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 4];
        nameSwapBuffer[2] = buffer[startIndex + 5];
        nameSwapBuffer[1] = buffer[startIndex + 6];
        nameSwapBuffer[0] = 0;
        entryValue.SortName_4_2 = *key.UintNameSwapBufferPtr;
        #endregion sorting name

        return ref entryValue;
    }

    private static unsafe ref Entry ProcessLen8(DebugOptStationKey key, ref int count)
    {
        var nameLen = key.NameLen;
        var usedBucket = key.buckets[nameLen];
        var usedEntries = key.entries[nameLen];

        var buffer = key.SourceArray!;
        var startIndex = key.StartIndex;

        byte firstChar = (byte)(buffer[startIndex] - 0x41);
        var secondChar = (byte)(buffer[startIndex + 1] - 0x61);
        var thirdChar = (byte)(buffer[startIndex + 2] - 0x61);

        var targetBucket = (firstChar << 3) + (secondChar << 2) + thirdChar;

        byte* indexPtr = key.BytePtr + startIndex;
        ulong* pUint64 = (ulong*)indexPtr;
        key.Hash_8_1 = *pUint64;

        var hashCode = key.Hash_8_1;

        for (int i = usedBucket[targetBucket]; i >= 0; i = usedEntries[i].next)
        {
            var entry = usedEntries[i];
            if (entry.Hash_8_1 == hashCode)
            {
                return ref usedEntries[i];
            }
        }

        var index = count;
        count++;

        usedEntries[index].next = usedBucket[targetBucket];
        usedBucket[targetBucket] = index;

        ref var entryValue = ref usedEntries[index]!;
        entryValue.NameLen = key.NameLen;
        entryValue.Hash_8_1 = hashCode;
        entryValue.TargetBucket = targetBucket;

        #region sorting name
        var nameSwapBuffer = key.NameSwapBuffer;

        nameSwapBuffer[3] = buffer[startIndex];
        nameSwapBuffer[2] = buffer[startIndex + 1];
        nameSwapBuffer[1] = buffer[startIndex + 2];
        nameSwapBuffer[0] = buffer[startIndex + 3];
        entryValue.SortName_4_1 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 4];
        nameSwapBuffer[2] = buffer[startIndex + 5];
        nameSwapBuffer[1] = buffer[startIndex + 6];
        nameSwapBuffer[0] = buffer[startIndex + 7];
        entryValue.SortName_4_2 = *key.UintNameSwapBufferPtr;
        #endregion sorting name

        return ref entryValue;
    }

    private static unsafe ref Entry ProcessLen9(DebugOptStationKey key, ref int count)
    {
        var nameLen = key.NameLen;
        var usedBucket = key.buckets[nameLen];
        var usedEntries = key.entries[nameLen];

        var buffer = key.SourceArray!;
        var startIndex = key.StartIndex;

        byte firstChar = (byte)(buffer[startIndex] - 0x41);
        var secondChar = (byte)(buffer[startIndex + 1] - 0x61);
        var thirdChar = (byte)(buffer[startIndex + 2] - 0x61);

        var targetBucket = (firstChar << 3) + (secondChar << 2) + thirdChar;

        byte* indexPtr = key.BytePtr + startIndex;
        ulong* pUint64 = (ulong*)indexPtr;
        key.Hash_8_1 = *pUint64;

        uint* pUlong = (uint*)(indexPtr + 8);
        key.Hash_4_1 = *pUlong;
        key.Hash_4_1 &= 0x000000FF;
        var hashCode = key.Hash_8_1;
        var hashCode2 = key.Hash_4_1;

        for (int i = usedBucket[targetBucket]; i >= 0; i = usedEntries[i].next)
        {
            var entry = usedEntries[i];
            if (entry.Hash_8_1 == hashCode && entry.Hash_4_1 == hashCode2)
            {
                return ref usedEntries[i];
            }
        }

        var index = count;
        count++;

        usedEntries[index].next = usedBucket[targetBucket];
        usedBucket[targetBucket] = index;

        ref var entryValue = ref usedEntries[index]!;
        entryValue.NameLen = key.NameLen;
        entryValue.Hash_8_1 = hashCode;
        entryValue.Hash_4_1 = hashCode2;
        entryValue.TargetBucket = targetBucket;

        #region sorting name
        var nameSwapBuffer = key.NameSwapBuffer;

        nameSwapBuffer[3] = buffer[startIndex];
        nameSwapBuffer[2] = buffer[startIndex + 1];
        nameSwapBuffer[1] = buffer[startIndex + 2];
        nameSwapBuffer[0] = buffer[startIndex + 3];
        entryValue.SortName_4_1 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 4];
        nameSwapBuffer[2] = buffer[startIndex + 5];
        nameSwapBuffer[1] = buffer[startIndex + 6];
        nameSwapBuffer[0] = buffer[startIndex + 7];
        entryValue.SortName_4_2 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 8];
        nameSwapBuffer[2] = 0;
        nameSwapBuffer[1] = 0;
        nameSwapBuffer[0] = 0;
        entryValue.SortName_4_3 = *key.UintNameSwapBufferPtr;
        #endregion sorting name

        return ref entryValue;
    }

    private static unsafe ref Entry ProcessLen10(DebugOptStationKey key, ref int count)
    {
        var nameLen = key.NameLen;
        var usedBucket = key.buckets[nameLen];
        var usedEntries = key.entries[nameLen];

        var buffer = key.SourceArray!;
        var startIndex = key.StartIndex;

        byte firstChar = (byte)(buffer[startIndex] - 0x41);
        var secondChar = (byte)(buffer[startIndex + 1] - 0x61);
        var thirdChar = (byte)(buffer[startIndex + 2] - 0x61);

        var targetBucket = (firstChar << 3) + (secondChar << 2) + thirdChar;

        byte* indexPtr = key.BytePtr + startIndex;
        ulong* pUint64 = (ulong*)indexPtr;
        key.Hash_8_1 = *pUint64;

        uint* pUlong = (uint*)(indexPtr + 8);
        key.Hash_4_1 = *pUlong;
        key.Hash_4_1 &= 0x0000FFFF;
        var hashCode = key.Hash_8_1;
        var hashCode2 = key.Hash_4_1;

        for (int i = usedBucket[targetBucket]; i >= 0; i = usedEntries[i].next)
        {
            var entry = usedEntries[i];
            if (entry.Hash_8_1 == hashCode && entry.Hash_4_1 == hashCode2)
            {
                return ref usedEntries[i];
            }
        }

        var index = count;
        count++;

        usedEntries[index].next = usedBucket[targetBucket];
        usedBucket[targetBucket] = index;

        ref var entryValue = ref usedEntries[index]!;
        entryValue.NameLen = key.NameLen;
        entryValue.Hash_8_1 = hashCode;
        entryValue.Hash_4_1 = hashCode2;
        entryValue.TargetBucket = targetBucket;

        #region sorting name
        var nameSwapBuffer = key.NameSwapBuffer;

        nameSwapBuffer[3] = buffer[startIndex];
        nameSwapBuffer[2] = buffer[startIndex + 1];
        nameSwapBuffer[1] = buffer[startIndex + 2];
        nameSwapBuffer[0] = buffer[startIndex + 3];
        entryValue.SortName_4_1 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 4];
        nameSwapBuffer[2] = buffer[startIndex + 5];
        nameSwapBuffer[1] = buffer[startIndex + 6];
        nameSwapBuffer[0] = buffer[startIndex + 7];
        entryValue.SortName_4_2 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 8];
        nameSwapBuffer[2] = buffer[startIndex + 9];
        nameSwapBuffer[1] = 0;
        nameSwapBuffer[0] = 0;
        entryValue.SortName_4_3 = *key.UintNameSwapBufferPtr;
        #endregion sorting name

        return ref entryValue;
    }

    private static unsafe ref Entry ProcessLen11(DebugOptStationKey key, ref int count)
    {
        var nameLen = key.NameLen;
        var usedBucket = key.buckets[nameLen];
        var usedEntries = key.entries[nameLen];

        var buffer = key.SourceArray!;
        var startIndex = key.StartIndex;

        byte firstChar = (byte)(buffer[startIndex] - 0x41);
        var secondChar = (byte)(buffer[startIndex + 1] - 0x61);
        var thirdChar = (byte)(buffer[startIndex + 2] - 0x61);

        var targetBucket = (firstChar << 3) + (secondChar << 2) + thirdChar;

        byte* indexPtr = key.BytePtr + startIndex;
        ulong* pUint64 = (ulong*)indexPtr;
        key.Hash_8_1 = *pUint64;

        uint* pUlong = (uint*)(indexPtr + 8);
        key.Hash_4_1 = *pUlong;
        key.Hash_4_1 &= 0x00FFFFFF;
        var hashCode = key.Hash_8_1;
        var hashCode2 = key.Hash_4_1;

        for (int i = usedBucket[targetBucket]; i >= 0; i = usedEntries[i].next)
        {
            var entry = usedEntries[i];
            if (entry.Hash_8_1 == hashCode && entry.Hash_4_1 == hashCode2)
            {
                return ref usedEntries[i];
            }
        }

        var index = count;
        count++;

        usedEntries[index].next = usedBucket[targetBucket];
        usedBucket[targetBucket] = index;

        ref var entryValue = ref usedEntries[index]!;
        entryValue.NameLen = key.NameLen;
        entryValue.Hash_8_1 = hashCode;
        entryValue.Hash_4_1 = hashCode2;
        entryValue.TargetBucket = targetBucket;

        #region sorting name
        var nameSwapBuffer = key.NameSwapBuffer;

        nameSwapBuffer[3] = buffer[startIndex];
        nameSwapBuffer[2] = buffer[startIndex + 1];
        nameSwapBuffer[1] = buffer[startIndex + 2];
        nameSwapBuffer[0] = buffer[startIndex + 3];
        entryValue.SortName_4_1 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 4];
        nameSwapBuffer[2] = buffer[startIndex + 5];
        nameSwapBuffer[1] = buffer[startIndex + 6];
        nameSwapBuffer[0] = buffer[startIndex + 7];
        entryValue.SortName_4_2 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 8];
        nameSwapBuffer[2] = buffer[startIndex + 9];
        nameSwapBuffer[1] = buffer[startIndex + 10];
        nameSwapBuffer[0] = 0;
        entryValue.SortName_4_3 = *key.UintNameSwapBufferPtr;
        #endregion sorting name

        return ref entryValue;
    }

    private static unsafe ref Entry ProcessLen12(DebugOptStationKey key, ref int count)
    {
        var nameLen = key.NameLen;
        var usedBucket = key.buckets[nameLen];
        var usedEntries = key.entries[nameLen];

        var buffer = key.SourceArray!;
        var startIndex = key.StartIndex;

        byte firstChar = (byte)(buffer[startIndex] - 0x41);
        var secondChar = (byte)(buffer[startIndex + 1] - 0x61);
        var thirdChar = (byte)(buffer[startIndex + 2] - 0x61);

        var targetBucket = (firstChar << 3) + (secondChar << 2) + thirdChar;

        byte* indexPtr = key.BytePtr + startIndex;
        ulong* pUint64 = (ulong*)indexPtr;
        key.Hash_8_1 = *pUint64;

        uint* pUlong = (uint*)(indexPtr + 8);
        key.Hash_4_1 = *pUlong;
        var hashCode = key.Hash_8_1;
        var hashCode2 = key.Hash_4_1;

        for (int i = usedBucket[targetBucket]; i >= 0; i = usedEntries[i].next)
        {
            var entry = usedEntries[i];
            if (entry.Hash_8_1 == hashCode && entry.Hash_4_1 == hashCode2)
            {
                return ref usedEntries[i];
            }
        }

        var index = count;
        count++;

        usedEntries[index].next = usedBucket[targetBucket];
        usedBucket[targetBucket] = index;

        ref var entryValue = ref usedEntries[index]!;
        entryValue.NameLen = key.NameLen;
        entryValue.Hash_8_1 = hashCode;
        entryValue.Hash_4_1 = hashCode2;
        entryValue.TargetBucket = targetBucket;

        #region sorting name
        var nameSwapBuffer = key.NameSwapBuffer;

        nameSwapBuffer[3] = buffer[startIndex];
        nameSwapBuffer[2] = buffer[startIndex + 1];
        nameSwapBuffer[1] = buffer[startIndex + 2];
        nameSwapBuffer[0] = buffer[startIndex + 3];
        entryValue.SortName_4_1 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 4];
        nameSwapBuffer[2] = buffer[startIndex + 5];
        nameSwapBuffer[1] = buffer[startIndex + 6];
        nameSwapBuffer[0] = buffer[startIndex + 7];
        entryValue.SortName_4_2 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 8];
        nameSwapBuffer[2] = buffer[startIndex + 9];
        nameSwapBuffer[1] = buffer[startIndex + 10];
        nameSwapBuffer[0] = buffer[startIndex + 11];
        entryValue.SortName_4_3 = *key.UintNameSwapBufferPtr;
        #endregion sorting name

        return ref entryValue;
    }

    private static unsafe ref Entry ProcessLen13(DebugOptStationKey key, ref int count)
    {
        var nameLen = key.NameLen;
        var usedBucket = key.buckets[nameLen];
        var usedEntries = key.entries[nameLen];

        var buffer = key.SourceArray!;
        var startIndex = key.StartIndex;

        byte firstChar = (byte)(buffer[startIndex] - 0x41);
        var secondChar = (byte)(buffer[startIndex + 1] - 0x61);
        var thirdChar = (byte)(buffer[startIndex + 2] - 0x61);

        var targetBucket = (firstChar << 3) + (secondChar << 2) + thirdChar;

        byte* indexPtr = key.BytePtr + startIndex;
        ulong* pUint64 = (ulong*)indexPtr;
        key.Hash_8_1 = *pUint64;

        pUint64 = (ulong*)(indexPtr + 8);
        key.Hash_8_2 = *pUint64;
        key.Hash_8_2 &= 0x000000FFFFFFFFFF;

        var hashCode = key.Hash_8_1;
        var hashCode2 = key.Hash_8_2;

        for (int i = usedBucket[targetBucket]; i >= 0; i = usedEntries[i].next)
        {
            var entry = usedEntries[i];
            if (entry.Hash_8_1 == hashCode && entry.Hash_8_2 == hashCode2)
            {
                return ref usedEntries[i];
            }
        }

        var index = count;
        count++;

        usedEntries[index].next = usedBucket[targetBucket];
        usedBucket[targetBucket] = index;

        ref var entryValue = ref usedEntries[index]!;
        entryValue.NameLen = key.NameLen;
        entryValue.Hash_8_1 = hashCode;
        entryValue.Hash_8_2 = hashCode2;
        entryValue.TargetBucket = targetBucket;

        #region sorting name
        var nameSwapBuffer = key.NameSwapBuffer;

        nameSwapBuffer[3] = buffer[startIndex];
        nameSwapBuffer[2] = buffer[startIndex + 1];
        nameSwapBuffer[1] = buffer[startIndex + 2];
        nameSwapBuffer[0] = buffer[startIndex + 3];
        entryValue.SortName_4_1 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 4];
        nameSwapBuffer[2] = buffer[startIndex + 5];
        nameSwapBuffer[1] = buffer[startIndex + 6];
        nameSwapBuffer[0] = buffer[startIndex + 7];
        entryValue.SortName_4_2 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 8];
        nameSwapBuffer[2] = buffer[startIndex + 9];
        nameSwapBuffer[1] = buffer[startIndex + 10];
        nameSwapBuffer[0] = buffer[startIndex + 11];
        entryValue.SortName_4_3 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 12];
        nameSwapBuffer[2] = 0;
        nameSwapBuffer[1] = 0;
        nameSwapBuffer[0] = 0;
        entryValue.SortName_4_4 = *key.UintNameSwapBufferPtr;
        #endregion sorting name

        return ref entryValue;
    }

    private static unsafe ref Entry ProcessLen14(DebugOptStationKey key, ref int count)
    {
        var nameLen = key.NameLen;
        var usedBucket = key.buckets[nameLen];
        var usedEntries = key.entries[nameLen];

        var buffer = key.SourceArray!;
        var startIndex = key.StartIndex;

        byte firstChar = (byte)(buffer[startIndex] - 0x41);
        var secondChar = (byte)(buffer[startIndex + 1] - 0x61);
        var thirdChar = (byte)(buffer[startIndex + 2] - 0x61);

        var targetBucket = (firstChar << 3) + (secondChar << 2) + thirdChar;

        byte* indexPtr = key.BytePtr + startIndex;
        ulong* pUint64 = (ulong*)indexPtr;
        key.Hash_8_1 = *pUint64;

        pUint64 = (ulong*)(indexPtr + 8);
        key.Hash_8_2 = *pUint64;
        key.Hash_8_2 &= 0x0000FFFFFFFFFFFF;

        var hashCode = key.Hash_8_1;
        var hashCode2 = key.Hash_8_2;

        for (int i = usedBucket[targetBucket]; i >= 0; i = usedEntries[i].next)
        {
            var entry = usedEntries[i];
            if (entry.Hash_8_1 == hashCode && entry.Hash_8_2 == hashCode2)
            {
                return ref usedEntries[i];
            }
        }

        var index = count;
        count++;

        usedEntries[index].next = usedBucket[targetBucket];
        usedBucket[targetBucket] = index;

        ref var entryValue = ref usedEntries[index]!;
        entryValue.NameLen = key.NameLen;
        entryValue.Hash_8_1 = hashCode;
        entryValue.Hash_8_2 = hashCode2;
        entryValue.TargetBucket = targetBucket;

        #region sorting name
        var nameSwapBuffer = key.NameSwapBuffer;

        nameSwapBuffer[3] = buffer[startIndex];
        nameSwapBuffer[2] = buffer[startIndex + 1];
        nameSwapBuffer[1] = buffer[startIndex + 2];
        nameSwapBuffer[0] = buffer[startIndex + 3];
        entryValue.SortName_4_1 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 4];
        nameSwapBuffer[2] = buffer[startIndex + 5];
        nameSwapBuffer[1] = buffer[startIndex + 6];
        nameSwapBuffer[0] = buffer[startIndex + 7];
        entryValue.SortName_4_2 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 8];
        nameSwapBuffer[2] = buffer[startIndex + 9];
        nameSwapBuffer[1] = buffer[startIndex + 10];
        nameSwapBuffer[0] = buffer[startIndex + 11];
        entryValue.SortName_4_3 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 12];
        nameSwapBuffer[2] = buffer[startIndex + 13];
        nameSwapBuffer[1] = 0;
        nameSwapBuffer[0] = 0;
        entryValue.SortName_4_4 = *key.UintNameSwapBufferPtr;
        #endregion sorting name

        return ref entryValue;
    }

    private static unsafe ref Entry ProcessLen15(DebugOptStationKey key, ref int count)
    {
        var nameLen = key.NameLen;
        var usedBucket = key.buckets[nameLen];
        var usedEntries = key.entries[nameLen];

        var buffer = key.SourceArray!;
        var startIndex = key.StartIndex;

        byte firstChar = (byte)(buffer[startIndex] - 0x41);
        var secondChar = (byte)(buffer[startIndex + 1] - 0x61);
        var thirdChar = (byte)(buffer[startIndex + 2] - 0x61);

        var targetBucket = (firstChar << 3) + (secondChar << 2) + thirdChar;

        byte* indexPtr = key.BytePtr + startIndex;
        ulong* pUint64 = (ulong*)indexPtr;
        key.Hash_8_1 = *pUint64;

        pUint64 = (ulong*)(indexPtr + 8);
        key.Hash_8_2 = *pUint64;
        key.Hash_8_2 &= 0x00FFFFFFFFFFFFFF;

        var hashCode = key.Hash_8_1;
        var hashCode2 = key.Hash_8_2;

        for (int i = usedBucket[targetBucket]; i >= 0; i = usedEntries[i].next)
        {
            var entry = usedEntries[i];
            if (entry.Hash_8_1 == hashCode && entry.Hash_8_2 == hashCode2)
            {
                return ref usedEntries[i];
            }
        }

        var index = count;
        count++;

        usedEntries[index].next = usedBucket[targetBucket];
        usedBucket[targetBucket] = index;

        ref var entryValue = ref usedEntries[index]!;
        entryValue.NameLen = key.NameLen;
        entryValue.Hash_8_1 = hashCode;
        entryValue.Hash_8_2 = hashCode2;
        entryValue.TargetBucket = targetBucket;

        #region sorting name
        var nameSwapBuffer = key.NameSwapBuffer;

        nameSwapBuffer[3] = buffer[startIndex];
        nameSwapBuffer[2] = buffer[startIndex + 1];
        nameSwapBuffer[1] = buffer[startIndex + 2];
        nameSwapBuffer[0] = buffer[startIndex + 3];
        entryValue.SortName_4_1 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 4];
        nameSwapBuffer[2] = buffer[startIndex + 5];
        nameSwapBuffer[1] = buffer[startIndex + 6];
        nameSwapBuffer[0] = buffer[startIndex + 7];
        entryValue.SortName_4_2 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 8];
        nameSwapBuffer[2] = buffer[startIndex + 9];
        nameSwapBuffer[1] = buffer[startIndex + 10];
        nameSwapBuffer[0] = buffer[startIndex + 11];
        entryValue.SortName_4_3 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 12];
        nameSwapBuffer[2] = buffer[startIndex + 13];
        nameSwapBuffer[1] = buffer[startIndex + 14];
        nameSwapBuffer[0] = 0;
        entryValue.SortName_4_4 = *key.UintNameSwapBufferPtr;
        #endregion sorting name

        return ref entryValue;
    }

    private static unsafe ref Entry ProcessLen16(DebugOptStationKey key, ref int count)
    {
        var nameLen = key.NameLen;
        var usedBucket = key.buckets[nameLen];
        var usedEntries = key.entries[nameLen];

        var buffer = key.SourceArray!;
        var startIndex = key.StartIndex;

        byte firstChar = (byte)(buffer[startIndex] - 0x41);
        var secondChar = (byte)(buffer[startIndex + 1] - 0x61);
        var thirdChar = (byte)(buffer[startIndex + 2] - 0x61);

        var targetBucket = (firstChar << 3) + (secondChar << 2) + thirdChar;

        byte* indexPtr = key.BytePtr + startIndex;
        ulong* pUint64 = (ulong*)indexPtr;
        key.Hash_8_1 = *pUint64;

        pUint64 = (ulong*)(indexPtr + 8);
        key.Hash_8_2 = *pUint64;

        var hashCode = key.Hash_8_1;
        var hashCode2 = key.Hash_8_2;

        for (int i = usedBucket[targetBucket]; i >= 0; i = usedEntries[i].next)
        {
            var entry = usedEntries[i];
            if (entry.Hash_8_1 == hashCode && entry.Hash_8_2 == hashCode2)
            {
                return ref usedEntries[i];
            }
        }

        var index = count;
        count++;

        usedEntries[index].next = usedBucket[targetBucket];
        usedBucket[targetBucket] = index;

        ref var entryValue = ref usedEntries[index]!;
        entryValue.NameLen = key.NameLen;
        entryValue.Hash_8_1 = hashCode;
        entryValue.Hash_8_2 = hashCode2;
        entryValue.TargetBucket = targetBucket;

        #region sorting name
        var nameSwapBuffer = key.NameSwapBuffer;

        nameSwapBuffer[3] = buffer[startIndex];
        nameSwapBuffer[2] = buffer[startIndex + 1];
        nameSwapBuffer[1] = buffer[startIndex + 2];
        nameSwapBuffer[0] = buffer[startIndex + 3];
        entryValue.SortName_4_1 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 4];
        nameSwapBuffer[2] = buffer[startIndex + 5];
        nameSwapBuffer[1] = buffer[startIndex + 6];
        nameSwapBuffer[0] = buffer[startIndex + 7];
        entryValue.SortName_4_2 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 8];
        nameSwapBuffer[2] = buffer[startIndex + 9];
        nameSwapBuffer[1] = buffer[startIndex + 10];
        nameSwapBuffer[0] = buffer[startIndex + 11];
        entryValue.SortName_4_3 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 12];
        nameSwapBuffer[2] = buffer[startIndex + 13];
        nameSwapBuffer[1] = buffer[startIndex + 14];
        nameSwapBuffer[0] = buffer[startIndex + 15];
        entryValue.SortName_4_4 = *key.UintNameSwapBufferPtr;
        #endregion sorting name

        return ref entryValue;
    }

    private static unsafe ref Entry ProcessLen17(DebugOptStationKey key, ref int count)
    {
        var nameLen = key.NameLen;
        var usedBucket = key.buckets[nameLen];
        var usedEntries = key.entries[nameLen];

        var buffer = key.SourceArray!;
        var startIndex = key.StartIndex;

        byte firstChar = (byte)(buffer[startIndex] - 0x41);
        var secondChar = (byte)(buffer[startIndex + 1] - 0x61);
        var thirdChar = (byte)(buffer[startIndex + 2] - 0x61);

        var targetBucket = (firstChar << 3) + (secondChar << 2) + thirdChar;

        byte* indexPtr = key.BytePtr + startIndex;
        ulong* pUint64 = (ulong*)indexPtr;
        key.Hash_8_1 = *pUint64;

        pUint64 = (ulong*)(indexPtr + 8);
        key.Hash_8_2 = *pUint64;

        uint* pUint = (uint*)(indexPtr + 16);
        key.Hash_4_1 = *pUint;
        key.Hash_4_1 &= 0x000000FF;

        var hashCode = key.Hash_8_1;
        var hashCode2 = key.Hash_8_2;
        var hashCode3 = key.Hash_4_1;

        for (int i = usedBucket[targetBucket]; i >= 0; i = usedEntries[i].next)
        {
            var entry = usedEntries[i];
            if (entry.Hash_8_1 == hashCode && entry.Hash_8_2 == hashCode2 && entry.Hash_4_1 == hashCode3)
            {
                return ref usedEntries[i];
            }
        }

        var index = count;
        count++;

        usedEntries[index].next = usedBucket[targetBucket];
        usedBucket[targetBucket] = index;

        ref var entryValue = ref usedEntries[index]!;
        entryValue.NameLen = key.NameLen;
        entryValue.Hash_8_1 = hashCode;
        entryValue.Hash_8_2 = hashCode2;
        entryValue.Hash_4_1 = hashCode3;
        entryValue.TargetBucket = targetBucket;

        #region sorting name
        var nameSwapBuffer = key.NameSwapBuffer;

        nameSwapBuffer[3] = buffer[startIndex];
        nameSwapBuffer[2] = buffer[startIndex + 1];
        nameSwapBuffer[1] = buffer[startIndex + 2];
        nameSwapBuffer[0] = buffer[startIndex + 3];
        entryValue.SortName_4_1 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 4];
        nameSwapBuffer[2] = buffer[startIndex + 5];
        nameSwapBuffer[1] = buffer[startIndex + 6];
        nameSwapBuffer[0] = buffer[startIndex + 7];
        entryValue.SortName_4_2 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 8];
        nameSwapBuffer[2] = buffer[startIndex + 9];
        nameSwapBuffer[1] = buffer[startIndex + 10];
        nameSwapBuffer[0] = buffer[startIndex + 11];
        entryValue.SortName_4_3 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 12];
        nameSwapBuffer[2] = buffer[startIndex + 13];
        nameSwapBuffer[1] = buffer[startIndex + 14];
        nameSwapBuffer[0] = buffer[startIndex + 15];
        entryValue.SortName_4_4 = *key.UintNameSwapBufferPtr;
        #endregion sorting name

        return ref entryValue;
    }

    private static unsafe ref Entry ProcessLen18(DebugOptStationKey key, ref int count)
    {
        var nameLen = key.NameLen;
        var usedBucket = key.buckets[nameLen];
        var usedEntries = key.entries[nameLen];

        var buffer = key.SourceArray!;
        var startIndex = key.StartIndex;

        byte firstChar = (byte)(buffer[startIndex] - 0x41);
        var secondChar = (byte)(buffer[startIndex + 1] - 0x61);
        var thirdChar = (byte)(buffer[startIndex + 2] - 0x61);

        var targetBucket = (firstChar << 3) + (secondChar << 2) + thirdChar;

        byte* indexPtr = key.BytePtr + startIndex;
        ulong* pUint64 = (ulong*)indexPtr;
        key.Hash_8_1 = *pUint64;

        pUint64 = (ulong*)(indexPtr + 8);
        key.Hash_8_2 = *pUint64;

        uint* pUint = (uint*)(indexPtr + 16);
        key.Hash_4_1 = *pUint;
        key.Hash_4_1 &= 0x0000FFFF;

        var hashCode = key.Hash_8_1;
        var hashCode2 = key.Hash_8_2;
        var hashCode3 = key.Hash_4_1;

        for (int i = usedBucket[targetBucket]; i >= 0; i = usedEntries[i].next)
        {
            var entry = usedEntries[i];
            if (entry.Hash_8_1 == hashCode && entry.Hash_8_2 == hashCode2 && entry.Hash_4_1 == hashCode3)
            {
                return ref usedEntries[i];
            }
        }

        var index = count;
        count++;

        usedEntries[index].next = usedBucket[targetBucket];
        usedBucket[targetBucket] = index;

        ref var entryValue = ref usedEntries[index]!;
        entryValue.NameLen = key.NameLen;
        entryValue.Hash_8_1 = hashCode;
        entryValue.Hash_8_2 = hashCode2;
        entryValue.Hash_4_1 = hashCode3;
        entryValue.TargetBucket = targetBucket;

        #region sorting name
        var nameSwapBuffer = key.NameSwapBuffer;

        nameSwapBuffer[3] = buffer[startIndex];
        nameSwapBuffer[2] = buffer[startIndex + 1];
        nameSwapBuffer[1] = buffer[startIndex + 2];
        nameSwapBuffer[0] = buffer[startIndex + 3];
        entryValue.SortName_4_1 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 4];
        nameSwapBuffer[2] = buffer[startIndex + 5];
        nameSwapBuffer[1] = buffer[startIndex + 6];
        nameSwapBuffer[0] = buffer[startIndex + 7];
        entryValue.SortName_4_2 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 8];
        nameSwapBuffer[2] = buffer[startIndex + 9];
        nameSwapBuffer[1] = buffer[startIndex + 10];
        nameSwapBuffer[0] = buffer[startIndex + 11];
        entryValue.SortName_4_3 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 12];
        nameSwapBuffer[2] = buffer[startIndex + 13];
        nameSwapBuffer[1] = buffer[startIndex + 14];
        nameSwapBuffer[0] = buffer[startIndex + 15];
        entryValue.SortName_4_4 = *key.UintNameSwapBufferPtr;
        #endregion sorting name

        return ref entryValue;
    }

    private static unsafe ref Entry ProcessLen19(DebugOptStationKey key, ref int count)
    {
        var nameLen = key.NameLen;
        var usedBucket = key.buckets[nameLen];
        var usedEntries = key.entries[nameLen];

        var buffer = key.SourceArray!;
        var startIndex = key.StartIndex;

        byte firstChar = (byte)(buffer[startIndex] - 0x41);
        var secondChar = (byte)(buffer[startIndex + 1] - 0x61);
        var thirdChar = (byte)(buffer[startIndex + 2] - 0x61);

        var targetBucket = (firstChar << 3) + (secondChar << 2) + thirdChar;

        byte* indexPtr = key.BytePtr + startIndex;
        ulong* pUint64 = (ulong*)indexPtr;
        key.Hash_8_1 = *pUint64;

        pUint64 = (ulong*)(indexPtr + 8);
        key.Hash_8_2 = *pUint64;

        uint* pUint = (uint*)(indexPtr + 16);
        key.Hash_4_1 = *pUint;
        key.Hash_4_1 &= 0x00FFFFFF;

        var hashCode = key.Hash_8_1;
        var hashCode2 = key.Hash_8_2;
        var hashCode3 = key.Hash_4_1;

        for (int i = usedBucket[targetBucket]; i >= 0; i = usedEntries[i].next)
        {
            var entry = usedEntries[i];
            if (entry.Hash_8_1 == hashCode && entry.Hash_8_2 == hashCode2 && entry.Hash_4_1 == hashCode3)
            {
                return ref usedEntries[i];
            }
        }

        var index = count;
        count++;

        usedEntries[index].next = usedBucket[targetBucket];
        usedBucket[targetBucket] = index;

        ref var entryValue = ref usedEntries[index]!;
        entryValue.NameLen = key.NameLen;
        entryValue.Hash_8_1 = hashCode;
        entryValue.Hash_8_2 = hashCode2;
        entryValue.Hash_4_1 = hashCode3;
        entryValue.TargetBucket = targetBucket;

        #region sorting name
        var nameSwapBuffer = key.NameSwapBuffer;

        nameSwapBuffer[3] = buffer[startIndex];
        nameSwapBuffer[2] = buffer[startIndex + 1];
        nameSwapBuffer[1] = buffer[startIndex + 2];
        nameSwapBuffer[0] = buffer[startIndex + 3];
        entryValue.SortName_4_1 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 4];
        nameSwapBuffer[2] = buffer[startIndex + 5];
        nameSwapBuffer[1] = buffer[startIndex + 6];
        nameSwapBuffer[0] = buffer[startIndex + 7];
        entryValue.SortName_4_2 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 8];
        nameSwapBuffer[2] = buffer[startIndex + 9];
        nameSwapBuffer[1] = buffer[startIndex + 10];
        nameSwapBuffer[0] = buffer[startIndex + 11];
        entryValue.SortName_4_3 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 12];
        nameSwapBuffer[2] = buffer[startIndex + 13];
        nameSwapBuffer[1] = buffer[startIndex + 14];
        nameSwapBuffer[0] = buffer[startIndex + 15];
        entryValue.SortName_4_4 = *key.UintNameSwapBufferPtr;
        #endregion sorting name

        return ref entryValue;
    }

    private static unsafe ref Entry ProcessLen20(DebugOptStationKey key, ref int count)
    {
        var nameLen = key.NameLen;
        var usedBucket = key.buckets[nameLen];
        var usedEntries = key.entries[nameLen];

        var buffer = key.SourceArray!;
        var startIndex = key.StartIndex;

        byte firstChar = (byte)(buffer[startIndex] - 0x41);
        var secondChar = (byte)(buffer[startIndex + 1] - 0x61);
        var thirdChar = (byte)(buffer[startIndex + 2] - 0x61);

        var targetBucket = (firstChar << 3) + (secondChar << 2) + thirdChar;

        byte* indexPtr = key.BytePtr + startIndex;
        ulong* pUint64 = (ulong*)indexPtr;
        key.Hash_8_1 = *pUint64;

        pUint64 = (ulong*)(indexPtr + 8);
        key.Hash_8_2 = *pUint64;

        uint* pUint = (uint*)(indexPtr + 16);
        key.Hash_4_1 = *pUint;

        var hashCode = key.Hash_8_1;
        var hashCode2 = key.Hash_8_2;
        var hashCode3 = key.Hash_4_1;

        for (int i = usedBucket[targetBucket]; i >= 0; i = usedEntries[i].next)
        {
            var entry = usedEntries[i];
            if (entry.Hash_8_1 == hashCode && entry.Hash_8_2 == hashCode2 && entry.Hash_4_1 == hashCode3)
            {
                return ref usedEntries[i];
            }
        }

        var index = count;
        count++;

        usedEntries[index].next = usedBucket[targetBucket];
        usedBucket[targetBucket] = index;

        ref var entryValue = ref usedEntries[index]!;
        entryValue.NameLen = key.NameLen;
        entryValue.Hash_8_1 = hashCode;
        entryValue.Hash_8_2 = hashCode2;
        entryValue.Hash_4_1 = hashCode3;
        entryValue.TargetBucket = targetBucket;

        #region sorting name
        var nameSwapBuffer = key.NameSwapBuffer;

        nameSwapBuffer[3] = buffer[startIndex];
        nameSwapBuffer[2] = buffer[startIndex + 1];
        nameSwapBuffer[1] = buffer[startIndex + 2];
        nameSwapBuffer[0] = buffer[startIndex + 3];
        entryValue.SortName_4_1 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 4];
        nameSwapBuffer[2] = buffer[startIndex + 5];
        nameSwapBuffer[1] = buffer[startIndex + 6];
        nameSwapBuffer[0] = buffer[startIndex + 7];
        entryValue.SortName_4_2 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 8];
        nameSwapBuffer[2] = buffer[startIndex + 9];
        nameSwapBuffer[1] = buffer[startIndex + 10];
        nameSwapBuffer[0] = buffer[startIndex + 11];
        entryValue.SortName_4_3 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 12];
        nameSwapBuffer[2] = buffer[startIndex + 13];
        nameSwapBuffer[1] = buffer[startIndex + 14];
        nameSwapBuffer[0] = buffer[startIndex + 15];
        entryValue.SortName_4_4 = *key.UintNameSwapBufferPtr;
        #endregion sorting name

        return ref entryValue;
    }

    private static unsafe ref Entry ProcessLen21(DebugOptStationKey key, ref int count)
    {
        var nameLen = key.NameLen;
        var usedBucket = key.buckets[nameLen];
        var usedEntries = key.entries[nameLen];

        var buffer = key.SourceArray!;
        var startIndex = key.StartIndex;

        byte firstChar = (byte)(buffer[startIndex] - 0x41);
        var secondChar = (byte)(buffer[startIndex + 1] - 0x61);
        var thirdChar = (byte)(buffer[startIndex + 2] - 0x61);

        var targetBucket = (firstChar << 3) + (secondChar << 2) + thirdChar;

        byte* indexPtr = key.BytePtr + startIndex;
        ulong* pUint64 = (ulong*)indexPtr;
        key.Hash_8_1 = *pUint64;

        pUint64 = (ulong*)(indexPtr + 8);
        key.Hash_8_2 = *pUint64;

        pUint64 = (ulong*)(indexPtr + 16);
        key.Hash_8_3 = *pUint64;
        key.Hash_8_3 &= 0x000000FFFFFFFFFF;

        var hashCode = key.Hash_8_1;
        var hashCode2 = key.Hash_8_2;
        var hashCode3 = key.Hash_8_3;

        for (int i = usedBucket[targetBucket]; i >= 0; i = usedEntries[i].next)
        {
            var entry = usedEntries[i];
            if (entry.Hash_8_1 == hashCode && entry.Hash_8_2 == hashCode2 && entry.Hash_8_3 == hashCode3)
            {
                return ref usedEntries[i];
            }
        }

        var index = count;
        count++;

        usedEntries[index].next = usedBucket[targetBucket];
        usedBucket[targetBucket] = index;

        ref var entryValue = ref usedEntries[index]!;
        entryValue.NameLen = key.NameLen;
        entryValue.Hash_8_1 = hashCode;
        entryValue.Hash_8_2 = hashCode2;
        entryValue.Hash_8_3 = hashCode3;
        entryValue.TargetBucket = targetBucket;

        #region sorting name
        var nameSwapBuffer = key.NameSwapBuffer;

        nameSwapBuffer[3] = buffer[startIndex];
        nameSwapBuffer[2] = buffer[startIndex + 1];
        nameSwapBuffer[1] = buffer[startIndex + 2];
        nameSwapBuffer[0] = buffer[startIndex + 3];
        entryValue.SortName_4_1 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 4];
        nameSwapBuffer[2] = buffer[startIndex + 5];
        nameSwapBuffer[1] = buffer[startIndex + 6];
        nameSwapBuffer[0] = buffer[startIndex + 7];
        entryValue.SortName_4_2 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 8];
        nameSwapBuffer[2] = buffer[startIndex + 9];
        nameSwapBuffer[1] = buffer[startIndex + 10];
        nameSwapBuffer[0] = buffer[startIndex + 11];
        entryValue.SortName_4_3 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 12];
        nameSwapBuffer[2] = buffer[startIndex + 13];
        nameSwapBuffer[1] = buffer[startIndex + 14];
        nameSwapBuffer[0] = buffer[startIndex + 15];
        entryValue.SortName_4_4 = *key.UintNameSwapBufferPtr;
        #endregion sorting name

        return ref entryValue;
    }

    private static unsafe ref Entry ProcessLen22(DebugOptStationKey key, ref int count)
    {
        var nameLen = key.NameLen;
        var usedBucket = key.buckets[nameLen];
        var usedEntries = key.entries[nameLen];

        var buffer = key.SourceArray!;
        var startIndex = key.StartIndex;

        byte firstChar = (byte)(buffer[startIndex] - 0x41);
        var secondChar = (byte)(buffer[startIndex + 1] - 0x61);
        var thirdChar = (byte)(buffer[startIndex + 2] - 0x61);

        var targetBucket = (firstChar << 3) + (secondChar << 2) + thirdChar;

        byte* indexPtr = key.BytePtr + startIndex;
        ulong* pUint64 = (ulong*)indexPtr;
        key.Hash_8_1 = *pUint64;

        pUint64 = (ulong*)(indexPtr + 8);
        key.Hash_8_2 = *pUint64;

        pUint64 = (ulong*)(indexPtr + 16);
        key.Hash_8_3 = *pUint64;
        key.Hash_8_3 &= 0x0000FFFFFFFFFFFF;

        var hashCode = key.Hash_8_1;
        var hashCode2 = key.Hash_8_2;
        var hashCode3 = key.Hash_8_3;

        for (int i = usedBucket[targetBucket]; i >= 0; i = usedEntries[i].next)
        {
            var entry = usedEntries[i];
            if (entry.Hash_8_1 == hashCode && entry.Hash_8_2 == hashCode2 && entry.Hash_8_3 == hashCode3)
            {
                return ref usedEntries[i];
            }
        }

        var index = count;
        count++;

        usedEntries[index].next = usedBucket[targetBucket];
        usedBucket[targetBucket] = index;

        ref var entryValue = ref usedEntries[index]!;
        entryValue.NameLen = key.NameLen;
        entryValue.Hash_8_1 = hashCode;
        entryValue.Hash_8_2 = hashCode2;
        entryValue.Hash_8_3 = hashCode3;
        entryValue.TargetBucket = targetBucket;

        #region sorting name
        var nameSwapBuffer = key.NameSwapBuffer;

        nameSwapBuffer[3] = buffer[startIndex];
        nameSwapBuffer[2] = buffer[startIndex + 1];
        nameSwapBuffer[1] = buffer[startIndex + 2];
        nameSwapBuffer[0] = buffer[startIndex + 3];
        entryValue.SortName_4_1 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 4];
        nameSwapBuffer[2] = buffer[startIndex + 5];
        nameSwapBuffer[1] = buffer[startIndex + 6];
        nameSwapBuffer[0] = buffer[startIndex + 7];
        entryValue.SortName_4_2 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 8];
        nameSwapBuffer[2] = buffer[startIndex + 9];
        nameSwapBuffer[1] = buffer[startIndex + 10];
        nameSwapBuffer[0] = buffer[startIndex + 11];
        entryValue.SortName_4_3 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 12];
        nameSwapBuffer[2] = buffer[startIndex + 13];
        nameSwapBuffer[1] = buffer[startIndex + 14];
        nameSwapBuffer[0] = buffer[startIndex + 15];
        entryValue.SortName_4_4 = *key.UintNameSwapBufferPtr;
        #endregion sorting name

        return ref entryValue;
    }

    private static unsafe ref Entry ProcessLen23(DebugOptStationKey key, ref int count)
    {
        var nameLen = key.NameLen;
        var usedBucket = key.buckets[nameLen];
        var usedEntries = key.entries[nameLen];

        var buffer = key.SourceArray!;
        var startIndex = key.StartIndex;

        byte firstChar = (byte)(buffer[startIndex] - 0x41);
        var secondChar = (byte)(buffer[startIndex + 1] - 0x61);
        var thirdChar = (byte)(buffer[startIndex + 2] - 0x61);

        var targetBucket = (firstChar << 3) + (secondChar << 2) + thirdChar;

        byte* indexPtr = key.BytePtr + startIndex;
        ulong* pUint64 = (ulong*)indexPtr;
        key.Hash_8_1 = *pUint64;

        pUint64 = (ulong*)(indexPtr + 8);
        key.Hash_8_2 = *pUint64;

        pUint64 = (ulong*)(indexPtr + 16);
        key.Hash_8_3 = *pUint64;
        key.Hash_8_3 &= 0x00FFFFFFFFFFFFFF;

        var hashCode = key.Hash_8_1;
        var hashCode2 = key.Hash_8_2;
        var hashCode3 = key.Hash_8_3;

        for (int i = usedBucket[targetBucket]; i >= 0; i = usedEntries[i].next)
        {
            var entry = usedEntries[i];
            if (entry.Hash_8_1 == hashCode && entry.Hash_8_2 == hashCode2 && entry.Hash_8_3 == hashCode3)
            {
                return ref usedEntries[i];
            }
        }

        var index = count;
        count++;

        usedEntries[index].next = usedBucket[targetBucket];
        usedBucket[targetBucket] = index;

        ref var entryValue = ref usedEntries[index]!;
        entryValue.NameLen = key.NameLen;
        entryValue.Hash_8_1 = hashCode;
        entryValue.Hash_8_2 = hashCode2;
        entryValue.Hash_8_3 = hashCode3;
        entryValue.TargetBucket = targetBucket;

        #region sorting name
        var nameSwapBuffer = key.NameSwapBuffer;

        nameSwapBuffer[3] = buffer[startIndex];
        nameSwapBuffer[2] = buffer[startIndex + 1];
        nameSwapBuffer[1] = buffer[startIndex + 2];
        nameSwapBuffer[0] = buffer[startIndex + 3];
        entryValue.SortName_4_1 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 4];
        nameSwapBuffer[2] = buffer[startIndex + 5];
        nameSwapBuffer[1] = buffer[startIndex + 6];
        nameSwapBuffer[0] = buffer[startIndex + 7];
        entryValue.SortName_4_2 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 8];
        nameSwapBuffer[2] = buffer[startIndex + 9];
        nameSwapBuffer[1] = buffer[startIndex + 10];
        nameSwapBuffer[0] = buffer[startIndex + 11];
        entryValue.SortName_4_3 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 12];
        nameSwapBuffer[2] = buffer[startIndex + 13];
        nameSwapBuffer[1] = buffer[startIndex + 14];
        nameSwapBuffer[0] = buffer[startIndex + 15];
        entryValue.SortName_4_4 = *key.UintNameSwapBufferPtr;
        #endregion sorting name

        return ref entryValue;
    }

    private static unsafe ref Entry ProcessLen24(DebugOptStationKey key, ref int count)
    {
        var nameLen = key.NameLen;
        var usedBucket = key.buckets[nameLen];
        var usedEntries = key.entries[nameLen];

        var buffer = key.SourceArray!;
        var startIndex = key.StartIndex;

        byte firstChar = (byte)(buffer[startIndex] - 0x41);
        var secondChar = (byte)(buffer[startIndex + 1] - 0x61);
        var thirdChar = (byte)(buffer[startIndex + 2] - 0x61);

        var targetBucket = (firstChar << 3) + (secondChar << 2) + thirdChar;

        byte* indexPtr = key.BytePtr + startIndex;
        ulong* pUint64 = (ulong*)indexPtr;
        key.Hash_8_1 = *pUint64;

        pUint64 = (ulong*)(indexPtr + 8);
        key.Hash_8_2 = *pUint64;

        pUint64 = (ulong*)(indexPtr + 16);
        key.Hash_8_3 = *pUint64;

        var hashCode = key.Hash_8_1;
        var hashCode2 = key.Hash_8_2;
        var hashCode3 = key.Hash_8_3;

        for (int i = usedBucket[targetBucket]; i >= 0; i = usedEntries[i].next)
        {
            var entry = usedEntries[i];
            if (entry.Hash_8_1 == hashCode && entry.Hash_8_2 == hashCode2 && entry.Hash_8_3 == hashCode3)
            {
                return ref usedEntries[i];
            }
        }

        var index = count;
        count++;

        usedEntries[index].next = usedBucket[targetBucket];
        usedBucket[targetBucket] = index;

        ref var entryValue = ref usedEntries[index]!;
        entryValue.NameLen = key.NameLen;
        entryValue.Hash_8_1 = hashCode;
        entryValue.Hash_8_2 = hashCode2;
        entryValue.Hash_8_3 = hashCode3;
        entryValue.TargetBucket = targetBucket;

        #region sorting name
        var nameSwapBuffer = key.NameSwapBuffer;

        nameSwapBuffer[3] = buffer[startIndex];
        nameSwapBuffer[2] = buffer[startIndex + 1];
        nameSwapBuffer[1] = buffer[startIndex + 2];
        nameSwapBuffer[0] = buffer[startIndex + 3];
        entryValue.SortName_4_1 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 4];
        nameSwapBuffer[2] = buffer[startIndex + 5];
        nameSwapBuffer[1] = buffer[startIndex + 6];
        nameSwapBuffer[0] = buffer[startIndex + 7];
        entryValue.SortName_4_2 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 8];
        nameSwapBuffer[2] = buffer[startIndex + 9];
        nameSwapBuffer[1] = buffer[startIndex + 10];
        nameSwapBuffer[0] = buffer[startIndex + 11];
        entryValue.SortName_4_3 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 12];
        nameSwapBuffer[2] = buffer[startIndex + 13];
        nameSwapBuffer[1] = buffer[startIndex + 14];
        nameSwapBuffer[0] = buffer[startIndex + 15];
        entryValue.SortName_4_4 = *key.UintNameSwapBufferPtr;
        #endregion sorting name

        return ref entryValue;
    }

    private static unsafe ref Entry ProcessLen25(DebugOptStationKey key, ref int count)
    {
        var nameLen = key.NameLen;
        var usedBucket = key.buckets[nameLen];
        var usedEntries = key.entries[nameLen];

        var buffer = key.SourceArray!;
        var startIndex = key.StartIndex;

        byte firstChar = (byte)(buffer[startIndex] - 0x41);
        var secondChar = (byte)(buffer[startIndex + 1] - 0x61);
        var thirdChar = (byte)(buffer[startIndex + 2] - 0x61);

        var targetBucket = (firstChar << 3) + (secondChar << 2) + thirdChar;

        byte* indexPtr = key.BytePtr + startIndex;
        ulong* pUint64 = (ulong*)indexPtr;
        key.Hash_8_1 = *pUint64;

        pUint64 = (ulong*)(indexPtr + 8);
        key.Hash_8_2 = *pUint64;

        pUint64 = (ulong*)(indexPtr + 16);
        key.Hash_8_3 = *pUint64;

        uint* pUint = (uint*)(indexPtr + 24);
        key.Hash_4_1 = *pUint;
        key.Hash_4_1 &= 0x000000FF;

        var hashCode = key.Hash_8_1;
        var hashCode2 = key.Hash_8_2;
        var hashCode3 = key.Hash_8_3;
        var hashCode4 = key.Hash_4_1;

        for (int i = usedBucket[targetBucket]; i >= 0; i = usedEntries[i].next)
        {
            var entry = usedEntries[i];
            if (entry.Hash_8_1 == hashCode && entry.Hash_8_2 == hashCode2 && entry.Hash_8_3 == hashCode3 && entry.Hash_4_1 == hashCode4)
            {
                return ref usedEntries[i];
            }
        }

        var index = count;
        count++;

        usedEntries[index].next = usedBucket[targetBucket];
        usedBucket[targetBucket] = index;

        ref var entryValue = ref usedEntries[index]!;
        entryValue.NameLen = key.NameLen;
        entryValue.Hash_8_1 = hashCode;
        entryValue.Hash_8_2 = hashCode2;
        entryValue.Hash_8_3 = hashCode3;
        entryValue.Hash_4_1 = hashCode4;
        entryValue.TargetBucket = targetBucket;

        #region sorting name
        var nameSwapBuffer = key.NameSwapBuffer;

        nameSwapBuffer[3] = buffer[startIndex];
        nameSwapBuffer[2] = buffer[startIndex + 1];
        nameSwapBuffer[1] = buffer[startIndex + 2];
        nameSwapBuffer[0] = buffer[startIndex + 3];
        entryValue.SortName_4_1 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 4];
        nameSwapBuffer[2] = buffer[startIndex + 5];
        nameSwapBuffer[1] = buffer[startIndex + 6];
        nameSwapBuffer[0] = buffer[startIndex + 7];
        entryValue.SortName_4_2 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 8];
        nameSwapBuffer[2] = buffer[startIndex + 9];
        nameSwapBuffer[1] = buffer[startIndex + 10];
        nameSwapBuffer[0] = buffer[startIndex + 11];
        entryValue.SortName_4_3 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 12];
        nameSwapBuffer[2] = buffer[startIndex + 13];
        nameSwapBuffer[1] = buffer[startIndex + 14];
        nameSwapBuffer[0] = buffer[startIndex + 15];
        entryValue.SortName_4_4 = *key.UintNameSwapBufferPtr;
        #endregion sorting name

        return ref entryValue;
    }

    private static unsafe ref Entry ProcessLen26(DebugOptStationKey key, ref int count)
    {
        var nameLen = key.NameLen;
        var usedBucket = key.buckets[nameLen];
        var usedEntries = key.entries[nameLen];

        var buffer = key.SourceArray!;
        var startIndex = key.StartIndex;

        byte firstChar = (byte)(buffer[startIndex] - 0x41);
        var secondChar = (byte)(buffer[startIndex + 1] - 0x61);
        var thirdChar = (byte)(buffer[startIndex + 2] - 0x61);

        var targetBucket = (firstChar << 3) + (secondChar << 2) + thirdChar;

        byte* indexPtr = key.BytePtr + startIndex;
        ulong* pUint64 = (ulong*)indexPtr;
        key.Hash_8_1 = *pUint64;

        pUint64 = (ulong*)(indexPtr + 8);
        key.Hash_8_2 = *pUint64;

        pUint64 = (ulong*)(indexPtr + 16);
        key.Hash_8_3 = *pUint64;

        uint* pUint = (uint*)(indexPtr + 24);
        key.Hash_4_1 = *pUint;
        key.Hash_4_1 &= 0x0000FFFF;

        var hashCode = key.Hash_8_1;
        var hashCode2 = key.Hash_8_2;
        var hashCode3 = key.Hash_8_3;
        var hashCode4 = key.Hash_4_1;

        for (int i = usedBucket[targetBucket]; i >= 0; i = usedEntries[i].next)
        {
            var entry = usedEntries[i];
            if (entry.Hash_8_1 == hashCode && entry.Hash_8_2 == hashCode2 && entry.Hash_8_3 == hashCode3 && entry.Hash_4_1 == hashCode4)
            {
                return ref usedEntries[i];
            }
        }

        var index = count;
        count++;

        usedEntries[index].next = usedBucket[targetBucket];
        usedBucket[targetBucket] = index;

        ref var entryValue = ref usedEntries[index]!;
        entryValue.NameLen = key.NameLen;
        entryValue.Hash_8_1 = hashCode;
        entryValue.Hash_8_2 = hashCode2;
        entryValue.Hash_8_3 = hashCode3;
        entryValue.Hash_4_1 = hashCode4;
        entryValue.TargetBucket = targetBucket;

        #region sorting name
        var nameSwapBuffer = key.NameSwapBuffer;

        nameSwapBuffer[3] = buffer[startIndex];
        nameSwapBuffer[2] = buffer[startIndex + 1];
        nameSwapBuffer[1] = buffer[startIndex + 2];
        nameSwapBuffer[0] = buffer[startIndex + 3];
        entryValue.SortName_4_1 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 4];
        nameSwapBuffer[2] = buffer[startIndex + 5];
        nameSwapBuffer[1] = buffer[startIndex + 6];
        nameSwapBuffer[0] = buffer[startIndex + 7];
        entryValue.SortName_4_2 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 8];
        nameSwapBuffer[2] = buffer[startIndex + 9];
        nameSwapBuffer[1] = buffer[startIndex + 10];
        nameSwapBuffer[0] = buffer[startIndex + 11];
        entryValue.SortName_4_3 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 12];
        nameSwapBuffer[2] = buffer[startIndex + 13];
        nameSwapBuffer[1] = buffer[startIndex + 14];
        nameSwapBuffer[0] = buffer[startIndex + 15];
        entryValue.SortName_4_4 = *key.UintNameSwapBufferPtr;
        #endregion sorting name

        return ref entryValue;
    }

    private static unsafe ref Entry ProcessLen27(DebugOptStationKey key, ref int count)
    {
        var nameLen = key.NameLen;
        var usedBucket = key.buckets[nameLen];
        var usedEntries = key.entries[nameLen];

        var buffer = key.SourceArray!;
        var startIndex = key.StartIndex;

        byte firstChar = (byte)(buffer[startIndex] - 0x41);
        var secondChar = (byte)(buffer[startIndex + 1] - 0x61);
        var thirdChar = (byte)(buffer[startIndex + 2] - 0x61);

        var targetBucket = (firstChar << 3) + (secondChar << 2) + thirdChar;

        byte* indexPtr = key.BytePtr + startIndex;
        ulong* pUint64 = (ulong*)indexPtr;
        key.Hash_8_1 = *pUint64;

        pUint64 = (ulong*)(indexPtr + 8);
        key.Hash_8_2 = *pUint64;

        pUint64 = (ulong*)(indexPtr + 16);
        key.Hash_8_3 = *pUint64;

        uint* pUint = (uint*)(indexPtr + 24);
        key.Hash_4_1 = *pUint;
        key.Hash_4_1 &= 0x00FFFFFF;

        var hashCode = key.Hash_8_1;
        var hashCode2 = key.Hash_8_2;
        var hashCode3 = key.Hash_8_3;
        var hashCode4 = key.Hash_4_1;

        for (int i = usedBucket[targetBucket]; i >= 0; i = usedEntries[i].next)
        {
            var entry = usedEntries[i];
            if (entry.Hash_8_1 == hashCode && entry.Hash_8_2 == hashCode2 && entry.Hash_8_3 == hashCode3 && entry.Hash_4_1 == hashCode4)
            {
                return ref usedEntries[i];
            }
        }

        var index = count;
        count++;

        usedEntries[index].next = usedBucket[targetBucket];
        usedBucket[targetBucket] = index;

        ref var entryValue = ref usedEntries[index]!;
        entryValue.NameLen = key.NameLen;
        entryValue.Hash_8_1 = hashCode;
        entryValue.Hash_8_2 = hashCode2;
        entryValue.Hash_8_3 = hashCode3;
        entryValue.Hash_4_1 = hashCode4;
        entryValue.TargetBucket = targetBucket;

        #region sorting name
        var nameSwapBuffer = key.NameSwapBuffer;

        nameSwapBuffer[3] = buffer[startIndex];
        nameSwapBuffer[2] = buffer[startIndex + 1];
        nameSwapBuffer[1] = buffer[startIndex + 2];
        nameSwapBuffer[0] = buffer[startIndex + 3];
        entryValue.SortName_4_1 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 4];
        nameSwapBuffer[2] = buffer[startIndex + 5];
        nameSwapBuffer[1] = buffer[startIndex + 6];
        nameSwapBuffer[0] = buffer[startIndex + 7];
        entryValue.SortName_4_2 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 8];
        nameSwapBuffer[2] = buffer[startIndex + 9];
        nameSwapBuffer[1] = buffer[startIndex + 10];
        nameSwapBuffer[0] = buffer[startIndex + 11];
        entryValue.SortName_4_3 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 12];
        nameSwapBuffer[2] = buffer[startIndex + 13];
        nameSwapBuffer[1] = buffer[startIndex + 14];
        nameSwapBuffer[0] = buffer[startIndex + 15];
        entryValue.SortName_4_4 = *key.UintNameSwapBufferPtr;
        #endregion sorting name

        return ref entryValue;
    }

    private static unsafe ref Entry ProcessLen28(DebugOptStationKey key, ref int count)
    {
        var nameLen = key.NameLen;
        var usedBucket = key.buckets[nameLen];
        var usedEntries = key.entries[nameLen];

        var buffer = key.SourceArray!;
        var startIndex = key.StartIndex;

        byte firstChar = (byte)(buffer[startIndex] - 0x41);
        var secondChar = (byte)(buffer[startIndex + 1] - 0x61);
        var thirdChar = (byte)(buffer[startIndex + 2] - 0x61);

        var targetBucket = (firstChar << 3) + (secondChar << 2) + thirdChar;

        byte* indexPtr = key.BytePtr + startIndex;
        ulong* pUint64 = (ulong*)indexPtr;
        key.Hash_8_1 = *pUint64;

        pUint64 = (ulong*)(indexPtr + 8);
        key.Hash_8_2 = *pUint64;

        pUint64 = (ulong*)(indexPtr + 16);
        key.Hash_8_3 = *pUint64;

        uint* pUint = (uint*)(indexPtr + 24);
        key.Hash_4_1 = *pUint;

        var hashCode = key.Hash_8_1;
        var hashCode2 = key.Hash_8_2;
        var hashCode3 = key.Hash_8_3;
        var hashCode4 = key.Hash_4_1;

        for (int i = usedBucket[targetBucket]; i >= 0; i = usedEntries[i].next)
        {
            var entry = usedEntries[i];
            if (entry.Hash_8_1 == hashCode && entry.Hash_8_2 == hashCode2 && entry.Hash_8_3 == hashCode3 && entry.Hash_4_1 == hashCode4)
            {
                return ref usedEntries[i];
            }
        }

        var index = count;
        count++;

        usedEntries[index].next = usedBucket[targetBucket];
        usedBucket[targetBucket] = index;

        ref var entryValue = ref usedEntries[index]!;
        entryValue.NameLen = key.NameLen;
        entryValue.Hash_8_1 = hashCode;
        entryValue.Hash_8_2 = hashCode2;
        entryValue.Hash_8_3 = hashCode3;
        entryValue.Hash_4_1 = hashCode4;
        entryValue.TargetBucket = targetBucket;

        #region sorting name
        var nameSwapBuffer = key.NameSwapBuffer;

        nameSwapBuffer[3] = buffer[startIndex];
        nameSwapBuffer[2] = buffer[startIndex + 1];
        nameSwapBuffer[1] = buffer[startIndex + 2];
        nameSwapBuffer[0] = buffer[startIndex + 3];
        entryValue.SortName_4_1 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 4];
        nameSwapBuffer[2] = buffer[startIndex + 5];
        nameSwapBuffer[1] = buffer[startIndex + 6];
        nameSwapBuffer[0] = buffer[startIndex + 7];
        entryValue.SortName_4_2 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 8];
        nameSwapBuffer[2] = buffer[startIndex + 9];
        nameSwapBuffer[1] = buffer[startIndex + 10];
        nameSwapBuffer[0] = buffer[startIndex + 11];
        entryValue.SortName_4_3 = *key.UintNameSwapBufferPtr;

        nameSwapBuffer[3] = buffer[startIndex + 12];
        nameSwapBuffer[2] = buffer[startIndex + 13];
        nameSwapBuffer[1] = buffer[startIndex + 14];
        nameSwapBuffer[0] = buffer[startIndex + 15];
        entryValue.SortName_4_4 = *key.UintNameSwapBufferPtr;
        #endregion sorting name

        return ref entryValue;
    }
}

public class DebugOptStationKey
{
    public int StartIndex;
    public int EndIndex;
    public required byte[]? SourceArray;

    public int NameLen;

    public int[][] buckets;
    public DebugOptimizedStationDictionary.Entry[][] entries;

    public ulong Hash_8_1;
    public ulong Hash_8_2;
    public ulong Hash_8_3;
    public ulong Hash_8_4;

    public uint Hash_4_1;
    public uint Hash_4_2;

    public unsafe byte* BytePtr;
    public unsafe uint* UintPtr;
    public unsafe ulong* UlongPtr;

    public byte[] NameSwapBuffer = new byte[NameSwapBufferLen];
    public unsafe uint* UintNameSwapBufferPtr;

    public static int NameSwapBufferLen = 4;

    public int CollisionCount;
}

public class DebugStationResult
{
    public string Name;

    public decimal MinTemp;
    public decimal MaxTemp;
    public decimal MeanTemp;
}
