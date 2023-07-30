SLN := cslox.sln
LOX_PRJ := src/Lox/Lox.csproj
AST_PRJ := src/AstGenerator/AstGenerator.csproj
IR_DIR := ~/code/cslox/src/Lox/IR
FLAG :=

.DEFAULT_GOAL = run

.PHONY: clean build run ast-gen

clean:
	dotnet clean $(SLN)

build:
	dotnet build $(SLN)

run:
	dotnet run --project $(LOX_PRJ) -- $(FLAG)

ast-gen:
	dotnet run --project $(AST_PRJ) -- $(IR_DIR)
