# F# Ray Tracer
By Aaron Fritz

I took a short break from my C++ programming and decided to learn the basics of F#. It is a functional language derived from OCaml that also allows for some imperative and object-oriented style.

This particular ray tracer was a success, and currently has neat features like soft shadows and ambient occlusion. It's nice that the .NET framework allows simple parallelization of code with the "Parallel.For" function, too. 

One more thing to note is that I seem to have circular dependencies in nearly all of my ray tracers in the past, but this is the first one to have zero of them. This is in part because the Visual F# project enforces sorting files from top to bottom according to their compilation order, and it seems no forward declarations are allowed, unlike other languages. Because of this restriction, I changed my ray tracer design a bit, and I think it was for the better, though now the World type has much more of the shading code that was originally in the Material type.
