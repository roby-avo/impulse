# Asset provenance

Arena geometry, robot shapes, prop silhouettes, HUD, synthesized sound waveforms,
and impact effects are original procedural assets created for this project.
No external art, music, textures, or Blender files are required.

Unity packages are installed through Unity Package Manager under their respective
licenses. Local Laya uses `convaiinnovations/laya` (Apache-2.0 model); weights live
in the user's Hugging Face cache and are not committed or bundled in the game.

The scene contains the Arena bootstrap. It deterministically creates the rooftop
at runtime, so the same layout is used in editor tests and the standalone game.
Resources/Surface.mat and Resources/Particles.mat retain the URP shaders in builds.
