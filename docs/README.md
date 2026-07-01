# OneGround ZGW APIs Documentation

This directory contains detailed documentation for the OneGround ZGW APIs implementation.

## Available Documentation

- **[Authentication Guide](./AUTHENTICATION.md)** - Complete guide for authenticating against the ZGW APIs, including OAuth2 access tokens and legacy ZGW standard tokens
- **[Logging Configuration](./LOGS.md)** - Detailed logging strategy using Serilog with file rotation, size limits, and retention policies
- **[Data Protection & Encryption](./DATAPROTECTION.md)** - Security guide for DataProtection key storage, HMAC hashing for searchable lookups, and certificate-based encryption at rest
- **[Audit trail](./AUDITTRAIL.md)** - Guid with a description of the various audit tracks and how they work.
- **[Circuit Breaker](./nrc/CIRCUIT_BREAKER.md)** - Per-subscription circuit breaker for notification delivery: state machine, Redis key design, automatic blocking, and unblock flow.
