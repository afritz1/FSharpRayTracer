namespace FSharpRayTracer

module Shapes =
    open System

    open Intersections
    open Materials
    open Rays
    open Vectors

    type BoundingBox (min : Vector3, max : Vector3) =
        let extent = max - min

        member this.Min = min
        member this.Max = max
        member this.Extent = extent

        member this.Centroid () = min + (extent * 0.5)

        member this.LongestAxis () =
            if (extent.X > extent.Y) && (extent.X > extent.Z) then 
                Axis.X
            else if (extent.Y > extent.Z) then
                Axis.Y
            else
                Axis.Z

        member this.ExpandToInclude (point : Vector3) =
            BoundingBox(min.ComponentMin(point), max.ComponentMax(point))

        member this.ExpandToInclude (box : BoundingBox) =
            BoundingBox(min.ComponentMin(box.Min), max.ComponentMax(box.Max))

        member this.Intersect (ray : Ray) = 
            let point = this.Centroid()
            let width = max.X - min.X
            let height = max.Y - min.Y
            let depth = max.Z - min.Z

            let mutable tMin = 0.0
            let mutable tMax = 0.0

            let tX1 = (point.X - width - ray.Point.X) / ray.Direction.X
            let tX2 = (point.X + width - ray.Point.X) / ray.Direction.X

            let tY1 = (point.Y - height - ray.Point.Y) / ray.Direction.Y
            let tY2 = (point.Y + height - ray.Point.Y) / ray.Direction.Y

            let tZ1 = (point.Z - depth - ray.Point.Z) / ray.Direction.Z
            let tZ2 = (point.Z + depth - ray.Point.Z) / ray.Direction.Z

            if (tX1 < tX2) then
                tMin <- tX1
                tMax <- tX2
            else
                tMin <- tX2
                tMax <- tX1

            if (tY1 < tY2) then
                if (tY1 > tMin) then
                    tMin <- tY1
                if (tY2 < tMax) then
                    tMax <- tY2
            else
                if (tY2 > tMin) then
                    tMin <- tY2
                if (tY1 < tMax) then
                    tMax <- tY1

            if (tZ1 < tZ2) then
                if (tZ1 > tMin) then
                    tMin <- tZ1
                if (tZ2 < tMax) then
                    tMax <- tZ2
            else
                if (tZ2 > tMin) then
                    tMin <- tZ2
                if (tZ1 < tMax) then
                    tMax <- tZ1

            let tNear = (if (tMin < tMax) then tMin else tMax)
            let tFar = (if (tMax > tNear) then tMax else tNear)
            (tMax >= tMin, tNear, tFar)
    
    [<AbstractClass>]
    type Shape () =
        abstract member Centroid : unit -> Vector3
        abstract member BoundingBox : unit -> BoundingBox
        abstract member Intersect : Ray -> Intersection

    type Sphere (point : Vector3, radius : float, material : Material) =
        inherit Shape()

        let radiusSquared = radius * radius
        let radiusRecip = 1.0 / radius

        override this.Centroid () = point

        override this.BoundingBox () =
            let radVec = Vector3(radius, radius, radius)
            BoundingBox(point - radVec, point + radVec)

        override this.Intersect (ray : Ray) =
            let diff = point - ray.Point
            let b = diff.Dot(ray.Direction)
            let determinant = (b * b) - diff.Dot(diff) + radiusSquared
            if (determinant < 0.0) then
                Intersection()
            else
                let detSqrt = Math.Sqrt(determinant)
                let b1 = b - detSqrt
                let b2 = b + detSqrt
                let t = 
                    if (b1 > EPSILON) then b1 
                    elif (b2 > EPSILON) then b2 
                    else Intersection.TMax()
                let point = ray.PointAt(t)
                let normal = (point - this.Centroid()) * radiusRecip
                Intersection(t, Some(point), Some(normal), Some(material))

    type Cuboid (point : Vector3, width : float, height : float, depth : float, material : Material) = 
        inherit Shape()

        let point = point
        let width = width
        let height = height
        let depth = depth
        let material = material

        override this.Centroid () = point

        override this.BoundingBox () =
            let halfDiagonal = Vector3(width * 0.5, height * 0.5, depth * 0.5)
            BoundingBox(point - halfDiagonal, point + halfDiagonal)

        override this.Intersect (ray : Ray) =
            let point = this.Centroid()
            
            // I'm not sure how to do this in a functional way yet. Maybe roll all 
            // of the if statements' conditions into some booleans and then assign 
            // to tMin and tMax through ternary operators once.
            // I.e., instead of comparing tY1 to tMin, compare it to tX1 or tX2?

            let mutable tMin = 0.0
            let mutable tMax = 0.0

            let mutable nMinX = 0.0
            let mutable nMinY = 0.0
            let mutable nMinZ = 0.0

            let mutable nMaxX = 0.0
            let mutable nMaxY = 0.0
            let mutable nMaxZ = 0.0

            let tX1 = (point.X - width - ray.Point.X) / ray.Direction.X
            let tX2 = (point.X + width - ray.Point.X) / ray.Direction.X

            let tY1 = (point.Y - height - ray.Point.Y) / ray.Direction.Y
            let tY2 = (point.Y + height - ray.Point.Y) / ray.Direction.Y

            let tZ1 = (point.Z - depth - ray.Point.Z) / ray.Direction.Z
            let tZ2 = (point.Z + depth - ray.Point.Z) / ray.Direction.Z

            if (tX1 < tX2) then
                tMin <- tX1
                tMax <- tX2
                nMinX <- (-width)
                nMaxX <- width
            else
                tMin <- tX2
                tMax <- tX1
                nMinX <- width
                nMaxX <- (-width)

            if (tY1 < tY2) then
                if (tY1 > tMin) then
                    tMin <- tY1
                    nMinX <- 0.0
                    nMinY <- (-height)
                if (tY2 < tMax) then
                    tMax <- tY2
                    nMaxX <- 0.0
                    nMaxY <- height
            else
                if (tY2 > tMin) then
                    tMin <- tY2
                    nMinX <- 0.0
                    nMinY <- height
                if (tY1 < tMax) then
                    tMax <- tY1
                    nMaxX <- 0.0
                    nMaxY <- (-height)

            if (tZ1 < tZ2) then
                if (tZ1 > tMin) then
                    tMin <- tZ1
                    nMinX <- 0.0
                    nMinY <- 0.0
                    nMinZ <- (-depth)
                if (tZ2 < tMax) then
                    tMax <- tZ2
                    nMaxX <- 0.0
                    nMaxY <- 0.0
                    nMaxZ <- depth
            else
                if (tZ2 > tMin) then
                    tMin <- tZ2
                    nMinX <- 0.0
                    nMinY <- 0.0
                    nMinZ <- depth
                if (tZ1 < tMax) then
                    tMax <- tZ1
                    nMaxX <- 0.0
                    nMaxY <- 0.0
                    nMaxZ <- (-depth)

            if ((tMax < 0.0) || (tMin > tMax)) then
                Intersection()
            else
                if (tMin < 0.0) then
                    tMin <- tMax
                    nMinX <- nMaxX
                    nMinY <- nMaxY
                    nMinZ <- nMaxZ
                let t = tMin
                let point = ray.PointAt(t)
                let normal = Vector3(nMinX, nMinY, nMinZ).Normalized()
                Intersection(t, Some(point), Some(normal), Some(material))