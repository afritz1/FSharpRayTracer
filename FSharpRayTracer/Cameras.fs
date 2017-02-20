namespace FSharpRayTracer

module Cameras =
    open System

    open Vectors

    type Camera (eye : Vector3, forward : Vector3, right : Vector3, up : Vector3) =
        member this.Eye = eye
        member this.Forward = forward
        member this.Right = right
        member this.Up = up

        static member GlobalUp () = Vector3.UnitY()

        static member LookAt (eye : Vector3, focus : Vector3, fovY : float, aspect : float) =
            let zoom = 1.0 / Math.Tan((fovY * 0.5) * (Math.PI / 180.0))
            let forward = (focus - eye).Normalized().ScaledBy(zoom)
            let right = forward.Cross(Camera.GlobalUp()).Normalized().ScaledBy(aspect)
            let up = right.Cross(forward).Normalized()
            Camera(eye, forward, right, up)

        member inline this.GenerateDirection (xPercent : float, yPercent : float) =
            let topLeft = this.Forward + this.Up - this.Right
            let rightComp = this.Right.ScaledBy(2.0 * xPercent)
            let upComp = this.Up.ScaledBy(2.0 * yPercent)
            (topLeft + rightComp - upComp).Normalized()