//bool support added, but may not be as useful as J

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace App {
    using JSharp;
    public class Program
    {
        public static void Main(string[] args)
        {
            //System.Diagnostics.Debugger.Launch();
            if (args.Length > 0)
            {
                long kbAtExecution = GC.GetTotalMemory(false) / 1024;
                var watch = new Stopwatch();
                watch.Start();
                var ret = new Parser().parse(args[0]);
                watch.Stop();
                Console.WriteLine("Output: " + ret.ToString());
                Console.WriteLine(String.Format("Took: {0} ms", watch.ElapsedMilliseconds));
                long kbAfter1 = GC.GetTotalMemory(false) / 1024;
                long kbAfter2 = GC.GetTotalMemory(true) / 1024;

                Console.WriteLine(kbAtExecution + " Started with this kb.");
                Console.WriteLine(kbAfter1 + " After the test.");
                Console.WriteLine(kbAfter1 - kbAtExecution + " Amt. Added.");
                Console.WriteLine(kbAfter2 + " Amt. After Collection");
                Console.WriteLine(kbAfter2 - kbAfter1 + " Amt. Collected by GC.");       
            }
            new Tests().TestAll();
        }
    }
}

namespace JSharp
{
    public enum Type { Undefined, Int, String, Double, Verb, Int32, Bool };

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
        public int[] ri32;
        public string[] rs;
        public double[] rd;
        public Verb Verb;
        public bool[] rb;
        
        public A(Type type, long n, int rank=0,long[] shape=null) {
            Type = type;
            Count = n;
            if (n > 1 && rank == 0) { rank = 1; shape = new long[1]; shape[0] = n; }
            if (Type == Type.Int) { ri = new long[n]; }
            else if (Type == Type.Double) { rd = new double[n]; }
            else if (Type == Type.Int32) { ri32 = new int[n]; }
            else if (Type == Type.Bool) { rb = new bool[n]; }
            else if (Type == Type.String) { rs = new string[n]; }
            Rank = rank;
            Shape = shape;
        }

        public A(string word) {
            int val;
            double vald;
            if (word.StartsWith("'")) {
                Count = 1; //word.Length - 2;
                rs = new string[] { word.Substring(1, word.Length-2) };
                Type = Type.String;
                Rank = 0;
                Shape = new long[] { 1 };
            }
            if (word.Contains(" ") && !word.Contains(".")) {
                var longs = new List<long>();
                foreach (var part in word.Split(' ')) {
                    longs.Add(Int32.Parse(part));
                }
                Rank = 1;

                //can convert to bool
                if (longs.Where(x=>x!=0&&x!=1).Count() == 0) {
                    Type = Type.Bool;
                    rb = longs.Select(x=>System.Convert.ToBoolean(x)).ToArray();
                } else {
                    Type = Type.Int;
                    ri = longs.ToArray();
                }
                Count = longs.Count;
                Shape = new long[] { Count };


            }
            else if (word.Contains(" ") && word.Contains(".")) {
                var floats = new List<double>();
                foreach (var part in word.Split(' ')) {
                    floats.Add(Double.Parse(part));
                }
                Rank = 1;
                Type = Type.Double;
                rd = floats.ToArray();
                Count = floats.Count;
                Shape = new long[] { Count };

            }
            else if (Int32.TryParse(word, out val)) {
                Type = Type.Int;
                ri = new long[1];
                Rank = 0;
                Count = 1;
                ri[0] = val;
            }
            else if (Double.TryParse(word, out vald)) {
                Type = Type.Double;
                rd = new double[1];
                Rank = 0;
                Count = 1;
                rd[0] = vald;
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
            if (this.Type == Type.Bool) {
                for(var i = 0; i < Count; i++ ) {
                    z.ri[i] = System.Convert.ToInt32(this.rb[i]);
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
            else if (Type == Type.Double && Count == 1) {
                return rd[0].ToString();
            }
            else if (Type == Type.Int32 && Count == 1) {
                return ri32[0].ToString();
            }
            else if (Type == Type.Bool && Count == 1) {
                return (System.Convert.ToInt32(rb[0])).ToString();
            }
            else if (Type == Type.String && Count == 1) {
                return rs[0];
            }
            else if (Count > 1) {
                var z = new StringBuilder();
                long[] odometer = new long[Shape.Length];
                for(var i = 0; i < Count; i++) {
                    if (Type == Type.Int) { z.Append(ri[i]);}
                    else if (Type == Type.Double) { z.Append(rd[i]);}
                    else if (Type == Type.Int32) { z.Append(ri32[i]);}
                    else if (Type == Type.String) { z.Append(rs[i]); }
                    else if (Type == Type.Bool) { z.Append(System.Convert.ToInt32(rb[i]));}
                    
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
            A v = null; 
            v = new A(Type, 1);
            if (Type == Type.Double) { v.rd[0] = rd[i]; }
            else if (Type == Type.Int) { v.ri[0] = ri[i]; }
            else if (Type == Type.Int32) { v.ri32[0] = ri32[i]; }
            else if (Type == Type.Bool) { v.rb[0] = rb[i]; }
            return v;
        }

        //merges a list of As together
        public static A Merge(A[] y, long length, int rank, long[] shape) {
            var type = y[0].Type;
            var v = new A(type, length, rank, shape);
            for(var i = 0; i < length;i++) {
                if (type == Type.Int) { v.ri[i] = y[i].ri[0]; }
                if (type == Type.Double) { v.rd[i] = y[i].rd[0]; }
            }
            return v;
        }
    }

    public static class Verbs {

        public static long prod(long[] ri) {
            return ri.Aggregate(1L, (prod, next)=> prod*next);
        }
        
        public static A iota(A y) {
            var ct = prod(y.ri); 

            var v = new A(Type.Int, ct, y.ri.Length, y.ri.Length == 1 ? new long[] { y.ri[0] } : y.ri);
            for (var i = 0; i < ct; i++) { v.ri[i] = i; }

            //use int32 for iota
            //var v = new A(Type.Int32, ct, y.ri.Length, y.ri.Length == 1 ? new long[] { y.ri[0] } : y.ri);
            //for (var i = 0; i < ct; i++) { v.ri32[i] = i; }
            return v;
        }
        public static A shape(A y) {
            var v = new A(Type.Int, y.Rank, 1, new long[] { y.Rank });
            v.ri = y.Shape;
            return v;
        }
        public static A reshape(A x, A y) {
            var ct = prod(x.ri);
            A v = null; 
            long offset = 0;

            char[] chars = null;
            var ylen = y.Count;
            if (y.Type == Type.String) {
                chars = new char[ct];
                //System.Diagnostics.Debugger.Launch();
                ylen = y.rs[0].Length;
            } else {
                v = new A(y.Type, ct, x.Rank, x.ri);

            }
            
            for(var i = 0; i < ct; i++ ) {
                if (y.Type == Type.Int) { v.ri[i] = y.ri[offset]; }
                else if (y.Type == Type.Double) { v.rd[i] = y.rd[offset]; }
                else if (y.Type == Type.Int32) { v.ri32[i] = y.ri32[offset]; }
                else if (y.Type == Type.Bool) { v.rb[i] = y.rb[offset]; }
                else if (y.Type == Type.String) { chars[i] = y.rs[0L][(int)offset]; }
                offset++;
                if (offset > ylen-1) { offset = 0; }
            }
            if (y.Type == Type.String) {

                var size = x.ri[0];
                var len = x.ri[1];
                v = new A(y.Type, size, x.Rank, new long[] { size, 1 });
                for(var i = 0; i < size; i++) {
                    //intern string saves 4x memory in simple test and 20% slower
                    v.rs[i] = String.Intern(new String(chars, (int)(i*len), (int)len));
                }
            }
            return v;
        }
        
        public static A transpose(A y) {
            var shape = y.Shape.Reverse().ToArray();
            var v = new A(y.Type, y.Count, y.Rank, shape);
            var offsets = new long[y.Shape.Length];
            for(var i = 1; i <= y.Shape.Length-1; i++) {
                offsets[(i-1)] = y.Shape.Skip(i).Aggregate(1L, (prod, next)=> prod*next);
            }
            offsets[y.Shape.Length-1] = 1;
            offsets = offsets.Reverse().ToArray();
            var idx = 0;
            long[] odometer = new long[shape.Length];
            for(var i = 0; i < y.Count; i++) {
                var offset = 0L;
                for(var k = y.Shape.Length-1;k>=0;k--) {
                    offset = offset + (offsets[k] * odometer[k]);
                }
                v.ri[idx] = y.ri[offset];
                idx++;

                odometer[shape.Length-1]++;

                for(var k = y.Shape.Length-1;k>0;k--) {
                    if (odometer[k] == shape[k]) {
                        odometer[k] = 0;
                        odometer[k-1]++;
                    }
                }
            }

            return v;
        }
        public static A add(A x, A y) {
            return math(x,y,(a,w)=>(a+w),(a,w)=>(a+w));
        }
        public static A subtract(A x, A y) { return math(x,y,(a,w)=>(a-w),(a,w)=>(a-w)); }
        public static A multiply(A x, A y) { return math(x,y,(a,w)=>(a*w),(a,w)=>(a*w)); }
        public static A divide(A x, A y) {
            if (y.Type == Type.Int) {
                y = y.Convert(Type.Double);
            }
            return math(x,y,null,(a,w)=>(a/w));
        }
    
        public static A math(A x, A y, Func<long, long, long> intop, Func<double, double, double> doubleop) {

            Type type = Type.Undefined;

            //upcast bools
            if (y.Type == Type.Bool) {
                y = y.Convert(Type.Int);
            }
            if (x.Type == Type.Bool) {
                x = x.Convert(Type.Int);
            }
            
            if (y.Type == x.Type) { type = y.Type; }
            else if (y.Type == Type.Double || x.Type == Type.Double) { type = Type.Double; }

            if (y.Type == Type.Int32) { type = Type.Int; }

            var v = new A(type, y.Count, y.Rank, y.Shape);
            var offsetX =  (x.Count == y.Count ? 1 : 0);
            
            if (y.Type == Type.Int32)
            {
                for (var i = 0; i < y.Count; i++) { v.ri[i] = x.ri == null ? x.ri32[i*offsetX] : x.ri[i*offsetX] +  y.ri32[i]; }
            }
            else if (type == Type.Int)
            {
                for (var i = 0; i < y.Count; i++) v.SetInt(i, intop(x.AsInt(i * offsetX),y.AsInt(i)));
            }
            else if (type == Type.Double)
            {
                for (var i = 0; i < y.Count; i++) v.SetDouble(i, doubleop(x.AsDouble(i * offsetX), y.AsDouble(i)));
            }
            else
            {
                throw new ArgumentException("argument mismatch");
            }
            return v;
        }
    }

    public static class Adverbs {
        //todo special code for +/
        public static A reduce(Func<A, A, A> op, A y) {
            A v = null;
            //System.Diagnostics.Debugger.Launch();
            if (y.Rank == 1) {
                v = new A(y.Type, 1, 0, null);
                for (var i = 0; i < y.Count; i++) {
                    if (v.Rank == 0) {
                        v = op(v, y.Copy(i)); //copy the ith item for procesing
                    }
                }
            } else {
                var newShape = y.Shape.Skip(1).ToArray();
                var ct = newShape.Aggregate(1L, (prod, next) => prod * next);
            
                var va = new A[ct];
                for(var i = 0; i < ct; i++ ){
                    va[i] = new A(y.Type, 1, 0, null);
                }
                for(var i = 0; i < ct; i++) {
                    for(var k = 0; k < y.Shape[0];k++) {
                        var n = i+(k*ct);
                        va[i] = op(va[i], y.Copy(n));
                    }
                }
                //using merge because the adverb shouldn't have type-specific code
                return A.Merge(va, ct, y.Rank-1, newShape);
            }
            return v;
        }

    }
    public class Parser {
        static A makeVerb(Func<A, A> monad, Func<A, A, A> dyad) {
            A z = new A(Type.Verb, 0);
            z.Verb = new Verb { monad = monad, dyad = dyad };
            return z;
        }

        A call1(A verb, A y) {
            if (verb.Verb.adverb != null) return verb.Verb.adverb(verb.Verb.dyad, y);
            else return verb.Verb.monad(y);
        }
        A call2(A verb, A x, A y) { return verb.Verb.dyad(x, y); }

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

            bool inQuote = false;

            foreach (var c in w)
            {
                if (c == '\'' && !inQuote) {  emit(); currentWord.Append(c); inQuote=true; }
                else if (c == '\'' && inQuote) { currentWord.Append(c); emit();  inQuote = !inQuote; }
                else if (!inQuote && !Char.IsDigit(p) && c == ' ') { emit(); }
                else if (!inQuote && p == ' ' && !Char.IsDigit(c)) { emit(); currentWord.Append(c); }
                else if (!inQuote && Char.IsDigit(p) && c != ' ' && c!= '.' && !Char.IsDigit(c)) { emit(); currentWord.Append(c); }
                else if (!inQuote && c == '(' || c == ')') { emit(); currentWord.Append(c); emit(); }
                else if (!inQuote && isSymbol(p) && Char.IsLetter(c)) { emit(); currentWord.Append(c); }
                else if (!inQuote && isSymbol(p) && isSymbol(c)) { emit(); currentWord.Append(c); emit(); }
                else currentWord.Append(c);
                p = c;
            }
            emit();
            return z.ToArray();
        }

        public A parse(string cmd) {
            var adverbs = new Dictionary<string, Func<Func<A, A, A>, A, A>>();
            var verbs = new Dictionary<string, A>();


            verbs["+"] = makeVerb(null, Verbs.add);
            verbs["-"] = makeVerb(null, Verbs.subtract);
            verbs["*"] = makeVerb(null, Verbs.multiply);
            verbs["%"] = makeVerb(null, Verbs.divide);
            verbs["i."] = makeVerb(Verbs.iota, null);
            verbs["|:"] = makeVerb(Verbs.transpose, null);
            verbs["$"] = makeVerb(Verbs.shape, Verbs.reshape);
            
            adverbs["/"] = Adverbs.reduce;

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
                    if (step == 0) { //monad
                        var p1 = stack.Pop();
                        var op = stack.Pop();
                        var xt = stack.Pop();
                        var x = (xt.val == null) ? new A(xt.word) : xt.val;
                        var z = call1(op.val != null ? op.val : verbs[op.word], x);
                        stack.Push(new Token { val = z });
                        stack.Push(p1);
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
                        var p1 = stack.Pop();
                        var xt = stack.Pop();
                        var x = (xt.val == null) ? new A(xt.word) : xt.val;

                        var op = stack.Pop();
                        var yt = stack.Pop();
                        var y = (yt.val == null) ? new A(yt.word) : yt.val;
                        var z = call2(verbs[op.word], x, y);
                        stack.Push(new Token { val = z });
                        stack.Push(p1);
                    }
                    else if (step == 3) { //adverb

                        var p1 = stack.Pop();
                        var op = stack.Pop();
                        var adv = stack.Pop();
                        var y = (op.val == null) ? verbs[op.word] : op.val;
                        var z = makeVerb(y.Verb.monad, y.Verb.dyad);
                        z.Verb.adverb = adverbs[adv.word];
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

                        //try to parse word before putting on stack
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
            stack.Pop();
            var ret = stack.Pop().val;
            return ret;

        }
    }

    public class Tests {
        bool equals(string[] a1, params string[] a2) {
            return a1.OrderBy(a => a).SequenceEqual(a2.OrderBy(a => a));
        }
    
        bool equals(long[] a1, params long[] a2) {
            return a1.OrderBy(a => a).SequenceEqual(a2.OrderBy(a => a));
        }
        bool equals(double[] a1, params double[] a2) {
            return a1.OrderBy(a => a).SequenceEqual(a2.OrderBy(a => a));
        }
        bool equals(bool[] a1, params bool[] a2) {
            return a1.OrderBy(a => a).SequenceEqual(a2.OrderBy(a => a));
        }

        public void TestAll()
        {
            var j = new Parser();

            var tests = new Dictionary<string, Func<bool>>();

            Func<string, string[]> toWords = (w) => j.toWords(w);
            Func<string, A> parse = (cmd) => j.parse(cmd);
                
            tests["returns itself"] = () => equals(toWords("abc"), "abc");
            tests["parses spaces"] = () => equals(toWords("+ -"), new string[] { "+", "-" });
            tests["parses strings"] = () => equals(toWords("1 'hello world' 2"), new string[] { "1", "'hello world'", "2" });
            tests["parses strings with number"] = () => equals(toWords("1 'hello 2 world' 2"), new string[] { "1", "'hello 2 world'", "2" });

            //todo failing
            //tests["parses strings with embedded quote"] = () => equals(toWords("'hello ''this'' world'"), new string[] { "'hello 'this' world'" });
            tests["parentheses"] = () => equals(toWords("(abc)"), new string[] { "(", "abc", ")" });
            tests["parentheses2"] = () => equals(toWords("((abc))"), new string[] { "(", "(", "abc", ")", ")" });
            tests["numbers"] = () => equals(toWords("1 2 3 4"), new string[] { "1 2 3 4" });
            tests["floats"] = () => equals(toWords("1.5 2 3 4"), new string[] { "1.5 2 3 4" });
            tests["op with numbers"] = () => equals(toWords("# 1 2 3 4"), new string[] { "#", "1 2 3 4" });
            tests["op with numbers 2"] = () => equals(toWords("1 + 2"), new string[] { "1", "+", "2" });
            tests["op with no spaces"] = () => equals(toWords("1+i. 10"), new string[] { "1", "+", "i.", "10" });
            tests["adverb +/"] = () => equals(toWords("+/ 1 2 3"), new string[] { "+", "/", "1 2 3" });

            //parse tests
            tests["basic add"] = () => equals(parse("1 + 3").ri, new long[] { 4 });
            tests["basic subtract"] = () => equals(parse("5 - 3").ri, new long[] { 2 });
            tests["basic multiply"] = () => equals(parse("5 * 3").ri, new long[] { 15 });

            //tests["basic divide int"] = () => equals(parse("15 % 3").rd, new double[] { 3 });
            tests["basic divide float"] = () => equals(parse("1 % 4").rd, new double[] { 0.25 });
            tests["iota"] = () => equals(parse("i. 3").ri, new long[] { 0, 1, 2 });
            tests["adds 1 to iota"] = () => equals(parse("1 + i. 3").ri, new long[] { 1, 2, 3 });
            tests["adverb +/ with number list"] = () => equals(parse("+/ 2 2 2").ri, new long[] { 6 });
            tests["adverb +/ verb"] = () => equals(parse("+/ i. 10").ri, new long[] { 45 });

            tests["reshape bool"] = new Func<bool>(()=> equals(parse("3 $ 1 0 1").rb, new bool[] {true,false,true }));

            Func<object, object, object[]> pair = (a,w) => new object[] { a,w };
            var eqTests = new Dictionary<string, object[]>();
            eqTests["string rep number"] = pair(new A("1").ToString(),"1");
            eqTests["string rep number list"] = pair(new A("1 2 3").ToString(),"1 2 3");
            eqTests["multi-dimensional"] = pair(parse("i. 2 3").ToString(),"0 1 2\n3 4 5");
            eqTests["multi-dimensional 2"] = pair(parse("i. 2 2 2").ToString(),"0 1\n2 3\n\n4 5\n6 7");
            eqTests["multi-dimensional add "] = pair(parse("1 + i. 2 2").ToString(),"1 2\n3 4");
            eqTests["multi-dimensional sum"] = pair(parse("+/ i. 2 3").ToString(),"3 5 7");
            eqTests["multi-dimensional sum higher rank"] = pair(parse("+/ i. 2 2 2").ToString(),"4 6\n8 10");
            eqTests["multi-dimensional sum higher rank 2"] = pair(parse("+/ i. 4 3 2").ToString(),"36 40\n44 48\n52 56");
            eqTests["tranpose"] = pair(parse("|: i. 2 3").ToString(),"0 3\n1 4\n2 5");

            eqTests["reshape int"] = pair(parse("3 $ 3").ToString(),"3 3 3");
            eqTests["reshape double"] = pair(parse("3 $ 3.2").ToString(),"3.2 3.2 3.2");
            eqTests["reshape bool"] = pair(parse("3 $ 0 1").ToString(),"0 1 0");
            eqTests["upcast math that looks bool"] = pair(parse("+/ 1 1 1").ToString(),"3");
            eqTests["reshape int matrix"] = pair(parse("3 2 $ i. 3").ToString(),"0 1\n2 0\n1 2");
            eqTests["reshape string"] = pair(parse("3 2 $ 'abc'").ToString(),"ab\nca\nbc");
            //eqTests["reshape string multi"] = pair(parse("3 2 $ 3").ToString(),"3 3 3");
            
            foreach (var key in tests.Keys) {
                if (!tests[key]()) {
                    throw new ApplicationException(key);
                }
            }
            
            foreach (var key in eqTests.Keys) {
                var x=eqTests[key][0];
                var y=eqTests[key][1];
                if (x.ToString() != y.ToString()) {
                    Console.WriteLine(String.Format("{0}\n{1} != {2}", key, x.ToString(), y.ToString()));
                    //System.Diagnostics.Debugger.Launch();
                    //System.Diagnostics.Debugger.Break();

                    //throw new ApplicationException(key);
                }
            }
        }
    }
}

