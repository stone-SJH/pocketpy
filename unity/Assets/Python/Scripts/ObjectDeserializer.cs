// unset

using System;
using System.Runtime.InteropServices;

namespace Python
{
    public class ObjectDeserializer
    {
        public string[] tokens;
        public int pos;
        public string source;

        string current => tokens[pos];

        public ObjectDeserializer(string buffer, string source)
        {
            this.source = source;
            this.tokens = buffer.Split("\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            this.pos = 0;
        }

        void AssertHeader(char h)
        {
            if (current[0] != h)
            {
                throw new CsharpException("Expected header '" + h + "' but got '" + current[0] + "'");
            }
        }

        public void Advance()
        {
            // Debug.Log(current);
            pos++;
        }

        public string ReadStr()
        {
            // example s"ab\n123c"
            AssertHeader('s');
            if (current.Length <= 3)
            {
                Advance();
                return string.Empty;
            }
            string x = current.Substring(2, current.Length - 3);
            x = x.Unescape();
            Advance();
            return x;
        }

        public int ReadInt()
        {
            // example i123
            AssertHeader('i');
            if (current.Length == 1)
            {
                Advance();
                return -1;
            }
            int x = int.Parse(current.Substring(1));
            Advance();
            return x;
        }

        public StrName ReadName()
        {
            // example n123
            AssertHeader('n');
            int x = int.Parse(current.Substring(1));
            Advance();
            return new StrName(x);
        }

        public object[] ReadTuple()
        {
            AssertHeader('t');
            int count = int.Parse(current.Substring(1));
            object[] tuple = new object[count];
            Advance();
            for (int i = 0; i < count; i++)
            {
                object value = ReadObject();
                tuple[i] = value;
            }
            return tuple;
        }

        public object ReadObject()
        {
            switch (current[0])
            {
                case 's': return ReadStr();
                case 'i': return ReadInt();
                case 'n': return ReadName();
                case 'f': return ReadFloat();
                case 'b': return ReadBool();
                case 'x': return ReadBytes();
                case 't': return ReadTuple();
                case 'N':
                    Advance();
                    return VM.None;
                case 'E':
                    Advance();
                    return VM.Ellipsis;
                default: throw new CsharpException("Unknown object type: " + current[0]);
            }
        }

        public float ReadFloat()
        {
            // example f123.456
            AssertHeader('f');
            float x = float.Parse(current.Substring(1));
            Advance();
            return x;
        }

        public bool ReadBool()
        {
            // example b1 or b0
            AssertHeader('b');
            bool x = current[1] == '1';
            Advance();
            return x;
        }

        public byte[] ReadBytes()
        {
            // example x1234567890abcdef
            AssertHeader('x');
            string x = current.Substring(1);
            byte[] bytes = new byte[x.Length / 2];
            for (int i = 0; i < x.Length; i += 2)
            {
                bytes[i / 2] = byte.Parse(x.Substring(i, 2), System.Globalization.NumberStyles.HexNumber);
            }
            Advance();
            return bytes;
        }

        public T ReadStruct<T>()
        {
            // https://stackoverflow.com/questions/6335153/casting-a-byte-array-to-a-managed-structure
            byte[] bytes = ReadBytes();
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            T stuff = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();
            return stuff;
        }

        public void VerifyVersion()
        {
            string ver = ReadStr();
            Utils.Assert(ver == Version.Frontend, $"Version mismatch: {ver} != {Version.Frontend}");
        }

        public void ConsumeBeginMark()
        {
            AssertHeader('[');
            Advance();
        }

        public void ConsumeEndMark()
        {
            AssertHeader(']');
            Advance();
        }

        public void ConsumeLeftParen()
        {
            AssertHeader('(');
            Advance();
        }

        public void ConsumeRightParen()
        {
            AssertHeader(')');
            Advance();
        }

        public bool MatchEndMark()
        {
            bool ok = current == "]";
            if (ok) Advance();
            return ok;
        }
    }

    public class CodeObjectDeserializer : ObjectDeserializer
    {
        public CodeObjectDeserializer(string buffer, string source) : base(buffer, source) {}
        public CodeObject ReadCode()
        {
            CodeObject co = new CodeObject();
            co.source = source;
            ConsumeLeftParen();

            ConsumeBeginMark();
            co.filename = ReadStr();
            co.mode = (CompileMode)ReadInt();
            ConsumeEndMark();

            co.name = ReadStr();
            co.isGenerator = ReadBool();

            ConsumeBeginMark();
            while (!MatchEndMark())
            {
                Bytecode bc = ReadStruct<Bytecode>();
                co.codes.Add(bc);
            }

            ConsumeBeginMark();
            while (!MatchEndMark())
            {
                int iblock = ReadInt();
                co.iblocks.Add(iblock);
            }

            ConsumeBeginMark();
            while (!MatchEndMark())
            {
                int line = ReadInt();
                co.lines.Add(line);
            }

            Utils.Assert(co.lines.Count == co.codes.Count);

            ConsumeBeginMark();
            while (!MatchEndMark())
            {
                object o = ReadObject();
                co.consts.Add(o);
            }

            ConsumeBeginMark();
            while (!MatchEndMark())
            {
                StrName name = ReadName();
                co.varnames.Add(name);
            }

            ConsumeBeginMark();
            while (!MatchEndMark())
            {
                CodeBlock block = ReadStruct<CodeBlock>();
                co.blocks.Add(block);
            }

            ConsumeBeginMark();
            while (!MatchEndMark())
            {
                StrName name = ReadName();
                int pos = ReadInt();
                co.labels[name] = pos;
            }

            ConsumeBeginMark();
            while (!MatchEndMark())
            {
                FuncDecl decl = new FuncDecl();
                decl.code = ReadCode();
                ConsumeBeginMark();
                while (!MatchEndMark())
                {
                    int arg = ReadInt();
                    decl.args.Add(arg);
                }

                ConsumeBeginMark();
                while (!MatchEndMark())
                {
                    int key = ReadInt();
                    object value = ReadObject();
                    decl.kwargs.Add(new FuncDecl.KwArg(key, value));
                }

                decl.starredArg = ReadInt();
                decl.starredKwarg = ReadInt();
                decl.nested = ReadBool();

                co.funcDecls.Add(decl);
            }

            ConsumeRightParen();
            return co;
        }

        public CodeObject Deserialize()
        {
            VerifyVersion();
            CodeObject co = ReadCode();
            while (pos < tokens.Length)
            {
                StrName key = ReadName();
                string value = ReadStr();
                CodeObject.nameMapping[key] = value;
            }
            return co;
        }
    }

    public class CompileErrorObjectDeserializer : ObjectDeserializer
    {
        public CompileErrorObjectDeserializer(string buffer, string source) : base(buffer, source) {}

        public CompileErrorObject Deserialize()
        {
            CompileErrorObject ceo = new CompileErrorObject();
            ceo.type = ReadStr();
            ceo.msg = ReadStr();
            ceo.filename = ReadStr();
            ceo.lineNo = ReadInt();
            ceo.name = ReadStr();
            ceo.columnNo = ReadInt();

            while (pos < tokens.Length)
            {
                ceo.snapshot += tokens[pos];
                ceo.snapshot += "\n";
                pos++;
            }
            return ceo;
        }
    }
}
