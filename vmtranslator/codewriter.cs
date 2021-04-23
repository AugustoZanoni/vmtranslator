using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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

        static readonly string
            local = "LCL",
            argument= "ARG",
            THIS= "THIS",
            THAT= "THAT";

        Dictionary<string,int> labels = new Dictionary<string, int>(){};
        #endregion

        #region constructor
        public codewriter(string fname)
        {
            fname = fname.Replace(".vm", ".asm"); //.s .S .asm
            wr = File.CreateText(fname);
        }
        #endregion

        #region methods      
        public string[] setD(string baseAddr, int? offset)
        {
            string[] asm =
            {
                $"@{baseAddr}",
                (int.TryParse(baseAddr,out _))?"D=A" : "D=M",                
            };

            if (offset.HasValue)
            {
                string[] offsetasm = {
                    $"@{ offset}",
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
            return $"{functionName}$ret.${labels[functionName]}";
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

        }
        public void writepushpop(string command, string segment, int index)
        {

        }
        public void writelabel(string label)
        {
            string[] asm = { "(${ this.functionName + '$' + label})" };
            this.writecode(asm);
        }
        public void writeif(string label)
        {

        }
        public void writegoto(string label)
        {

        }
        public void writefunction(string functionName, int numLocals)
        {

        }
        public void writereturn()
        {

        }
        public void writecall(string functionname, int numargs)
        {

        }
        public void close()
        {
            wr.Flush();
            wr.Close();
        }
        #endregion
    }
}
