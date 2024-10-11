#r "nuget: Dapper"
#r "nuget: Npgsql"
#r "nuget: CsvHelper"

open System
open System.IO
open Dapper
open Npgsql
open CsvHelper
open CsvHelper.Configuration
open System.Globalization

// Define the Imovel type (matching the database table structure)
type Imovel = {
    link: string
    imagem: string
    descricao: string
    endereco: string
    preco: string
}

// Define database connection string (update credentials accordingly)
let connectionString = "Host=homelab-dev;Port=5455;Database=imscrapperdb;Username=imscrapper;Password=impassword"

// Function to read CSV file and return a list of Imovel records
let readCsv (filePath: string) =
  File.ReadLines(filePath)
  |> Seq.map (fun line -> line.Split([|';'|]))
  |> Seq.map (fun words -> { link = words.[0];imagem = words.[1];descricao = words.[2];endereco = words.[3];preco = words.[4]})
  |> Seq.toList 
let insertToDatabase (imoveis: Imovel list) =
  use conn = new NpgsqlConnection(connectionString)
  conn.Open()
  let insertQuery = """INSERT INTO imoveis (url, imagem_url, descricao, endereco, preco) VALUES (@link, @imagem, @descricao, @endereco, @preco)"""
  imoveis |> Seq.iter (fun imovel -> conn.Execute(insertQuery, imovel) |> ignore)
  //for imovel in imoveis do
  // conn.Execute(insertQuery, imovel) |> ignore 

  conn.Close()
let files = Directory.GetFiles("./Scrapper.Web/out/lopes/")
files 
|> Array.map (fun filePath -> 
  //let filePath = "scraped_data.csv" // Update the path to your CSV file

  try
      let imoveis = readCsv filePath
      insertToDatabase imoveis
      printfn "Data successfully inserted into the database!"
  with
      | ex -> printfn "An error occurred: %O" ex)


