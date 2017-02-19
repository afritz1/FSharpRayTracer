namespace FSharpRayTracer

module Lights =
    open Intersections
    open Materials
    open Rays
    open Shapes
    open Vectors
    
    [<AbstractClass>]
    type Light () =
        inherit Shape()

        abstract member RandomPoint : unit -> Vector3

    type SphereLight (point : Vector3, radius : float) =
        inherit Light()

        let sphere = Sphere(point, radius, SolidColor(Vector3.RandomColor()))

        override this.Centroid () = sphere.Centroid()
        override this.BoundingBox () = sphere.BoundingBox()
        override this.Intersect (ray : Ray) = sphere.Intersect(ray)
        override this.RandomPoint () = point + Vector3.RandomPointInSphere(radius)

    type CuboidLight (point : Vector3, width : float, height : float, depth : float) =
        inherit Light()

        let cuboid = Cuboid(point, width, height, depth, SolidColor(Vector3.RandomColor()))
        
        override this.Centroid () = cuboid.Centroid()
        override this.BoundingBox () = cuboid.BoundingBox()
        override this.Intersect (ray : Ray) = cuboid.Intersect(ray)
        override this.RandomPoint () = point + Vector3.RandomPointInCuboid(width, height, depth)