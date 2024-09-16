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

Benchmarking shows AltCurve evaluations run at least 85% quicker than a Curve with 2 keyframes, and scales signficiantly better.

```
BenchmarkDotNet v0.14.0, Windows 11 (10.0.22631.4169/23H2/2023Update/SunValley3)
Intel Core i9-9900KF CPU 3.60GHz (Coffee Lake), 1 CPU, 16 logical and 8 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
```
| Method   | Keys | Mean          | Error      | StdDev     | Ratio |
|--------- |----- |--------------:|-----------:|-----------:|------:|
| **Curve**    | **1**    |     **55.156 ns** |  **0.0776 ns** |  **0.0688 ns** |  **1.00** |
| AltCurve | 1    |      3.384 ns |  0.0713 ns |  0.0632 ns |  0.06 |
|          |      |               |            |            |       |
| **Curve**    | **2**    |    **109.973 ns** |  **0.1830 ns** |  **0.1528 ns** |  **1.00** |
| AltCurve | 2    |     15.369 ns |  0.1992 ns |  0.1863 ns |  0.14 |
|          |      |               |            |            |       |
| **Curve**    | **5**    |    **170.725 ns** |  **0.3472 ns** |  **0.3248 ns** |  **1.00** |
| AltCurve | 5    |     16.564 ns |  0.0287 ns |  0.0239 ns |  0.10 |
|          |      |               |            |            |       |
| **Curve**    | **25**   |    **273.834 ns** |  **0.2214 ns** |  **0.1729 ns** |  **1.00** |
| AltCurve | 25   |     18.720 ns |  0.0782 ns |  0.0653 ns |  0.07 |
|          |      |               |            |            |       |
| **Curve**    | **50**   |    **221.819 ns** |  **1.1613 ns** |  **0.9697 ns** |  **1.00** |
| AltCurve | 50   |     20.825 ns |  0.0494 ns |  0.0413 ns |  0.09 |
|          |      |               |            |            |       |
| **Curve**    | **100**  |    **289.034 ns** |  **2.9633 ns** |  **2.7719 ns** |  **1.00** |
| AltCurve | 100  |     23.611 ns |  0.1488 ns |  0.1392 ns |  0.08 |
|          |      |               |            |            |       |
| **Curve**    | **1000** | **12,965.685 ns** | **46.4060 ns** | **41.1378 ns** | **1.000** |
| AltCurve | 1000 |     27.605 ns |  0.0890 ns |  0.0743 ns | 0.002 |
