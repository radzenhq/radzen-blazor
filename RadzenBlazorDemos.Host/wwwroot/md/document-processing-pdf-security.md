# Security

Encrypt PDF documents with AES-256 and set user and owner passwords and permissions in Blazor and C# - entirely in the browser. Open password-protected files.

Keywords: document, processing, pdf, security, encrypt, encryption, aes, password, permissions, protect, owner, user

## Examples

## PDF Security

Encrypt documents with AES-256, set user and owner passwords with granular permissions, and open password-protected files - all in the browser, using its own Web Crypto implementation.

### Encryption & permissions

Assign `EncryptionOptions` to a document: AES-256, AES-128, or RC4, user and owner passwords, and permission flags for printing, copying, modification, form filling, and annotation.

### Protected files

Open an encrypted document by passing the password in `LoadOptions`, then read or edit it like any other file.
