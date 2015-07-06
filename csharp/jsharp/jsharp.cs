using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;


public class Program
{
    public static void Main(string[] args)
    {
        //System.Diagnostics.Debugger.Launch();
        new JSharp().tests();
        if (args.Length > 0)
        {
            var watch = new Stopwatch();
            watch.Start();
            var ret = new JSharp().parse(args[0]);
            watch.Stop();
            Console.WriteLine(ret.ri[0]);
            Console.WriteLine(String.Format("Took: {0} ms", watch.ElapsedMilliseconds));
        }
        //var jt = new JSharp();
        //jt.tests();

        //fail
        //Console.WriteLine(new JSharp().parse("+/ i. 3").ri[0].ToString());
        //parse("+/ (i. 1000000)").Dump();
        //parse("+/ ( 1 + i. 1000 )").Dump();
    }
}

public class JSharp
{
    public enum Type { Undefined, Int, String, Double, Verb };

    public class Verb {
        public Func<A, A> monad;
        public Func<A, A, A> dyad;
        public Func<Func<A, A, A>, A, A> adverb;
    }
    public class A {

        public Type Type;
        public long Count;
        public int Rank;
        public long[] Shape;
        public long[] ri;
        public string[] rs;
        public double[] rd;
        public Verb Verb;

        public A(Type type, long n, int rank=0,long[] shape=null) {
            Type = type;
            Count = n;
            if (n > 1 && rank == 0) { rank = 1; shape = new long[n]; }
            if (Type == Type.Int) { ri = new long[n]; }
            if (Type == Type.Double) { rd = new double[n]; }
            Rank = rank;
            Shape = shape;
        }

        public A(string word) {
            int val;

            if (word.Contains(" ")) {
                var longs = new List<long>();
                foreach (var part in word.Split(' ')) {
                    longs.Add(Int32.Parse(part));
                }
                Rank = 1;
                Type = Type.Int;
                ri = longs.ToArray();
                Count = longs.Count;
                Shape = new long[] { Count };

            }
            else if (Int32.TryParse(word, out val)) {
                Type = Type.Int;
                ri = new long[1];
                Rank = 1;
                Count = 1;
                ri[0] = val;
            }
            else {
                new A(Type.Undefined, 0);
            }
        }

        //todo add other conversions
        public A Convert(Type Type) {
            A z = new A(Type,Count);
            if (this.Type == Type.Int && Type == Type.Double) {
                for(var i = 0; i < Count; i++ ) {
                    z.rd[i] = (double) this.ri[i];
                }
            }
            return z;
        }
        public override string ToString() {
            if (Type == Type.Undefined) {
                throw new ArgumentException("Cannot convert undefined to string");
            }

            if (Type == Type.Int && Count == 1) {
                return ri[0].ToString();
            }
            var z = new StringBuilder();
            if (Type == Type.Int && Count > 1) {
                long[] odometer = new long[Shape.Length];
                for(var i = 0; i < Count; i++) {
                    z.Append(ri[i]);
                    odometer[Shape.Length-1]++;

                    if (odometer[Shape.Length-1] != Shape[Shape.Length-1]) {
                        z.Append(" ");
                    }
                    
                    for(var k = Shape.Length-1;k>0;k--) {
                        if (odometer[k] == Shape[k]) {
                            odometer[k] = 0;
                            z.Append("\n");
                            odometer[k-1]++;
                        }
                    }
                }
                var ret = z.ToString();
                ret = ret.Substring(0, ret.Length - (Shape.Length-1));
                return ret;
            }
            return "";
        }

        public A(Type type, long n, double y) : this(type, n) {
            rd[0] = y;
        }

        public long AsInt(int i) {
            if (Type == Type.Int) { return ri[i]; }
            if (Type == Type.Double) { return (int)rd[i]; }
            return 0;
        }
        public double AsDouble(int i) {
            if (Type == Type.Double) { return rd[i]; }
            if (Type == Type.Int) { return (double)ri[i]; }
            return 0;
        }
        
        public void SetInt(int i, long val) {
            ri[i] = val;
        }
        public void SetDouble(int i, double val) {
            rd[i] = val;
        }
        public A Copy(long i) {
            A v = new A(Type, 1);
            if (Type == Type.Double) { v.rd[0] = rd[i]; }
            if (Type == Type.Int) { v.ri[0] = ri[i]; }
            return v;
        }
    }

    A makeVerb(Func<A, A> monad, Func<A, A, A> dyad) {
        A z = new A(Type.Verb, 0);
        z.Verb = new Verb { monad = monad, dyad = dyad };
        return z;
    }

    A iota(A y) {
        //System.Diagnostics.Debugger.Launch();
        //System.Diagnostics.Debugger.Break();
        var ct = y.ri.Aggregate(1L, (prod, next)=> prod*next);
        var v = new A(Type.Int, ct, (int)y.ri[0], y.ri);
        for (var i = 0; i < ct; i++) { v.ri[i] = i; }
        return v;
    }

    
   
    A add(A x, A y) { return math(x,y,(a,w)=>(a+w),(a,w)=>(a+w));  }
    A subtract(A x, A y) { return math(x,y,(a,w)=>(a-w),(a,w)=>(a-w)); }
    A multiply(A x, A y) { return math(x,y,(a,w)=>(a*w),(a,w)=>(a*w)); }
    A divide(A x, A y) {
        if (y.Type == Type.Int) {
            y = y.Convert(Type.Double);
        }
        return math(x,y,null,(a,w)=>(a/w));
    }
    
    A math(A x, A y, Func<long, long, long> intop, Func<double, double, double> doubleop) {
        Type type = Type.Undefined;
        if (y.Type == x.Type) { type = y.Type; }
        if (y.Type == Type.Double || x.Type == Type.Double) { type = Type.Double; }

        var v = new A(type, y.Count);
        if (type == Type.Int)
        {
            for (var i = 0; i < y.Count; i++) v.SetInt(i, intop(x.AsInt(i * (x.Count == y.Count ? 1 : 0)),y.AsInt(i)));
        }
        else if (type == Type.Double)
        {
            for (var i = 0; i < y.Count; i++) v.SetDouble(i, doubleop(x.AsDouble(i * (x.Count == y.Count ? 1 : 0)), y.AsDouble(i)));
        }
        else
        {
            throw new ArgumentException("argument mismatch");
        }
        return v;
    }

    //todo special code for +/
    A reduce(Func<A, A, A> op, A y) {
        var v = new A(y.Type, 1);
        for (var i = 0; i < y.Count; i++) {
            v = op(v, y.Copy(i)); //copy the ith item for procesing
        }
        return v;
    }

    A call1(A verb, A y) {
        if (verb.Verb.adverb != null) return verb.Verb.adverb(verb.Verb.dyad, y);
        else return verb.Verb.monad(y);
    }
    A call2(A verb, A x, A y) { return verb.Verb.dyad(x, y); }

    A sum(A y) {
        var v = new A(y.Type, 1);
        for (var i = 0; i < y.Count; i++)
        {
            if (y.Type == Type.Int) v.ri[0] += y.ri[i];
            if (y.Type == Type.Double) v.rd[0] += y.rd[i];
        }
        return v;
    }

    public struct Token {
        public string word;
        public A val;
    }

    public string[] toWords(string w) {
        var z = new List<string>();
        var currentWord = new StringBuilder();

        //using trim is a hack
        var emit = new Action(() => { if (currentWord.Length > 0) { z.Add(currentWord.ToString().Trim()); } currentWord = new StringBuilder(); });
        char p = '\0';
        Func<char, bool> isSymbol = (c) => c == '+' || c == '/';
        foreach (var c in w)
        {
            if (!Char.IsDigit(p) && c == ' ') { emit(); }
            else if (p == ' ' && !Char.IsDigit(c)) { emit(); currentWord.Append(c); }
            else if (Char.IsDigit(p) && c != ' ' && !Char.IsDigit(c)) { emit(); currentWord.Append(c); }
            else if (c == '(' || c == ')') { emit(); currentWord.Append(c); emit(); }
            else if (isSymbol(p) && Char.IsLetter(c)) { emit(); currentWord.Append(c); }
            else if (isSymbol(p) && isSymbol(c)) { emit(); currentWord.Append(c); emit(); }
            else currentWord.Append(c);
            p = c;
        }
        emit();
        return z.ToArray();
    }

    bool equals(string[] a1, params string[] a2) {
        return a1.OrderBy(a => a).SequenceEqual(a2.OrderBy(a => a));
    }
    
    bool equals(long[] a1, params long[] a2) {
        return a1.OrderBy(a => a).SequenceEqual(a2.OrderBy(a => a));
    }
    bool equals(double[] a1, params double[] a2) {
        return a1.OrderBy(a => a).SequenceEqual(a2.OrderBy(a => a));
    }

    public A parse(string cmd) {
        var adverbs = new Dictionary<string, Func<Func<A, A, A>, A, A>>();
        var verbs = new Dictionary<string, A>();


        verbs["+"] = makeVerb(null, add);
        verbs["-"] = makeVerb(null, subtract);
        verbs["*"] = makeVerb(null, multiply);
        verbs["%"] = makeVerb(null, divide);
        verbs["i."] = makeVerb(iota, null);
        verbs["sum"] = makeVerb(iota, add);

        adverbs["/"] = reduce;

        var MARKER = "`";
        cmd = MARKER + " " + cmd;

        Func<Token, bool> isEdge = (token) => token.word == MARKER || token.word == "=:" || token.word == "(";
        Func<Token, bool> isVerb = (token) => (token.val != null && token.val.Verb != null) || (token.word != null && verbs.ContainsKey(token.word));
        Func<Token, bool> isAdverb = (token) => token.word != null && adverbs.ContainsKey(token.word);
        Func<Token, bool> isNoun = (token) => token.val != null && token.val.Type != Type.Verb;
        Func<Token, bool> isEdgeOrNotConj = (token) => isEdge(token) || isVerb(token) || isNoun(token) || token.word == "";


        var words = toWords(cmd);


        var stack = new Stack<Token>();
        var queue = new Queue<Token>();
        for (var k = words.Length - 1; k >= 0; k--) {
            queue.Enqueue(new Token { word = words[k] });
        }
        int i = 0;
        //just a safety check for now
        while (i < 100) {
            var sarr = stack.ToArray().ToList();
            var w1 = sarr.Count > 0 ? sarr[0] : new Token { word = "" };
            var w2 = sarr.Count > 1 ? sarr[1] : new Token { word = "" };
            var w3 = sarr.Count > 2 ? sarr[2] : new Token { word = "" };
            var w4 = sarr.Count > 3 ? sarr[3] : new Token { word = "" };
            //new Token[] { w1,w2,w3,w4 }.Dump();

            var step = -1;
            if (isEdge(w1) && isVerb(w2) && isNoun(w3) && true) { step = 0; }
            else if (isEdgeOrNotConj(w1) && isVerb(w2) && isVerb(w3) && isNoun(w4)) { step = 1; }
            else if (isEdgeOrNotConj(w1) && isNoun(w2) && isVerb(w3) && isNoun(w4)) { step = 2; }
            else if (isEdgeOrNotConj(w1) && (isNoun(w2) || isVerb(w2)) && isAdverb(w3) && true) { step = 3; } //adverb
            else if (w1.word == "(" && isNoun(w2) && w3.word == ")" && true) { step = 8; }

            if (step >= 0) {
                //Console.WriteLine("STEP: " + step);
                if (step == 0) { //monad
                    var p1 = stack.Pop();
                    var op = stack.Pop();
                    var xt = stack.Pop();
                    var x = (xt.val == null) ? new A(xt.word) : xt.val;
                    var z = call1(op.val != null ? op.val : verbs[op.word], x);
                    stack.Push(new Token { val = z });
                    stack.Push(p1);
                    //Console.WriteLine("A STACK: " + String.Join("",stack.ToArray()));
                    //stack.Push(top);
                }
                else if (step == 1) {   //monad                         
                    var p1 = stack.Pop();
                    var p2 = stack.Pop();
                    var op = stack.Pop();
                    var xt = stack.Pop();
                    var x = (xt.val == null) ? new A(xt.word) : xt.val;
                    var z = call1(verbs[op.word], x);
                    stack.Push(new Token { val = z });
                    stack.Push(p2);
                    stack.Push(p1);
                    
                }
                else if (step == 2) { //dyad
                    //Console.WriteLine("STACK B: " + String.Join("",stack.ToArray()));                                 
                    var p1 = stack.Pop();
                    var xt = stack.Pop();
                    var x = (xt.val == null) ? new A(xt.word) : xt.val;

                    var op = stack.Pop();
                    var yt = stack.Pop();
                    var y = (yt.val == null) ? new A(yt.word) : yt.val;
                    var z = call2(verbs[op.word], x, y);
                    stack.Push(new Token { val = z });
                    stack.Push(p1);
                    //Console.WriteLine("STACK A: " + String.Join("",stack.ToArray()));
                }
                else if (step == 3) { //adverb
                    //todo: adverb should not evaluate, but add a new verb to stack
                    //stack.Dump();
                    var p1 = stack.Pop();
                    var op = stack.Pop();
                    var adv = stack.Pop();
                    //var yt = stack.Pop();
                    var y = (op.val == null) ? verbs[op.word] : op.val;
                    var z = makeVerb(y.Verb.monad, y.Verb.dyad);
                    z.Verb.adverb = adverbs[adv.word];
                    //var z = adverbs[adv.word](dyads[op.word],y);
                    //stack.Push(new Token { val=y });
                    stack.Push(new Token { val = z });
                    stack.Push(p1);
                }
                else if (step == 8) {
                    var lpar = stack.Pop();
                    var x = stack.Pop();
                    var rpar = stack.Pop();
                    stack.Push(x);
                }
            }
            else {
                if (queue.Count() != 0) {
                    var newWord = queue.Dequeue();
                    var val = new A(newWord.word);
                    var token = new Token();
                    if (val.Type == Type.Undefined) {
                        token.word = newWord.word;
                    }
                    else {
                        token.val = val;
                    }
                    stack.Push(token);
                }
                if (queue.Count() == 0 && (stack.Count() == 1 || stack.Count() == 2)) {
                    //Console.WriteLine("DONE");
                    break;
                }
            }
            i++;

        }
        //stack.Dump();
        stack.Pop();
        var ret = stack.Pop().val;
        return ret;

    }

    public void tests()
    {
        var tests = new Dictionary<string, Func<bool>>();
        tests["returns itself"] = () => equals(toWords("abc"), "abc");
        tests["parses spaces"] = () => equals(toWords("+ -"), new string[] { "+", "-" });
        tests["parentheses"] = () => equals(toWords("(abc)"), new string[] { "(", "abc", ")" });
        tests["parentheses2"] = () => equals(toWords("((abc))"), new string[] { "(", "(", "abc", ")", ")" });
        tests["numbers"] = () => equals(toWords("1 2 3 4"), new string[] { "1 2 3 4" });
        tests["op with numbers"] = () => equals(toWords("# 1 2 3 4"), new string[] { "#", "1 2 3 4" });
        tests["op with numbers 2"] = () => equals(toWords("1 + 2"), new string[] { "1", "+", "2" });
        tests["op with no spaces"] = () => equals(toWords("1+i. 10"), new string[] { "1", "+", "i.", "10" });
        tests["adverb +/"] = () => equals(toWords("+/ 1 2 3"), new string[] { "+", "/", "1 2 3" });

        //parse tests
        tests["basic add"] = () => equals(parse("1 + 3").ri, new long[] { 4 });
        tests["basic subtract"] = () => equals(parse("5 - 3").ri, new long[] { 2 });
        tests["basic multiply"] = () => equals(parse("5 * 3").ri, new long[] { 15 });
        //tests["basic divide"] = () => equals(parse("15 % 3").ri, new long[] { 5 });
        tests["basic divide"] = () => equals(parse("1 % 4").rd, new double[] { 0.25 });
        tests["iota"] = () => equals(parse("i. 3").ri, new long[] { 0, 1, 2 });
        tests["adds 1 to iota"] = () => equals(parse("1 + i. 3").ri, new long[] { 1, 2, 3 });
        tests["adverb +/ with number list"] = () => equals(parse("+/ 2 2 2").ri, new long[] { 6 });
        tests["adverb +/ verb"] = () => equals(parse("+/ i. 10").ri, new long[] { 45 });
        //tests["adverb +/"] = () => equals(parse("+/ i. 4").ri,new long[] { 6 });

        foreach (var key in tests.Keys) {
            if (!tests[key]()) {
                throw new ApplicationException(key);
            }
        }
        Func<object, object, object[]> pair = (a,w) => new object[] { a,w };
        var eqTests = new Dictionary<string, object[]>();
        eqTests["string rep number"] = pair(new A("1").ToString(),"1");
        eqTests["string rep number list"] = pair(new A("1 2 3").ToString(),"1 2 3");
        eqTests["multi-dimensional"] = pair(parse("i. 2 3").ToString(),"0 1 2\n3 4 5");
        eqTests["multi-dimensional 2"] = pair(parse("i. 2 2 2").ToString(),"0 1\n2 3\n\n4 5\n6 7");
        foreach (var key in eqTests.Keys) {
            var x=eqTests[key][0];
            var y=eqTests[key][1];
            if (x.ToString() != y.ToString()) {
                Console.WriteLine(String.Format("{0}\n{1} != {2}", key, x.ToString(), y.ToString()));
                System.Diagnostics.Debugger.Launch();
                System.Diagnostics.Debugger.Break();

                throw new ApplicationException(key);
            }
        }
    }
}

//new JSharp().tests();
