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

    /// <summary>
    /// Represents a list instance.
    /// </summary>
    private class Instance : LoxInstance
    {
        private readonly List<object> _list = [];

        public Instance(LoxClass cls) : base(cls) { }

        public override object Get(Token name)
        {
            // Notice: like other methods in Lox, these are created _per access_ (here, it's so we
            // can close over the native list; in user methods, it's to close over `this`). That
            // means two accessed methods will never be equal, even if they're the same method on
            // the same list. That's not a problem per se; we make no promises to the user about
            // method equality. Just something to be aware of.
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
                    return $"list({string.Join(", ", _list)})";
                }),

                _ => throw new RuntimeError(name, $"Undefined property '{name.Lexeme}'.")
            };
        }

        public override void Set(Token name, object value)
        {
            throw new RuntimeError(name, "Can't set properties on a native instance.");
        }

        /// <summary>
        /// Validates that the given object is an integer that can index into this instance's list.
        /// </summary>
        /// <param name="index">The index to validate.</param>
        /// <param name="token">The method name token.</param>
        /// <returns>The index as an int.</returns>
        /// <exception cref="RuntimeError">Thrown if the index is invalid.</exception>
        private int ValidateIndex(object index, Token token)
        {
            if (index is not double number // all Lox numbers are doubles in the runtime
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

        /// <summary>
        /// Represents a callable method in a list instance.
        /// </summary>
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

            public override string ToString() => "<native fn>";
        }
    }
}
