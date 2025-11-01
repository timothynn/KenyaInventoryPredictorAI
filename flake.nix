{
  description = "Nix flake for InventoryPredictor .NET MAUI + Blazor Hybrid application";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs = { self, nixpkgs, flake-utils, ... }:
    flake-utils.lib.eachDefaultSystem (system:
      let
        pkgs = import nixpkgs { inherit system; };
        src = ./.;
        # Use the .NET 8 SDK available in nixpkgs. If your channel uses a different
        # attribute name (eg. dotnet-sdk_8 or dotnet-sdk-8), update this line.
        dotnet = pkgs.dotnet-sdk;

        buildDotnetProject = { name, projectPath, publishArgs ? "" }:
          pkgs.runCommand name { inherit dotnet; } ''
            export DOTNET_CLI_TELEMETRY_OPTOUT=1
            mkdir -p $TMPDIR/src
            cp -r ${src}/* $TMPDIR/src/
            cd $TMPDIR/src/${projectPath}
            # restore workloads and packages, then publish
            ${dotnet}/bin/dotnet restore
            ${dotnet}/bin/dotnet publish -c Release ${publishArgs}
            mkdir -p $out
            cp -r bin/Release/* $out/
          '';

      in {
        packages = {
          # Buildable packages for each supported system
          api = buildDotnetProject {
            name = "inventorypredictor-api";
            projectPath = "src/InventoryPredictor.Api";
            publishArgs = "-f net8.0";
          };

          maui = buildDotnetProject {
            name = "inventorypredictor-maui";
            projectPath = "src/InventoryPredictor.MauiBlazor";
            # Note: MAUI targets are platform-specific (android/ios/windows). This
            # attempt publishes the default targets; you may need to pass a
            # platform-specific -f (eg. net8.0-android) when building on a host
            # with appropriate SDKs installed.
            publishArgs = "-f net8.0";
          };
        };

        devShells = {
          default = pkgs.mkShell {
            buildInputs = [ dotnet pkgs.git pkgs.unzip pkgs.zip pkgs.cacert pkgs.jq ];
            shellHook = ''
              export DOTNET_CLI_TELEMETRY_OPTOUT=1
              echo "Dev shell: dotnet available at ${dotnet}/bin/dotnet"
              echo "Use: ${dotnet}/bin/dotnet build src/InventoryPredictor.Api"
            '';
          };
        };
      }
    );
}
