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
        
    let Run (width : int, height : int, superSamples : int, shapeCount : int, lightCount : int, lightSamples : int, ambientSamples : int, filename : string) =
        let eye = Vector3(6.0, 3.0, 12.0)
        let focus = Vector3(0.0, 0.0, 0.0)
        let fovY = 60.0
        let aspect = float(width) / float(height)
        let camera = Camera.LookAt(eye, focus, fovY, aspect)

        let world = World(shapeCount, lightCount, lightSamples, ambientSamples)

        let widthRecip = 1.0 / float(width)
        let heightRecip = 1.0 / float(height)
        let superSamplesRecip = 1.0 / float(superSamples)
        let superSamplesSquaredRecip = 1.0 / float(superSamples * superSamples)
        
        let obj = Object()
        let image = new Bitmap(width, height)
        let result = Parallel.For(0, height, fun y -> 
            for x in 0 .. (width - 1) do
                let mutable color = Vector3.Zero()
                for j in 0 .. (superSamples - 1) do
                    let jj = float(j) * superSamplesRecip
                    let yy = (float(y) + jj) * heightRecip
                    for i in 0 .. (superSamples - 1) do
                        let ii = float(i) * superSamplesRecip
                        let xx = (float(x) + ii) * widthRecip
                        let direction = camera.GenerateDirection(xx, yy)
                        let ray = Ray(camera.Eye, direction, Ray.DefaultDepth())
                        color <- color + world.TraceRay(ray)
                let finalColor = color.ScaledBy(superSamplesSquaredRecip).Clamped()
                lock obj (fun () -> image.SetPixel(x, y, finalColor.ToColor())))
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
    Console.Write("Done! Took " + stopwatch.Elapsed.TotalSeconds.ToString("n2") + "s.");
    ignore(Console.ReadLine())