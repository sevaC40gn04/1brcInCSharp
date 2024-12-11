using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestEntry.Common;

public class OptimizedStationDictionary
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


        public int MaxTemp;
        public int MinTemp;
        public int TempCount = 0;
        public long TempSum = 0;

        public Entry() { }
    }

    public int[][] buckets;
    public Entry[][] entries;
    public int[] counts;
    public int lenCapacity;

    public OptimizedStationDictionary(int lenCapacity)
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

    public unsafe ref Entry AddGet(OptStationKey key)
    {
        var nameLen = key.EndIndex - key.StartIndex + 1;
        key.NameLen = nameLen;

        var processingHandler = len2HandlerMap[nameLen];
        return ref processingHandler(key, ref counts[nameLen]);
    }

    public unsafe OptStationKey GenerateKey(byte[] buffer)
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
            new OptStationKey()
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

    public unsafe List<StationResult> GetSortedCalculatedList()
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
        var stationResults = new List<StationResult>(totalCount);

        foreach (var indexSet in tempResult)
        {
            ref var srcEntry = ref entries[indexSet.lenBucket][indexSet.bucketIndex];
            StationResult aResultStation = new()
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

    delegate void CopyName(ref Entry srcEntry, StationResult stationResult);

    private static CopyName[] entry2StationResultHandler = new CopyName[] 
    {
        null,

        BuildNameFromHash_4_1, 
        BuildNameFromHash_4_1, 
        BuildNameFromHash_4_1, 
        BuildNameFromHash_4_1,

        BuildNameFromHash_8_1, 
        BuildNameFromHash_8_1, 
        BuildNameFromHash_8_1, 
        BuildNameFromHash_8_1,

        BuildNameFromHash_8_1_4_1, 
        BuildNameFromHash_8_1_4_1, 
        BuildNameFromHash_8_1_4_1, 
        BuildNameFromHash_8_1_4_1,

        BuildNameFromHash_8_1_8_2, 
        BuildNameFromHash_8_1_8_2, 
        BuildNameFromHash_8_1_8_2, 
        BuildNameFromHash_8_1_8_2,

        BuildNameFromHash_8_1_8_2_4_1, 
        BuildNameFromHash_8_1_8_2_4_1, 
        BuildNameFromHash_8_1_8_2_4_1, 
        BuildNameFromHash_8_1_8_2_4_1,

        BuildNameFromHash_8_1_8_2_8_3, 
        BuildNameFromHash_8_1_8_2_8_3, 
        BuildNameFromHash_8_1_8_2_8_3, 
        BuildNameFromHash_8_1_8_2_8_3,

        BuildNameFromHash_8_1_8_2_8_3_4_1, 
        BuildNameFromHash_8_1_8_2_8_3_4_1, 
        BuildNameFromHash_8_1_8_2_8_3_4_1, 
        BuildNameFromHash_8_1_8_2_8_3_4_1,
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

    private static unsafe void BuildNameFromHash_4_1(ref Entry srcEntry, StationResult stationResult)
    {
        *ctnbUintPtr = srcEntry.Hash_4_1;
        stationResult.Name = Encoding.UTF8.GetString(convertToNameBuffer, 0, srcEntry.NameLen);
    }
    private static unsafe void BuildNameFromHash_8_1(ref Entry srcEntry, StationResult stationResult)
    {
        *ctnbUlongPtr = srcEntry.Hash_8_1;
        stationResult.Name = Encoding.UTF8.GetString(convertToNameBuffer, 0, srcEntry.NameLen);
    }
    private static unsafe void BuildNameFromHash_8_1_4_1(ref Entry srcEntry, StationResult stationResult)
    {
        *ctnbUlongPtr = srcEntry.Hash_8_1;
        *ctnbUint8Ptr = srcEntry.Hash_4_1;
        stationResult.Name = Encoding.UTF8.GetString(convertToNameBuffer, 0, srcEntry.NameLen);
    }
    private static unsafe void BuildNameFromHash_8_1_8_2(ref Entry srcEntry, StationResult stationResult)
    {
        *ctnbUlongPtr = srcEntry.Hash_8_1;
        *ctnbUlong8Ptr = srcEntry.Hash_8_2;
        stationResult.Name = Encoding.UTF8.GetString(convertToNameBuffer, 0, srcEntry.NameLen);
    }
    private static unsafe void BuildNameFromHash_8_1_8_2_4_1(ref Entry srcEntry, StationResult stationResult)
    {
        *ctnbUlongPtr = srcEntry.Hash_8_1;
        *ctnbUlong8Ptr = srcEntry.Hash_8_2;
        *ctnbUint16Ptr = srcEntry.Hash_4_1;
        stationResult.Name = Encoding.UTF8.GetString(convertToNameBuffer, 0, srcEntry.NameLen);
    }
    private static unsafe void BuildNameFromHash_8_1_8_2_8_3(ref Entry srcEntry, StationResult stationResult)
    {
        *ctnbUlongPtr = srcEntry.Hash_8_1;
        *ctnbUlong8Ptr = srcEntry.Hash_8_2;
        *ctnbUlong16Ptr = srcEntry.Hash_8_3;
        stationResult.Name = Encoding.UTF8.GetString(convertToNameBuffer, 0, srcEntry.NameLen);
    }
    private static unsafe void BuildNameFromHash_8_1_8_2_8_3_4_1(ref Entry srcEntry, StationResult stationResult)
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

    public void AddDictionary(OptimizedStationDictionary srcDictionary)
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

    private static AddUpdateEntry[] len2AddUpdateHandler = new AddUpdateEntry[]
    {
        null,  // 0, 

        Hash_4_1_Processing, // 1, 2, 3, 4
        Hash_4_1_Processing,
        Hash_4_1_Processing,
        Hash_4_1_Processing,

        Hash_8_1_Processing, // 5, 6, 7, 8
        Hash_8_1_Processing,
        Hash_8_1_Processing,
        Hash_8_1_Processing,

        Hash_8_1_4_1_Processing, // 9, 10, 11, 12
        Hash_8_1_4_1_Processing,
        Hash_8_1_4_1_Processing,
        Hash_8_1_4_1_Processing,

        Hash_8_1_8_2_Processing, // 13, 14, 15, 16
        Hash_8_1_8_2_Processing,
        Hash_8_1_8_2_Processing,
        Hash_8_1_8_2_Processing,

        Hash_8_1_8_2_4_1_Processing, // 17, 18, 19, 20
        Hash_8_1_8_2_4_1_Processing,
        Hash_8_1_8_2_4_1_Processing,
        Hash_8_1_8_2_4_1_Processing,

        Hash_8_1_8_2_8_3_Processing, // 21, 22, 23, 24
        Hash_8_1_8_2_8_3_Processing,
        Hash_8_1_8_2_8_3_Processing,
        Hash_8_1_8_2_8_3_Processing,

        Hash_8_1_8_2_8_3_4_1_Processing, // 25, 26, 27, 28
        Hash_8_1_8_2_8_3_4_1_Processing,
        Hash_8_1_8_2_8_3_4_1_Processing,
        Hash_8_1_8_2_8_3_4_1_Processing,
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

    delegate ref Entry SetHash(OptStationKey key, ref int count);

    private static SetHash[] len2HandlerMap = new SetHash[]
    {
        null,

        ProcessLen1,
        ProcessLen2,
        ProcessLen3,
        ProcessLen4,
        ProcessLen5,
        ProcessLen6,
        ProcessLen7,
        ProcessLen8,
        ProcessLen9,
        ProcessLen10,
        ProcessLen11,
        ProcessLen12,
        ProcessLen13,
        ProcessLen14,
        ProcessLen15,
        ProcessLen16,
        ProcessLen17,
        ProcessLen18,
        ProcessLen19,
        ProcessLen20,
        ProcessLen21,
        ProcessLen22,
        ProcessLen23,
        ProcessLen24,
        ProcessLen25,
        ProcessLen26,
        ProcessLen27,
        ProcessLen28,
    };

    private static unsafe ref Entry ProcessLen1(OptStationKey key, ref int count)
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

        entryValue.MaxTemp = int.MinValue;
        entryValue.MinTemp = int.MaxValue;

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

    private static unsafe ref Entry ProcessLen2(OptStationKey key, ref int count)
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

        entryValue.MaxTemp = int.MinValue;
        entryValue.MinTemp = int.MaxValue;

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

    private static unsafe ref Entry ProcessLen3(OptStationKey key, ref int count)
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

        entryValue.MaxTemp = int.MinValue;
        entryValue.MinTemp = int.MaxValue;

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

    private static unsafe ref Entry ProcessLen4(OptStationKey key, ref int count)
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

        entryValue.MaxTemp = int.MinValue;
        entryValue.MinTemp = int.MaxValue;

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

    private static unsafe ref Entry ProcessLen5(OptStationKey key, ref int count)
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

        entryValue.MaxTemp = int.MinValue;
        entryValue.MinTemp = int.MaxValue;

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

    private static unsafe ref Entry ProcessLen6(OptStationKey key, ref int count)
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

        entryValue.MaxTemp = int.MinValue;
        entryValue.MinTemp = int.MaxValue;

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

    public static unsafe ref Entry ProcessLen7(OptStationKey key, ref int count)
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

        entryValue.MaxTemp = int.MinValue;
        entryValue.MinTemp = int.MaxValue;

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

    private static unsafe ref Entry ProcessLen8(OptStationKey key, ref int count)
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

        entryValue.MaxTemp = int.MinValue;
        entryValue.MinTemp = int.MaxValue;

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

    private static unsafe ref Entry ProcessLen9(OptStationKey key, ref int count)
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

        entryValue.MaxTemp = int.MinValue;
        entryValue.MinTemp = int.MaxValue;

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

    private static unsafe ref Entry ProcessLen10(OptStationKey key, ref int count)
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

        entryValue.MaxTemp = int.MinValue;
        entryValue.MinTemp = int.MaxValue;

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

    private static unsafe ref Entry ProcessLen11(OptStationKey key, ref int count)
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

        entryValue.MaxTemp = int.MinValue;
        entryValue.MinTemp = int.MaxValue;

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

    private static unsafe ref Entry ProcessLen12(OptStationKey key, ref int count)
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

        entryValue.MaxTemp = int.MinValue;
        entryValue.MinTemp = int.MaxValue;

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

    private static unsafe ref Entry ProcessLen13(OptStationKey key, ref int count)
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

        entryValue.MaxTemp = int.MinValue;
        entryValue.MinTemp = int.MaxValue;

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

    private static unsafe ref Entry ProcessLen14(OptStationKey key, ref int count)
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

        entryValue.MaxTemp = int.MinValue;
        entryValue.MinTemp = int.MaxValue;

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

    private static unsafe ref Entry ProcessLen15(OptStationKey key, ref int count)
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

        entryValue.MaxTemp = int.MinValue;
        entryValue.MinTemp = int.MaxValue;

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

    private static unsafe ref Entry ProcessLen16(OptStationKey key, ref int count)
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

        entryValue.MaxTemp = int.MinValue;
        entryValue.MinTemp = int.MaxValue;

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

    private static unsafe ref Entry ProcessLen17(OptStationKey key, ref int count)
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

        entryValue.MaxTemp = int.MinValue;
        entryValue.MinTemp = int.MaxValue;

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

    private static unsafe ref Entry ProcessLen18(OptStationKey key, ref int count)
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

        entryValue.MaxTemp = int.MinValue;
        entryValue.MinTemp = int.MaxValue;

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

    private static unsafe ref Entry ProcessLen19(OptStationKey key, ref int count)
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

        entryValue.MaxTemp = int.MinValue;
        entryValue.MinTemp = int.MaxValue;

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

    private static unsafe ref Entry ProcessLen20(OptStationKey key, ref int count)
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

        entryValue.MaxTemp = int.MinValue;
        entryValue.MinTemp = int.MaxValue;

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

    private static unsafe ref Entry ProcessLen21(OptStationKey key, ref int count)
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

        entryValue.MaxTemp = int.MinValue;
        entryValue.MinTemp = int.MaxValue;

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

    private static unsafe ref Entry ProcessLen22(OptStationKey key, ref int count)
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

        entryValue.MaxTemp = int.MinValue;
        entryValue.MinTemp = int.MaxValue;

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

    private static unsafe ref Entry ProcessLen23(OptStationKey key, ref int count)
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

        entryValue.MaxTemp = int.MinValue;
        entryValue.MinTemp = int.MaxValue;

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

    private static unsafe ref Entry ProcessLen24(OptStationKey key, ref int count)
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

        entryValue.MaxTemp = int.MinValue;
        entryValue.MinTemp = int.MaxValue;

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

    private static unsafe ref Entry ProcessLen25(OptStationKey key, ref int count)
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

        entryValue.MaxTemp = int.MinValue;
        entryValue.MinTemp = int.MaxValue;

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

    private static unsafe ref Entry ProcessLen26(OptStationKey key, ref int count)
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

        entryValue.MaxTemp = int.MinValue;
        entryValue.MinTemp = int.MaxValue;

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

    private static unsafe ref Entry ProcessLen27(OptStationKey key, ref int count)
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

        entryValue.MaxTemp = int.MinValue;
        entryValue.MinTemp = int.MaxValue;

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

    private static unsafe ref Entry ProcessLen28(OptStationKey key, ref int count)
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

        entryValue.MaxTemp = int.MinValue;
        entryValue.MinTemp = int.MaxValue;

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

public class OptStationKey
{
    public int StartIndex;
    public int EndIndex;
    public required byte[]? SourceArray;

    public int NameLen;

    public int[][] buckets;
    public OptimizedStationDictionary.Entry[][] entries;

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
}

public class StationResult
{
    public string Name;

    public decimal MinTemp;
    public decimal MaxTemp;
    public decimal MeanTemp;
}
