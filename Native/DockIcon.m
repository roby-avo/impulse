#import <Cocoa/Cocoa.h>
#import <stdatomic.h>

// Refresh this running application's Dock tile directly. No Dock restart or
// changes to other applications, the user's pinned items, or global icon caches.
static atomic_int iconStatus = 0;
__attribute__((visibility("default"))) int PlaygroundDockIconStatus(void) {
    return atomic_load(&iconStatus);
}
__attribute__((visibility("default"))) void PlaygroundInstallDockIcon(const char *verificationPath) {
    NSString *output = verificationPath ? [NSString stringWithUTF8String:verificationPath] : nil;
    void (^install)(void) = ^{
        NSString *path = [[NSBundle mainBundle] pathForResource:@"PlayerIcon" ofType:@"icns"];
        NSImage *icon = path ? [[NSImage alloc] initWithContentsOfFile:path] : nil;
        if (!icon || !icon.isValid) { atomic_store(&iconStatus, -1); return; }
        NSApplication.sharedApplication.applicationIconImage = icon;
        [NSApplication.sharedApplication.dockTile display];
        // AppKit may copy or re-encode NSImage. Export the actual property for
        // visual verification rather than comparing object identity/TIFF bytes.
        NSImage *current = NSApplication.sharedApplication.applicationIconImage;
        NSData *actual = current.TIFFRepresentation;
        if (output && actual) {
            NSBitmapImageRep *bitmap = [NSBitmapImageRep imageRepWithData:actual];
            [[bitmap representationUsingType:NSBitmapImageFileTypePNG properties:@{}] writeToFile:output atomically:YES];
        }
        atomic_store(&iconStatus, current.isValid && actual.length > 0 ? 1 : -2);
    };
    if (NSThread.isMainThread) install();
    else dispatch_async(dispatch_get_main_queue(), install);
}
