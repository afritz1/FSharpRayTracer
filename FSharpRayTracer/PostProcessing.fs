namespace FSharpRayTracer

module PostProcessing =
    open System
    open System.Drawing

    open Vectors

    // Rough draft (and naive) Gaussian blur.
    let GaussianBlur (input : Vector3[], width : int, height : int) =
        let output : Vector3[] = Array.zeroCreate input.Length

        // Generate weight matrix (this 1D helper should be symmetrical).
        let weights1d = [| 1.0; 3.0; 8.0; 11.0; 8.0; 3.0; 1.0; |]
        let weights = Array.init (weights1d.Length * weights1d.Length) (fun i -> 0.0)
        for y in 0 .. (weights1d.Length - 1) do
            for x in 0 .. (weights1d.Length - 1) do
                let index = x + (y * weights1d.Length)
                weights.[index] <- weights1d.[x] * weights1d.[y]

        let totalWeight = Array.sum(weights)

        // To do: separate into horizontal and vertical blurs.
        for y in 0 .. (height - 1) do
            for x in 0 .. (width - 1) do
                let mutable color = Vector3.Zero()

                // Distance to walk from the current pixel (truncated).
                let stepDist = weights1d.Length / 2

                for j in -stepDist .. stepDist do
                    for i in -stepDist .. stepDist do
                        let xi = Math.Min(Math.Max(x + i, 0), width - 1)
                        let yj = Math.Min(Math.Max(y + j, 0), height - 1)
                        let index = xi + (yj * width)
                        let weightIndex = (i + stepDist) + ((j + stepDist) * weights1d.Length)
                        color <- color + (input.[index] * (weights.[weightIndex] / totalWeight))

                // Assign the blurred pixel.
                let index = x + (y * width)
                output.[index] <- color
        output

    // Rough draft bloom post-processing. It doesn't look very good yet.
    let Bloom (input : Vector3[], width : int, height : int) =
        // Make a copy of the input buffer.
        let mutable temp = Array.init input.Length (fun i -> input.[i])
        
        // Filter out dark pixels.
        for i in 0 .. (temp.Length - 1) do
            let pixel = temp.[i]
            let brightEnough = (pixel.X > 1.0) || (pixel.Y > 1.0) || (pixel.Z > 1.0)
            if (not brightEnough) then
                temp.[i] <- Vector3.Zero()

        // Blur.
        temp <- GaussianBlur(temp, width, height)

        // Combine input and temp into output.
        let output : Vector3[] = Array.zeroCreate input.Length
        for i in 0 .. (output.Length - 1) do
            let inputColor = input.[i]
            let tempColor = temp.[i]
            output.[i] <- inputColor + tempColor
        output

    // Simple tone mapping function.
    let ToneMap (input : Vector3[], blurred : Vector3[]) =
        let output : Vector3[] = Array.zeroCreate input.Length
        
        let exposure = 2.20
        let gammaCorrection = 0.55

        for i in 0 .. (output.Length - 1) do
            let mutable blendedColor = input.[i].Lerp(blurred.[i], 0.40)
            blendedColor <- blendedColor * exposure
            blendedColor <- new Vector3(
                Math.Pow(blendedColor.X, gammaCorrection),
                Math.Pow(blendedColor.Y, gammaCorrection),
                Math.Pow(blendedColor.Z, gammaCorrection))
            output.[i] <- blendedColor
        output

    // Clamp RGB values between 0.0-1.0 for later conversion to 0-255 format.
    let Clamp (input : Vector3[]) =
        let output : Vector3[] = Array.zeroCreate input.Length
        for i in 0 .. (output.Length - 1) do
            output.[i] <- input.[i].Clamped()
        output

    // Creates a bitmap from a buffer of 0.0-1.0 RGB color values.
    let ToBitmap (input : Vector3[], width : int, height : int) =
        let bitmap = new Bitmap(width, height)
        for y in 0 .. (height - 1) do
            for x in 0 .. (width - 1) do
                let index = x + (y * width)
                bitmap.SetPixel(x, y, input.[index].ToColor())
        bitmap