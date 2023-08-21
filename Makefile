AST_DIR := src/Lox/AbstractSyntaxTree
AST_PRJ := src/AstGenerator/AstGenerator.csproj
FLAG    :=
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

publish:
	@echo "Publishing executable..."
	@dotnet publish $(LOX_PRJ) \
		--configuration Release \
		--property:PublishSingleFile=true \
		--self-contained true \
		--output $(PWD)

run:
	@dotnet run --project $(LOX_PRJ) -- $(FLAG)

tree:
	@echo "Generating abstract syntax tree..."
	@dotnet run --project $(AST_PRJ) -- $(AST_DIR)
