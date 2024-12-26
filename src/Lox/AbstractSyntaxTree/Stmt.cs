namespace Lox;

// Generated code; see AstGenerator to make changes.
internal abstract class Stmt
{
    public abstract T Accept<T>(IVisitor<T> visitor);

    internal interface IVisitor<T>
    {
        T VisitBlockStmt(Block stmt);

        T VisitClassStmt(Class stmt);

        T VisitExpressionStmt(Expression stmt);

        T VisitFunctionStmt(Function stmt);

        T VisitIfStmt(If stmt);

        T VisitPrintStmt(Print stmt);

        T VisitReturnStmt(Return stmt);

        T VisitVarStmt(Var stmt);

        T VisitWhileStmt(While stmt);
    }

    internal class Block : Stmt
    {
        public List<Stmt> Statements { get; }

        public Block(List<Stmt> statements)
        {
            Statements = statements;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitBlockStmt(this);
        }
    }

    internal class Class : Stmt
    {
        public Token Name { get; }
        public Expr.Variable? Superclass { get; }
        public List<Function> Methods { get; }

        public Class(Token name, Expr.Variable? superclass, List<Function> methods)
        {
            Name = name;
            Superclass = superclass;
            Methods = methods;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitClassStmt(this);
        }
    }

    internal class Expression : Stmt
    {
        public Expr Expr { get; }

        public Expression(Expr expr)
        {
            Expr = expr;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitExpressionStmt(this);
        }
    }

    internal class Function : Stmt
    {
        public Token Name { get; }
        public List<Token> Params { get; }
        public List<Stmt> Body { get; }

        public Function(Token name, List<Token> @params, List<Stmt> body)
        {
            Name = name;
            Params = @params;
            Body = body;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitFunctionStmt(this);
        }
    }

    internal class If : Stmt
    {
        public Expr Condition { get; }
        public Stmt ThenBranch { get; }
        public Stmt? ElseBranch { get; }

        public If(Expr condition, Stmt thenBranch, Stmt? elseBranch)
        {
            Condition = condition;
            ThenBranch = thenBranch;
            ElseBranch = elseBranch;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitIfStmt(this);
        }
    }

    internal class Print : Stmt
    {
        public Expr Expr { get; }

        public Print(Expr expr)
        {
            Expr = expr;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitPrintStmt(this);
        }
    }

    internal class Return : Stmt
    {
        public Token Keyword { get; }
        public Expr? Value { get; }

        public Return(Token keyword, Expr? value)
        {
            Keyword = keyword;
            Value = value;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitReturnStmt(this);
        }
    }

    internal class Var : Stmt
    {
        public Token Name { get; }
        public Expr? Initializer { get; }

        public Var(Token name, Expr? initializer)
        {
            Name = name;
            Initializer = initializer;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitVarStmt(this);
        }
    }

    internal class While : Stmt
    {
        public Expr Condition { get; }
        public Stmt Body { get; }

        public While(Expr condition, Stmt body)
        {
            Condition = condition;
            Body = body;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitWhileStmt(this);
        }
    }
}
