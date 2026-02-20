'use strict';

const fs = require('fs');
const path = require('path');
const {
  analyze,
  isMsiV2Issue,
  collectLogText,
  extractCodeBlocks,
  filterMsalLines,
  redactPii,
  parseLine,
  parseEvents,
  detectStages,
  classifyFailure,
  buildEvidence,
  loadSignatures,
} = require('../index');

const SIGNATURES_PATH = path.join(__dirname, '..', 'signatures.yml');
const TESTDATA = path.join(__dirname, '..', 'testdata');

function readTestLog(filename) {
  return fs.readFileSync(path.join(TESTDATA, filename), 'utf8');
}

// ─── Test 1: #5755 mTLS + SCHANNEL failure scenario ─────────────────────────
describe('Test #5755 mTLS+SCHANNEL scenario', () => {
  let result;

  beforeAll(() => {
    const logContent = readTestLog('issue-5755-mtls-schannel.log');
    const issueBody = '```\n' + logContent + '\n```';
    result = analyze({
      issueBody,
      labels: ['scenario:ManagedIdentity'],
      signaturesPath: SIGNATURES_PATH,
    });
  });

  test('should produce a comment', () => {
    expect(result.shouldComment).toBe(true);
  });

  test('should classify as MIv2.STS.mTLS.Token', () => {
    expect(result.stage).toBe('MIv2.STS.mTLS.Token');
  });

  test('should have high confidence', () => {
    expect(result.confidence).toBe('high');
  });

  test('comment should mention cert cache reuse check', () => {
    expect(result.comment).toMatch(/cert.*cache|PersistentCert.*Reused/i);
  });

  test('comment should mention KeyGuard or private key', () => {
    expect(result.comment).toMatch(/KeyGuard|private key/i);
  });
});

// ─── Test 2: Cache HIT parsing ───────────────────────────────────────────────
describe('Cache HIT parsing', () => {
  test('parseLine detects [CertCache] HIT', () => {
    const event = parseLine('[CertCache] HIT - valid certificate found, thumbprint: abc123');
    expect(event.cacheOp).toBe('HIT');
  });

  test('detect stage MIv2.Cache.ReuseOrMintCert from HIT', () => {
    const events = parseEvents('[CertCache] HIT - certificate found\n[MSAL] ManagedIdentityClient');
    const stages = detectStages(events);
    expect(stages).toContain('MIv2.Cache.ReuseOrMintCert');
  });
});

// ─── Test 3: Cache MISS parsing ──────────────────────────────────────────────
describe('Cache MISS parsing', () => {
  test('parseLine detects [CertCache] MISS', () => {
    const event = parseLine('[CertCache] MISS - no valid certificate found in persistent store');
    expect(event.cacheOp).toBe('MISS');
  });

  test('detect stage MIv2.Cache.ReuseOrMintCert from MISS', () => {
    const events = parseEvents('[CertCache] MISS - no cert\n[MSAL] ManagedIdentityClient');
    const stages = detectStages(events);
    expect(stages).toContain('MIv2.Cache.ReuseOrMintCert');
  });
});

// ─── Test 4: Persistent cert reuse recognition ───────────────────────────────
describe('Persistent cert reuse', () => {
  test('parseLine detects [PersistentCert] Reused certificate', () => {
    const event = parseLine('[PersistentCert] Reused certificate from persistent cache, thumbprint: abc123');
    expect(event.certReuse).toBe(true);
  });

  test('detect stage MIv2.Cache.ReuseOrMintCert from cert reuse', () => {
    const events = parseEvents('[PersistentCert] Reused certificate from persistent cache\n[MSAL] ManagedIdentityClient');
    const stages = detectStages(events);
    expect(stages).toContain('MIv2.Cache.ReuseOrMintCert');
  });
});

// ─── Test 5: mTLS binding cache HIT (memory/persistent) ─────────────────────
describe('mTLS binding cache HIT', () => {
  test('parseLine detects mTLS binding cache HIT (memory)', () => {
    const event = parseLine('[MSAL] mTLS binding cache HIT (memory) - skipping issuecredential');
    expect(event.mtlsCacheHit).toBe(true);
    expect(event.certReuse).toBe(true);
  });

  test('parseLine detects mTLS binding cache HIT (persistent)', () => {
    const event = parseLine('[MSAL] mTLS binding cache HIT (persistent) - cert valid');
    expect(event.mtlsCacheHit).toBe(true);
  });
});

// ─── Test 6: HttpManager request parsing ─────────────────────────────────────
describe('HttpManager request parsing', () => {
  test('parses method and URI from [HttpManager] line', () => {
    const event = parseLine('[HttpManager] POST https://login.microsoftonline.com/common/oauth2/v2.0/token correlation_id: abc');
    expect(event.method).toBe('POST');
    expect(event.uri).toMatch(/oauth2\/v2\.0\/token/);
  });

  test('parses Binding Certificate: True', () => {
    const event = parseLine('[HttpManager] Binding Certificate: True');
    expect(event.bindingCertificate).toBe(true);
  });

  test('Binding Certificate: False does not set bindingCertificate', () => {
    const event = parseLine('[HttpManager] Binding Certificate: False');
    expect(event.bindingCertificate).toBe(false);
  });

  test('parses HTTP status code', () => {
    const event = parseLine('[HttpManager] Response status_code: 404 - Not Found');
    expect(event.statusCode).toBe(404);
  });
});

// ─── Test 7: PII redaction ────────────────────────────────────────────────────
describe('PII redaction', () => {
  test('JWT tokens are redacted', () => {
    const input = 'eyJhbGciOiJSUzI1NiJ9.eyJzdWIiOiIxMjM0In0.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c';
    const redacted = redactPii(input);
    expect(redacted).toBe('<redacted-jwt>');
    expect(redacted).not.toContain('eyJ');
  });

  test('correlation IDs are preserved after redaction', () => {
    const correlationId = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890';
    const input = `[MSAL] correlation_id: ${correlationId} request started`;
    const redacted = redactPii(input);
    expect(redacted).toContain(correlationId);
  });

  test('non-JWT content is not redacted', () => {
    const input = '[MSAL] Request completed successfully';
    const redacted = redactPii(input);
    expect(redacted).toBe(input);
  });
});

// ─── Test 8: Non-MSI v2 logs should not trigger ───────────────────────────────
describe('Non-MSI v2 logs', () => {
  test('public client logs should not trigger triage', () => {
    const logContent = readTestLog('non-miv2.log');
    const issueBody = '```\n' + logContent + '\n```';
    const result = analyze({
      issueBody,
      labels: [],
      signaturesPath: SIGNATURES_PATH,
    });
    expect(result.shouldComment).toBe(false);
  });

  test('empty issue body should not trigger triage', () => {
    const result = analyze({
      issueBody: '',
      labels: [],
      signaturesPath: SIGNATURES_PATH,
    });
    expect(result.shouldComment).toBe(false);
  });

  test('issue body without MSI markers should not trigger triage', () => {
    const result = analyze({
      issueBody: '```\nsome random log line\nanother line\n```',
      labels: [],
      signaturesPath: SIGNATURES_PATH,
    });
    expect(result.shouldComment).toBe(false);
  });
});

// ─── Test 9: Gating logic ─────────────────────────────────────────────────────
describe('Gating logic', () => {
  test('ManagedIdentityClient in body triggers gating', () => {
    const issueBody = '```\n[MSAL] ManagedIdentityClient init\n[ImdsV2] GET https://169.254.169.254/metadata/identity/getplatformmetadata\n```';
    const hasLogs = isMsiV2Issue(issueBody, [], '[MSAL] ManagedIdentityClient init\n[ImdsV2] GET https://169.254.169.254/metadata/identity/getplatformmetadata');
    expect(hasLogs).toBe(true);
  });

  test('scenario:ManagedIdentity label with logs triggers gating', () => {
    const logText = '[MSAL] some log line with ImdsV2 data';
    const hasLogs = isMsiV2Issue('some body', ['scenario:ManagedIdentity'], logText);
    expect(hasLogs).toBe(true);
  });

  test('mi-v2 label with logs triggers gating', () => {
    const logText = '[MSAL] some log line with ManagedIdentityClient data';
    const hasLogs = isMsiV2Issue('some body', ['mi-v2:imds'], logText);
    expect(hasLogs).toBe(true);
  });

  test('no markers and no MI label does not trigger gating', () => {
    const logText = '[MSAL] PublicClient token acquired successfully';
    const hasLogs = isMsiV2Issue('Regular issue about MSAL', [], logText);
    expect(hasLogs).toBe(false);
  });

  test('MSI marker in body but no log lines does not trigger gating', () => {
    const hasLogs = isMsiV2Issue('ManagedIdentityClient issue', [], '');
    expect(hasLogs).toBe(false);
  });
});

// ─── Test 10: IMDS failure classification ────────────────────────────────────
describe('IMDS failure classification', () => {
  test('should classify IMDS getplatformmetadata 404 as MIv2.IMDS.GetPlatformMetadata', () => {
    const logContent = readTestLog('imds-failure.log');
    const issueBody = '```\n' + logContent + '\n```';
    const result = analyze({
      issueBody,
      labels: ['scenario:ManagedIdentity'],
      signaturesPath: SIGNATURES_PATH,
    });
    expect(result.shouldComment).toBe(true);
    expect(result.stage).toBe('MIv2.IMDS.GetPlatformMetadata');
  });
});

// ─── Test 11: Cache HIT/MISS log file ────────────────────────────────────────
describe('Cache HIT/MISS log file analysis', () => {
  test('should detect cache operations from cache-hit-miss.log', () => {
    const logContent = readTestLog('cache-hit-miss.log');
    const events = parseEvents(logContent);
    const cacheEvents = events.filter(e => e.cacheOp);
    expect(cacheEvents.length).toBeGreaterThan(0);
    const ops = cacheEvents.map(e => e.cacheOp);
    expect(ops).toContain('HIT');
    expect(ops).toContain('MISS');
    expect(ops).toContain('SET');
  });

  test('should detect cert reuse in cache-hit-miss.log', () => {
    const logContent = readTestLog('cache-hit-miss.log');
    const events = parseEvents(logContent);
    const reuseEvent = events.find(e => e.certReuse);
    expect(reuseEvent).toBeDefined();
  });
});

// ─── Test 12: extractCodeBlocks ───────────────────────────────────────────────
describe('extractCodeBlocks', () => {
  test('extracts content from single code block', () => {
    const md = 'Some text\n```\nlog line 1\nlog line 2\n```\nMore text';
    const extracted = extractCodeBlocks(md);
    expect(extracted).toContain('log line 1');
    expect(extracted).toContain('log line 2');
  });

  test('extracts content from multiple code blocks', () => {
    const md = '```\nblock 1\n```\nSome text\n```\nblock 2\n```';
    const extracted = extractCodeBlocks(md);
    expect(extracted).toContain('block 1');
    expect(extracted).toContain('block 2');
  });

  test('returns raw text when no code blocks', () => {
    const md = 'Some plain text without code blocks';
    const extracted = extractCodeBlocks(md);
    expect(extracted).toBe(md);
  });
});

// ─── Test 13: Comment format validation ──────────────────────────────────────
describe('Comment format', () => {
  test('comment includes triage marker for update detection', () => {
    const logContent = readTestLog('issue-5755-mtls-schannel.log');
    const issueBody = '```\n' + logContent + '\n```';
    const result = analyze({
      issueBody,
      labels: ['scenario:ManagedIdentity'],
      signaturesPath: SIGNATURES_PATH,
    });
    expect(result.comment).toContain('<!-- msal-log-triage:');
  });

  test('comment includes MSAL MSI v2 header', () => {
    const logContent = readTestLog('issue-5755-mtls-schannel.log');
    const issueBody = '```\n' + logContent + '\n```';
    const result = analyze({
      issueBody,
      labels: ['scenario:ManagedIdentity'],
      signaturesPath: SIGNATURES_PATH,
    });
    expect(result.comment).toContain('MSAL MSI v2 automated log triage');
  });

  test('comment includes next triage steps for high-confidence match', () => {
    const logContent = readTestLog('issue-5755-mtls-schannel.log');
    const issueBody = '```\n' + logContent + '\n```';
    const result = analyze({
      issueBody,
      labels: ['scenario:ManagedIdentity'],
      signaturesPath: SIGNATURES_PATH,
    });
    expect(result.comment).toContain('Next triage steps');
  });
});
