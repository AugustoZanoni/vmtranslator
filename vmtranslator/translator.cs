using System;
using System.IO;

namespace vmtranslator
{
    class translator
    {
        static void Main(string[] args)
        {
            foreach (string file in folderfile(args[0]))
            {
                Console.WriteLine(file);
                parser ps = new parser(file);
                codewriter cw = new codewriter(file);
                if (args.Length > 1)
                    if (args[1] != "nobootstrap") cw.writeInit();
                    else;
                else
                    cw.writeInit();


                ps.avance();
                while (ps.hasmorecommands()) { 
                     
                    //Console.WriteLine(ps.line);
                    var command = ps.commandtype();
                    var arg1 = ps.arg1();
                    var arg2 = ps.arg2();
                    
                    switch (command)
                    {
                        case "C_ARITHMETIC":
                            cw.writearithmetic(arg1);
                            break;
                        case "C_PUSH":
                        case "C_POP":
                            cw.writepushpop(command, arg1, Convert.ToInt32(arg2));
                            break;
                        case "C_LABEL":
                            cw.writelabel(arg1);
                            break;
                        case "C_IF":
                            cw.writeif(arg1);
                            break;
                        case "C_GOTO":
                            cw.writegoto(arg1);
                            break;
                        case "C_FUNCTION":
                            cw.writefunction(arg1, Convert.ToInt32(arg2));
                            break;
                        case "C_RETURN":
                            cw.writereturn();
                            break;
                        case "C_CALL":
                            cw.writecall(arg1, Convert.ToInt32(arg2));
                            break;
                        default:
                            Console.WriteLine("Unknown command in CodeWriter");
                            break;
                    }
                    ps.avance();
                }
                cw.close();
            }
            //parser ps = new parser(args[0]);
            //while (ps.hasmorecommands()) { ps.avance(); Console.WriteLine(ps.line); }
        }

        static string[] folderfile(string path)
        {
            if (path == ".")
                path = Environment.CurrentDirectory;
            else if (path.EndsWith('/'))
                path = Environment.CurrentDirectory + path;
            else
                path = Environment.CurrentDirectory + "/" + path;

            string[] filesfound = { "" };
            if (path.EndsWith(".vm"))
                filesfound[0] = path;
            else
                filesfound = Directory.GetFiles(path,"*.vm");
            return filesfound;
        }
    }
}
