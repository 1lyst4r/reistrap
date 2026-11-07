# Verification and antivirus investigation

## Reproduction

On November 6, 2026, Microsoft Defender signature 1.463.163.0 detected Trojan:Win32/Bearfoos.A!ml in the official v1.3.9 release executable during a custom scan. Real-time protection was already disabled on this machine; no protection settings were changed. The scan used -DisableRemediation to inspect samples without quarantining them.

- v1.3.9 SHA-256: 96195534e5bd23450aa944dcc78c96653e4ac699da896065dd235b63d2589ce8
- v1.3.8 SHA-256: c899628394cf17a5f6472664d46744c8b120c2d266041f6eeaee73e35e289a77

Both hashes match GitHub release metadata. Both published executables are unsigned. The same scan did not detect v1.3.8 or a locally published, unchanged source build. This does not disprove the reported v1.3.8 detection or establish that the locally built program will remain undetected on other systems.

## Assessment

The root cause is unresolved. Multi-instance creates named mutex handles; this is not evidence that it causes the detection. Other reviewed behaviors include SOCD's low-level keyboard hook, launching downloaded updates, uninstall cleanup using cmd.exe, process queries and window/wallpaper control. The release workflow produces an unsigned .NET single-file executable without an obfuscation or packing step. No causal attribution to any of these behaviors is justified by this scan.

Submit the exact affected release binary and detection name through [Microsoft's software developer submission form](https://www.microsoft.com/en-us/wdsi/filesubmission). A Microsoft determination is needed before calling this a confirmed false positive. Signing future releases can establish publisher identity, but is not a guarantee against antivirus detection. Do not disable protection or add exclusions as a product workaround.

No claim is made that the updater or analytics fixes resolve Bearfoos. No vendor submission has been made during this review.

## Verified fixes

- Updater: a release with checksums or another attachment first would try to execute that attachment. Select the expected reistrap executable by filename, reject path-bearing names and ignore releases without an executable.
- Analytics: inherited object property names such as constructor and toString passed the metric allowlist and caused a TypeError. Check only own properties and return HTTP 400 for invalid keys.
- Features: document missing tools and clarify what backups actually include.

## Build and tests

Requires Windows and the .NET 6.0.428 SDK pinned by global.json. Initialize dependencies with `git submodule update --init --recursive`.

```powershell
dotnet build Bloxstrap.sln -c Release
dotnet run --project Tests/Regression.csproj -c Release
node Tests/analytics.mjs
dotnet publish Bloxstrap/Bloxstrap.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

Run `Bloxstrap/bin/Release/net6.0-windows/win-x64/publish/reistrap.exe` with the .NET 6 Desktop Runtime installed. The regression project exercises release selection without installing or launching Roblox. The Node checks exercise the worker with a mock database; they do not deploy the service.

Baseline Release build and single-file publish passed. Five updater checks and five analytics checks passed after the fixes. The dependency build emitted an existing MSB4011 duplicate-import warning. Full interactive Roblox, Discord, Studio, installation, upgrade and uninstall flows have not been tested. This review is not an exhaustive correctness or security certification of every repository file.
