namespace FSharpRayTracer

// Inlining applicable functions helps with performance by about 15%.
// It probably cuts down on the number of parameters being copied.

module Vectors =
    open System
    open System.Threading

    let EPSILON = 1.0e-6

    type Axis =
        | X
        | Y
        | Z

    let private random = new ThreadLocal<Random>(fun () -> Random())

    type Vector3 (x : float, y : float, z : float) =
        member this.X = x
        member this.Y = y
        member this.Z = z

        static member private GetRand () = random.Value.NextDouble()

        static member inline Zero () = Vector3(0.0, 0.0, 0.0)
        static member inline UnitX () = Vector3(1.0, 0.0, 0.0)
        static member inline UnitY () = Vector3(0.0, 1.0, 0.0)
        static member inline UnitZ () = Vector3(0.0, 0.0, 1.0)
        
        static member inline ( + ) (v1 : Vector3, v2 : Vector3) = 
            Vector3(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z)

        static member inline ( - ) (v1 : Vector3, v2 : Vector3) = 
            Vector3(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z)

        override this.ToString () = 
            let x = this.X
            let y = this.Y
            let z = this.Z
            "[" + x.ToString() + " " + y.ToString() + " " + z.ToString() + "]"

        member inline this.Dot (v : Vector3) = 
            (this.X * v.X) + (this.Y * v.Y) + (this.Z * v.Z)

        member inline this.Cross (v : Vector3) = 
            let x = (this.Y * v.Z) - (v.Y * this.Z)
            let y = (v.X * this.Z) - (this.X * v.Z)
            let z = (this.X * v.Y) - (v.X * this.Y)
            Vector3(x, y, z)

        member inline this.LengthSquared () = 
            (this.X * this.X) + (this.Y * this.Y) + (this.Z * this.Z)

        member inline this.Length () = 
            Math.Sqrt(this.LengthSquared())
        
        member inline this.Normalized () =
            let lenRecip = 1.0 / this.Length()
            Vector3(this.X * lenRecip, this.Y * lenRecip, this.Z * lenRecip)

        member inline this.Negated () = 
            Vector3(-this.X, -this.Y, -this.Z)

        member inline this.ScaledBy (m : float) = 
            Vector3(this.X * m, this.Y * m, this.Z * m)

        member inline this.ScaledBy (v : Vector3) = 
            Vector3(this.X * v.X, this.Y * v.Y, this.Z * v.Z)

        member inline this.Reflected (normal : Vector3) =
            let vnDot = this.Dot(normal)
            let vnSign = (if (vnDot > 0.0) then 1.0 else (if (vnDot < 0.0) then -1.0 else 0.0))
            normal.ScaledBy(vnSign * (2.0 * vnDot)) - this

        member inline this.Lerp (v : Vector3, percent : float) =
            this.ScaledBy(1.0 - percent) + v.ScaledBy(percent)

        member inline this.Slerp (v : Vector3, percent : float) =
            let theta = Math.Acos(this.Dot(v) / (this.Length() * v.Length()))
            let sinTheta = Math.Sin(theta)
            let percentTheta = percent * theta
            this.ScaledBy(Math.Sin(theta - percentTheta) / sinTheta) +
                v.ScaledBy(Math.Sin(percentTheta) / sinTheta)

        member inline this.Clamped () = 
            let low = 0.0
            let high = 1.0
            let x = (if (this.X > high) then high else (if (this.X < low) then low else this.X))
            let y = (if (this.Y > high) then high else (if (this.Y < low) then low else this.Y))
            let z = (if (this.Z > high) then high else (if (this.Z < low) then low else this.Z))
            Vector3(x, y, z)

        member inline this.ComponentMin (v : Vector3) = 
            let x = (if (this.X < v.X) then this.X else v.X)
            let y = (if (this.Y < v.Y) then this.Y else v.Y)
            let z = (if (this.Z < v.Z) then this.Z else v.Z)
            Vector3(x, y, z)

        member inline this.ComponentMax (v : Vector3) = 
            let x = (if (this.X > v.X) then this.X else v.X)
            let y = (if (this.Y > v.Y) then this.Y else v.Y)
            let z = (if (this.Z > v.Z) then this.Z else v.Z)
            Vector3(x, y, z)

        static member RandomColor () = 
            Vector3(Vector3.GetRand(), Vector3.GetRand(), Vector3.GetRand())

        static member RandomPointInSphere (radius : float) =
            let randX = (2.0 * Vector3.GetRand()) - 1.0
            let randY = (2.0 * Vector3.GetRand()) - 1.0
            let randZ = (2.0 * Vector3.GetRand()) - 1.0
            let randPoint = Vector3(randX, randY, randZ)
            randPoint.Normalized().ScaledBy(radius * Vector3.GetRand())

        static member RandomPointInCuboid (width : float, height : float, depth : float) =
            let randX = Vector3.GetRand() - 0.50
            let randY = Vector3.GetRand() - 0.50
            let randZ = Vector3.GetRand() - 0.50
            let randPoint = Vector3(width * randX, height * randY, depth * randZ)
            randPoint

        static member RandomDirectionInHemisphere (normal : Vector3) =
            let randX = (2.0 * Vector3.GetRand()) - 1.0
            let randY = (2.0 * Vector3.GetRand()) - 1.0
            let randZ = (2.0 * Vector3.GetRand()) - 1.0
            let randDir = Vector3(randX, randY, randZ).Normalized()
            (if (randDir.Dot(normal) >= 0.0) then randDir else randDir.Negated())