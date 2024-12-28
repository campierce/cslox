namespace Lox;

internal class LoxList : ILoxCallable
{
    public int Arity => 0;

    public object Call(Interpreter interpreter, List<object> arguments)
    {
        LoxClass cls = new("list", null, []);
        return new Instance(cls);
    }

    public override string ToString() => "<native class>";

    private class Instance : LoxInstance
    {
        private readonly List<object> _list = [];

        public Instance(LoxClass cls) : base(cls) { }

        public override object Get(Token name)
        {
            return name.Lexeme switch
            {
                "add" => new Method(1, args =>
                {
                    _list.Add(args[0]);
                    return this;
                }),

                "clear" => new Method(0, _ =>
                {
                    _list.Clear();
                    return this;
                }),

                "get" => new Method(1, args =>
                {
                    int idx = ValidateIndex(args[0], name);
                    return _list[idx];
                }),

                "length" => new Method(0, _ =>
                {
                    return (double)_list.Count;
                }),

                "remove" => new Method(1, args =>
                {
                    int idx = ValidateIndex(args[0], name);
                    _list.RemoveAt(idx);
                    return this;
                }),

                "set" => new Method(2, args =>
                {
                    int idx = ValidateIndex(args[0], name);
                    _list[idx] = args[1];
                    return this;
                }),

                "toString" => new Method(0, _ =>
                {
                    return $"[{string.Join(", ", _list)}]";
                }),

                _ => throw new RuntimeError(name, $"Undefined property '{name.Lexeme}'.")
            };
        }

        public override void Set(Token name, object value)
        {
            throw new RuntimeError(name, "Can't set properties on a native instance.");
        }

        private int ValidateIndex(object index, Token token)
        {
            if (index is not double number
                || Math.Floor(number) != number)
            {
                throw new RuntimeError(token, "Index must be an integer.");
            }
            int idx = (int)number;
            if (idx < 0 || idx >= _list.Count)
            {
                throw new RuntimeError(token, "Index out of bounds.");
            }
            return idx;
        }

        private class Method : ILoxCallable
        {
            public delegate object Implementation(List<object> arguments);

            private readonly Implementation _implementation;

            public int Arity { get; }

            public Method(int arity, Implementation implementation)
            {
                Arity = arity;
                _implementation = implementation;
            }

            public object Call(Interpreter interpreter, List<object> arguments)
            {
                return _implementation(arguments);
            }
        }
    }
}
