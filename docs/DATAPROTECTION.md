# Data Protection & Encryption for OneGround ZGW APIs

This guide covers the DataProtection key storage security considerations and the encryption features available for protecting sensitive personal data in ZGW APIs.

## Security Notice: DataProtection Key Storage

By default, ASP.NET Core DataProtection keys are stored in the **same PostgreSQL database** as application data, separated only by a dedicated `data_protection` schema. Without additional configuration, these keys are stored **as plaintext**, which could expose sensitive data if the database is compromised.

**Secure alternatives:**

- **Certificate encryption** — Set `DataProtection:Certificate` to a Base64-encoded PFX certificate and optionally `DataProtection:CertificatePassword` to protect keys at rest.
- **Azure Key Vault** — Use Azure Key Vault to manage and protect DataProtection keys.
- **File-system storage** — Store keys on the file system with restricted OS-level permissions to limit access.

For more information, see the [Microsoft ASP.NET Core DataProtection documentation](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/introduction).

---

## Sensitive Data Encryption

ZGW APIs support encryption of sensitive personal data stored in the database. Currently available in the Zaken API and extendable to any service. This uses two complementary mechanisms:

### 1. HMAC Hashing (for searchable lookups)

Sensitive values are hashed using HMAC-SHA256 so they can be searched without storing plaintext. Configure the HMAC key using the following environment variable:

```dotenv
HmacHasher__HmacKey=<base64-encoded-key-minimum-32-bytes>
```

**Generate a key:**

```bash
# Linux/macOS
openssl rand -base64 32
```

```powershell
# PowerShell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }) -as [byte[]])
```

> **Warning:** The HMAC key is permanent — if you change it, existing hashes become unsearchable. Back it up securely.

---

### 2. DataProtection Encryption (for reversible encryption at rest)

Sensitive values are also encrypted using ASP.NET Core DataProtection. Encryption keys are stored in the database (`data_protection.DataProtectionKeys` table). Optionally, these keys can be protected with an X.509 certificate.

Configure the certificate using the following environment variables:

```dotenv
DataProtection__Certificate=<base64-encoded-pfx>
DataProtection__CertificatePassword=<pfx-password>
```

If no certificate is configured, DataProtection keys are stored unencrypted in the database. This is acceptable for local development but **not recommended for production**.

> **Warning:** If the certificate is lost, all encrypted data in the database becomes permanently unreadable. Always back up the PFX file.

**Generate a certificate using the provided scripts:**

Run the following commands from the repository root (or adjust the path accordingly):

**Linux/macOS:**

```bash
chmod +x tools/oneground-certificates-generator/generate-dataprotection-certificate.sh
tools/oneground-certificates-generator/generate-dataprotection-certificate.sh
```

**Windows (PowerShell):**

```powershell
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process
.\tools\oneground-certificates-generator\generate-dataprotection-certificate.ps1
```

The script outputs the `DataProtection__Certificate` and `DataProtection__CertificatePassword` values ready to paste into your `.env` file.
