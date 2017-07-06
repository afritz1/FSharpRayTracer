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
        let makePoint () = Vector3.RandomPointInSphere(worldRadius)
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
        let makePoint () = Vector3.UnitY() + Vector3.RandomPointInSphere(worldRadius)
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
            let zenithColor = horizonColor * 0.70
            let elevation = ray.Direction.Y
            let percent = if (elevation < 0.0) then 0.0 else elevation
            (horizonColor * (1.0 - percent)) + (zenithColor * percent)

        member inline private this.GetAmbientPercent(point : Vector3, normal : Vector3) =
            let normEps = point + (normal * EPSILON)
            let mutable visibleSamples = 0
            for n in 1 .. ambientSamples do
                let hemisphereDir = Vector3.RandomDirectionInHemisphere(normal)
                let hemisphereRay = Ray(normEps, hemisphereDir, Ray.DefaultDepth())
                let currentTry = this.NearestHit(hemisphereRay)
                visibleSamples <- visibleSamples + (if (currentTry.T < Intersection.TMax()) then 0 else 1)
            float(visibleSamples) / float(ambientSamples)

        // This method is different from "true" ambient occlusion because it offers a variable for 
        // how far ambient occlusion should be considered. Instead of calculating "total visibility
        // in a hemisphere", it's more along the lines of "what's the scale of what we're rendering,
        // and how intense should dark corners be?".
        member inline private this.GetVariableAmbientPercent(point : Vector3, normal : Vector3, maxOccDist : float) = 
            let normEps = point + (normal * EPSILON)
            let maxOccDistRecip = 1.0 / maxOccDist
            let mutable percent = 0.0
            for n in 1 .. ambientSamples do
                let hemisphereDir = Vector3.RandomDirectionInHemisphere(normal)
                let hemisphereRay = Ray(normEps, hemisphereDir, Ray.DefaultDepth())
                let currentTry = this.NearestHit(hemisphereRay)
                let occPercent = currentTry.T * maxOccDistRecip
                percent <- percent + (if (currentTry.T > maxOccDist) then 1.0 else occPercent)
            percent / float(ambientSamples)

        member inline private this.PhongAt(intersection : Intersection, ray : Ray) =
            let ambientComponent (material : Material, point : Vector3, normal : Vector3) =
                let ambientColor = material.Diffuse() * this.BackgroundColor(ray)
                let ambientPercent = this.GetAmbientPercent(point, normal)
                ambientColor * (material.Ambient() * ambientPercent)
            let diffuseComponent (material : Material, point : Vector3, normal : Vector3, view : Vector3) =
                let normEps = point + (normal * EPSILON)
                let lightSamplesRecip = 1.0 / float(lightSamples)
                let mutable total = Vector3.Zero()
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
                            let highlight = lightColor * (material.Specularity() * shinyAmount)
                            let diffuse = (material.Diffuse() * lightColor) * (Math.Max(0.0, lnDot))
                            totalDiffuse <- totalDiffuse + diffuse
                            totalHighlight <- totalHighlight + (if (lnDot >= 0.0) then highlight else Vector3.Zero())
                    let lightContribution = float(visibleSamples) * lightSamplesRecip
                    totalDiffuse <- totalDiffuse * lightSamplesRecip
                    totalHighlight <- totalHighlight * lightSamplesRecip
                    total <- total + (totalDiffuse + totalHighlight) * lightContribution
                total
            let reflectedComponent (material : Material, point : Vector3, normal : Vector3, view : Vector3, depth : int) =
                if (depth >= Ray.MaxDepth()) then
                    Vector3.Zero()
                else
                    let normEps = point + (normal * EPSILON)
                    let reflectedDirection = view.Reflected(normal).Normalized()
                    let reflectedRay = Ray(normEps, reflectedDirection, depth + 1)
                    let reflectedColor : Vector3 = this.TraceRay(reflectedRay)
                    reflectedColor * material.Specularity()

            let point = intersection.Point.Value
            let normal = intersection.Normal.Value
            let material = intersection.Material.Value
            let view = ray.Direction.Negated()
            let vnDot = view.Dot(normal)
            let vnSign = (if (vnDot > 0.0) then 1.0 else (if (vnDot < 0.0) then -1.0 else 0.0))
            let localNormal = normal * vnSign
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