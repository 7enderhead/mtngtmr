open System
open System.Diagnostics
open Newtonsoft.Json
open CommandLine

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
    { Shortcuts: seq<Shortcut>
    ; Sessions: seq<Session> }

let jsonSettings () =
    let settings = JsonSerializerSettings()
    settings.Formatting <- Formatting.Indented
    settings

let testData () =
    let t1 = { Participant = "Reinhard"; Duration = new TimeSpan(0, 4, 30) }
    let t2 = { Participant = "Christian"; Duration = new TimeSpan(0, 2, 00) }
    let session =
        { Name = "Stand Up"
        ; Start = DateTime.Now.Subtract(new TimeSpan(0, 30, 0))
        ; End = DateTime.Now
        ; Times = seq {t1; t2} }
    let data =
        { Shortcuts = seq { {Id='r'; Name = "Reinhard"}; {Id='c'; Name = "Christian"}  }
        ; Sessions = seq { session } }
    data

[<EntryPoint>]
let main argv =
    JsonConvert.DefaultSettings <- System.Func<_>(jsonSettings)
//    
//    
//    let json = JsonConvert.SerializeObject (testData ())
//    let dataDeserialized = JsonConvert.DeserializeObject<Data> json
//    printfn "%A" dataDeserialized

    0 // return an integer exit code
