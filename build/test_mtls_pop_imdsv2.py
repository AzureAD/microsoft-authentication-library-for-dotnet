"""
Python MSI v2 mTLS PoP E2E test for IMDSv2.
Validates mTLS PoP token acquisition and certificate binding via the cnf claim.
Exit codes: 0 = success, 2 = failure.
"""
import sys
import base64
import json

try:
    import requests
    from msal import msi_v2

    print("=== Python MSI v2 mTLS PoP E2E Test ===")

    session = requests.Session()

    # Call MSI v2 directly to get mTLS PoP token
    print("Acquiring mTLS PoP token via MSI v2...")
    result = msi_v2.obtain_token(
        http_client=session,
        managed_identity=None,  # Use default system-assigned
        resource="https://graph.microsoft.com/",
        attestation_enabled=True
    )

    if result is None or "access_token" not in result:
        print(f"ERROR: Token acquisition failed: {json.dumps(result, indent=2)}", file=sys.stderr)
        sys.exit(2)

    # Validate token type is strictly mtls_pop
    token_type = (result.get("token_type") or "").lower()
    print(f"Token type: {token_type}")
    if token_type != "mtls_pop":
        print(f"ERROR: Expected token_type=mtls_pop, got: {result.get('token_type')}", file=sys.stderr)
        print(f"Full result: {json.dumps(result, indent=2)}", file=sys.stderr)
        sys.exit(2)
    print("[PASS] token_type is mtls_pop")

    # Extract and display certificate information
    cert_pem = result.get("cert_pem")
    cert_thumbprint = result.get("cert_thumbprint_sha256")
    cert_der_b64 = result.get("cert_der_b64")

    if not cert_pem:
        print("ERROR: cert_pem not present in result", file=sys.stderr)
        print(f"Result keys: {list(result.keys())}", file=sys.stderr)
        sys.exit(2)

    print(f"\n[Certificate Information]")
    print(f"Certificate PEM:\n{cert_pem}")
    
    with open("binding_cert.pem", "w") as f:
        f.write(cert_pem)
    print("[OK] Certificate saved to binding_cert.pem")

    if cert_der_b64:
        print(f"\nCertificate DER (base64): {cert_der_b64[:100]}...")

    if cert_thumbprint:
        print(f"Certificate SHA256 thumbprint (base64url): {cert_thumbprint}")
    else:
        print("ERROR: cert_thumbprint_sha256 not present in result", file=sys.stderr)
        sys.exit(2)

    # Display and validate token claims
    print(f"\n[Token Claims]")
    access_token = result.get("access_token", "")
    token_parts = access_token.split(".")
    if len(token_parts) >= 2:
        try:
            padding = (4 - len(token_parts[1]) % 4) % 4
            payload_json = base64.urlsafe_b64decode(token_parts[1] + "=" * padding)
            claims = json.loads(payload_json)
            
            # Print safe claims
            safe_claims = ["sub", "oid", "tid", "appid", "iss", "aud", "exp", "iat"]
            for claim in safe_claims:
                if claim in claims:
                    print(f"  {claim}: {claims[claim]}")

            # Validate certificate binding via cnf claim with x5t#S256
            cnf = claims.get("cnf", {})
            x5t_s256 = cnf.get("x5t#S256", "")
            
            if not x5t_s256:
                print("ERROR: cnf.x5t#S256 claim is missing from token", file=sys.stderr)
                sys.exit(2)
            
            print(f"\n[Certificate Binding]")
            print(f"  cnf.x5t#S256: {x5t_s256}")
            print(f"  Certificate thumbprint: {cert_thumbprint}")
            
            if x5t_s256 != cert_thumbprint:
                print(f"ERROR: cnf.x5t#S256 mismatch!", file=sys.stderr)
                print(f"  Token expects: {x5t_s256}", file=sys.stderr)
                print(f"  Certificate has: {cert_thumbprint}", file=sys.stderr)
                sys.exit(2)
            
            print("[PASS] cnf.x5t#S256 matches certificate thumbprint")
        except Exception as e:
            print(f"ERROR: Could not decode token claims: {e}", file=sys.stderr)
            import traceback
            traceback.print_exc()
            sys.exit(2)
    else:
        print("ERROR: Could not parse token (expected 3 parts separated by dots)", file=sys.stderr)
        sys.exit(2)

    print("\n" + "="*70)
    print("=== All checks passed ===")
    print("="*70)
    sys.exit(0)

except Exception as e:
    print(f"\nERROR: {e}", file=sys.stderr)
    import traceback
    traceback.print_exc()
    sys.exit(2)
