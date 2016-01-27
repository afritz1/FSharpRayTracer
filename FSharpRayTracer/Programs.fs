namespace FSharpRayTracer

module Programs =
    open System
    open System.Collections
    open System.Diagnostics
    open System.Drawing
    open System.Threading.Tasks

    open Cameras
    open Rays
    open Vectors
    open Worlds

    let GenerateRay (camera : Camera, xPercent : float, yPercent : float) =
        let topLeft = camera.Forward + camera.Up - camera.Right
        let rightComp = camera.Right.ScaledBy(2.0 * xPercent)
        let upComp = camera.Up.ScaledBy(2.0 * yPercent)
        Ray(camera.Eye, (topLeft + rightComp - upComp).Normalized(), Ray.DefaultDepth())

    let VecToColor (v : Vector3) =
        let r = int(v.X * 255.0)
        let g = int(v.Y * 255.0)
        let b = int(v.Z * 255.0)
        Color.FromArgb(r, g, b)
        
    let Run (width : int, height : int, superSamples : int, shapeCount : int, lightCount : int, lightSamples : int, ambientSamples : int, filename : string) =
        let eye = Vector3(6.0, 3.0, 12.0)
        let focus = Vector3(0.0, 0.0, 0.0)
        let aspect = float(width) / float(height)        
        let zoom = 1.75
        let camera = Camera.LookAt(eye, focus, aspect, zoom)

        let world = World(shapeCount, lightCount, lightSamples, ambientSamples)

        let widthRecip = 1.0 / float(width)
        let heightRecip = 1.0 / float(height)
        let superSamplesRecip = 1.0 / float(superSamples)
        let superSamplesSquaredRecip = 1.0 / float(superSamples * superSamples)
        
        let obj = Object()
        let image = new Bitmap(width, height)
        let result = Parallel.For(0, height, fun y -> 
            for x in 0 .. (width - 1) do
                let left = float(x) - 0.5
                let right = left + 1.0
                let top = float(y) - 0.5
                let bottom = top + 1.0
                let radius = ((right + bottom - left - top) * 0.25) * superSamplesRecip
                let radiusDoubled = radius * 2.0
                let mutable i = left + radius
                let mutable color = Vector3.Zero()
                while (i < right) do
                    let mutable j = top + radius
                    while (j < bottom) do
                        let ray = GenerateRay(camera, i * widthRecip, j * heightRecip)
                        color <- color + world.TraceRay(ray).Clamped()
                        j <- j + radiusDoubled
                    i <- i + radiusDoubled
                let totalColor = color.ScaledBy(superSamplesSquaredRecip)
                lock obj (fun () -> image.SetPixel(x, y, VecToColor(totalColor))))
        image.Save(filename + ".png", Imaging.ImageFormat.Png)
        image.Dispose()

    // Ask for ray tracing parameters.
    Console.Write("Output width (i.e., 1-3840): ");
    let width = Int32.Parse(Console.ReadLine())

    Console.Write("Output height (i.e., 1-2160): ");
    let height = Int32.Parse(Console.ReadLine())

    Console.Write("Super samples (i.e., 1-4): ")
    let superSamples = Int32.Parse(Console.ReadLine())

    Console.Write("Shape count (i.e., 0-100): ")
    let shapeCount = Int32.Parse(Console.ReadLine())

    Console.Write("Light count (i.e., 0-5): ")
    let lightCount = Int32.Parse(Console.ReadLine())

    Console.Write("Lighting samples (i.e., 1-1024): ")
    let lightSamples = Int32.Parse(Console.ReadLine())

    Console.Write("Indirect shadow samples (i.e., 1-1024): ")
    let ambientSamples = Int32.Parse(Console.ReadLine())

    Console.Write("Output filename (i.e., \"output\"): ");
    let filename = Console.ReadLine()

    // Ray trace and output the finished image to file.
    let stopwatch = Stopwatch.StartNew()
    Run(width, height, superSamples, shapeCount, lightCount, lightSamples, ambientSamples, filename)
    stopwatch.Stop()

    // Tell that the ray tracer is finished.
    let totalSeconds = stopwatch.Elapsed.TotalSeconds
    Console.Write("Done! Took " + totalSeconds.ToString("n2") + "s.");
    ignore(Console.ReadLine())