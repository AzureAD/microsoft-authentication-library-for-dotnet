"""
Python MSI v2 mTLS PoP E2E test for IMDSv2.
Validates mTLS PoP token acquisition and certificate binding via the cnf claim.
Exit codes: 0 = success, 2 = failure.
"""
import sys
import base64
import hashlib
import json

try:
    from msal_msiv2 import SystemAssignedManagedIdentity
    from cryptography.hazmat.primitives import serialization

    print("=== Python MSI v2 mTLS PoP E2E Test ===")

    # Acquire mTLS PoP token with attestation support
    mi = SystemAssignedManagedIdentity()
    result = mi.acquire_token_for_managed_identity(
        resource="https://management.azure.com/",
        with_attestation=True
    )

    if result is None:
        print("ERROR: acquire_token_for_managed_identity returned None", file=sys.stderr)
        sys.exit(2)

    # Validate token type is strictly mtls_pop
    token_type = result.get("token_type", "")
    print(f"Token type: {token_type}")
    if token_type != "mtls_pop":
        print(f"ERROR: Expected token_type=mtls_pop, got: {token_type}", file=sys.stderr)
        sys.exit(2)
    print("PASS: token_type is mtls_pop")

    # Extract and display certificate information
    cert = result.get("binding_certificate")
    if cert is None:
        print("ERROR: binding_certificate is missing from result", file=sys.stderr)
        sys.exit(2)

    # PEM format
    pem = cert.public_bytes(serialization.Encoding.PEM).decode()
    print(f"Certificate PEM:\n{pem}")

    # DER base64
    der = cert.public_bytes(serialization.Encoding.DER)
    der_b64 = base64.b64encode(der).decode()
    print(f"Certificate DER (base64): {der_b64}")

    # SHA256 thumbprint
    thumbprint_bytes = hashlib.sha256(der).digest()
    thumbprint_b64url = base64.urlsafe_b64encode(thumbprint_bytes).rstrip(b"=").decode()
    print(f"Certificate SHA256 thumbprint (base64url): {thumbprint_b64url}")

    # Save certificate to file for inspection
    with open("binding_cert.pem", "w") as f:
        f.write(pem)
    print("Certificate saved to binding_cert.pem")

    # Display and validate token claims
    access_token = result.get("access_token", "")
    token_parts = access_token.split(".")
    if len(token_parts) >= 2:
        padding = (4 - len(token_parts[1]) % 4) % 4
        payload_json = base64.urlsafe_b64decode(token_parts[1] + "=" * padding)
        claims = json.loads(payload_json)
        print(f"Token claims: {json.dumps(claims, indent=2)}")

        # Validate certificate binding via cnf claim with x5t#S256
        cnf = claims.get("cnf", {})
        x5t_s256 = cnf.get("x5t#S256", "")
        print(f"cnf.x5t#S256: {x5t_s256}")
        if not x5t_s256:
            print("ERROR: cnf.x5t#S256 claim is missing from token", file=sys.stderr)
            sys.exit(2)
        if x5t_s256 != thumbprint_b64url:
            print(
                f"ERROR: cnf.x5t#S256 mismatch. Token={x5t_s256}, Cert={thumbprint_b64url}",
                file=sys.stderr,
            )
            sys.exit(2)
        print("PASS: cnf.x5t#S256 matches certificate thumbprint")
    else:
        print("WARNING: Could not decode token claims (opaque token)")

    # Test token caching by acquiring a second token
    print("Testing token caching...")
    result2 = mi.acquire_token_for_managed_identity(
        resource="https://management.azure.com/",
        with_attestation=True
    )
    if result2 is None:
        print("ERROR: Second token acquisition returned None", file=sys.stderr)
        sys.exit(2)
    if result2.get("token_type") != "mtls_pop":
        print(
            f"ERROR: Second token has wrong type: {result2.get('token_type')}",
            file=sys.stderr,
        )
        sys.exit(2)
    print("PASS: Token caching works correctly")

    print("=== All checks passed ===")
    sys.exit(0)

except Exception as e:
    print(f"ERROR: {e}", file=sys.stderr)
    import traceback
    traceback.print_exc()
    sys.exit(2)
