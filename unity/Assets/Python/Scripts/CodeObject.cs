using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;
using System.Text;

namespace Python
{
    public enum CompileMode
    {
        EXEC_MODE,
        EVAL_MODE,
        REPL_MODE,
        JSON_MODE,
        CELL_MODE
    }



    [StructLayout(LayoutKind.Sequential)]
    public struct Bytecode
    {
        public sbyte op;
        public ushort arg;
    }

    public struct StrName
    {
        public int index;

        public StrName(int index)
        {
            this.index = index;
        }
    }

    public enum CodeBlockType
    {
        NO_BLOCK,
        FOR_LOOP,
        WHILE_LOOP,
        CONTEXT_MANAGER,
        TRY_EXCEPT,
    };

    public class FuncDecl
    {
        public struct KwArg
        {
            public int key;
            public object value;

            public KwArg(int key, object value)
            {
                this.key = key;
                this.value = value;
            }
        };

        public CodeObject code;
        public List<int> args = new List<int>();
        public List<KwArg> kwargs = new List<KwArg>();
        public int starredArg;
        public int starredKwarg;
        public bool nested;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CodeBlock
    {
        public CodeBlockType type; // how many bytes???
        public int parent;
        public int base_stack_size;
        public int start;
        public int end;
        public int end2;
    }

    public class CompileErrorObject
    {
        public string type;
        public string msg;

        public string filename;
        public int lineNo = -1;
        public int columnNo = -1;
        public string name;
        public string snapshot;

        public static CompileErrorObject FromBytes(string buffer, string source)
        {
            CompileErrorObjectDeserializer deserializer = new CompileErrorObjectDeserializer(buffer, source);
            return deserializer.Deserialize();
        }
    }

    public class CodeObject
    {
        public static Dictionary<StrName, string> nameMapping = new Dictionary<StrName, string>();

        public string source;      // SourceData.source
        public string filename;    // SourceData.filename
        public CompileMode mode;   // SourceData.mode

        public string name;
        public bool isGenerator;

        public List<Bytecode> codes = new List<Bytecode>();
        public List<int> iblocks = new List<int>();    // block index for each bytecode
        public List<int> lines = new List<int>();
        public List<object> consts = new List<object>();
        public List<StrName> varnames = new List<StrName>();
        public List<CodeBlock> blocks = new List<CodeBlock>();
        public Dictionary<StrName, int> labels = new Dictionary<StrName, int>();
        public List<FuncDecl> funcDecls = new List<FuncDecl>();

        private string[] cachedLines;
        public string GetLine(int ip)
        {
            Utils.Assert(source != null);
            int i = lines[ip];
            if (cachedLines == null)
            {
                cachedLines = source.Split("\n".ToCharArray(), StringSplitOptions.None);
            }
            return cachedLines[i - 1];
        }

        public static CodeObject FromBytes(string buffer, string source)
        {
            CodeObjectDeserializer deserializer = new CodeObjectDeserializer(buffer, source);
            return deserializer.Deserialize();
        }


        public CodeBlock GetBlockCode(int ip)
        {
            return blocks[iblocks[ip]];
        }

        public string Disassemble()
        {
            StringBuilder sb = new StringBuilder();
            int prevLine = -1;
            for (int i = 0; i < codes.Count; i++)
            {
                Bytecode bc = codes[i];
                string line = lines[i].ToString();
                if (lines[i] == prevLine)
                    line = "";
                else
                {
                    if (prevLine != -1) sb.Append("\n");
                    prevLine = lines[i];
                }

                string pointer = "   ";
                sb.Append(line.PadRight(8) + pointer + i.ToString().PadRight(3));
                string opName = ((Opcode)bc.op).ToString();
                sb.Append(" " + opName.PadRight(25) + " ");
                sb.Append(bc.arg.ToString());
                if (i != codes.Count - 1) sb.Append('\n');
            }

            foreach (FuncDecl decl in funcDecls)
            {
                sb.Append("\n\n" + "Disassembly of " + decl.code.name + ":\n");
                sb.Append(decl.code.Disassemble());
            }
            sb.Append("\n");
            return sb.ToString();
        }
    }

}
