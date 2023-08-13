AST_PRJ := src/AstGenerator/AstGenerator.csproj
FLAG    :=
IR_DIR  := src/Lox/IR
LOX_PRJ := src/Lox/Lox.csproj
SLN     := cslox.sln

.DEFAULT_GOAL = run

.PHONY: ast-gen build clean publish run

ast-gen:
	@echo "Generating intermediate representation..."
	@dotnet run --project $(AST_PRJ) -- $(IR_DIR)

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
