Nix flake for InventoryPredictor (.NET 8 MAUI + Blazor Hybrid)
=============================================

This flake provides a development shell with a .NET 8 SDK and simple build targets
for the API and the MAUI Blazor project.

Files added

- `flake.nix` - Nix flake that exposes:
  - `devShells.default` — interactive shell with dotnet SDK
  - `packages.api` — a built/published output for `src/InventoryPredictor.Api`
  - `packages.maui` — a built/published output for `src/InventoryPredictor.MauiBlazor`

Quick start

1. Enter the dev shell:

```bash
nix develop
```

Inside the dev shell you can run dotnet (the flake tries to expose the SDK binary path):

```bash
${dotnet:-dotnet} --version
```

2. Build the API (from repo root):

```bash
${dotnet:-dotnet} build src/InventoryPredictor.Api
```

3. Publish the API (release):

```bash
${dotnet:-dotnet} publish -c Release -f net8.0 src/InventoryPredictor.Api
```

Caveats for the MAUI project

- Building/publishing MAUI apps is platform-specific. Targets like `net8.0-android`,
  `net8.0-ios`, and `net8.0-windows10.0.19041.0` require additional SDKs (Android SDK/NDK,
  Xcode for iOS, Windows SDK for Windows) and dotnet workloads installed.
- The flake includes a generic attempt to `dotnet publish -f net8.0` for the MAUI project,
  which may be sufficient for some CI scenarios but on many hosts you'll need to pass a
  platform-specific `-f` and ensure the host has the required native SDKs available.

Customizing the flake

- If `nixpkgs` in your environment exposes a different attribute name for the .NET 8
  SDK (for example `dotnet-sdk-8` or `dotnet-sdk`), update the `dotnet = pkgs.dotnet-sdk_8;`
  line in `flake.nix` accordingly.

Notes

- The flake is intentionally minimal to get you started. If you want fully reproducible
  builds for Android/iOS you should extend the flake to provide the Android SDK/NDK
  packages and set up required environment variables used by MSBuild and the
  MAUI workloads.
