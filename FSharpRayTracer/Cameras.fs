namespace FSharpRayTracer

module Cameras =
    open Vectors

    type Camera (eye : Vector3, forward : Vector3, right : Vector3, up : Vector3) =
        member this.Eye = eye
        member this.Forward = forward
        member this.Right = right
        member this.Up = up

        static member GlobalUp () = Vector3.UnitY()

        static member LookAt (eye : Vector3, focus : Vector3, aspect : float, zoom : float) =
            let forward = (focus - eye).Normalized().ScaledBy(zoom)
            let right = forward.Cross(Camera.GlobalUp()).Normalized().ScaledBy(aspect)
            let up = right.Cross(forward).Normalized()
            Camera(eye, forward, right, up)