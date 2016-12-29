namespace FSharpRayTracer

module Rays =
    open Vectors

    type Ray (point : Vector3, direction : Vector3, depth : int) =
        member this.Point = point
        member this.Direction = direction
        member this.Depth = depth

        static member DefaultDepth () = 0
        static member MaxDepth () = 4

        member this.PointAt (distance : float) = 
            point + direction.ScaledBy(distance)