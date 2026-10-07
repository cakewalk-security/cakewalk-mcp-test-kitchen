# Security Policy

## Reporting a vulnerability

Please **do not open a public issue** for security problems.

Report vulnerabilities privately through GitHub:
[**Report a vulnerability**](https://github.com/cakewalk-security/cakewalk-mcp-test-kitchen/security/advisories/new)
(the repository's *Security* tab → *Report a vulnerability*).

Include what you can of the following:

- The affected component (API, web console, a specific scenario, deployment config).
- Steps to reproduce, or a proof of concept.
- The impact you expect (for example: access to another user's data, privilege escalation).

We aim to acknowledge reports within 3 business days and to keep you updated until a fix ships.
We are happy to credit reporters in the advisory unless you prefer to stay anonymous.

## Scope

In scope:

- The code in this repository.
- The hosted instance at `https://mcp-test-kitchen.cakewalk.security`.

Out of scope:

- The scenarios' intentionally misbehaving MCP responses (errors, malformed payloads, dropped
  connections). Producing broken protocol traffic on purpose is what this server is for.
- Denial of service through volumetric traffic against the hosted instance.
- Findings that need a compromised account, browser or device.

Please test only against your own account, and do not access or modify other users' data.

## Supported versions

Only the latest commit on `main` is supported. Fixes are not backported.
