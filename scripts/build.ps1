$EnvScript = ".build.env.ps1"

if(Test-Path $EnvScript)
{
    . $EnvScript
}
if("${Env:TIMESTAMPER_URL}" -eq "")
{
    Write-Error "ERROR: Env: TIMESTAMPER_URL must points to a valid timestamp service url"
    return
}
if("${Env:CERT_FILE}" -eq "" -or !(Test-Path ${Env:CERT_FILE}))
{
    Write-Error "ERROR: Env CERT_FILE must points to a valid certificate file file"
    return
}

$Version=Select-Xml -Path ..\FileFormat.Drako\FileFormat.Drako.csproj -XPath '/Project/PropertyGroup/Version' | ForEach-Object { $_.Node.InnerXML }
$nupkg="bin\Release\FileFormat.Drako.$Version.nupkg"

Write-Host "Building FileFormat.Drako Version $Version"
try
{
    Push-Location ../FileFormat.Drako
    dotnet clean
    if(Test-Path $nupkg) {
        Remove-Item $nupkg
    }



    dotnet pack /p:SignTool=${Env:SIGN_TOOL} /p:CertFile=${Env:CERT_FILE} /p:CertPassword=${Env:CERT_PASSWD} /p:TimestamperUrl=${Env:TIMESTAMPER_URL}/authenticode
    if($LASTEXITCODE -eq '1') {
        Write-Host "Build failed."
        return
    }
    Write-Host "Signing the package"
    dotnet nuget sign $nupkg --certificate-path ${Env:CERT_FILE} --certificate-password ${Env:CERT_PASSWD} --timestamper ${Env:TIMESTAMPER_URL}

}
finally
{
    Pop-Location
}
