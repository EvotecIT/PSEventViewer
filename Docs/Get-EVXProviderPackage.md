---
external help file: PSEventViewer-help.xml
Module Name: PSEventViewer
online version: https://github.com/EvotecIT/PSEventViewer
schema: 2.0.0
---
# Get-EVXProviderPackage
## SYNOPSIS
Inspects a portable provider package or lists EventViewerX-managed installations.

Package inspection verifies declared hashes and any detached signature before returning its typed definition. Without Path, the command returns the active machine-wide EventViewerX provider catalog.

## SYNTAX
### Package
```powershell
Get-EVXProviderPackage [[-Path] <string>] [<CommonParameters>]
```

## DESCRIPTION
Inspects a portable provider package or lists EventViewerX-managed installations.

Package inspection verifies declared hashes and any detached signature before returning its typed definition. Without Path, the command returns the active machine-wide EventViewerX provider catalog.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-EVXProviderPackage -Path 'C:\Path'
```


## PARAMETERS

### -Path
Optional .evxprovider package to verify and inspect.

```yaml
Type: String
Parameter Sets: Package
Aliases: FullName, OutputPath, PackagePath
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByValue, ByPropertyName)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.String`

## OUTPUTS

- `EventViewerX.Providers.EventProviderPackage`
- `EventViewerX.Providers.InstalledEventProviderPackage`

## RELATED LINKS

- None
