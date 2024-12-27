ARGS    :=
AST_DIR := src/Lox/AbstractSyntaxTree
AST_PRJ := src/AstGenerator/AstGenerator.csproj
LOX_PRJ := src/Lox/Lox.csproj
SLN     := cslox.sln

.DEFAULT_GOAL = run

.PHONY: build clean publish run tree

build:
	@echo "Building solution..."
	@dotnet build $(SLN)

clean:
	@echo "Removing build artifacts..."
	@dotnet clean $(SLN)
	@echo "Removing published executable..."
	@rm -f cslox # must match <AssemblyName>

publish:
	@echo "Publishing executable..."
	@dotnet publish $(LOX_PRJ) \
		--configuration Release \
		--property:PublishSingleFile=true \
		--self-contained true \
		--output $(PWD)

run:
	@dotnet run --project $(LOX_PRJ) -- $(ARGS)

tree:
	@echo "Generating abstract syntax tree..."
	@dotnet run --project $(AST_PRJ) -- $(AST_DIR)
