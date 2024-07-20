namespace ImoveisScrapper.Db

open System.Data
open Dapper
open System.IO
open Npgsql
open ImoveisScrapper
//#indent OFF
module Database =
    // Function to execute an SQL script from a file
    let executeSqlFromFile (filePath: string) (connection: IDbConnection) =
        try
            connection.Open()
            let script = File.ReadAllText(filePath)
            use cmd = connection.CreateCommand()
            cmd.CommandText <- script
            cmd.ExecuteNonQuery() |> ignore
            printfn "SQL script executed successfully"
        with
        | ex ->
            printfn "Error executing SQL script: %s" ex.Message
    
    let createPostgresConnectionWith (connStr:string) : IDbConnection = new NpgsqlConnection(connStr)
    let createConnectionWith (connStr:string) : IDbConnection = createPostgresConnectionWith connStr
    let createConnection () : IDbConnection = createConnectionWith Env.connStr    