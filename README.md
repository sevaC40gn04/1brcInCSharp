# 1 billion record challenge - in C# (1brcInCSharp)

## Challenge accepted! :-)

I really liked the challenge idea - a good way to put your skills up to a test.
And it does not matter that challenge ended back in Jan, 2024 and targeted Java community.
it's a great opportunity to learn, explore and experiment using any platform.

I was able to start working on the project (challenge) only back in March, on weekends (some). Finally completed by June.

Initially, there was no any plan to post solution on GitHub,
as a result I did not keep detailed track of timing improvements from version to version, from idea to idea.

## Record Challenge results

The fastest Java implementations are in the range of 1.5 sec (and up). The test runs are performed on quite powerful hardware, which I do not have opportunity to try my solution on to compare to others, on 'Apple-to-Apple' basis.

The C# fastest solution I found is here https://1brc.dev/

## Apples to Apples compare

I downloaded C# solution source code from the site (above), built it and using the same data file (with 1 billion temperatures measures), ran under Highest performance profile
it processed 1B records in 2.4 secs.
My solution processed the same 1B records in 6.7 seconds.
These tests are run under Windows 11 (all latest updates are installed)

As an addition, both solutions were run under Windows 10, virtual machine under ProxMox host.
This machine is more powerful, installed on iCore-7 11700 (14 threads allocated for the VM)

the fastest solution produced result in 1.710 sec, my solution in 4.2 sec.

## Final solution performance

Initial solution - single thread, standard Dictionary<>, temps as decimals produced about 11 minutes 41 seconds run.

I have an old notebook (about 5 years of age), with iCore 7, 6 cores, 12 threads (tones of memory though).

I did not track the history and speeding-up, thus below a bullet points of main key optimizations.

## Algorithm

The approach is simple - byte scanning - subjectively, it simplifies conditions implementation - once a specific byte-mark is found, a specific logic/parsing is applied. Though, I knew that reading wider data (word, double word e.t.c.) should be faster, the question stayed - how fast. At the same time, it definitely would bring in a level of complexity (it requires skills to read and process data, specifically using SIMD). I am sticking to the original idea.

## Profiling

As they always say - measure, measure and measure your code run. Any code change under the measuring glass can at least give a clue if there is a significant performance degradation, or better - performance gain.

## Check generated Assembly

Literally, each C# code line, I analyzed in Visual Studio disassembly debug window to see/check generated CPU code. Changing/experimenting with C# code, and rechecking generated low level code (Assembly) helped to stick to the most optimal implementation. There were gains achieved by using this analysis (below).

## Multi-threading processing

The core logic is developed with a notion of one thread. In order to support multi threading the file byte stream is split into sequential sub-streams, where N+1 sub-stream is the continuation of the N one. It's a high chance that the last line of sub-stream is incomplete (similar, fot the N+1 the very first one). Each pair of N/N+1 streams has a stitch buffer, where torn lines are combined into one complete piece of data.

## GOTO to the rescue

The funny fact - for sure buffer stitching logic depends on how a temperature measurement line is being processed. At the very end of project completion (the experiments stretched across two months and a change on weekends), I realized that I forgot to add stitching logic. Once I implemented it, and tried to find the most optimal way to integrate it into main byte scanning logic, a use of GOTO was the fastest. No matter what code tricks I used/experimented - GOTO produced the least running time.

## Data structure

Temperatures are stored as a set of integers values, with one exception - mean sum - it's a signed long - allowing to keep proper sum value of max/min values of one billion records (or 1B / N-processing threads).
Use of integer type allows simplify/making fast parsing from string (ASCII byte array) to 'a numeric value', use more compact byte print - 4 bytes. Any integer arithmetic is done via CPU registers, opposite to floating point processing via FPU.

```C#
        public int MinTemp = int.MaxValue;
        public int MaxTemp = int.MinValue;
        public int TempCount = 0;
        public long TempSum = 0;
```

## Temperature String parsing

It's very straight forward. Back in Assembly time, if you need to convert a number to a character, just add 0x30 value, if you need to get a digit back out of a character - subtract 0x30.
To apply this transformation at once, a fraction character byte is copied into decimal point place

```C#
*((byte*)(decimalPointPtr)) = *((byte*)(decimalPointPtr + 1));
...
*(pTempUint32) = *(pTempUint32) - 0x30303000;
```

## Accessing data via pointers (by ref, only)

Funny fact - working on the challenge implementation, I had a feeling that I am trying to write code in C/Assembly, where C# was doing a great job to be on my way (of cause, it's managed environment and strictly typed). It was the very first time I tried unsafe technics in C# - practically, to get access to direct pointers to data.

Basically all memory manipulations are done via pointers (by ref).

## Pointers use on getting data from arrays (Assembly optimization)

if to use a conventional way to access an element in C# array, generated code rechecks the array boundary

C# code

```C#
byteToProcess = buffer[byteToProcessIndex];
```

generates Assembly

```Assembly
00007FFDFADDF7AB  mov         rcx,qword ptr [rbp+1F8h]
00007FFDFADDF7B2  mov         edx,dword ptr [rbp+1DCh]
00007FFDFADDF7B8  cmp         edx,dword ptr [rcx+8]
00007FFDFADDF7BB  jb          TestEntry.Solutions.Solution2.ReadAndProcessFileChunk(Int32, StringToken, ThreadProcessingDataContext)+0552h (07FFDFADDF7C2h)
00007FFDFADDF7BD  call        00007FFE5AA5F8F0
00007FFDFADDF7C2  mov         eax,edx
00007FFDFADDF7C4  lea         rcx,[rcx+rax+10h]
00007FFDFADDF7C9  movzx       ecx,byte ptr [rcx]
00007FFDFADDF7CC  mov         dword ptr [rbp+1E0h],ecx
```

you do not need to know much Assembly code details, but by just comparing this code to one using 'direct' pointer (below), it's clear that it will be executed faster

C# code

```C#
byteToProcess = *((byte*)(bufferPtr + byteToProcessIndex));
```

generated code:

```Assembly
00007FFDFADDF791  mov         rcx,qword ptr [rbp+1E8h]
00007FFDFADDF798  mov         edx,dword ptr [rbp+1DCh]
00007FFDFADDF79E  movsxd      rdx,edx
00007FFDFADDF7A1  movzx       ecx,byte ptr [rcx+rdx]
00007FFDFADDF7A5  mov         dword ptr [rbp+1E0h],ecx
```

Definitely, CPU performs less operations, and what is more important - eliminating branching

```Assembly
00007FFDFADDF7BB  jb          TestEntry.Solutions.Solution2.ReadAndProcessFileChunk(Int32, StringToken, ThreadProcessingDataContext)+0552h (07FFDFADDF7C2h)
```

For the original fastest multi threaded approach - just by eliminating boundary check - it gained a couple of seconds.

## Temperature conversion (Assembly/CPU tricks)

Since the number of decimal places is fixed - just one, the decimal point value is copied into decimal point place.
Next, hexadecimal 0x30 is subtracted to get actual decimal values. The result is in BCD (Binary-Coded Decimal). It requires an extra step to convert value into an actual integer value. Each digit place of the BCD, in order to be converted into an integer value has to be transformed using the following - 10 powered by digit place order (starting with 0). Results of each digital place are summed.

For example, 1.3.4 (which represents +13.4 degrees) is 1 _ 10^2 + 3 _ 10^1 + 4 \* 10 ^0 = 100 + 30 + 4 = 134
Note: 1.3.4 - denotes that values are in separate bytes.

### Times by 100 is two times by 10

Turned out that when multiplier is presented by two multiplications (by 10), C# compiler uses two LEA CPU ops, instead of one integer multiplication

```Assembly
00007FFA16C723FD  lea         ecx,[rcx+rcx*4]
00007FFA16C72400  add         ecx,ecx
00007FFA16C72402  lea         ecx,[rcx+rcx*4]
00007FFA16C72405  add         ecx,ecx
```

```C#
parsedTemp = (short)((short)(*((byte*)(decimalPointPtr)) * 0x0A * 0x0A) + parsedTemp);
```

when multiplier is 100

```C#
parsedTemp = (short)((short)(*((byte*)(decimalPointPtr)) * 100) + parsedTemp);
```

respective generated code is

```Assembly
00007FFA15EEF97A  movzx       ecx,byte ptr [rcx]
00007FFA15EEF97D  imul        ecx,ecx,64h
```

In general, LEA requires only one CPU clock cycle opposite to 3-5 cycles of iMUL (based on x86 implementation).
Hardware implementation slows down execution from ADD to iMUL to iDIV.

## Byte scan (byte read) opposite to LONG read

Once the logic started to work, I was very curious to check speed difference between byte and long (8 bytes) traverse.
To try it out, I picked a step to find a separator between city name and temperature - character ';'. In existing implementation, it seemed to be straight forward to 'optimize'. Using bits manipulation, and 'Lzcnt.X64.LeadingZeroCount' op, the logic to identify separator offset gave approximately 20% speed boost, at least.
The logic processes 8 or 16 bytes. An attempt to add extra 8 bytes to logic (to handle cite name longer than 16 characters), subjectively degraded performance - since, statistically, test data set has the city names (length) from 3 to 16 byes long the most.

## Mappers

Where possible, use arrays vs Dictionaries - it's faster.

## Dictionary (custom)

Originally, city data was accumulated in a standard Dictionary<,>.
Though, with all other parts of the logic polished, the solution could not pass 17-19 seconds mark.
No matter what I tried, improvement hit the wall. I experimented with Microsoft Dictionary implementation trying to understand culprit logic.
I took Dictionary source code from Microsoft web site, stripped down parts I did not need and left only what was used by the main code (including collisions counter).
Still, 17-19 seconds mark was unbreakable.

Logically, a dictionary data structure is intended to process any one access operation to any element within O(1) time. The approach to find a proper data bucket is to get an 'almost unique' key (hash), where if a bucket is not empty to traverse via a bucket linked list to find proper data element (traverse takes O(M) time). If traverse happens, we say that a hash key had a collision. Less collisions happening, or none at all is better. Number of collisions depends on the 'quality of the generated key hash' - more hashes are 'similar', more collisions we have and more likely the performance goes from O(1) to O(N) overall (if all dictionary elements have the same hash and ended up in the same bucket).
The suspect is the Hash collision.
After some experimenting, the result was surprising (well, at least for me). An integer set of keys worked with nearly 0 collisions, and string set had almost 100% collisions (on average, one collision per one access), producing respective benchmarking (below).

| Method                                 | AccessTimes |       Mean |      Error |     StdDev |     Median | Ratio | RatioSD |
| -------------------------------------- | ----------- | ---------: | ---------: | ---------: | ---------: | ----: | ------: |
| AccessToIntKeyDictionary               | 1           |   8.926 ns |  0.2011 ns |  0.1679 ns |   8.866 ns |  1.00 |    0.03 |
| AccessToIntKeyDictionaryMisses         | 1           |   8.733 ns |  0.0668 ns |  0.0625 ns |   8.704 ns |  0.98 |    0.02 |
| AccessToStrKeyDictionary               | 1           |  25.620 ns |  0.5387 ns |  0.8387 ns |  25.230 ns |  2.87 |    0.11 |
| AccessToStrKeyDictionaryVarLen         | 1           |  30.340 ns |  0.3233 ns |  0.2866 ns |  30.300 ns |  3.40 |    0.07 |
| AccessToCityNameDictionaryVarLen       | 1           |  29.786 ns |  0.6262 ns |  0.8359 ns |  29.486 ns |  3.34 |    0.11 |
| AccessToStrKeyDictionaryVarLenMisses   | 1           |  43.825 ns |  0.8745 ns |  2.4523 ns |  43.323 ns |  4.91 |    0.29 |
| AccessToCityNameDictionaryVarLenMisses | 1           |  41.713 ns |  0.7381 ns |  0.6543 ns |  41.606 ns |  4.67 |    0.11 |
|                                        |             |            |            |            |            |       |         |
| AccessToIntKeyDictionary               | 10          |  86.475 ns |  0.7948 ns |  0.6637 ns |  86.405 ns |  1.00 |    0.01 |
| AccessToIntKeyDictionaryMisses         | 10          |  88.636 ns |  1.6761 ns |  1.5679 ns |  88.415 ns |  1.03 |    0.02 |
| AccessToStrKeyDictionary               | 10          | 255.860 ns |  5.1186 ns |  8.8293 ns | 252.997 ns |  2.96 |    0.10 |
| AccessToStrKeyDictionaryVarLen         | 10          | 292.266 ns |  5.8232 ns | 11.4944 ns | 287.633 ns |  3.38 |    0.13 |
| AccessToCityNameDictionaryVarLen       | 10          | 289.069 ns |  3.1811 ns |  2.9756 ns | 289.082 ns |  3.34 |    0.04 |
| AccessToStrKeyDictionaryVarLenMisses   | 10          | 414.976 ns |  7.2224 ns | 15.8533 ns | 408.140 ns |  4.80 |    0.19 |
| AccessToCityNameDictionaryVarLenMisses | 10          | 459.570 ns | 10.4117 ns | 30.5357 ns | 447.779 ns |  5.31 |    0.35 |

Noticeably, string based key is always three times slower.
(Measurements with Misses suffix assume a scenario when an dictionary access produces no returned structure - the key to access Dictionary (on purpose) generated - does not exists in the dictionary)

A couple of more things to notice:

- if a key source is a variable length string, accesses timing is more;
- String misses processing takes much more time opposite to integer key misses

A city name is a byte array, though a string based key statistics can be applied here too.

So, what takes 3x times longer to process a string key.
After digging Microsoft dictionary code (and debugging it), it was clear that string hash is calculated based on entirely randomized base. Withing the same dictionary instance, it will be calculated as the same value, but once the same string key added to a different dictionary instance, hash is different. The integer hash across different dictionary instances will be the same. it's understandable that string processing takes more time, but - is it a source of the big difference ?

Seems that the collision fact is correlated to the nature of the key value.

Check out unit tests (DictionaryCollisionTest) that is trying to calculate number of key hash collisions based on the nature of the key and predictability of data set.
It contains three tests:

1. Key is int, where value set is defined as sequential values (predictable in a sense);
2. Key is string, where a set of values is defined similar to the first test (as well predictable);
3. Key is int, where value set is defined as a set of purely randomized values (big value distribution);

The produced results are as following:

1. int key dictionary, getAccess.count=1000000 collision.count=0
2. str key dictionary, getAccess.count=1000000 collision.count=973048
3. int Random key dictionary, getAccess.count=1000000 collision.count=959588

Seems that integer key with predictably sequential set of values produces clean O(1) access,
any other combination pushes performance to find respective bucket to O(N) collisions. Each access op is slowed down by a collision.

A conclusion that can be made is that when a nature of the key (no matter what key's data type - integer or a string) is purely random, the generated hash value will have higher chance of collision due to (presumably) Gaussian distribution. When you control (in a sense) the distribution the collision chance is nearly none, hence, the first test result - where we control predictability (even up distribution).

This conclusion (idea) was a key on how to calculate hash of the byte arrays representing city name.
The city name itself composed out of a predictable set of bytes (ASCII codes) starting with 0x41. And statistically, the name set of the file to process, nearly unique if to analyze only first three bytes (even if it's not unique, the chance of duplications is quite low).

```C#
        byte firstChar = (byte)(buffer[startIndex] - 0x41);
        var secondChar = (byte)(buffer[startIndex + 1] - 0x61);
        var thirdChar = (byte)(buffer[startIndex + 2] - 0x61);

        var targetBucket = (firstChar << 3) + (secondChar << 2) + thirdChar;
```

On top of that, different length names are stored in different data sets. It's just an extra quick trick to help to avoid collisions of a similar first three letters Hash codes, but different names overall.

Once the new dictionary was designed based on this approach the final result dropped from 17-19 seconds to 7-8 range.

## Disclaimer

I did not have time to make custom dictionary generic enough to accommodate 10K city names set (second part of the challenge). It's working fine with 400-500 names set.
just enough to prototype and check the concept and pass the first test.

## Other small tricks

Since cities were sorted out into different data sets by length, it was relatively easy to write code to operate with name data as respective uint/ulong values.

## AVX/AVX2

It was quite tempting to utilize streamed data processing (a.k.a SIMD). I managed to build a logic that utilized AVX to process temperature parsing - 4 values at once. And even though, it worked 10% slower than byte sequential processing (yeah, I know, funny), it was a good lesson-experience - first of all, there was a working prototype, second (aftermath) I realized where I 'degraded' overall performance - data preparation - you have to constantly check generated Assembly to make sure that you are not 'injecting inefficiency'. At the end, it's all positive - failure is an experience too, you do not try, you do not realize :-)

## Span

Did not try at all, but wondering if it could bring own speedup benefits, or simplify logic.

Happy coding
