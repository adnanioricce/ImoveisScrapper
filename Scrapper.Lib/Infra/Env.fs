namespace ImoveisScrapper

open System

module Env =
    let webEngine = Environment.GetEnvironmentVariable("IMOVEISSCRAPPER_WEBENGINE_PATH")
    let connStr = Environment.GetEnvironmentVariable("IMOVEISSCRAPPER_CONNECTION_STRING")

