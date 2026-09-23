# Asset provenance

All art is original to this project. M6 replaces prototype visuals with ten
modular Blender models: armored robot body, glove, boot, crate, barrel, chair,
cargo box, vent, rooftop and skyline tower. Generated FBX meshes and their
manifest are in `Assets/Resources/Visuals`. Editable source is in
`ArtSource/PhysicsPlayground.blend`; `ArtSource/build_assets.py` regenerates it
with Blender 5.2.2 LTS. Blender is only required to modify/regenerate assets,
not to play or build the game from the committed FBX files.

HUD, synthesized sounds, impact effects and role-based URP materials are also
original. No third-party art, textures or music were downloaded. Runtime visual
meshes are combined by material; physics uses unchanged primitive proxies.

Unity packages are installed through Unity Package Manager under their respective
licenses. Local Laya uses `convaiinnovations/laya` (Apache-2.0 model); weights live
in the user's Hugging Face cache and are not committed or bundled in the game.

The scene contains the Arena bootstrap. It deterministically creates the rooftop
at runtime, so the same layout is used in editor tests and the standalone game.
Resources/Surface.mat and Resources/Particles.mat retain the URP shaders in builds.
