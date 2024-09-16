This uses BenchmarkDotNet to benchmark against the stock curve asset.

This isn't part of any supported S&box configuration, so it's using a hard-coded offset path for accessing the sbox dlls. 

Run via dotnet run -c Release

Initial results show us running roughly 5x faster in the worst case:

BenchmarkDotNet v0.14.0, Windows 11 (10.0.22631.4169/23H2/2023Update/SunValley3)
Intel Core i9-9900KF CPU 3.60GHz (Coffee Lake), 1 CPU, 16 logical and 8 physical cores
.NET SDK 8.0.400
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2


| Method     | Keys | Mean           | Error        | StdDev       | Ratio | RatioSD |
|----------- |----- |---------------:|-------------:|-------------:|------:|--------:|
| StockCurve | 1    |     5,477.0 ns |     70.24 ns |     58.66 ns |  1.00 |    0.01 |
| AltCurve   | 1    |       241.7 ns |      3.56 ns |      2.97 ns |  0.04 |    0.00 |
|            |      |                |              |              |       |         |
| StockCurve | 2    |    13,246.1 ns |    256.12 ns |    375.42 ns |  1.00 |    0.04 |
| AltCurve   | 2    |     2,331.4 ns |     46.07 ns |     43.10 ns |  0.18 |    0.01 |
|            |      |                |              |              |       |         |
| StockCurve | 5    |    13,323.1 ns |    256.07 ns |    375.34 ns |  1.00 |    0.04 |
| AltCurve   | 5    |     2,552.2 ns |     33.34 ns |     26.03 ns |  0.19 |    0.01 |
|            |      |                |              |              |       |         |
| StockCurve | 10   |    21,370.0 ns |    425.35 ns |    553.08 ns |  1.00 |    0.04 |
| AltCurve   | 10   |     2,960.3 ns |     59.23 ns |     93.95 ns |  0.14 |    0.01 |
|            |      |                |              |              |       |         |
| StockCurve | 50   |    36,079.1 ns |    676.36 ns |    564.79 ns |  1.00 |    0.02 |
| AltCurve   | 50   |     3,070.9 ns |     59.82 ns |     55.95 ns |  0.09 |    0.00 |
|            |      |                |              |              |       |         |
| StockCurve | 100  |    74,090.2 ns |  1,260.69 ns |  1,294.64 ns |  1.00 |    0.02 |
| AltCurve   | 100  |     3,243.0 ns |     54.01 ns |     50.52 ns |  0.04 |    0.00 |
|            |      |                |              |              |       |         |
| StockCurve | 1000 | 1,703,686.6 ns | 19,144.86 ns | 16,971.42 ns | 1.000 |    0.01 |
| AltCurve   | 1000 |     4,111.1 ns |     80.10 ns |    104.16 ns | 0.002 |    0.00 |