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
    
    let createPostgresConnectionWith (connStr:string) : IDbConnection =
        new NpgsqlConnection(connStr)
    let createConnectionWith (connStr:string) : IDbConnection =
        createPostgresConnectionWith connStr
    let createConnection () : IDbConnection =        
        createConnectionWith Env.connStr
        
module Imoveis =
    [<Struct>]
    [<CLIMutable>]
    type ImovelDto = {
        QuantidadeBanheiros: int
        QuantidadeQuartos: int
        QuantidadeVagas: int
        Preco: decimal
        Titulo: string
        Endereco: string
        Adicionais: string
        Status: string
        Images: string array
    }
    let insert (dto:ImovelDto seq) (conn:IDbConnection) = async {
        try
            let query = @"
            INSERT INTO Imoveis(QuantidadeBanheiros,QuantidadeQuartos,QuantidadeVagas,Preco,Titulo,Endereco,Adicionais,Status)
	        VALUES (@QuantidadeBanheiros,@QuantidadeQuartos,@QuantidadeVagas,@Preco,@Titulo,@Endereco,@Adicionais,@Status)"
            let! res = conn.ExecuteAsync(query,dto) |> Async.AwaitTask
            return Result.Ok res
        with
            | ex -> return Result.Error ex
    }   
    let get (offset:int) (limit:int) (conn:IDbConnection) = async {
        try
            let query = @"
            SELECT * FROM Imoveis
            LIMIT @Limit
            OFFSET @Offset"
            let! res = conn.QueryAsync<ImovelDto>(query,{| Offset = offset;Limit = limit |}) |> Async.AwaitTask
            return Result.Ok res
        with
            | ex -> return Result.Error ex
    }
    