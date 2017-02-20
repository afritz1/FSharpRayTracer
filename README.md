# F# Ray Tracer
By Aaron Fritz

This particular ray tracer was a success, and currently has neat features like soft shadows, ambient occlusion, and depth of field. It's nice that the .NET framework allows simple parallelization of code with the `Parallel.For` function, too.

<br/>
![Image 4](Image4_depth_of_field.png)

One more thing to note is that I seem to have circular dependencies in nearly all of my ray tracers in the past, but this is the first one to have zero of them. This is in part because the Visual F# project enforces sorting files from top to bottom according to their compilation order, and it seems no forward declarations are allowed, unlike other languages. Because of this restriction, I changed my ray tracer design a bit, and I think it was for the better, though now the World type has much more of the shading code that was originally in the Material type.

### About F# #

F# is a functional language derived from OCaml that also allows for some imperative and object-oriented style. I really like that values are immutable by default in F#; it's a nice break from having to write `const` all the time in C++. Other interesting traits of the language, like only allowing a type to be nullable if it should be nullable, are much appreciated, and they frequently lead to code being easier to prove correct.

If C++ can be considered "mostly imperative, and a little functional", then F# is "mostly functional, and a little imperative". It isn't always practical to write purely functional code, and it isn't always smart to write purely imperative code, so I feel F# manages to provide the most useful elements of both paradigms.
