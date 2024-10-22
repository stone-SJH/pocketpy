// unset

using System.Collections.Generic;
using System.Text;

namespace Python
{
    public class CsharpException : System.Exception
    {
        public PyException InnerException;
        public CompileErrorObject CompileErrorInfo;
        public CsharpException(string msg) : base(msg) { }

        public CsharpException(string msg, PyException pyex)  : base(msg)
        {
            this.InnerException = pyex;
            this.CompileErrorInfo = null;
        }

        public CsharpException(string msg, CompileErrorObject ceo) : base(msg)
        {
            this.InnerException = null;
            this.CompileErrorInfo = ceo;
        }
     }

    public class PyException : PyObject
    {
        public struct StacktraceLine
        {
            public CodeObject co;
            public int line;
            public string funcname;
            public int column;
        }

        public string msg;
        public PyTypeObject type;
        public bool reRaise;
        public int ipOnError;
        public CodeObject coOnError;

        public Stack<StacktraceLine> stacktrace;

        public PyException(PyTypeObject type, string msg = "")
        {
            this.type = type;
            this.msg = msg;
            this.reRaise = false;
            this.ipOnError = -1;
            this.coOnError = null;
            this.stacktrace = new Stack<StacktraceLine>();
        }

        public void PushStacktrace(CodeObject co, int line, string func, int column = -1)
        {
            //max stacktrace count is 8
            if (stacktrace.Count > 8)
                return;

            StacktraceLine stl = new StacktraceLine() {column = column, line = line, funcname = func, co = co};
            stacktrace.Push(stl);
        }

        public string PrintStacktrace()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Traceback (most recent call last):\n");
            while (stacktrace.Count != 0)
            {
                StacktraceLine stl = stacktrace.Pop();
                sb.Append("File \"" + stl.co.filename + "\", line " + stl.co.lines[stl.line]);
                if (!string.IsNullOrEmpty(stl.funcname))
                {
                    sb.Append(", in " + stl.funcname);
                }
                if (!string.IsNullOrEmpty(stl.co.source))
                {
                    sb.Append("\n");
                    string line = stl.co.GetLine(stl.line);
                    if (string.IsNullOrEmpty(line))
                        line = "<?>";
                    sb.Append(line);
                }
                sb.Append("\n");
            }
            if (!string.IsNullOrEmpty(msg))
            {
                sb.Append(type.Name + ": " + msg);
            }
            else
            {
                sb.Append(type.Name);
            }
            return sb.ToString();
        }
    }

    public class HandledException : System.Exception
    {

    }

    public class UnhandledException : System.Exception
    {

    }

    public class ToBeRaisedException : System.Exception
    {

    }
}
