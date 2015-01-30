import asyncdispatch, asynchttpserver
  
var server = newAsyncHttpServer()
proc cb(req: Request) {.async.} =
  await req.respond(Http200, "Hello World")

asyncCheck server.serve(Port(8080), cb)
runForever()
