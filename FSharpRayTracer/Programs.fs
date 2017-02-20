namespace FSharpRayTracer

module Programs =
    open System
    open System.Collections
    open System.Diagnostics
    open System.Drawing
    open System.Threading
    open System.Threading.Tasks

    open Cameras
    open Rays
    open Vectors
    open Worlds
        
    let Run (width : int, height : int, superSamples : int, shapeCount : int, lightCount : int, lightSamples : int, ambientSamples : int, dofSamples : int, apertureRadius : float, focalDistance : float, filename : string) =
        let eye = Vector3(6.0, 3.0, 12.0)
        let focus = Vector3(0.0, 0.0, 0.0)
        let fovY = 60.0
        let aspect = float(width) / float(height)
        let camera = Camera.LookAt(eye, focus, fovY, aspect)

        let world = World(shapeCount, lightCount, lightSamples, ambientSamples)

        // Axes normalized for random point calculation on aperture.
        let dofRight = camera.Right.Normalized()
        let dofUp = camera.Up.Normalized()

        let widthRecip = 1.0 / float(width)
        let heightRecip = 1.0 / float(height)
        let superSamplesRecip = 1.0 / float(superSamples)
        let superSamplesSquaredRecip = 1.0 / float(superSamples * superSamples)
        let dofSamplesRecip = 1.0 / float(dofSamples)
        
        // Random number generator for depth-of-field points on aperture.
        let random = new ThreadLocal<Random>(fun () -> Random())
        
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
                        let focalPoint = camera.Eye + camera.GenerateDirection(xx, yy).ScaledBy(focalDistance)
                        for n in 1 .. dofSamples do
                            let dofRightComp = dofRight.ScaledBy((2.0 * random.Value.NextDouble()) - 1.0)
                            let dofUpComp = dofUp.ScaledBy((2.0 * random.Value.NextDouble()) - 1.0)
                            let apertureMultiplier = apertureRadius * random.Value.NextDouble()
                            let apertureOffset = (dofRightComp + dofUpComp).Normalized().ScaledBy(apertureMultiplier)
                            let apertureEye = camera.Eye + apertureOffset
                            let direction = (focalPoint - apertureEye).Normalized()
                            let ray = Ray(apertureEye, direction, Ray.DefaultDepth())
                            color <- color + world.TraceRay(ray)
                let finalColor = color.ScaledBy(superSamplesSquaredRecip * dofSamplesRecip).Clamped()
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

    Console.Write("Depth of field samples (i.e., 1-128): ")
    let dofSamples = Int32.Parse(Console.ReadLine())

    Console.Write("Aperture radius (i.e., 0.0-1.0): ")
    let apertureRadius = Double.Parse(Console.ReadLine())

    Console.Write("Focal distance (i.e., 1.0-1000.0): ")
    let focalDistance = Double.Parse(Console.ReadLine())

    Console.Write("Output filename (i.e., \"output\"): ");
    let filename = Console.ReadLine()

    // Ray trace and output the finished image to file.
    let stopwatch = Stopwatch.StartNew()
    Run(width, height, superSamples, shapeCount, lightCount, lightSamples, ambientSamples, dofSamples, apertureRadius, focalDistance, filename)
    stopwatch.Stop()

    // Tell that the ray tracer is finished.
    Console.Write("Done! Took " + stopwatch.Elapsed.TotalSeconds.ToString("n2") + "s.");
    ignore(Console.ReadLine())