# TiledRescale

A .net core library for resizing Tiled maps and correctly rescaling data in .Tmx files.  The Tiled editor has a map resize function but does not support data rescaling - this library will resize maps and recreate tile and object data scaled to match map dimensions.

### Tested / working

* Map element w/h scales
* Layer element w/h scales
* Data element (raw tile data) transformed to scale
* Object element x/y w/h scales
* Polyline element Points attribute transformed to scale

### Using TiledRescale lib

Rescaling with explicit width/height
```cs
var rescaler = new TiledRescale.Rescaler();
var message = rescaler.RescaleMap("map.tmx", 256, 512, null);
```
Rescaling by scale
```cs
var rescaler = new TiledRescale.Rescaler();
var message = rescaler.RescaleMap("map.tmx", null, null, 1.33f);
```

### Using ConsoleTest app

Recursively target .tmx files in a directory
```cs
dotnet ConsoleTest.dll -d "c:\dev\projects\tmx\testfiles" --scale 1.33
```

Target a specific file
```cs
dotnet ConsoleTest.dll -f "c:\dev\projects\tmx\testfiles\lvl3\map2.tmx" -w 512 -h 512
```

