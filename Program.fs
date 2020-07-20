open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open Newtonsoft.Json
open ConsoleTables

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
    { SchemaVersion: Version
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
    { SchemaVersion = Version(0, 3)
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

let getCursorPosition () =
    Console.CursorLeft, Console.CursorTop

let setCursorPosition (position: int * int) =
    Console.SetCursorPosition(fst position, snd position)

let stopAll (timers: Timers) =
    timers
    |> Seq.iter (fun entry -> entry.Value.Watch.Stop())

let format (span: TimeSpan) =
    span.ToString(@"hh\:mm\:ss")

let showOutput (timers: Timers) =
    let startPosition = getCursorPosition () 
    let table = ConsoleTable("Act.", "Id", "Name", "Talk Time", "Perc.")
    let totalTime = timers |> Seq.fold (fun (acc: TimeSpan) t -> acc.Add(t.Value.Watch.Elapsed)) (TimeSpan())
    timers
    |> Seq.iter
           (fun t ->
                let elapsed = t.Value.Watch.Elapsed
                table.AddRow((if t.Value.Watch.IsRunning then "--->" else String.Empty),
                             t.Key,
                             t.Value.Shortcut.Name,
                             format elapsed,
                             sprintf "%s%%" (String.Format("{0,3:0}", elapsed.Divide(totalTime) * 100.0)))
                |> ignore)
    table.AddRow(String.Empty, ' ', "Total Talk", format totalTime, "100%")
    |> (fun t-> t.Write(Format.Minimal))
    let endPosition = getCursorPosition ()
    setCursorPosition startPosition
    endPosition

let handleKeyPress (input: ConsoleKeyInfo) (timers: Timers) =
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
        elif not (Char.IsControl(c)) then
            stopAll timers
            let watch = Stopwatch()
            watch.Start()
            timers.Add(c,
                       { Shortcut =
                           { Id = c
                           ; Name = sprintf "Unknown '%c'" c }
                       ; Watch = watch })

let inputLoop (timers: Timers) =
    let mutable endPosition = showOutput timers
    let mutable input = Console.ReadKey(true)
    let mutable newKeyPress = true
    while not (input.Key = ConsoleKey.Escape) do
        if newKeyPress then
            handleKeyPress input timers
            newKeyPress <- false
        endPosition <- showOutput timers
        Threading.Thread.Sleep(300)
        if Console.KeyAvailable then
            input <- Console.ReadKey(true)
            newKeyPress <- true
    setCursorPosition endPosition
     
let getSessionData (name: string) (shortcuts: seq<Shortcut>) =
    let start = DateTime.Now
    let timers =
        shortcuts
        |> Seq.map (fun s -> s.Id, { Shortcut = s; Watch = Stopwatch() })
        |> dict
        |> (fun d -> Dictionary(d))
    inputLoop timers 
    let theEnd = DateTime.Now
    let times =
        timers
        |> Seq.filter (fun entry -> not (entry.Value.Watch.Elapsed.Equals(TimeSpan.Zero)))
        |> Seq.map (fun entry ->
            { Participant = entry.Value.Shortcut.Name
            ; Duration = entry.Value.Watch.Elapsed })
    { Name = name; Start = start; End = theEnd; Times = times}

let timeSum (times: seq<Time>) =
    times
    |> Seq.fold (fun (sum: TimeSpan) time -> sum.Add(time.Duration)) TimeSpan.Zero

let session (dataPath: string) (name: string) =
    let data = load dataPath
    let newSession = getSessionData name data.Shortcuts
    if not ((timeSum newSession.Times).Equals(TimeSpan.Zero)) then
        let newData = { data with Sessions = Seq.append data.Sessions (Seq.singleton newSession) }
        save dataPath newData

[<EntryPoint>]
let main argv =
    let visible = Console.CursorVisible
    try
        JsonConvert.DefaultSettings <- System.Func<_>(jsonSettings)
        Console.CursorVisible <- false
        let dataPath = argv.[0]
        match argv.[1] with
        | "session" -> session dataPath (if Array.length argv >= 3 then argv.[2] else String.Empty)
        | "create" -> create dataPath
        | _ -> failwith "unknown command, try 'session', 'create'"
        0
    finally
        Console.CursorVisible <- visible