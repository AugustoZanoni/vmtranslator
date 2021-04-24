using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace vmtranslator
{
    class parser
    {
        #region atributes
        private string VALID_NAME_RE = "[a-zA-Z][\\w\\.:_]*";
        public string line = "";
        public int lineindex = 0;
        StreamReader sr;
        public string command;
        #endregion

        #region constructor
        public parser(string fname)
        {
            {
                try
                {
                    sr = new StreamReader(fname);                                                             
                }
                catch (IOException e)
                {
                    throw e;
                }
            }
        }
        #endregion

        #region methods
        static object commands
        {
            get
            {
                return new
                {
                    C_ARITHMETIC = "C_ARITHMETIC",
                    C_PUSH = "C_PUSH",
                    C_POP = "C_POP",
                    C_LABEL = "C_LABEL",
                    C_GOTO = "C_GOTO",
                    C_IF = "C_IF",
                    C_FUNCTION = "C_FUNCTION",
                    C_RETURN = "C_RETURN",
                    C_CALL = "C_CALL",
                };
            }
        }

        public string commandtype()
        {            
            dynamic c = commands;
            Regex pop = new Regex(@"^pop\s+\w+\s+\d+$");
            Regex push = new Regex(@"^push\s+\w+\s+\d+$");
            Regex arithLogic = new Regex(@"^(add|sub|neg|eq|gt|lt|and|or|not)$");
            Regex label = new Regex($@"^label\s+{ VALID_NAME_RE}$");
            Regex Goto = new Regex($@"^goto\s+{VALID_NAME_RE}$");
            Regex ifgoto = new Regex($@"^if-goto\s+{VALID_NAME_RE}$");
            Regex func = new Regex($@"^function\s{VALID_NAME_RE}\s+\d+$");
            Regex call = new Regex($@"^call\s+{VALID_NAME_RE}\s+\d+$");

            if (pop.IsMatch(line))                         
                command = c.C_POP;
            else if (push.IsMatch(line))            
                command = c.C_PUSH;            
            else if (arithLogic.IsMatch(line))            
                command = c.C_ARITHMETIC;            
            else if (label.IsMatch(line))            
                command = c.C_LABEL;            
            else if (Goto.IsMatch(line))            
                command = c.C_GOTO;            
            else if (ifgoto.IsMatch(line))            
                command = c.C_IF;            
            else if (func.IsMatch(line))            
                command = c.C_FUNCTION;            
            else if (line == "return")            
                command = c.C_RETURN;            
            else if (call.IsMatch(line))            
                command = c.C_CALL;            
            else            
                throw new Exception($"Unknown command or invalid command syntax on line $ lineindex : $ line ");            

            return command;
        }

        public bool hasmorecommands()
        {
            return (line != null);
        }

        public void avance()
        {
            line = sr.ReadLine();
            if(line != null)
                line = line.Split("//")[0].Trim();
            lineindex++;

            if (line == "") this.avance();
        }

        public string arg1()
        {            
            switch (command)
            {
                case "C_ARITHMETIC":
                    return line;
                case "C_PUSH":
                case "C_POP":
                case "C_LABEL":
                case "C_GOTO":
                case "C_IF":
                case "C_FUNCTION":
                case "C_CALL":
                    return Regex.Split(line, @"\s+")[1];//line.Split("/\s+/")[1];
            }
            return null;
        }

        public string arg2()
        {
            switch (command)
            {               
                case "C_PUSH":
                case "C_POP":
                case "C_CALL":
                case "C_FUNCTION":
                    return Regex.Split(line, @"\s+")[2];//line.Split("/\s+/")[1];

            }
            return null;
        }
        #endregion
    }
}
