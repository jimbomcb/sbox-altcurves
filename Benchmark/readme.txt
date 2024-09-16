This uses BenchmarkDotNet to benchmark against the stock curve asset.

This isn't part of any supported S&box configuration, so it's using a hard-coded offset path for accessing the sbox dlls. 

Run via dotnet run -c Release

Initial results:

Intel Core i9-9900KF CPU 3.60GHz (Coffee Lake), 1 CPU, 16 logical and 8 physical cores
.NET SDK 8.0.400
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2


| Method     | Keys | Mean           | Error       | StdDev      | Ratio | RatioSD |
|----------- |----- |---------------:|------------:|------------:|------:|--------:|
| StockCurve | 1    |     5,418.3 ns |    94.08 ns |    83.40 ns |  1.00 |    0.02 |
| AltCurve   | 1    |       238.1 ns |     2.26 ns |     2.00 ns |  0.04 |    0.00 |
|            |      |                |             |             |       |         |
| StockCurve | 5    |    15,317.6 ns |   186.51 ns |   174.46 ns |  1.00 |    0.02 |
| AltCurve   | 5    |     2,411.6 ns |    16.70 ns |    13.94 ns |  0.16 |    0.00 |
|            |      |                |             |             |       |         |
| StockCurve | 10   |    19,567.5 ns |   111.60 ns |    98.93 ns |  1.00 |    0.01 |
| AltCurve   | 10   |     2,332.4 ns |    42.17 ns |    39.45 ns |  0.12 |    0.00 |
|            |      |                |             |             |       |         |
| StockCurve | 50   |    42,502.1 ns |   820.24 ns |   767.26 ns |  1.00 |    0.02 |
| AltCurve   | 50   |     2,898.0 ns |    45.52 ns |    42.58 ns |  0.07 |    0.00 |
|            |      |                |             |             |       |         |
| StockCurve | 100  |    71,718.1 ns |   912.69 ns |   809.07 ns |  1.00 |    0.02 |
| AltCurve   | 100  |     2,947.0 ns |    40.38 ns |    33.72 ns |  0.04 |    0.00 |
|            |      |                |             |             |       |         |
| StockCurve | 2000 | 4,183,503.5 ns | 9,975.64 ns | 8,330.11 ns | 1.000 |    0.00 |
| AltCurve   | 2000 |     4,330.0 ns |    30.39 ns |    23.72 ns | 0.001 |    0.00 |
