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

type Timer =
    { Shortcut: Shortcut
    ; Watch: Stopwatch }

type Timers = IDictionary<char, Timer>

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



let stopAll (timers: Timers) =
    timers
    |> Seq.iter (fun entry -> entry.Value.Watch.Stop())

let showOutput (timers: Timers) =
    let left = Console.CursorLeft
    let top = Console.CursorTop
    
    timers
    |> Seq.iter
           (fun t ->
                printfn "%s %c - %s: %s"
                    (if t.Value.Watch.IsRunning then "==>" else "   ")
                    t.Key
                    t.Value.Shortcut.Name
                    (t.Value.Watch.Elapsed.ToString(@"hh\:mm\:ss")))
           
    Console.SetCursorPosition(left, top)

let inputLoop (timers: Timers) =
    let originalPosition = Console.CursorLeft, Console.CursorTop
    showOutput timers
    let mutable input = Console.ReadKey(true)
    let mutable newKeyPress = true
    while not (input.Key = ConsoleKey.Escape) do
        if newKeyPress then
            if input.Key = ConsoleKey.Spacebar then
                stopAll timers
            else
                let c = input.KeyChar
                if timers.ContainsKey(c) then
                    let watch = timers.Item(c).Watch
                    if watch.IsRunning then
                        watch.Stop()
                    else
                        stopAll timers
                        watch.Start()
            newKeyPress <- false
            
        showOutput timers
        System.Threading.Thread.Sleep(500)
        
        if Console.KeyAvailable then
            input <- Console.ReadKey(true)
            newKeyPress <- true
     
    Console.SetCursorPosition(fst originalPosition, snd originalPosition)
     
let getSessionData (name: string) (shortcuts: seq<Shortcut>) =
    let start = DateTime.Now
    
    let timers =
        shortcuts
        |> Seq.map (fun s -> s.Id, { Shortcut = s; Watch = Stopwatch() })
        |> dict
    
    inputLoop timers 
    
    let theEnd = DateTime.Now
    
    let times =
        timers
        |> Seq.map (fun entry ->
            { Participant = entry.Value.Shortcut.Name
            ; Duration = entry.Value.Watch.Elapsed })
        
    { Name = name
    ; Start = start
    ; End = theEnd
    ; Times = times}

let session (dataPath: string) (name: string) =
    let data = load dataPath
    let newData = { data with Sessions = Seq.append data.Sessions (Seq.singleton (getSessionData name data.Shortcuts)) }
    save dataPath newData

[<EntryPoint>]
let main argv =
    JsonConvert.DefaultSettings <- System.Func<_>(jsonSettings)
    let visible = Console.CursorVisible
    Console.CursorVisible <- false
    
    let dataPath = argv.[0]
    match argv.[1] with
    | "session" -> session dataPath argv.[2]
    | "create" -> create dataPath
    | _ -> failwith "unknown command, try 'session', 'create'"

    Console.CursorVisible <- visible
    
    0
