{
  description = "Playwright, .NET SDK, PowerShell, Chromium for web scrapping";

  inputs = {
    # Pull the latest nixpkgs as an input, you can specify a version or use the latest
    #TODO: pin a specific commit(if necessary)
    nixpkgs.url = "github:NixOS/nixpkgs/nixpkgs-unstable"; 
  };

  outputs = { self, nixpkgs }:
    let
      pkgs = import nixpkgs {
        #TODO: keep a specific arch ?
        system = "x86_64-linux";
      };
    in {
      devShells.default = pkgs.mkShell {
        buildInputs = [          
          pkgs.dotnetCorePackages.dotnet_9.sdk
          
          pkgs.nodejs_22
          pkgs.yarn

          pkgs.powershell
          
          (pkgs.chromium.overrideAttrs (old: {
            version = "116.0.5845.140"; 
            pname = "chromium";
          }))
        ];
        
        shellHook = ''          
          playwright install chromium
        '';
      };
    };
}
