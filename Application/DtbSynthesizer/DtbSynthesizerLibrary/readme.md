# Class Library for synthesizing DTBs

## Using

The library is available as a nuget package from N:\Software\Nota\Nuget_Packages

## Packaging

You create a new nuget package by running (in a PowerShell console from the current directory):

```
nuget pack -build -Properties Configuration=Release
```

## Deploying

You deploy a nuget package by running

```
nuget add -Source N:\Software\Nota\Nuget_Packages .\DtbSynthesizerLibrary.X.Y.Z.W.nupkg
```

Where `Y.X.Z.W` is the package version.
