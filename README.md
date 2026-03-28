# SimpleChat

A real-time chat application built with .NET 10 Clean Architecture and Angular 21.

Based on the [Clean.Architecture.Solution.Template](https://github.com/jasontaylordev/CleanArchitecture) v10.8.0.

## Build

```bash
dotnet build SimpleChat.slnx
```

## Run

```bash
dotnet run --project src/Web
```

The API will be available at `https://localhost:7089` with Scalar API docs at `/scalar`.

## Test

```bash
dotnet test
```

Angular tests:

```bash
cd src/Web/ClientApp
npx ng test
```

## Code Styles & Formatting

The **.editorconfig** file defines coding styles for the solution (PascalCase public members, _camelCase private fields).

## Code Scaffolding

From the `src/Application/` folder:

```bash
dotnet new ca-usecase --name CreateMessage --feature-name Messaging --usecase-type command --return-type long
dotnet new ca-usecase -n GetConversations -fn Messaging -ut query -rt ConversationsVm
```
