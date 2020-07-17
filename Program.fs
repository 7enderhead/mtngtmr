open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open Newtonsoft.Json

type Shortcut =
    { Id: char
    ; Name: string }

type Time =
    { Participant: string
    ; Duration: TimeSpan }

type Session =
    { Name: string
    ; Start: DateTime
    ; End: DateTime
    ; Times: seq<Time> }

type Data =
    { Version: int
    ; Shortcuts: seq<Shortcut>
    ; Sessions: seq<Session> }

let jsonSettings () =
    let settings = JsonSerializerSettings()
    settings.Formatting <- Formatting.Indented
    settings

let defaultData  =
    { Version = 1
    ; Shortcuts = seq { {Id='a'; Name = "Angelina"}; {Id='b'; Name = "Bernardo"}  }
    ; Sessions = Seq.empty }

let load (dataPath: string) =
    let json = File.ReadAllText(dataPath)
    JsonConvert.DeserializeObject<Data> json

let save (dataPath: string) (data: Data) =
    let json = JsonConvert.SerializeObject data
    File.WriteAllText(dataPath, json)

let create (dataPath: string) =
    if File.Exists(dataPath) then
        failwith (sprintf "data file %s already exists" dataPath)
    else save dataPath defaultData

let participantFromId (shortcuts: seq<Shortcut>) (id: char) =
    shortcuts
    |> Seq.find (fun s -> s.Id = id)
    |> (fun s -> s.Name)

let rec inputLoop (watches: IDictionary<char, Stopwatch>) =
    let mutable input = Console.ReadKey(true)
    while not (input.Key = ConsoleKey.Escape) do
        for entry in watches do
            entry.Value.Stop()
        let c = input.KeyChar
        if watches.ContainsKey(c) then
            let watch = watches.Item(c)
            if watch.IsRunning then
                watch.Stop()
                printfn "stopped %c" c
            else
                watch.Start()
                printfn "started %c" c
        else
        input <- Console.ReadKey(true)

let printShortcuts (shortcuts: seq<Shortcut>) =
    shortcuts
    |> Seq.iter (fun s -> printfn "%c: %s" s.Id s.Name)

let getSessionData (name: string) (shortcuts: seq<Shortcut>) =
    let start = DateTime.Now
    
    let watches =
        shortcuts
        |> Seq.map (fun s -> s.Id, Stopwatch())
        |> dict
    
    inputLoop watches 
    
    let theEnd = DateTime.Now
    
    let times =
        watches
        |> Seq.map (fun pair ->
            { Participant = (participantFromId shortcuts pair.Key)
            ; Duration = pair.Value.Elapsed })
        
    { Name = name
    ; Start = start
    ; End = theEnd
    ; Times = times}

let session (dataPath: string) (name: string) =
    let data = load dataPath
    printShortcuts data.Shortcuts
    let newData = { data with Sessions = Seq.append data.Sessions (Seq.singleton (getSessionData name data.Shortcuts)) }
    save dataPath newData

[<EntryPoint>]
let main argv =
    JsonConvert.DefaultSettings <- System.Func<_>(jsonSettings)
    
    let dataPath = argv.[0]
    match argv.[1] with
    | "session" -> session dataPath argv.[2]
    | "create" -> create dataPath
    | _ -> failwith "unknown command, try 'session', 'create'"

    0
