![curves1](https://github.com/user-attachments/assets/ab5a00d7-7d76-4c0b-a206-a461f7dd3169)
![curves2](https://github.com/user-attachments/assets/0d138422-b0bc-4e8c-9274-caa13355f4a5)

This is a replacement to the stock Curve structure, with improved performance and an expanded editor including features such as automatic tangents, pre/post-infinity controls, snapping, undo/redo, and more.

## Shortcuts
_Navigation and View:_  
**F:** Focus view  
**Middle-Mouse:** Create keyframe at location  
**Left-Mouse (Hold):** Box select  
**Delete:** Remove selected keyframes  
**Ctrl+A:** Select all  
**Ctrl+Z:** Undo  
**Ctrl+Y:** Redo
 
_Snapping and Axis Locking:_  
**Z:** Toggle time snapping  
**X:** Toggle value snapping  
**Alt (Hold):** Disable snap  
**Shift (Hold):** Lock keyframe movement axis  
  
_Keyframe Manipulation:_  
**1:** Cubic interpolation - Auto tangents  
**2:** Cubic interpolation - Mirrored tangents  
**3:** Cubic interpolation - Split tangents  
**4:** Linear interpolation  
**5:** Stepped/Constant interpolation  
**6:** Flatten tangents

## Performance

At the time of publishing, the stock Curve asset uses an **O(n log n)** time complexity method to find keyframes for a time (as each ImmutableList index lookup takes **O(log n)** time, and this is used inside a loop iterating over each keyframe). 

AltCurve uses a single Binary Search to find the correct time, combined with constant time array usage this means that we run in **O(log n)** time.

Benchmarking shows AltCurve evaluations run at least 85% quicker than a Curve with 2 keyframes, and scales signficiantly better (95% quicker with 50 keyframes).

```
BenchmarkDotNet v0.14.0, Windows 11 (10.0.22631.4169/23H2/2023Update/SunValley3)
Intel Core i9-9900KF CPU 3.60GHz (Coffee Lake), 1 CPU, 16 logical and 8 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
```
| Method   | Keys | Samples | Mean          | Error       | StdDev      | Ratio |
|--------- |----- |-------- |--------------:|------------:|------------:|------:|
| **Curve**    | **1**    | **1**       |     **52.944 ns** |   **0.1480 ns** |   **0.1155 ns** |  **1.00** |
| AltCurve | 1    | 1       |      3.296 ns |   0.0224 ns |   0.0187 ns |  0.06 |
|          |      |         |               |             |             |       |
| **Curve**    | **2**    | **1**       |    **160.299 ns** |   **0.8037 ns** |   **0.7125 ns** |  **1.00** |
| AltCurve | 2    | 1       |     22.889 ns |   0.4456 ns |   0.4768 ns |  0.14 |
|          |      |         |               |             |             |       |
| **Curve**    | **5**    | **1**       |    **182.520 ns** |   **0.3205 ns** |   **0.2676 ns** |  **1.00** |
| AltCurve | 5    | 1       |     25.032 ns |   0.0982 ns |   0.0919 ns |  0.14 |
|          |      |         |               |             |             |       |
| **Curve**    | **25**   | **1**       |    **298.083 ns** |   **2.3001 ns** |   **1.7958 ns** |  **1.00** |
| AltCurve | 25   | 1       |     26.123 ns |   0.1148 ns |   0.0959 ns |  0.09 |
|          |      |         |               |             |             |       |
| **Curve**    | **50**   | **1**       |    **555.187 ns** |   **6.8444 ns** |   **6.0674 ns** |  **1.00** |
| AltCurve | 50   | 1       |     29.335 ns |   0.2120 ns |   0.1880 ns |  0.05 |
|          |      |         |               |             |             |       |
| **Curve**    | **100**  | **1**       |    **625.495 ns** |   **5.5010 ns** |   **4.5936 ns** |  **1.00** |
| AltCurve | 100  | 1       |     30.110 ns |   0.2265 ns |   0.1768 ns |  0.05 |
|          |      |         |               |             |             |       |
| **Curve**    | **1000** | **1**       | **31,306.718 ns** | **155.2554 ns** | **145.2260 ns** | **1.000** |
| AltCurve | 1000 | 1       |     39.458 ns |   0.6483 ns |   1.0283 ns | 0.001 |
