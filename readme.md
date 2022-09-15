# Deprecated

### Directory Services authentication provider has been [consolidated](https://github.com/OctopusDeploy/Issues/issues/7757) into Octopus Server and this repository will be archived soon. If you have a need to fork this repository and modify this provider to meet your needs, please reach out to support@octopus.com
---

Authentication providers are currently external dependencies for Octopus Server. We build and ship these out of band and allow customers to extend or implement their own flavours.

This has caused us some grief regarding engineering velocity; a small change becomes a painful exercise of updating versions on multiple projects. What should be a quick 1-hour fix turns into a week-long journey of multiple PRs.

To tackle this issue, we are consolidating authentication providers and other dependencies into Octopus Server.

We also plan to incorporate changes in forks into the auth extensions we manage to avoid needing a fork in the future.

----
This repository contains the [Octopus Deploy][1] DirectoryServices (Active Directory) authentication provider.

## Documentation
- [Active Directory authentication][2]

## Issues
Please see [Contributing](CONTRIBUTING.md)

## Internal tooling
[Scripts][3] to automate setting up an Active Directory domain controller.

[1]: https://octopus.com
[2]: http://g.octopushq.com/AuthAD
[3]: https://github.com/OctopusDeploy/SupportTools/tree/master/ActiveDirectoryDomainSetup
