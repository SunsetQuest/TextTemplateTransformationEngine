using System;
using System.Diagnostics; // For Timer

// CodeProject Link: http://www.codeproject.com/Articles/867045/Csharp-Based-Template-Transformation-Engine

// License Information
// Code Project Open License (CPOL) http://www.codeproject.com/info/cpol10.aspx 
// THIS WORK IS PROVIDED "AS IS", "WHERE IS" AND "AS AVAILABLE", 
// WITHOUT ANY EXPRESS OR IMPLIED WARRANTIES OR CONDITIONS OR GUARANTEES.

namespace T44_TextTemplateTransformation
{
    class ExampleProgram
    {
        static void Main(string[] args)
        {
            // use this sample for testing the bracket style code.
            string bracketStyleCode = @"
This first example will write Hello World three times:
  [[~ for(int i=0; i<3; i++) Format(""Hello World {0}! "",i); WriteLine();

Here is another way of doing the same thing:
  [[~ for(int i=0; i<3; i++){ 
Hello World [[ Write(i +""! ""); }]]

And, of course, you can just write it:
  Hello World 0! Hello World 1! Hello World 2!
 
  [[! This comment will not be added to the output. ]]

Write() will print any bool, string, char, decimal, float, int...  
  A Quadrillion is 1[[ for(int i=0; i<15; i++) Write(""0""); ]]

This will also write bool, string, char, decimal, float, int...
  Hello at [[=DateTime.Now]]! 

This is the temporary executable that generates the exec:
  [[=System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName]]

[[ for(int i=1; i<4; i++){ ]]	
  [[=""Hello "" + i + "" World""+ (i>1?""s!"":""!"") ]]	
  How are you? ""[[=i]]""  [[=""\r\n""]]						
[[ } ]]
Finished!
";
            // use this sample for testing the comment style code.
            string commentStyleCode = @"
This first example will write Hello World three times:
  //: for(int i=0; i<3; i++) Format(""Hello World {0}! "",i); WriteLine();

Here is another way of doing the same thing:
  //: for(int i=0; i<3; i++){ 
Hello World /*: Write(i + ""! ""); }:*/

And, of course, you can just write it:
  Hello World 0! Hello World 1! Hello World 2!
 
  /*! This comment will not be added to the output. :*/
  //! This comment will also not be displayed.
  /**/ This code will only be visible in the editor/**/

Write() will print any bool, string, char, decimal, float, int... 
  A Quadrillion is 1/*: for(int i=0; i<15; i++) Write(""0""); :*/

This will also write bool, string, char, decimal, float, int...
  Hello at /*=DateTime.Now:*/! 

This is the temporary executable that generates the exec:
  /*=System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName:*/

/*: for(int i=1; i<4; i++){ :*/	
  /*=""Hello "" + i + "" World""+ (i>1?""s!"":""!"") :*/	
  How are you? ""/*=i:*/""  /*=""\r\n"":*/							
/*: } :*/
Finished!
";

            string codeSampleToUse = bracketStyleCode; // or use bracketStyleCode
            Stopwatch sw = new Stopwatch();
            string output;

            sw.Start();
            bool success = T44.Expand(codeSampleToUse, out output);
            sw.Stop(); 

            if (!success)
                Console.WriteLine("Error in text template!");

            Console.WriteLine("===================== original =====================");
            Console.Write(codeSampleToUse);
            Console.WriteLine("===================== expanded =====================");
            Console.Write(output);
            Console.WriteLine("Time taken: {0}ms", sw.ElapsedMilliseconds);
            Console.ReadKey();
        }
    }
 

    public static class T44 //calling it T44 after Microsoft's T4
    {
        public static bool Expand(string input, out string output)
        {
            //////////////// Step 1 - Build the generator program ////////////////
            // For [[CODE]] , [[=EXPRESSION]] ,  [[~FULL_LINE_OF_CODE  &  [[!SKIP_ME]]
            // style uncomment the next 5 lines of code
            const string REG = @"(?<txt>.*?)" +      // grab any normal text
                @"(?<type>\[\[[!~=]?)" +             // get type of code block
                @"(?<code>.*?)" +                    // get the code or expression
                @"(\]\]|(?<=\[\[~[^\r\n]*?)\r\n)";   // terminate the code or expression
            const string NORM = @"[[", FULL = @"[[~", EXPR = @"[[=", TAIL = @"]]";

            //// For /*:CODE:*/ , /*=EXPRESSION:*/ , //:FULL_LINE_OF_CODE & //!SKIP_ME
            //// style uncomment the next 5 lines of code
            //const string REG ="(?<txt>.*?)" +          // grab any normal text
            //    @"((/\*\*/.*?/\*\*/)|(?<type>/(/!|\*:|\*=|/:|\*!))" + // get code 
            //    @"(?<code>.*?)" +                      // get the code or expression
            //    @"(:\*/|(?<=//[:|!][^\r\n]*)\r\n))";   // terminate the code or expression
            //const string NORM = @"/*:", FULL = @"//:", EXPR = @"/*=", TAIL = @":*/";               

            System.Text.StringBuilder prog = new System.Text.StringBuilder();
            prog.AppendLine(
@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
  class T44Class { 
  static StringBuilder sb = new StringBuilder();
  public static string Execute() {");
            foreach (System.Text.RegularExpressions.Match m in
                System.Text.RegularExpressions.Regex.Matches(input + NORM + TAIL, REG,
                System.Text.RegularExpressions.RegexOptions.Singleline))
            {
                prog.Append(" Write(@\"" + m.Groups["txt"].Value.Replace("\"", "\"\"") + "\");");
                string txt = m.Groups["code"].Value;
                switch (m.Groups["type"].Value)
                {
                    case NORM: prog.Append(txt); break;  // text to be added
                    case FULL: prog.AppendLine(txt); break;
                    case EXPR: prog.Append(" sb.Append(" + txt + ");"); break;
                }
                    }
                    prog.AppendLine(
@"  return sb.ToString();}
static void Write<T>(T val) { sb.Append(val);}
static void Format(string format, params object[] args) { sb.AppendFormat(format,args);}
static void WriteLine(string val) { sb.AppendLine(val);}
static void WriteLine() { sb.AppendLine();} 
static void Main(string[] args) { Execute(); Console.Write(sb.ToString()); } }");//"Main" only for debug
            string program = prog.ToString(); 
            //Tip: For additional debugging drop contents of program into a new VS Console app.

            //////////////// Step 2 - Compile the generator program ////////////////
            var res = (new Microsoft.CSharp.CSharpCodeProvider()).CompileAssemblyFromSource(
                new System.CodeDom.Compiler.CompilerParameters()
                {
                    GenerateInMemory = true, // note: this is not really "in memory"
                    ReferencedAssemblies = { "System.dll", "System.Core.dll" } // for linq
                }
                , program);

            res.TempFiles.KeepFiles = false; //clean up files in temp folder

            // Print any errors with the source code and line numbers
            if (res.Errors.HasErrors)
            {
                int cnt = 1;
                output = "There is one or more errors in the template code:\r\n";
                foreach (System.CodeDom.Compiler.CompilerError err in res.Errors)
                    output += "[Line " + err.Line + " Col " + err.Column + "] " + err.ErrorText + "\r\n";
                output += "\r\n================== Source (for debugging) =====================\r\n";
                output += "     0         10        20        30        40        50        60\r\n";
                output += "   1| " + System.Text.RegularExpressions.Regex.Replace(program, "\r\n",
                    m => { cnt++; return "\r\n" + cnt.ToString().PadLeft(4) + "| "; });
                return false;
            }

            //////////////// Step 3 - Run the program to collect the output ////////////////
            var type = res.CompiledAssembly.GetType("T44Class");
            var obj = System.Activator.CreateInstance(type);
            output = (string)type.GetMethod("Execute").Invoke(obj, new object[] { });
            return true;
        }   
    }
}