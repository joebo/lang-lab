using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

public class Program {
    public static void Main()
    {
	var jt = new JSharp();
        jt.tests();
        
	//fail
	//parse("+/ i. 1000000").Dump();
	//parse("+/ (i. 1000000)").Dump();
	//parse("+/ ( 1 + i. 1000 )").Dump();
    }
}

public class JSharp {
    public enum Type { Undefined, Int, String, Double};
    public class A {
	public Type Type;
	public long Count;
	public int Rank;
	public long[] Shape;
	public long[] ri;
	public string[] rs;
	public double[] rd;
	public A(Type type, long n) { 
            Type = type;
            Count = n;
            if (Type == Type.Int) { ri = new long[n]; }
            if (Type == Type.Double) { rd = new double[n]; }
	}
	public A(string word) {
            int val;
		
            //move to stack parsing
            if (word.Contains(" ")) {
                var longs = new List<long>();
                foreach(var part in word.Split(' ')) {
                    longs.Add(Int32.Parse(part));
                }
                Type = Type.Int;
                ri = longs.ToArray();
                Count = longs.Count;
            } 
            else if (Int32.TryParse(word, out val)) {
                Type = Type.Int;
                ri = new long[1];
                Count = 1;
                ri[0] = val;
            } else {
                throw new ArgumentException(word);
            }
	}
	
	public override string ToString() {
            if (Type == Type.Int && Count > 0) {
                return ri[0].ToString();
            }
            return "";
	}
	
	public A(Type type, long n, double y) : this(type,n) { 
            rd[0] = y;
	}
	public long AsInt(int i) { 
            if (Type == Type.Int) { return ri[i]; }
            if (Type == Type.Double) { return (int) rd[i]; }
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
            A v = new A(Type,1);
            if (Type == Type.Double) { v.rd[0] = rd[i]; }
            if (Type == Type.Int) { v.ri[0] = ri[i]; }		
            return v;
	}
    }


    A iota(A y) {
	var v = new A(Type.Int, y.ri[0]);
	for(var i = 0; i < y.ri[0]; i++ ) { v.ri[i] = i; }	
	return v;
    }

    A add(A x, A y) { 
	Type type = Type.Undefined;
	if (y.Type == x.Type) { type = y.Type; }
	if (y.Type == Type.Double || x.Type == Type.Double) { type = Type.Double; }
	
	var v = new A(type,y.Count);	
	if (type == Type.Int) {			
            for(var i = 0; i < y.Count; i++) v.SetInt(i, x.AsInt(i * (x.Count==y.Count ? 1 : 0)) + y.AsInt(i));
	} else if (type == Type.Double) {
            for(var i = 0; i < y.Count; i++) v.SetDouble(i, x.AsDouble(i * (x.Count==y.Count ? 1 : 0)) + y.AsDouble(i));
	} else {	
            throw new ArgumentException("argument mismatch");	
	}
	return v;
    }

    A multiply(A x, A y) { 
	Type type = Type.Undefined;
	if (y.Type == x.Type) { type = y.Type; }
	if (y.Type == Type.Double || x.Type == Type.Double) { type = Type.Double; }
	
	var v = new A(type,y.Count);	
	if (type == Type.Int) {			
            for(var i = 0; i < y.Count; i++) v.SetInt(i, x.AsInt(i * (x.Count==y.Count ? 1 : 0)) * y.AsInt(i));
	} else if (type == Type.Double) {
            for(var i = 0; i < y.Count; i++) v.SetDouble(i, x.AsDouble(i * (x.Count==y.Count ? 1 : 0)) * y.AsDouble(i));
	} else {	
            throw new ArgumentException("argument mismatch");	
	}
	return v;
    }


    A reduce(Func<A,A,A> op, A y) {
	var v = new A(y.Type,1);	
	for(var i = 0; i < y.Count; i++) {
            v = op(v, y.Copy(i));		
	}
	return v;
    }


    A sum(A y) {
	var v = new A(y.Type,1);	
	for(var i = 0; i < y.Count; i++) {
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
	var emit = new Action(()=> { if (currentWord.Length > 0) { z.Add(currentWord.ToString().Trim()); } currentWord = new StringBuilder(); });
	char p='\0';
	Func<char,bool> isSymbol = (c) => c=='+' || c=='/';
	foreach(var c in w) {
            if (!Char.IsDigit(p) && c==' ') { emit(); }
            else if (p == ' ' && !Char.IsDigit(c)) { emit(); currentWord.Append(c);  }
            else if (Char.IsDigit(p) && c != ' ' && !Char.IsDigit(c)) { emit(); currentWord.Append(c);  }
            else if (c=='(' || c==')') { emit(); currentWord.Append(c); emit(); }
            else if (isSymbol(p) && Char.IsLetter(c)) { emit(); currentWord.Append(c); }
            else if (isSymbol(p) && isSymbol(c)) { emit(); currentWord.Append(c); emit();}
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

    public A parse(string cmd) {
	var dyads = new Dictionary<string, Func<A,A,A>>();
	var monads = new Dictionary<string, Func<A,A>>();
	var adverbs = new Dictionary<string, Func<Func<A, A,A>,A,A>>();
	
	//var zz = reduce1(add, iota(new A("10"))).Dump();
	
	dyads["+"] = add;
	dyads["*"] = multiply;
	monads["sum"] = sum;
	monads["i."] = iota;
	adverbs["/"] = reduce;
	
	var MARKER = "`";
	cmd = MARKER + " " + cmd;
			
	Func<Token, bool> isEdge = (token) => token.word == MARKER || token.word == "=:" || token.word== "(";
	Func<Token, bool> isVerb = (token) => token.word != null && (dyads.ContainsKey(token.word) || monads.ContainsKey(token.word));
	Func<Token, bool> isAdverb = (token) => token.word != null && adverbs.ContainsKey(token.word);
	
	
	//TODO rework this to parse constants when moving to the stack and then check type of val
	Func<Token, bool> isNoun = (token) => {
            int num;
            if (token.val != null) { return true; }
            return (Int32.TryParse(token.word, out num));
	};
	Func<Token, bool> isEdgeOrNotConj = (token) => isEdge(token) || isVerb(token) || isNoun(token) || token.word == "";
	
	
	var words = toWords(cmd);
	
	/* TODO Add logic to place parsed value on stack for constants
           currently the stack contains string words or values. Values only get replaces on evaluation of monad/dyad/adverb.
	*/
	var stack = new Stack<Token>();
	var queue = new Queue<Token>();
	for(var k = words.Length-1;k>=0;k--) {
            queue.Enqueue(new Token { word=words[k] });
	}	
	int i = 0;
	while(i < 20) {
            var sarr = stack.ToArray().ToList();
            var w1 = sarr.Count > 0 ? sarr[0] : new Token { word=""};	
            var w2 = sarr.Count > 1 ? sarr[1] : new Token { word=""};
            var w3 = sarr.Count > 2 ? sarr[2] : new Token { word=""};
            var w4 = sarr.Count > 3 ? sarr[3] : new Token { word=""};
            //new Token[] { w1,w2,w3,w4 }.Dump();
		
            var step = -1;
            if (isEdge(w1) && isVerb(w2) && isNoun(w3) && true) { step = 0; }
            else if (isEdgeOrNotConj(w1) && isVerb(w2) && isVerb(w3) && isNoun(w4)) { step = 1; }
            else if (isEdgeOrNotConj(w1) && isNoun(w2) && isVerb(w3) && isNoun(w4)) { step = 2; }
            else if (isEdgeOrNotConj(w1) && (isNoun(w2)||isVerb(w2)) && isAdverb(w3) && true) { step = 3; } //adverb
            else if (w1.word == "(" && isNoun(w2) && w3.word == ")" && true) { step = 8; }
		
            if (step>=0) {
                Console.WriteLine("STEP: " + step);
                if (step == 0) { //monad
                    var p1 = stack.Pop();
                    var op = stack.Pop();
                    var xt = stack.Pop();				
                    var x = (xt.val == null) ? new A(xt.word) : xt.val;				
                    var z = monads[op.word](x);				
                    stack.Push(new Token { val=z });
                    stack.Push(p1);
                    //Console.WriteLine("A STACK: " + String.Join("",stack.ToArray()));
                    //stack.Push(top);
                }
                else if (step == 1) {	//monad			
                    Console.WriteLine("B STACK: " + String.Join("",stack.ToArray()));
                    var p1 = stack.Pop();
                    var p2 = stack.Pop();
                    var op = stack.Pop();
                    var xt = stack.Pop();
                    var x = (xt.val == null) ? new A(xt.word) : xt.val;				
                    var z = monads[op.word](x);
                    stack.Push(new Token { val=z });
                    stack.Push(p2);
                    stack.Push(p1);
                    //Console.WriteLine("A STACK: " + String.Join("",stack.ToArray()));
                }
                else if (step == 2) { //dyad
                    //Console.WriteLine("STACK B: " + String.Join("",stack.ToArray()));				
                    var p1 = stack.Pop();
                    var xt = stack.Pop();
                    var x = (xt.val == null) ? new A(xt.word) : xt.val;				
				
                    var op = stack.Pop();
                    var yt = stack.Pop();
                    var y = (yt.val == null) ? new A(yt.word) : yt.val;				
				
                    var z = dyads[op.word](x,y);
                    stack.Push(new Token { val=z });
                    stack.Push(p1);
                    //Console.WriteLine("STACK A: " + String.Join("",stack.ToArray()));
                }
                else if (step == 3) { //adverb
                    //todo: adverb should not evaluate, but add a new verb to stack
                    //stack.Dump();
                    var p1 = stack.Pop();
                    var op = stack.Pop();
                    var adv = stack.Pop();
                    var yt = stack.Pop();
                    var y = (yt.val == null) ? new A(yt.word) : yt.val;				
                    var z = adverbs[adv.word](dyads[op.word],y);
                    stack.Push(new Token { val=z });
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
                    stack.Push(queue.Dequeue());
                }
                if (queue.Count() == 0 && (stack.Count() == 1 || stack.Count() == 2))  { Console.WriteLine("DONE"); break; }
            }
            i++;
		
	}
	//stack.Dump();
	stack.Pop();
	return stack.Pop().val;
	
    }




    public void tests() {
	var tests = new Dictionary<string, Func<bool>>();
	tests["returns itself"] = ()=> equals(toWords("abc"),"abc");
	tests["parses spaces"] = ()=> equals(toWords("+ -"),new string[] { "+", "-"});
	tests["parentheses"] = ()=> equals(toWords("(abc)"),new string[] { "(", "abc", ")"});
	tests["parentheses2"] = ()=> equals(toWords("((abc))"),new string[] { "(", "(", "abc", ")", ")"});
	tests["numbers"] = ()=> equals(toWords("1 2 3 4"),new string[] { "1 2 3 4"});
	tests["op with numbers"] = ()=> equals(toWords("# 1 2 3 4"),new string[] { "#", "1 2 3 4"});
	tests["op with numbers 2"] = ()=> equals(toWords("1 + 2"),new string[] { "1", "+", "2"});
	tests["op with no spaces"] = ()=> equals(toWords("1+i. 10"),new string[] { "1", "+", "i.", "10"});	
	tests["adverb +/"] = ()=> equals(toWords("+/ 1 2 3"),new string[] { "+", "/", "1 2 3"});
	
	//parse tests
	tests["iota"] = () => equals(parse("i. 3").ri,new long[] { 0,1,2});
	tests["adds 1 to iota"] = () => equals(parse("1 + i. 3").ri,new long[] { 1,2,3});
	tests["adverb +/"] = () => equals(parse("+/ 2 2 2").ri,new long[] { 6 });
	
	foreach(var key in tests.Keys) {
            if (!tests[key]()) {
                throw new ApplicationException(key);
            }
	}
    }
}