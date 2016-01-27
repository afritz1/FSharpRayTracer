namespace FSharpRayTracer

module Intersections =
    open Materials
    open Vectors

    type Intersection (t : float, point : Vector3, normal : Vector3, material : Material) =
        let t = t
        let point = point
        let normal = normal
        let material = material

        member this.T = t
        member this.Point = point
        member this.Normal = normal
        member this.Material = material

        static member TMax () = System.Double.MaxValue

        new () = 
            let zeroVec = Vector3.Zero()
            Intersection(Intersection.TMax(), zeroVec, zeroVec, EmptyMaterial())