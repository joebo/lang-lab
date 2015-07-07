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
    public abstract class AType
    {
        public long[] Shape;
        public int Rank { get { return Shape.Length; } }


        public static AType MakeA(string word)  {
            int val;
            double vald;
            if (word.StartsWith("'")) {
                var str = word.Substring(1, word.Length-2);
                var a = new A<JString>(1);
                a.Ravel[0] = new JString { str = str };
                return a;
            }
            if (word.Contains(" ") && !word.Contains(".")) {
                var longs = new List<long>();
                foreach (var part in word.Split(' ')) {
                    longs.Add(Int32.Parse(part));
                }
                var a = new A<long>(longs.Count);
                a.Ravel = longs.ToArray();
                return a;
            }
            else if (Int32.TryParse(word, out val)) {
                A<long> a = new A<long>(1);
                a.Ravel[0] = val;
                return a;
            }
            else if (Double.TryParse(word, out vald)) {
                var a = new A<double>(1);
                a.Ravel[0] = vald;
                return a;
            }
            else if (Verbs.Words.Contains(word)) {
                var a = new A<Verb>(1);
                a.Ravel[0] = new Verb { op = word };
                return a;
            }
            return new A<Undefined>(0);
        }
    }

    public struct Undefined { }
    
    public struct Verb {
        public string op;
        public string adverb;
    }

    //tbd should we use chars instead?
    public struct JString {
        public string str;
        public override string ToString() {
            return str;
        }
    }
    public class A<T> : AType where T : struct {

        public T[] Ravel;
        public long Count { get { return Ravel.Length; } }

        public A(long n) {
            Ravel = new T[n];
            if (n > 0) {
                Shape = new long[] { n };
            }
        }

        public A(long n, long[] shape ) {
            Ravel = new T[n];
            Shape = shape;
        }
        
        public void Set(int n, T val) {
            Ravel[n] = val;
        }
        public override string ToString() {
            if (Ravel.Length == 1) {
                return Ravel[0].ToString();
            } else {
                var z = new StringBuilder();
                long[] odometer = new long[Rank];
                for(var i = 0; i < Count; i++) {
                    z.Append(Ravel[i].ToString());
                    odometer[Rank-1]++;

                    if (odometer[Rank-1] != Shape[Rank-1]) {
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
        }
    }

    public class Adverbs {
        public static string[] Words = new string[] { "/" };

        //todo move to utility
        public static long prod(long[] ri) {
            return ri.Aggregate(1L, (prod, next)=> prod*next);
        }

        public static A<T> reduce<T>(AType op, A<T> y) where T : struct {
            if (y.Rank == 1) {
                var v = new A<T>(1);
                for (var i = 0; i < y.Count; i++) {
                    var yi = new A<T>(1);
                    yi.Ravel[0] = y.Ravel[i];
                    v = (A<T>)Verbs.Call2(op, v, yi); //copy the ith item for procesing
                }
                return v;
            } else {
                var newShape = y.Shape.Skip(1).ToArray();
                var ct = prod(newShape);
                
                var v = new A<T>(ct);
                for(var i = 0; i < ct; i++) {
                    for(var k = 0; k < y.Shape[0];k++) {
                        var n = i+(k*ct);

                        var yi = new A<T>(1);
                        yi.Ravel[0] = y.Ravel[n];

                        var vi = new A<T>(1);
                        vi.Ravel[0] = v.Ravel[i];

                        v.Ravel[i] = ((A<T>)Verbs.Call2(op, vi, yi)).Ravel[0]; //copy the ith item for procesing
                    }
                }
                return v;
            }
            
            throw new NotImplementedException();
        }

        public static AType Call1(AType verb, AType y)  {
            if (y.GetType() == typeof(A<long>)) {
                return reduce<long>(verb, (A<long>)y);
            }
            throw new NotImplementedException();
        }
    }
    
    public class Verbs {

        public static string[] Words = new string[] { "+", "i.", "$" };
        
        public static A<long> iota<T>(A<T> y) where T : struct  {
            var shape = y.Ravel.Cast<long>().ToArray();
            long k = prod(shape);
            var z = new A<long>(k);
            if (y.Rank > 0) { z.Shape = shape; }
            for(var i = 0; i < k; i++) {
                z.Ravel[i] = i;
            }
            return z;
        }
        
        static T Add<T, T2>(T a, T2 b) {
            return (T) ((dynamic)a+((T)(dynamic)b));
        }
        
        public static A<long> addi(A<long> x, A<long> y) { 
            var z = new A<long>(y.Ravel.Length);
            for(var i = 0; i < y.Ravel.Length; i++) {                   
                z.Ravel[i] = x.Ravel[0] + y.Ravel[i];
            }
            return z;
        }
        public static A<double> addd<T,T2>(A<T> x, A<T2> y)  where T : struct where T2 : struct { 
            var z = new A<double>(y.Ravel.Length);
            for(var i = 0; i < y.Ravel.Length; i++) {                   
                z.Set(i, Convert.ToDouble(Add(x.Ravel[0], y.Ravel[i])));
            }
            return (A<double>)z;
        }

        public static A<long> shape(AType y) {
            var v = new A<long>(y.Rank);
            v.Ravel = y.Shape;
            return v;
        }

        public static long prod(long[] ri) {
            return ri.Aggregate(1L, (prod, next)=> prod*next);
        }

        public static A<T2> reshape<T2>(A<long> x, A<T2> y) where T2 : struct {

            var ct = prod(x.Ravel);
            long offset = 0;
            var ylen = y.Count;
            var v = new A<T2>((int)ct, x.Ravel);
            for(var i = 0; i < ct; i++ ) {
                v.Ravel[i] = y.Ravel[offset];
                offset++;
                if (offset > ylen-1) { offset = 0; }
            }
            return v;
        }

        public static A<JString> reshape_str(A<long> x, A<JString> y) {
            var ct = prod(x.Ravel);
            long offset = 0;
            var ylen = y.Count;
            
            char[] chars = new char[ct];
            ylen = y.Ravel[0].str.Length;
            
            for(var i = 0; i < ct; i++ ) {
                chars[i] = y.Ravel[0].str[(int)offset];
                offset++;
                if (offset > ylen-1) { offset = 0; }
            }
            var size = x.Ravel[0];
            var len = x.Ravel[1];
            var v = new A<JString>(size, new long[] { size, 1 });
            for(var i = 0; i < size; i++) {
                //intern string saves 4x memory in simple test and 20% slower
                v.Ravel[i].str =  String.Intern(new String(chars, (int)(i*len), (int)len));
                //v.Ravel[i].str =  new String(chars, (int)(i*len), (int)len);
            }
            
            return v;
        }

        public static AType Call2(AType method, AType x, AType y)  {
            var op = ((A<Verb>) method).Ravel[0].op;
            if (op == "+") {
                if (x.GetType() == typeof(A<long>) && y.GetType() == typeof(A<long>)) { 
                    return Verbs.addi((A<long>)x,(A<long>)y);
                }
                else if (x.GetType() == typeof(A<double>) && y.GetType() == typeof(A<double>)) { 
                    return Verbs.addd((A<double>)x,(A<double>)y);
                }
            } else if (op == "$") {
                if (x.GetType() == typeof(A<long>)) {
                    if (y.GetType() == typeof(A<long>)) {
                        return Verbs.reshape((A<long>)x,(A<long>)y);
                    }
                    else if (y.GetType() == typeof(A<double>)) {
                        return Verbs.reshape((A<long>)x,(A<double>)y);
                    } else if (y.GetType() == typeof(A<JString>)) {
                        return Verbs.reshape_str((A<long>)x,(A<JString>)y);
                    }
                }
            }
            throw new ArgumentException();
        }

        public static AType Call1(AType method, AType y)  {
            var verb = ((A<Verb>) method).Ravel[0];
            if (verb.adverb != null) {
                return Adverbs.Call1(method, y);
            }
            var op = verb.op;
            if (op == "i.") {
                if (y.GetType() == typeof(A<int>)) {
                    return Verbs.iota((A<int>)y);
                }
                else if (y.GetType() == typeof(A<long>)) {
                    return Verbs.iota((A<long>)y);
                }
            } else if (op == "$") {
                return Verbs.shape(y);
            }
            throw new ArgumentException();
        }
    }


    class Parser {
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

        public struct Token {
            public string word;
            public AType val;
        }

        public AType parse(string cmd) {
            //var parts = cmd.Split(' ');
            
            string[] parts = toWords(cmd);
            
            var MARKER = "`";
            cmd = MARKER + " " + cmd;

            Func<Token, bool> isEdge = (token) => token.word == MARKER || token.word == "=:" || token.word == "(";
            Func<Token, bool> isVerb = (token) => (token.val != null && token.val.GetType() == typeof(A<Verb>)); //|| (token.word != null && verbs.ContainsKey(token.word));
            Func<Token, bool> isAdverb = (token) => token.word != null && Adverbs.Words.Contains(token.word);
            Func<Token, bool> isNoun = (token) => (token.val != null && token.val.GetType() != typeof(A<Verb>));
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

                //Console.WriteLine(step);
                if (step >= 0) {
                    if (step == 0) { //monad
                        var p1 = stack.Pop();
                        var op = stack.Pop();
                        var y = stack.Pop();
                        var z = Verbs.Call1(op.val, y.val);
                        stack.Push(new Token { val = z });
                        stack.Push(p1);
                    }
                    else if (step == 1) {   //monad                         
                        var p1 = stack.Pop();
                        var p2 = stack.Pop();
                        var op = stack.Pop();
                        var x = stack.Pop();
                        var z = Verbs.Call1(op.val, x.val);
                        stack.Push(new Token { val = z });
                        stack.Push(p2);
                        stack.Push(p1);
                        
                    }
                    else if (step == 2) { //dyad
                        var p1 = stack.Pop();
                        var x = stack.Pop();                   
                        var op = stack.Pop();
                        var y = stack.Pop();
                        var z = Verbs.Call2(op.val, x.val, y.val);
                        stack.Push(new Token { val = z });
                        stack.Push(p1);
                    }
                    else if (step == 3) { //adverb
                        var p1 = stack.Pop();
                        var op = stack.Pop();
                        var adv = stack.Pop();
                        var z = new A<Verb>(1);
                        z.Ravel[0] = ((A<Verb>)op.val).Ravel[0];
                        z.Ravel[0].adverb = adv.word;
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
                        var val = AType.MakeA(newWord.word);
                        var token = new Token();
                        if (val.GetType() == typeof(A<Undefined>)) {
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

        public void TestAll() {
            var j = new Parser();

            Func<string, AType> parse = (cmd) => j.parse(cmd);
            Func<object, object, object[]> pair = (a,w) => new object[] { a,w };
            var eqTests = new Dictionary<string, object[]>();
            eqTests["iota simple"] = pair(parse("i. 3").ToString(), "0 1 2");
            eqTests["shape iota simple"] = pair(parse("$ i. 3").ToString(), "3");
            eqTests["reshape int"] = pair(parse("3 $ 3").ToString(),"3 3 3");
            eqTests["reshape double"] = pair(parse("3 $ 3.2").ToString(),"3.2 3.2 3.2");
            eqTests["reshape string"] = pair(parse("3 2 $ 'abc'").ToString(),"ab\nca\nbc");
            eqTests["adverb simple"] = pair(parse("+/ i. 4").ToString(), "6");
            eqTests["multi-dimensional sum"] = pair(parse("+/ i. 2 3").ToString(),"3 5 7");
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
