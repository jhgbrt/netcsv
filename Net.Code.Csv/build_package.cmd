msbuild /t:Build /p:Configuration="Release 4.5"
msbuild /t:Build /p:Configuration="Release 4.0"
nuget pack Net.Code.Csv.nuspec
