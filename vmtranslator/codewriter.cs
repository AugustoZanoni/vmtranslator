using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace vmtranslator
{
    class codewriter
    {
        #region atributes
        public StreamWriter wr;

        private int idcount = 0;
        int id { get { idcount++; return idcount; } }

        static readonly string[] 
            POP =
        {
            "@SP",
            "M=M-1", // point to top of stack
            "A=M",
            "D=M", // pop in D
        },
            PUSH = 
        {
            "@SP",
            "A=M",
            "M=D", // *SP = D
            "@SP",
            "M=M+1",
        };

        static readonly int TEMP_BASE_ADDR = 5; // endereço fixo para memoria temporária

        //static readonly string
        //    local = "LCL",
        //    argument= "ARG",
        //    THIS= "THIS",
        //    THAT= "THAT";


        delegate string[] VarFunc(string attr1, string attr2 = "");

        string functionName;

        string className;

        Dictionary<string,int> labels = new Dictionary<string, int>(){};

        readonly Dictionary<string, string> SYMBOL = new Dictionary<string, string>() {
            { "local","LCL" },
            { "argument", "ARG" },
            { "this", "THIS"},
            { "that", "THAT" }
        };
        #endregion

        #region constructor
        public codewriter(string fname)
        {
            this.className = Path.GetFileName(fname);
            fname = fname.Replace(".vm", ".asm"); //.s .S .asm
            wr = File.CreateText(fname);            
        }
        #endregion

        #region methods      
        public string[] setD(string baseAddr, int? offset = null)
        {
            string[] asm =
            {
                $"@{baseAddr}",
                (int.TryParse(baseAddr,out _))?"D=A" : "D=M",                
            };

            if (offset.HasValue)
            {
                string[] offsetasm = {
                    $"@{offset}",
                    "A=D+A",
                    "D=M",
                };
                asm = asm.Union(offsetasm).ToArray();   //or     //asm.Concat(offsetasm).Distinct().ToArray();
            }

            return asm;
        }
        public string genReturnLabel(string functionName)
        {
            int label = 0;
            if (labels.TryGetValue(functionName, out label))
               labels[functionName] = label + 1;
            else
                labels.Add(functionName,label+1);            
            return $"{functionName}$ret.{labels[functionName]}";
        }
        public void writeInit()
        {
            wr.WriteLine("// BEGIN BOOTSTRAP");

            string[] init =
            {
                "@256",
                "D=A",
                "@SP",
                "M=D", // SP = 256
            };
            writecode(init);

            writecall("Sys.init", 0);
            wr.WriteLine("// END BOOTSTRAP");
        }
        public void writecode(string [] asm)
        {
            foreach(string a in asm)
            {
                wr.WriteLine(a);
            }
        }
        public void writearithmetic(string command)
        {
            try
            {
                wr.WriteLine($"// {command}");

                string[] pop(bool setD = false)
                {
                    return new string[] {  
                        "@SP",
                        "M=M-1",
                        "A=M",
                        (setD?"D=M":"") 
                    };
                }               

                string[] compare(string command, string _labelId)
                { // compares M with D
                    _labelId = id.ToString();
                    return new string[] { 
                      "D=M-D",
                      $"@TRUE_{ _labelId}",
                      $"D;J{ command.ToUpper()}",
                      "D=0",
                      $"@THEN_{ _labelId}",
                      "0;JMP",
                      $"(TRUE_{ _labelId})",
                      "D=-1",
                      $"(THEN_{ _labelId})",
                    };
                };

                string[] asm = { };
                if (command == "add")
                {
                    asm = asm
                        .Union(pop())
                        .Union(pop(false))
                        .Append("D=D+M")
                        .Union(PUSH).ToArray();                
                }
                else if (command == "sub")
                {
                    asm = asm
                        .Union(pop())
                        .Union(pop(false))
                        .Append("D=M-D")
                        .Union(PUSH).ToArray();
                }
                else if (command == "eq" || command == "lt" || command == "gt")
                {
                    asm = asm
                        .Union(pop())
                        .Union(pop(false))
                        .Union(compare(command, ""))
                        .Union(PUSH).ToArray();       
                }
                else if (command == "neg" || command == "not")
                {
                    asm = asm
                        .Union(pop())
                      .Append($"D={ (command == "neg" ? '-' : '!')}D")
                      .Union(PUSH).ToArray();        
                }
                else if (command == "and" || command == "or")
                {
                    asm = asm
                        .Union(pop())
                        .Union(pop(false))
                        .Append($"D=D{(command == "and" ? '&' : '|')}M")
                        .Union(PUSH).ToArray();         
                }

                writecode(asm);
            }
            catch (Exception err)
            {
                throw err;
            }
        }
        public void writepushpop(string command, string segment, int index)
        {
            try
            {
                wr.WriteLine($"// {command} {segment} {index}");


                string[] asm = { };
                if (command == "C_PUSH")
                {
                    if (segment == "constant")
                    {
                        asm = asm.Union(
                          setD(index.ToString())                                        
                        ).Union(PUSH).ToArray();
                    }
                    else if (segment == "temp")
                    {
                        asm = asm.Union(
                          setD(TEMP_BASE_ADDR.ToString(), index))
                          .Union(PUSH).ToArray();                                      
                    }
                    else if (segment == "pointer")
                    {
                        asm = asm.Union(
                          setD(index == 0 ? "THIS" : "THAT"))
                          .Union(PUSH).ToArray();
                    }
                    else if (segment == "static")
                    {
                        asm = asm.Union(
                          setD($"{this.className}.{index}"))
                          .Union(PUSH).ToArray();
                    }
                    else
                    {
                        asm = asm.Union(
                          setD(SYMBOL[segment], index))
                          .Union(PUSH).ToArray();
                    }
                }
                else if (command == "C_POP")
                {
                    if (segment == "constant")
                    {
                        throw new Exception("Error on Pop Constant");
                    }

                    // set up temp variables to impl pop
                    string ADDR = "R13";


                    VarFunc addr = delegate (string baseAddr, string offset)
                    {
                        return new String[] {
                            $"@{baseAddr}",
                            (Int32.TryParse(baseAddr,out _)) ? "D=A" : "D=M",
                            $"@{offset}",
                            "D=D+A", // baseAddr + offset
                            $"@{ADDR}",
                            "M=D",
                        };
                    };

                    string[] addrPtr = {
                        $"@{ADDR}",
                        "A=M",
                        "M=D", // *addr = D
                    };

                    if (segment == "temp")
                    {
                        asm = asm.Union(
                           addr(TEMP_BASE_ADDR.ToString(), index.ToString()))
                          .Union(POP)
                          .Union(addrPtr).ToArray();                        
                    }
                    else if (segment == "pointer")
                    {
                        asm =
                          asm.Union(POP)
                          .Append($"@{(index == 0 ? "THIS" : "THAT")}")
                          .Append("M=D").ToArray();
                    }
                    else if (segment == "static")
                    {
                        asm = asm.Union(POP)
                          .Append($"@{this.className}.{index}")
                          .Append("M=D").ToArray();          
                    }
                    else
                    {
                        asm = asm.Union(
                          addr(SYMBOL[segment], index.ToString()))
                          .Union(POP)
                          .Union(addrPtr).ToArray();
                    }
                }
                writecode(asm);
            }
            catch (Exception err)
            {
                throw err;
            }
        }
        public void writelabel(string label)
        {
            string[] asm = { $"({ functionName + '$' + label})" };
            this.writecode(asm);
        }
        public void writeif(string label)
        {
            wr.WriteLine($"// C_IF {label}");

            string[] asm =
            {
             "@SP",
             "M=M-1",
             "A=M",
             "D=M",
              $"@{ this.functionName + "$" + label}",
              "D;JNE"
            };
            writecode(asm);
        }
        public void writegoto(string label)
        {
            wr.WriteLine( $"// C_GOTO {label}");

            string[] asm = {
                $"@{ this.functionName + '$' + label}",
                "0;JMP"
            };
            writecode(asm);
        }
        public void writefunction(string functionName, int numLocals)
        {
            this.functionName = functionName;
            wr.WriteLine($"// C_FUNCTION {functionName} {numLocals}");

            writecode(new string[] { $"({ functionName })" });           

            while(numLocals > 0)
            {
                numLocals--;
                string[] asm =
                    setD("0")
                    .Union(PUSH).ToArray();
                writecode(asm);
            }
        }
        public void writereturn()
        {
            string END_FRAME = "R13"
                , RETURN_ADDR = "R14";

            VarFunc setVarFromFrame = delegate (string target, string offset)
            {
                return new String[] {
                    $"@{END_FRAME}",
                    "D=M",
                    $"@{offset}",
                    "A=D-A",
                    "D=M",
                    $"@{target}",
                    "M=D"
                };
            };

            wr.WriteLine("// C_RETURN");

            string[] asm =
                {
                 "@LCL",
                 "D=M",
                 $"@{END_FRAME}",
                 "M=D" // END_FRAME = LCL
                };

            asm =
                asm.Union(setVarFromFrame(RETURN_ADDR, "5")) // RETURN_ADDR = END_FRAME - 5
                .Union(POP)
                .Append($"D=A")
                .Append("@ARG")
                .Append("A=M")
                .Append("M=D")                          // *ARG = *SP
                .Append("@ARG")
                .Append("D=M+1")
                .Append("@SP")
                .Append("M =D")                         // SP = ARG + 1
                .Union(setVarFromFrame("THAT", "1"))    // THAT = *(END_FRAME - 1)
                .Union(setVarFromFrame("THIS", "2"))    // THIS = *(END_FRAME - 2)
                .Union(setVarFromFrame("ARG", "3"))     // ARG = *(END_FRAME - 3)
                .Union(setVarFromFrame("LCL", "4"))     // LCL = *(END_FRAME - 4)
                .Append($"@{RETURN_ADDR}")
                .Append("A=M")
                .Append("0;JMP").ToArray();

            writecode(asm);
        }
        public void writecall(string functionname, int numargs)
        {
            VarFunc pushval = delegate (string addr, string line)
            {
                return new String[] {
                        $"@{addr}",
                        (Regex.IsMatch(addr, "/LCL|ARG|THIS|THAT/"))? "D=M" : "D=A",                        
                }.Union(PUSH).ToArray();
            };
            int FRAME_SIZE = 5;            

            wr.WriteLine($"// C_CALL {functionname} ${numargs}\n");
            var RETURN_LABEL = genReturnLabel(functionname);

            string[] asm =
                  pushval(RETURN_LABEL)
                  .Union(pushval("LCL"))               // push return address
                  .Union(pushval("LCL")) // push local segment base address
                  .Union(pushval("ARG")) // push arg segment base address
                  .Union(pushval("THIS")) // push this segment base address
                  .Union(pushval("THAT")) // push that segment base address
                  .Append($"@{FRAME_SIZE + numargs}")
                  .Append($"D=A")
                  .Append($"@SP")
                  .Append($"D=M-D")
                  .Append($"@ARG")
                  .Append($"M=D") // ARG = SP - 5 - numArgs
                  .Append($"@SP")
                  .Append($"D=M")
                  .Append($"@LCL")
                  .Append($"M=D") // LCL = SP
                  .Append($"@{functionname}")
                  .Append($"0;JMP") // goto functionName
                  .Append($"({ RETURN_LABEL})").ToArray();
            writecode(asm);
        }
        public void close()
        {
            wr.Flush();
            wr.Close();
        }
        #endregion
    }
}
