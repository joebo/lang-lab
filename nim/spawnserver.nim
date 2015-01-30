# Imports are by source, so this will import threadpool.nim, net.nim etc
# from ../lib relative to the location of the nim compiler. Everything
# will be compiled together into a single statically linked executable.
import threadpool, net, os, selectors, strutils

## A trivial spawning socket server where each connecting socket is handed
## over to the threadpool for handling and response.
##
## * Uses the new "threadpool" module to spawn off the actual handling of each request
## * Uses the new high level socket module called "net" which is above "rawsockets"
## * Uses the new abstract selector from module "selectors" to do efficient polling (epoll etc)
##
## Compile using Nim "bigbreak" with:
##   nim c --threads:on --d:release spawningserver.nim
##
## Run it and throw ab on it, 100 concurrent clients:
##   ab -r -n 500000 -c 100 http://localhost:8099/
##
## On my laptop for "bytes = 100"  I get about:
##
## Requests per second:    17133.43 [#/sec] (mean)
## Time per request:       5.837 [ms] (mean)
## Time per request:       0.058 [ms] (mean, across all concurrent requests)


# Just a regular kind of "Nim class definition" - the type "Server" inheriting
# the default root object with a single instance variable "socket" of type
# Socket.
type
  Server = ref object of RootObj
    socket: Socket

# Amount of data to send
const bytes = 100
# The payload
const content = repeatStr(bytes, "x")
# And the response
const response = "HTTP/1.1 200 OK\r\LContent-Length: " & $content.len & "\r\L\r\L" & content

# This is where we perform the response on the socket.
# This proc is spawned off in its own thread on the threadpool.
proc handle(client: Socket) =
  # TaintedString is used for strings coming from the outside, security mechanism.
  # The below is equivalent to TaintedString(r"") and TaintedString is a distinct type
  # of the type string. The "r" means a raw string.
  var buf = TaintedString""
  # Using try:finally: to make sure we close the client Socket
  # even if some exception is raised
  try:
    # Just read one line... and then send our premade response 
    client.readLine(buf, timeout = 20000)
    client.send(response)
  finally:
    # We may end up here if readLine above times out for example,
    # we just ignore (no raise to propagate further) and close.
    client.close()

# Eternal loop where we use the new selectors API.
# If we get an event on the listening socket
# we create a new Socket and accept the connection
# into it. Then we spawn the handle proc.
proc loop(self: Server) =
  # Create a Selector - cross platform abstraction for polling events.
  var selector = newSelector()
  # Register our listener socket's file descriptor, the events we want to wait for
  # and an optional user object associated with this file descriptor - we just use nil
  # since we are only listening on one Socket.
  discard selector.register(self.socket.getFD, {EvRead}, nil)
  while true:
    # Ask selector to wait up to 1 second, did we actually get a connection?
    if selector.select(1000).len > 0:
      # Socket is a ref object, so "Socket()" will allocate it on the heap.
      # Perhaps a bit needless since we will deepCopy it two lines down in spawn.
      var client: Socket = Socket()
      # Or like this, its equivalent:
      #   var client: Socket
      #   new(client)
      accept(self.socket, client)
      # Spawn it off into the new threadpool - nifty stuff. It is a self adapting
      # thread pool that checks number of cores etc. The argument is deepCopied over
      # ensuring threads do not share data.
      spawn handle(client)
# We create a listening port and then call loop() which does not return
proc listen(self: Server, port: int) =
  # First we create a Socket. newSocket is a convenient proc with good
  # default values.
  self.socket = newSocket()
  # Hmmm, where is InvalidSocket defined in bigbreak?
  #if self.socket == sockets.InvalidSocket: raiseOSError(osLastError())

  # Then we bind/listen and call the loop. Whichever way we exit the try:
  # block (exception raised or a normal return) Nim will call the finally:
  # block for cleanups where we make sure to close the socket.
  try:
    self.socket.bindAddr(port = Port(port))
    self.socket.listen()
    echo("Server listening on port " & $port)
    self.loop()
  finally:
    self.socket.close()


# Only compiled when this is not used as a module
when isMainModule:
  # Type inference makes port an int
  var port = 8080
  # Type inference makes server a Server, which is
  # a "ref" to an "object", see type definition at top.
  # If you call a ref object type like this - it acts
  # like a constructor and will use new to allocate the
  # type ref'ed on the heap.
  var server = Server()
  # The listen proc takes a Server as first param
  server.listen(port)