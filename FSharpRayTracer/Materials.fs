namespace FSharpRayTracer

// Maybe a discriminated union (i.e., members determined at construction) would
// be better than an abstract class? Match material type with a function or
// something.

module Materials =
    open Vectors

    type MaterialType =
    | SolidColor
    | Phong
    
    [<AbstractClass>]
    type Material () =
        // Each derived material can implement the methods themselves whether they
        // use them or not. A discriminated union could avoid this.
        abstract member Diffuse : unit -> Vector3
        abstract member Ambient : unit -> float
        abstract member Specularity : unit -> float
        abstract member Shininess : unit -> float
        abstract member MaterialType : unit -> MaterialType

    type SolidColor (color : Vector3) =
        inherit Material()

        override this.Diffuse () = color
        override this.Ambient () = 1.0
        override this.Specularity () = 0.0
        override this.Shininess () = 0.0
        override this.MaterialType () = MaterialType.SolidColor
        
    type Phong (diffuse : Vector3, ambient : float, specularity : float, shininess : float) =
        inherit Material()

        static member DefaultAmbient = 0.60
        static member DefaultSpecularity = 0.30
        static member DefaultShininess = 16.0

        new (diffuse : Vector3) = 
            Phong(diffuse, Phong.DefaultAmbient, Phong.DefaultSpecularity, Phong.DefaultShininess)

        override this.Diffuse () = diffuse
        override this.Ambient () = ambient
        override this.Specularity () = specularity
        override this.Shininess () = shininess
        override this.MaterialType () = MaterialType.Phong