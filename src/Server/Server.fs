module Server

open Microsoft.Extensions.Logging
open FSharp.Logf

open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Saturn

open Shared

module Storage =
    let todos = ResizeArray()

    let addTodo (todo: Todo) =
        if Todo.isValid todo.Description then
            todos.Add todo
            Ok()
        else
            Error "Invalid todo"

    do
        addTodo (Todo.create "Create new SAFE project")
        |> ignore

        addTodo (Todo.create "Write your app") |> ignore
        addTodo (Todo.create "Ship it !!!") |> ignore

let todosApi logger =
    { getTodos = fun () -> async { return Storage.todos |> List.ofSeq }
      addTodo =
        fun todo ->
            async {
                logfi logger "Adding Todo: %A{todo}" todo
                return
                    match Storage.addTodo todo with
                    | Ok () ->
                        logfi logger "Todo added: %A{todo}" todo
                        todo
                    | Error e ->
                        logfe logger "Error while adding Todo: %A{todo} %s{error message}" todo e
                        failwith e
            } }

let webApp logger =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue (todosApi logger)
    |> Remoting.buildHttpHandler

let app logger =
    application {
        use_router (webApp logger)
        memory_cache
        use_static "public"
        use_gzip
    }

[<EntryPoint>]
let main _ =
    let logger =
        LoggerFactory
            .Create(fun builder -> builder.AddConsole().SetMinimumLevel(LogLevel.Information) |> ignore)
            .CreateLogger()
    run (app logger)
    0