namespace FSharpRayTracer

// Using options for the bulk of the intersection data really helps with performance.
// It's better than making dummy values all the time.

module Intersections =
    open Materials
    open Vectors

    type Intersection (t : float, point : Vector3 option, normal : Vector3 option, material : Material option) =
        member this.T = t
        member this.Point = point
        member this.Normal = normal
        member this.Material = material

        static member inline TMax () = System.Double.MaxValue

        new () = Intersection(Intersection.TMax(), None, None, None)