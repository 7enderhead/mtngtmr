# mtngtmr
Meeting Timer to Measure Talk Times of Participants
```
Act.  Id  Name       Talk Time  Perc.
-------------------------------------
      a   Angelina   00:00:06    40%
      b   Bernardo   00:00:04    29%
--->  c   Charlotte  00:00:04    31%
          Total      00:00:15   100%
```
- based on json data files, which include participant and keyboard shortcut definitions as well as recorded meeting sessions
- `dotnet run test.json create` to create a new data file -> adapt participants / shortcuts
- `dotnet run test.json session ["session name"]` to run a new session
- press participant's id to activate her/his talk time
- press id again to stop or 'Space Bar' to stop all
- press 'Escape' to exit session