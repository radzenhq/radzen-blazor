#nullable enable
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;

namespace Radzen.Blazor.Pdf.Tests;

internal sealed class TestSigningIdentity : IDisposable
{
    private const string CertificateDer =
        "MIIDFzCCAf+gAwIBAgIUWaTfdg0h50zbirA4oHoaizyX1xUwDQYJKoZIhvcNAQELBQAwGzEZMBcGA1UEAwwQUmFkemVuIFBERiBUZXN0czAeFw0yNjA3MjkwNTU5MDdaFw00NjA3MjQwNTU5MDdaMBsxGTAXBgNVBAMMEFJhZHplbiBQREYgVGVzdHMwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQCqQqLosd2skU8a42DSrqop2+B6jo42HvAmra92abckHDMDfps3QxxaN1byH0z4Upm1LmhFinIibU9mWBERZoxq0SJRUhp1bzX4UhUcHYdtQIkwtfvFsy2+2LIRh+3fiB+DKH5/4UyYH4Xd0os9Dmp7Z2VD2Emqnr/jiNDk2PHvmMhXkroSTY7TyyQXjXFolg5kK7quOxXLxP/LJRuGXpTQ3cwkNEUJM3JKuQBG59vY/Gdut84DXiM0HVImfm51oioy8NZJoEpXhcWOjIbRlvW9wl+mL/CJo5Cuy7A0UHruTHf8YJa7CqsZzwZMKPGqrvihIYGcSpQsDFyjep+YnL6NAgMBAAGjUzBRMB0GA1UdDgQWBBRRWJalGRZdmNhOB+GNubQI4hmj4TAfBgNVHSMEGDAWgBRRWJalGRZdmNhOB+GNubQI4hmj4TAPBgNVHRMBAf8EBTADAQH/MA0GCSqGSIb3DQEBCwUAA4IBAQBqVaFcx4wWTsCpYQz4wCFo/KjaU/ckqmTruZalWtklJaCY4Nm53cV2gFNiM8fls01X3hBclrUoBEPe503lbUiu4OLSLVZzeQEljn7Z4ZterooTxp49LPIKPH8/zUUaLZ4FlHcQSWqz7QPvF7nbsvjuHVadardLdxhA8QFkMIOwLhUKXfVfKcR0hoSArgBeMBdM3Ov3t9xMXaFNfx6nsL7g3e2vkPhAe0A4L+gZ1WUnDui9XCePbPDk6PxnE/ZHmGuUNlqiihlYZp6eXLnlFoz1XrR1KTnes0RF1OY2T6kijr3G+rHrhCK1AoJbJugGCunD0pg488f73Trz1ouco2Wb";

    private const string PrivateKeyDer =
        "MIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQCqQqLosd2skU8a42DSrqop2+B6jo42HvAmra92abckHDMDfps3QxxaN1byH0z4Upm1LmhFinIibU9mWBERZoxq0SJRUhp1bzX4UhUcHYdtQIkwtfvFsy2+2LIRh+3fiB+DKH5/4UyYH4Xd0os9Dmp7Z2VD2Emqnr/jiNDk2PHvmMhXkroSTY7TyyQXjXFolg5kK7quOxXLxP/LJRuGXpTQ3cwkNEUJM3JKuQBG59vY/Gdut84DXiM0HVImfm51oioy8NZJoEpXhcWOjIbRlvW9wl+mL/CJo5Cuy7A0UHruTHf8YJa7CqsZzwZMKPGqrvihIYGcSpQsDFyjep+YnL6NAgMBAAECggEAPYKCcVek0q3Sas/fkG2qqyG0+QHcbcACma6g18I8eE65vVb1g2L68LrSbfmR6JqKzlJ8ODdaoYZlK3/Ads1aCFMSt1dWmLt7P4GD/9rJLNamWZM9aMChU7PcrQmzDdETNWKhRNjjv4eP6MIiLIYzQO7rPjX68ymUAINSmIKp8Rllhv/STBJ4tjRpH9lPyh3ByiM6nQQrh805oZzhr/L9Q8FN8qOdMNPdGtN0dCeW8hU0Gugt4ugupMPxK2X9oHmPabQh7bMI/Mlu+DLMEe+j8g0SrOidQMzNlaKXLOT9fKo0k3WmknDCsHyOyX6j0e+0yvEBcgl7cheXs9BQj5yobwKBgQDe6xvksGpYbz4LxoIS8xjJIrh52ATrBf/GbTI/E9bJ6fmOILqz3r1K055t+cBNGA6edWMCnBHVvCgc1cC/Ke8eFilijT+vjLJ30WSgyFxuBC41KWy2VnnkDvrKsuz7BwTBSYzn6647fpq6TWO9jfPNWnJdoxppEDvlBTt3BL4VBwKBgQDDhv57mOdRcCgt4Q46NKm80gczwEftC3hHPxhnamtSOjYURHkWdj1ePSYNHkxpH+SC6eukBLRRQ6mm39uQh4RcTL90c4d2Yigfzm0A0Nx8MQBq18+OoSVgrFW6n6J0xppbD8SVPjpZM0XSIMKKUZkeSI0ssA8ohg/iXr1mb6reywKBgCht35s0W7U6R7h/AixJpp8kCu0ePpYZenAUcd56zKPGKZqbWQEyToZ5puwvNa9Fw7D9fT2F7L4k5+mC4vhItLNyNYNINtqx29RbR7LZY9oYLAE8SBkxyd2Q0e9dUBmfBj/ABSwy1GC573oGeyZvzl3aH4/X+vw/E33P2x8U3xyVAoGBAK0oRQDCVcDqebq/v7DOaK+etOAg3dHQwZEfEIatWSP2B2SFi1LYHdryfltJxNOoed9yN8wGmoYJTRpKz5C8Yvy2vyrrEUFHBk+8qQ366fhWEQ+N1fNzRL3LgRSIQP/3zkTsvuSIunW6kY/YkVCbmOWXzOaReKsjpreLvWIVbJZnAoGAfDePEhZfdZ2v0eDVWFYeJ7CqWBbuVI/sOOIbpZZJLENDbtaxwa1W2QSuVkd510FFcjFxyrscBNiNIG8Hs70NLBqVxcQfizq6cQg2C2O1+ISETozfBrH68HDi0/Uf4X2XDW4+ko2htEkpkniWYnyymgSVe66zH+o4daSOgiObVXg=";

    private TestSigningIdentity(X509Certificate2 certificate, RSA privateKey)
    {
        Certificate = certificate;
        PrivateKey = privateKey;
    }

    public X509Certificate2 Certificate { get; }

    public RSA PrivateKey { get; }

    public static TestSigningIdentity Create()
    {
        var certificate = X509CertificateLoader.LoadCertificate(Convert.FromBase64String(CertificateDer));
        var privateKey = RSA.Create();
        privateKey.ImportPkcs8PrivateKey(Convert.FromBase64String(PrivateKeyDer), out _);
        return new TestSigningIdentity(certificate, privateKey);
    }

    public CmsSigner CmsSigner()
        => new(SubjectIdentifierType.IssuerAndSerialNumber, Certificate) { PrivateKey = PrivateKey };

    public void Dispose()
    {
        PrivateKey.Dispose();
        Certificate.Dispose();
    }
}
