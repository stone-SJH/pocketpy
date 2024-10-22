using System;
using System.Collections.Generic;

namespace Python
{
    public class Frame
    {
        public int ip = -1;
        public int nextIp = 0;
        public CodeObject co;
        public Dictionary<string, object> locals = new Dictionary<string, object>();
        public Dictionary<string, object> globals { get { return module.attr; } }
        public ValueStack s = new ValueStack();

        public int index;
        public PyModule module;
        public object callable;

        public Frame(CodeObject co, PyModule module)
        {
            this.co = co;
            this.module = module;
        }

        public Frame(CodeObject co, PyModule module, object callable)
        {
            this.co = co;
            this.module = module;
            this.callable = callable;
        }

        public Frame(CodeObject co, PyModule module, object callable, Dictionary<string, object> locals)
        {
            this.co = co;
            this.module = module;
            this.callable = callable;
            this.locals = locals;
        }


        public Bytecode NextBytecode()
        {
            ip = nextIp;
            nextIp += 1;
            return co.codes[ip];
        }

        public void JumpAbs(int arg)
        {
            nextIp = arg;
        }

        public string GetCurrentLine(out int lineno)
        {
            int safeIp = ip;
            if (safeIp < 0) safeIp = 0;
            lineno = co.lines[safeIp];
            string line = co.GetLine(safeIp);
            return line;
        }

        private int ExitBlock(int i)
        {
            if (co.blocks[i].type == CodeBlockType.FOR_LOOP) s.Pop();
            return co.blocks[i].parent;
        }

        public void JumpAbsBreak(int target)
        {
            int i = co.iblocks[ip];
            nextIp = target;
            if (nextIp >= co.codes.Count)
            {
                while (i >= 0) i = ExitBlock(i);
            }
            else
            {
                while (i >= 0 && co.iblocks[target] != i)
                {
                    i = ExitBlock(i);
                }
                Utils.Assert(i == co.iblocks[target]);
            }
        }

        public bool JumpToExceptionHandler()
        {
            int i = co.iblocks[ip];
            while (i >= 0)
            {
                if (co.blocks[i].type == CodeBlockType.TRY_EXCEPT)
                    break;

                i = co.blocks[i].parent;
            }

            if (i < 0) return false;

            //pop exception
            object exception = s.Pop();
            //get the stack size of the try block
            int tryBlockStackSize = co.blocks[i].base_stack_size;
            if (s.Count < tryBlockStackSize)
                throw new Exception("invalid state");
            //rollback the stack
            while (s.Count > tryBlockStackSize + locals.Count)
                s.Pop();
            //push back exception
            s.Push(exception);
            //goto exception handler block
            nextIp = co.blocks[i].end;
            return true;
        }
    }

}
