# DCSSynchronizer

A tool to create DTB with synthetic speech directly in DCSArchive, that is using a dtbook source e-book fra DCSArchive and saving the generated DTB to DCSArchive

## Installation

Before installation, make sure you have an installation zip file created by the [Package.ps1](Package.ps1) PowerShell script.

In order for the tool to work, make sure the following is installed:

- Carsten SAPI 5 voice (or any other SAPI 5 voice wanted)
- SpeechPlatformRuntime (x86 version)
- At least one tts for SpeechPlatformRuntime (e.g. MSSpeech_TTS_en-US_ZiraPro)

All can be found in `N:\Software\Nota\DCSSynchronizer`

Now unzip the files in installation zip file (e.g. DCSSynchronizerV1.0.0.35244.zip) to a local directory (e.g. C:\DCSSynchronizer)

## Test the installation

By default the folder `\\smb-files\Temp\DCSSynchronizer` is used as temp folder. Both the user running DCSSynchronizer and the http-DCSArchive service must have acces to this folder. The user running DCSSynchronizer must also have read access to `\\smb-dcsarchive`.

To test the installation, run the following from a command prompt:

```
C:\DCSSynchronizer>.\DCSSynchronizer.exe -sourcecode RUTE -destcode RUTL -year 2018 -number 37 -force -usedcspro
```
