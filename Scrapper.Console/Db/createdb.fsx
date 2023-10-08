#r "nuget: Microsoft.Data.SQLite"
//#load "FSharp.Data.SqlProvider.dll"  // Add a reference to your SQL provider if necessary
open System
open System.Data
open Microsoft.Data.Sqlite
open System.IO
#if DEBUG
let connectionString = $"Data Source={DateTime.Now:ddMMyyyy_hhmmssfff}_Extractor.db"
#endif
#if !DEBUG
let connectionString = $"Data Source=Extractor.db"
#endif 
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

// Create a new SQLite database file
let createDatabase (connectionString: string) =
    try
        use connection = new SqliteConnection(connectionString)
        connection.Open()
        printfn "SQLite database created successfully"
    with
    | ex ->
        printfn "Error creating SQLite database: %s" ex.Message

// Specify the path to your SQL file
let sqlFilePath = "init.sql"

// Create the SQLite database
createDatabase connectionString

// Execute the SQL script from the file
executeSqlFromFile sqlFilePath (new SqliteConnection(connectionString))
