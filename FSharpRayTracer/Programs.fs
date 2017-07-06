namespace FSharpRayTracer

module Programs =
    open System
    open System.Collections
    open System.Diagnostics
    open System.Drawing
    open System.Threading
    open System.Threading.Tasks

    open Cameras
    open PostProcessing
    open Rays
    open Vectors
    open Worlds

    [<NoComparison>] // (Silences the warning about Vector3 equality.)
    type InputSettings = {
        eye : Vector3
        focus : Vector3
        fovY : float
        width : int
        height : int
        superSamples : int
        shapeCount : int
        lightCount : int
        lightSamples : int
        ambientSamples : int
        dofSamples : int
        apertureRadius : float
        focalDistance : float
        filename : string
    }
    
    let Run (settings : InputSettings) =
        let aspect = float(settings.width) / float(settings.height)
        let camera = Camera.LookAt(settings.eye, settings.focus, settings.fovY, aspect)

        let world = World(settings.shapeCount, settings.lightCount, settings.lightSamples, settings.ambientSamples)

        let widthRecip = 1.0 / float(settings.width)
        let heightRecip = 1.0 / float(settings.height)
        let superSamplesRecip = 1.0 / float(settings.superSamples)
        let superSamplesSquaredRecip = 1.0 / float(settings.superSamples * settings.superSamples)
        let dofSamplesRecip = 1.0 / float(settings.dofSamples)
        
        // Random number generator for depth-of-field points on aperture.
        let random = new ThreadLocal<Random>(fun () -> Random())
        
        let pixelBuffer = Array.init (settings.width * settings.height) (fun i -> Vector3.Zero())
        let result = Parallel.For(0, settings.height, fun y -> 
            for x in 0 .. (settings.width - 1) do
                let mutable color = Vector3.Zero()
                for j in 0 .. (settings.superSamples - 1) do
                    let jj = float(j) * superSamplesRecip
                    let yy = (float(y) + jj) * heightRecip
                    for i in 0 .. (settings.superSamples - 1) do
                        let ii = float(i) * superSamplesRecip
                        let xx = (float(x) + ii) * widthRecip

                        let direction = camera.GenerateDirection(xx, yy)
                        let focalPoint = camera.Eye + (direction * settings.focalDistance)

                        // Depth-of-field frame for random point calculation on aperture.
                        let dofRight = direction.Cross(Vector3.UnitY()).Normalized()
                        let dofUp = dofRight.Cross(direction).Normalized()

                        for n in 1 .. settings.dofSamples do
                            let dofRightComp = dofRight * ((2.0 * random.Value.NextDouble()) - 1.0)
                            let dofUpComp = dofUp * ((2.0 * random.Value.NextDouble()) - 1.0)
                            let apertureMultiplier = settings.apertureRadius * random.Value.NextDouble()
                            let apertureOffset = (dofRightComp + dofUpComp).Normalized() * apertureMultiplier
                            let apertureEye = camera.Eye + apertureOffset
                            let dofDirection = (focalPoint - apertureEye).Normalized()
                            let ray = Ray(apertureEye, dofDirection, Ray.DefaultDepth())
                            color <- color + world.TraceRay(ray)
                let finalColor = color * (superSamplesSquaredRecip * dofSamplesRecip)
                pixelBuffer.[x + (y * settings.width)] <- finalColor)

        // Post-processing (other effects like bloom could be added once they look better).
        let postImage = PostProcessing.Clamp(pixelBuffer)

        // Convert to bitmap for PNG saving.
        let bitmap = PostProcessing.ToBitmap(postImage, settings.width, settings.height)
        bitmap.Save(settings.filename + ".png", Imaging.ImageFormat.Png)
        bitmap.Dispose()

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

    // Group up the settings and give them to the renderer.
    let eye = Vector3(6.0, 3.0, 12.0)
    let focus = Vector3(0.0, 0.0, 0.0)
    let fovY = 60.0
    let settings = {
        eye = eye;
        focus = focus;
        fovY = fovY;
        width = width;
        height = height;
        superSamples = superSamples;
        shapeCount = shapeCount;
        lightCount = lightCount;
        lightSamples = lightSamples;
        ambientSamples = ambientSamples;
        dofSamples = dofSamples; 
        apertureRadius = apertureRadius; 
        focalDistance = focalDistance; 
        filename = filename
    }

    // Ray trace and output the finished image to file.
    let stopwatch = Stopwatch.StartNew()
    Run(settings)
    stopwatch.Stop()

    // Tell that the ray tracer is finished.
    Console.Write("Done! Took " + stopwatch.Elapsed.TotalSeconds.ToString("n2") + "s.");
    ignore(Console.ReadLine())