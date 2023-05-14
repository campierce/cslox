SLN := cslox.sln
PRJ := src/Lox/Lox.csproj

.DEFAULT_GOAL = run

.PHONY: default clean build run

clean:
	dotnet clean $(SLN)

build:
	dotnet build $(SLN)

run:
	dotnet run --project $(PRJ)
