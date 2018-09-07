# Class Library for merging DTBs

## Using

The library is available as a nuget package from N:\Software\Nota\Nuget_Packages

## Packaging

You create a new nuget package by running:

```
nuget pack -build -Properties Configuration=Release
```

## Deploying

You deploy a nuget package by running

```
nuget add .\DtbMerger2Library.X.Y.Z.W.nupkg -Source N:\Software\Nota\Nuget_Packages
```

Where `Y.X.Z.W` is the package version.
