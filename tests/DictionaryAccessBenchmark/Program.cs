// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using DictionaryAccessBenchmark;

var summary = BenchmarkRunner.Run<DifferentKeyTypeDictionaryAccess>();
