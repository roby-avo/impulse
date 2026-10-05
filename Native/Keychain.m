#import <Foundation/Foundation.h>
#import <Security/Security.h>
#include <stdlib.h>
#include <string.h>

// Generic-password items in the user's login keychain. Secrets never pass through
// process arguments, files or logs; the caller frees returned strings explicitly.
static NSMutableDictionary *PlaygroundKeychainQuery(const char *service, const char *account) {
    return [@{
        (__bridge id)kSecClass: (__bridge id)kSecClassGenericPassword,
        (__bridge id)kSecAttrService: [NSString stringWithUTF8String:service],
        (__bridge id)kSecAttrAccount: [NSString stringWithUTF8String:account],
    } mutableCopy];
}

__attribute__((visibility("default"))) int PlaygroundKeychainSave(const char *service, const char *account, const char *label, const char *secret) {
    if (!service || !account || !secret) return (int)errSecParam;
    NSData *data = [[NSString stringWithUTF8String:secret] dataUsingEncoding:NSUTF8StringEncoding];
    NSMutableDictionary *query = PlaygroundKeychainQuery(service, account);
    OSStatus status = SecItemUpdate((__bridge CFDictionaryRef)query, (__bridge CFDictionaryRef)@{(__bridge id)kSecValueData: data});
    if (status != errSecItemNotFound) return (int)status;
    query[(__bridge id)kSecValueData] = data;
    if (label) query[(__bridge id)kSecAttrLabel] = [NSString stringWithUTF8String:label];
    return (int)SecItemAdd((__bridge CFDictionaryRef)query, NULL);
}

__attribute__((visibility("default"))) char *PlaygroundKeychainLoad(const char *service, const char *account, int *statusOut) {
    if (!service || !account) { if (statusOut) *statusOut = (int)errSecParam; return NULL; }
    NSMutableDictionary *query = PlaygroundKeychainQuery(service, account);
    query[(__bridge id)kSecReturnData] = @YES;
    query[(__bridge id)kSecMatchLimit] = (__bridge id)kSecMatchLimitOne;
    CFTypeRef result = NULL;
    OSStatus status = SecItemCopyMatching((__bridge CFDictionaryRef)query, &result);
    if (statusOut) *statusOut = (int)status;
    if (status != errSecSuccess || !result) return NULL;
    NSData *data = (__bridge_transfer NSData *)result;
    NSString *text = [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding];
    return text ? strdup(text.UTF8String) : NULL;
}

__attribute__((visibility("default"))) int PlaygroundKeychainDelete(const char *service, const char *account) {
    if (!service || !account) return (int)errSecParam;
    OSStatus status = SecItemDelete((__bridge CFDictionaryRef)PlaygroundKeychainQuery(service, account));
    return status == errSecItemNotFound ? (int)errSecSuccess : (int)status;
}

__attribute__((visibility("default"))) void PlaygroundKeychainFree(char *value) {
    if (value) { memset(value, 0, strlen(value)); free(value); }
}
