namespace FSharpRayTracer

module Worlds =
    open System
    open System.Collections

    open Intersections
    open Lights
    open Materials
    open Rays
    open Shapes
    open Vectors

    /// Returns an array of random shapes near the origin.
    let private randomShapes (count : int, worldRadius : float) =
        let random = Random()
        let makePoint () = Vector3.RandomPointInSphere(Vector3.Zero(), worldRadius)
        let makeMaterial () = Phong(Vector3.RandomColor())
        let makeDimension () = 0.5 + random.NextDouble()
        let makeSphere () =
            let point = makePoint()
            let radius = makeDimension()
            let material = makeMaterial()
            Sphere(point, radius, material)
        let makeCuboid () =
            let point = makePoint()
            let width = makeDimension()
            let height = makeDimension()
            let depth = makeDimension()
            let material = makeMaterial()
            Cuboid(point, width, height, depth, material)
        let makeShape (choice : int) =
            let shape : Shape = 
                if (choice = 0) then 
                    upcast makeSphere() 
                else 
                    upcast makeCuboid()
            shape
        let shapeArray = [| for i in 1 .. count -> makeShape(random.Next(2)) |]
        shapeArray

    /// Returns an array of random lights near the origin.
    let private randomLights (count : int, worldRadius : float) =
        let random = Random()
        let makePoint () = Vector3.RandomPointInSphere(Vector3.UnitY(), worldRadius)
        let makeDimension () = 0.5 + random.NextDouble()
        let makeSphereLight () =
            let point = makePoint()
            let radius = makeDimension()
            SphereLight(point, radius)
        let makeCuboidLight () =
            let point = makePoint()
            let width = makeDimension()
            let height = makeDimension()
            let depth = makeDimension()
            CuboidLight(point, width, height, depth)
        let makeLight (choice : int) =
            let light : Light =
                if (choice = 0) then 
                    upcast makeSphereLight() 
                else 
                    upcast makeCuboidLight()
            light
        let lightArray = [| for i in 1 .. count -> makeLight(random.Next(2)) |]
        lightArray

    type World (shapeCount : int, lightCount : int, lightSamples : int, ambientSamples : int) =
        let worldRadius = 10.0
        let shapes = randomShapes(shapeCount, worldRadius)
        let lights = randomLights(lightCount, worldRadius)

        member inline private this.NearestShape(ray : Ray) =
            let mutable nearestShape = Intersection()
            for shape in shapes do
                let currentTry = shape.Intersect(ray)
                if (currentTry.T < nearestShape.T) then
                    nearestShape <- currentTry
            nearestShape

        member inline private this.NearestLight(ray : Ray) =
            let mutable nearestLight = Intersection()
            for light in lights do
                let currentTry = light.Intersect(ray)
                if (currentTry.T < nearestLight.T) then
                    nearestLight <- currentTry
            nearestLight

        member inline private this.NearestHit(ray : Ray) =
            let nearestShape = this.NearestShape(ray)
            let nearestLight = this.NearestLight(ray)
            (if (nearestShape.T < nearestLight.T) then nearestShape else nearestLight)

        member inline private this.BackgroundColor(ray : Ray) =
            let horizonColor = Vector3(0.60, 0.80, 1.0)
            let zenithColor = horizonColor.ScaledBy(0.70)
            let elevation = ray.Direction.Y
            let percent = if (elevation < 0.0) then 0.0 else elevation
            horizonColor.ScaledBy(1.0 - percent) + zenithColor.ScaledBy(percent)

        member inline private this.GetAmbientPercent(point : Vector3, normal : Vector3) =
            let maxOccDist = 1.5
            let maxOccDistRecip = 1.0 / maxOccDist
            let normEps = point + normal.ScaledBy(EPSILON)
            let defaultRayDepth = Ray.DefaultDepth()
            let mutable percent = 0.0
            for n in 1 .. ambientSamples do
                let hemisphereDir = Vector3.RandomDirectionInHemisphere(normal)
                let hemisphereRay = Ray(normEps, hemisphereDir, defaultRayDepth)
                let currentTry = this.NearestHit(hemisphereRay)
                let occPercent = currentTry.T * maxOccDistRecip
                percent <- percent + (if (currentTry.T > maxOccDist) then 1.0 else occPercent)
            percent / float(ambientSamples)

        member inline private this.PhongAt(intersection : Intersection, ray : Ray) =
            let ambientComponent (material : Material, point : Vector3, normal : Vector3) =
                let ambientColor = material.Diffuse().ScaledBy(this.BackgroundColor(ray))
                let ambientPercent = this.GetAmbientPercent(point, normal)
                ambientColor.ScaledBy(material.Ambient() * ambientPercent)
            let diffuseComponent (material : Material, point : Vector3, normal : Vector3, view : Vector3) =
                let mutable total = Vector3.Zero()
                let lightSamplesRecip = 1.0 / float(lightSamples)
                let normEps = point + normal.ScaledBy(EPSILON)
                for light in lights do
                    let mutable totalDiffuse = Vector3.Zero()
                    let mutable totalHighlight = Vector3.Zero()
                    let mutable visibleSamples = 0
                    for n in 1 .. lightSamples do
                        let lightDirection = (light.RandomPoint() - point).Normalized()
                        let shadowRay = Ray(normEps, lightDirection, ray.Depth)
                        let lightTry = light.Intersect(shadowRay)
                        let shadowTry = this.NearestShape(shadowRay)
                        if (lightTry.T < shadowTry.T) then
                            visibleSamples <- visibleSamples + 1
                            let lnDot = lightDirection.Dot(normal)
                            let lnReflect = lightDirection.Reflected(normal).Normalized()
                            let lnReflectVDot = lnReflect.Dot(view)
                            let lightColor = lightTry.Material.Value.Diffuse()
                            let shinyAmount = Math.Pow(Math.Max(0.0, lnReflectVDot), material.Shininess())
                            let highlight = lightColor.ScaledBy(material.Specularity() * shinyAmount)
                            let diffuse = material.Diffuse().ScaledBy(lightColor).ScaledBy(Math.Max(0.0, lnDot))
                            totalDiffuse <- totalDiffuse + diffuse
                            totalHighlight <- totalHighlight + (if (lnDot >= 0.0) then highlight else Vector3.Zero())
                    let lightContribution = float(visibleSamples) * lightSamplesRecip
                    totalDiffuse <- totalDiffuse.ScaledBy(lightSamplesRecip)
                    totalHighlight <- totalHighlight.ScaledBy(lightSamplesRecip)
                    total <- total + (totalDiffuse + totalHighlight).ScaledBy(lightContribution)
                total
            let reflectedComponent (material : Material, point : Vector3, normal : Vector3, view : Vector3, depth : int) =
                if (depth > Ray.MaxDepth()) then
                    Vector3.Zero()
                else
                    let normEps = point + normal.ScaledBy(EPSILON)
                    let reflectedDirection = view.Reflected(normal).Normalized()
                    let reflectedRay = Ray(normEps, reflectedDirection, depth + 1)
                    let reflectedColor : Vector3 = this.TraceRay(reflectedRay)
                    reflectedColor.ScaledBy(material.Specularity())

            let point = intersection.Point.Value
            let normal = intersection.Normal.Value
            let material = intersection.Material.Value
            let view = ray.Direction.Negated()
            let vnDot = view.Dot(normal)
            let vnSign = (if (vnDot > 0.0) then 1.0 else (if (vnDot < 0.0) then -1.0 else 0.0))
            let localNormal = normal.ScaledBy(vnSign)
            let ambient = ambientComponent(material, point, localNormal)
            let diffuse = diffuseComponent(material, point, localNormal, view)
            let reflection = reflectedComponent(material, point, localNormal, view, ray.Depth)
            ambient + diffuse + reflection
        
        member this.TraceRay (ray : Ray) =
            let nearestHit = this.NearestHit(ray)
            if (nearestHit.T < Intersection.TMax()) then
                let material = nearestHit.Material.Value
                match material.MaterialType() with
                | MaterialType.SolidColor -> material.Diffuse()
                | MaterialType.Phong -> this.PhongAt(nearestHit, ray)
            else
                this.BackgroundColor(ray)