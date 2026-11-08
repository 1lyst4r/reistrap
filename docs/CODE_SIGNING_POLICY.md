# Code signing policy

reistrap is applying to the SignPath Foundation open-source signing program. Releases are currently unsigned unless their release notes explicitly state otherwise. SignPath has not yet approved reistrap or issued signing access.

If approved, the project intends to use free code signing provided by [SignPath.io](https://about.signpath.io), with a certificate by [SignPath Foundation](https://signpath.org/). The project will update this policy and its release pages when signing is operational.

## Roles

The project maintainer, [rei](https://github.com/1lyst4r), is responsible for repository changes and reviews. Signing approvals and any additional authorized people will be documented here once configured. External contributions require maintainer review. Team members involved in signing must use multi-factor authentication for GitHub and SignPath.

## Release process

The current release workflow builds the project from the public repository using GitHub Actions and produces a draft release. The maintainer reviews and publishes releases. Signing is not currently configured. If SignPath approves the project, the signing workflow will verify artifact origin and require a trusted maintainer to approve each signing request before signed artifacts are published.

## Privacy

See the [reistrap privacy policy](PRIVACY.md) for information about analytics, crash reports, setup sharing, and third-party services.
