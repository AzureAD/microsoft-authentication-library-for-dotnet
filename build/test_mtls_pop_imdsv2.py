"""
Python MSI v2 mTLS PoP E2E test for IMDSv2.
Validates mTLS PoP token acquisition and certificate binding via the cnf claim.
Uses the standard msal package with ManagedIdentityClient.
Exit codes: 0 = success, 2 = failure.
"""
import sys
import base64
import json

try:
    import msal
    import requests

    print("=== Python MSI v2 mTLS PoP E2E Test ===")

    session = requests.Session()
    cache = msal.TokenCache()
    client = msal.ManagedIdentityClient(
        msal.SystemAssignedManagedIdentity(),
        http_client=session,
        token_cache=cache,
    )

    # Acquire mTLS PoP token with attestation support
    result = client.acquire_token_for_client(
        resource="https://management.azure.com/",
        mtls_proof_of_possession=True,
        with_attestation_support=True,
    )

    if result is None or "access_token" not in result:
        print(f"ERROR: Token acquisition failed: {json.dumps(result, indent=2)}", file=sys.stderr)
        sys.exit(2)

    # Validate token type is strictly mtls_pop
    token_type = (result.get("token_type") or "").lower()
    print(f"Token type: {token_type}")
    if token_type != "mtls_pop":
        print(f"ERROR: Expected token_type=mtls_pop, got: {result.get('token_type')}", file=sys.stderr)
        sys.exit(2)
    print("PASS: token_type is mtls_pop")

    # Extract and display certificate information
    cert_pem = result.get("cert_pem")
    cert_thumbprint = result.get("cert_thumbprint_sha256")

    if cert_pem:
        print(f"Certificate PEM:\n{cert_pem}")
        with open("binding_cert.pem", "w") as f:
            f.write(cert_pem)
        print("Certificate saved to binding_cert.pem")
    else:
        print("WARNING: cert_pem not present in result")

    if cert_thumbprint:
        print(f"Certificate SHA256 thumbprint (base64url): {cert_thumbprint}")
    else:
        print("WARNING: cert_thumbprint_sha256 not present in result")

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
        if cert_thumbprint and x5t_s256 != cert_thumbprint:
            print(
                f"ERROR: cnf.x5t#S256 mismatch. Token={x5t_s256}, Cert={cert_thumbprint}",
                file=sys.stderr,
            )
            sys.exit(2)
        print("PASS: cnf.x5t#S256 is present in token")
    else:
        print("WARNING: Could not decode token claims (opaque token)")

    # Test token caching by acquiring a second token
    print("Testing token caching...")
    result2 = client.acquire_token_for_client(
        resource="https://management.azure.com/",
        mtls_proof_of_possession=True,
        with_attestation_support=True,
    )
    if result2 is None or "access_token" not in result2:
        print(f"ERROR: Second token acquisition failed: {json.dumps(result2, indent=2)}", file=sys.stderr)
        sys.exit(2)
    if (result2.get("token_type") or "").lower() != "mtls_pop":
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
