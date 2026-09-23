# Running Dock icon — 0.8.1

The 0.8.0 bundle contained the robot ICNS, but the user's screenshot showed a
generic running Dock tile. Checking the bundle alone did not validate that tile.

`Native/DockIcon.m` assigns the bundled image to the current application's
`applicationIconImage` on the AppKit main thread, then asks its Dock tile to
display. The managed bootstrap repeats this after startup and on focus.
[Apple documents this property as the application's Dock icon](https://developer.apple.com/documentation/appkit/nsapplication/applicationiconimage).

Validation on Apple M3 Pro, macOS, 2026-09-23:

- Unity 6000.4.0f1 universal standalone build succeeded.
- Plugin contains arm64 and x86_64 architectures and is included in Contents/PlugIns.
- Standalone launch loads and invokes the native entry points.
- Final 0.8.1 launch logs `DOCK ICON APPLIED` with no native loading errors.
- `--pg-verify-dock` exports the live `NSApplication.applicationIconImage`
  property to `~/Library/Application Support/Impulse/Physics Playground/dock-icon-runtime.png`.
  Visual inspection of that readback shows the intended robot artwork.
- Direct Dock UI inspection was unavailable: the UI tool timed out selecting Dock.
  The final UI attempt also reported that the Mac was locked.
  The evidence is the live AppKit property, not a screenshot of the Dock itself.

AppKit can copy/re-encode NSImage, so object identity and raw TIFF byte equality
are unsuitable assertions. The runtime log confirms a valid image after setting
the property; the exported property supplies the visual content check.

The build compiles the plugin using Xcode Command Line Tools and ad-hoc signs it.
No global Dock settings, pinned items, or system icon caches are changed.
