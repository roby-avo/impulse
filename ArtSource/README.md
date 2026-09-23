# Original Blender asset pipeline

All meshes are authored by `build_assets.py` in Blender 5.2.2 LTS. No external
models or textures are used. `PhysicsPlayground.blend` is the editable source
gallery; runtime FBX files are exported to `Assets/Resources/Visuals` in metres.

Regenerate from the repository root:

```sh
/Applications/Blender.app/Contents/MacOS/Blender -b -t 4 --python ArtSource/build_assets.py
```

The script builds beveled robot armor, independent gauntlets/boots, industrial
crate/barrel/chair/cargo models, a fan ventilation module, rooftop deck and edge
markings, and a modular city tower. Material roles are shared: Team, Shell,
Metal, Dark, Glow, Amber, Wood, Rubber, Deck, Paint and Window. Unity maps them
to URP materials, recoloring Team per fighter and batching each model by material.

The FBX importer enables mesh reading for runtime batching and never creates
colliders. Existing Unity collision proxies, masses, positions and ring-out
boundaries remain authoritative. Decorative roof beams and skyline have no
collision. The `.blend` lives outside `Assets` to avoid Unity importing it twice.

`asset-manifest.json` records mesh-part and vertex counts before batching.
