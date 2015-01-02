import os, seqUtils, unittest

proc getDLLName: string = 
  result = "c:/users/joe/j803/bin/j.dll" 

proc JInit(): int {.stdcall, importc, dynlib: getDLLName().}
proc JDo(j:int,cmd:cstring): int {.stdcall, importc, dynlib: getDLLName().}
proc JGetM(j:int,name:cstring, t: ptr int, r: ptr int, s: ptr ptr int, d: ptr int):int {.stdcall, importc, dynlib: getDLLName().}
proc JSetM(j:int,name:cstring, t: ptr int, r: ptr int, s: ptr ptr int, d: ptr int):int {.stdcall, importc, dynlib: getDLLName().}

var jt = JInit()

proc getShape(r: int, sptr: ptr int): seq[int] =
  toSeq(0..r-1).mapIt(int, cast[ptr int](cast[int](sptr) + it * sizeof(int))[])

proc getData[T](t: int, r: int, sptr: ptr int, d: int): seq[seq[T]] =
    var result: seq[seq[T]] = @[]
    var shapes = getShape(r, sptr)
    if shapes.len == 0:
      var row : seq[T] = @[]
      row.add(cast[ptr T](d)[])
      result.add(row)
    else:
      for s in shapes:
        var item = toSeq(0..s-1).mapIt(T, cast[ptr T](cast[int](d) + it * sizeof(T))[])
        result.add(item)
    result

suite "calling J":
  setup:
    jt = JInit()
  test "calling JDo":
    check:
      0 == JDo(jt, "a=:i.5")

  test "calling JGetM with an array":
    var t,r,d, ret: int
    var sptr: ptr int
    ret = JDo(jt, "a=:i.5")
    ret = JGetM(jt, "a", addr(t), addr(r), addr(sptr), addr(d))
    check:
      0 == ret
      t == 4
      r == 1
      getShape(r,sptr) == @[5]
      getData[int](t, r, sptr, d) == @[@[0,1,2,3,4]]

  test "calling JSetM with an array":
    var t,r,d, ret: int
    var sptr: ptr int
    t = 4
    r = 1
    var shape = [5]
    sptr = addr(shape[0])
    var data = [4,3,2,1,0]
    var dataptr:int = cast[int](addr(data[0]))
    ret = JSetM(jt, "b", addr(t), addr(r), addr(sptr), addr(dataptr))
    ret = JGetM(jt, "b", addr(t), addr(r), addr(sptr), addr(d))
    check:
      0 == ret
      getData[int](t, r, sptr, d) == @[@[4,3,2,1,0]]

  test "calling JGetM with an atom":
    var t,r,d, ret: int
    var sptr: ptr int
    ret = JDo(jt, "a=:123")
    ret = JGetM(jt, "a", addr(t), addr(r), addr(sptr), addr(d))
    check:
      0 == ret
      t == 4
      r == 0
      getShape(r,sptr).len == 0
      getData[int](t, r, sptr, d) == @[@[123]]

  test "calling JGetM with a string":
    var t,r,d, ret: int
    var sptr: ptr int
    ret = JDo(jt, "a=:'abc'")
    ret = JGetM(jt, "a", addr(t), addr(r), addr(sptr), addr(d))
    check:
      0 == ret
      t == 2
      r == 1
      getShape(r,sptr) == @[3]
      getData[char](t, r, sptr, d) == @[@['a','b','c']]

  test "calling JGetM with a character":
    var t,r,d, ret: int
    var sptr: ptr int
    ret = JDo(jt, "a=:'a'")
    ret = JGetM(jt, "a", addr(t), addr(r), addr(sptr), addr(d))
    check:
      0 == ret
      getShape(r,sptr).len == 0
      getData[char](t, r, sptr, d) == @[@['a']]
