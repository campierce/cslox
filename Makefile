.PHONY: default clean build run

default: run

clean:
	dotnet clean src/cslox.csproj

build:
	dotnet build src/cslox.csproj

run:
	dotnet run --project src/cslox.csproj
