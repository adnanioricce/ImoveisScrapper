namespace ImoveisScrapper

open System
open System.IO
open Microsoft.Extensions.Configuration
module Env =
    // let deserializeConfig (appsettingsPath: string) =
    //     //File.ReadAllText(appsettingsPath)
    //     let configBuilder = Configuration
    let mutable private _config:IConfiguration = null
    let init (config:IConfiguration) =
        _config <- config
    let webEngine = 
        let path = Environment.GetEnvironmentVariable("IMOVEISSCRAPPER_WEBENGINE_PATH")
        if String.IsNullOrWhiteSpace(path) then
            "/usr/local/bin/chromium"
        else 
            path            
    let connStr = Environment.GetEnvironmentVariable("IMOVEISSCRAPPER_CONNECTION_STRING")

