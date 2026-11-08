# Privacy policy

This policy describes the network and local data handling in reistrap. reistrap is an independent open-source Roblox bootstrapper maintained by rei. Questions can be raised at https://github.com/1lyst4r/reistrap/issues. Do not include passwords, cookies, private links, or other sensitive data in public issues.

## Local data

reistrap stores settings, logs, game profiles, Fast Flags, and, if enabled, playtime and performance history on your computer. Depending on the features you use, its settings and logs may contain game names, game IDs, server information, file paths, diagnostic details, and other information. You can manage settings and logs in reistrap and choose whether to keep data during uninstall.

## Analytics and crash reports

The installer offers an analytics switch, also available later on the reistrap settings page. It is enabled by default. When enabled, reistrap can send installation, upgrade and uninstall events, Roblox channel information and package download failures to the reistrap analytics service at reistrap-analytics.reistrap.workers.dev. The service records aggregate counts by date, event, value and reistrap version.

When enabled and an eligible production build encounters an unhandled exception, reistrap can also send the current diagnostic log to the same service. Logs can contain error details, game or server information, URLs and paths or other information from the session. The logger replaces the Windows user profile path with a placeholder, but this is not a guarantee that all sensitive information is removed. The service code schedules deletion of crash reports after 90 days. Infrastructure and provider logs may have separate retention practices.

Turn off analytics during installation or in reistrap settings to stop these analytics and crash-report requests. This switch does not disable the other network requests described below.

## Setup sharing

When you choose to share a setup, reistrap sends selected settings and Fast Flags to the reistrap service, which returns a code. Anyone with the code can retrieve the shared setup, so only share codes with people you trust and do not put secrets in Fast Flags or settings. Some fields, including game profiles, custom integrations and local image paths, are excluded. The service stores the setup for up to 180 days and uses a daily IP-derived hash to limit requests. Cleanup is scheduled, so actual deletion can occur later if the scheduled job does not run. Setup sharing is a separate, user-initiated feature from the analytics switch.

## Third-party services

reistrap contacts Roblox and its delivery networks to install, update, launch and obtain game or account information needed by enabled features. It contacts GitHub for reistrap updates and other project resources. Server location and region features can contact RoValra and ipinfo.io; location lookups can include a Roblox server IP address, and network requests also reveal your own IP address to the service receiving them. Discord Rich Presence sends activity information to Discord when enabled. Other optional integrations and features may contact the services needed to provide them. These services operate under their own policies:

- Roblox: https://en.help.roblox.com/hc/en-us/articles/115004630823-Roblox-Privacy-and-Cookie-Policy
- GitHub: https://docs.github.com/en/site-policy/privacy-policies/github-general-privacy-statement
- Cloudflare, which hosts the reistrap service: https://www.cloudflare.com/privacypolicy/
- RoValra: https://www.rovalra.com/
- ipinfo.io: https://ipinfo.io/privacy-policy
- Discord: https://discord.com/privacy

Network providers can receive ordinary connection information such as IP addresses and request metadata. Their retention and use are governed by their own practices.

## Changes

This policy may change as reistrap changes. The current version is published in the repository.
