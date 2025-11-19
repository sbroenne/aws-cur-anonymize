# Security Policy

## Supported Versions

The following versions of aws-cur-anonymize are currently supported with security updates:

| Version | Supported          |
| ------- | ------------------ |
| 1.0.x   | :white_check_mark: |

## Security Features

aws-cur-anonymize implements the following security features:

-   **Data Anonymization**:
    -   Deterministic hashing of AWS account IDs
    -   Salt-based anonymization for consistency across runs
    -   No storage of original account information
    -   Secure handling of sensitive cost data

-   **Input Validation**:
    -   File path validation and sanitization
    -   SQL injection prevention through parameterized queries
    -   Safe handling of user-provided salt values

-   **Development Practices**:
    -   Automated security scanning via GitHub Advanced Security
    -   Regular dependency updates through Dependabot
    -   CodeQL analysis for vulnerability detection
    -   Secure coding guidelines

## Reporting a Vulnerability

If you discover a security vulnerability in aws-cur-anonymize, please report it by:

1. **DO NOT** create a public GitHub issue
2. Email the maintainers with details of the vulnerability
3. Include steps to reproduce if possible
4. Allow up to 48 hours for an initial response

We take all security reports seriously and will respond promptly to fix confirmed vulnerabilities.

## Security Best Practices for Users

When using aws-cur-anonymize:

-   **Protect your salt value**: Store it securely (environment variables, secret managers)
-   **Use strong salts**: Generate cryptographically random salt values
-   **Verify outputs**: Always verify anonymized data doesn't leak sensitive information
-   **Access control**: Restrict access to both input CUR files and output files
-   **Audit trails**: Maintain logs of when and how the tool is used

## Security Update Policy

-   Critical security issues: Patched within 48 hours
-   High severity issues: Patched within 1 week
-   Medium/Low severity issues: Patched in next planned release

Security updates will be clearly marked in the CHANGELOG and release notes.
