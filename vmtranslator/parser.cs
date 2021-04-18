using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace vmtranslator
{
    class parser
    {
        string VALID_NAME_RE = "[a-zA-Z][\\w\\.:_]*";
        public parser(string fname)
        {
            {
                try
                {
                    using (var sr = new StreamReader(fname))
                    {

                    }
                }
                catch (IOException e)
                {

                }
            }
        }

        static object commands()
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


        public void commandtype()
        {
            string pop = @"/^pop\s+\w+\s+\d+$/";
            string push = @"/^push\s+\w+\s+\d+$/";
            string arithLogic = @"/^(add|sub|neg|eq|gt|lt|and|or|not)$/";
            string label = $"^label\\s+${ VALID_NAME_RE }$";
            string Goto = $"^goto\\s+${ VALID_NAME_RE}$";
            string ifgoto = $"^if-goto\\s+${ VALID_NAME_RE}$";
            string func = $"^function\\s+${ VALID_NAME_RE}\\s+\\d +$";
            string call = $"^call\\s+${ VALID_NAME_RE}\\s+\\d+$";
        }
    }
}
