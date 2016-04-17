# 
# File: ChocolateyInstall.ps1
# 
# Author: Akira Sugiura (urasandesu@gmail.com)
# 
# 
# Copyright (c) 2016 Akira Sugiura
#  
#  This software is MIT License.
#  
#  Permission is hereby granted, free of charge, to any person obtaining a copy
#  of this software and associated documentation files (the "Software"), to deal
#  in the Software without restriction, including without limitation the rights
#  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
#  copies of the Software, and to permit persons to whom the Software is
#  furnished to do so, subject to the following conditions:
#  
#  The above copyright notice and this permission notice shall be included in
#  all copies or substantial portions of the Software.
#  
#  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
#  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
#  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
#  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
#  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
#  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
#  THE SOFTWARE.
#


$pkgName = [IO.Path]::GetFileName($env:chocolateyPackageFolder)
$chocoToolsPath = [IO.Path]::Combine($env:chocolateyPackageFolder, 'tools')


foreach ($hedge in (dir $env:chocolateyPackageFolder -r | ? { $_.Extension -eq '.hedge' })) {
    $src = $hedge.FullName
    $dst = $src -replace '\.hedge$', ''
    "Renaming '$src' to '$dst'..."
    Move-Item $src $dst -Force
}


"Creating the nuget package '$pkgName'..."
$nugetPackageFolder = [IO.Path]::Combine($chocoToolsPath, 'NuGet')
nuget pack ([IO.Path]::Combine($nugetPackageFolder, "$pkgName.nuspec")) -OutputDirectory $chocoToolsPath


$name = "$pkgName Source"
$source = $chocoToolsPath
"Registering the nuget source '$source' as '$name'..."
$namePattern = '^{0}$' -f [regex]::Escape($name)
$nameRecordPattern = '^\s+\d+\.\s+'
$nameExtractPattern = '^\s+\d+\.\s+(?<name>.*)(( \[Enabled\])|( \[Disabled\]))$'
$installedSources = 
    @(nuget sources list | 
        where { $_ -match $nameRecordPattern } | 
        foreach { if ($_ -match $nameExtractPattern) { $Matches['name'] } } | 
        where { $_ -match $namePattern })
if (0 -lt $installedSources.Length) {
    nuget sources update -name $name -source "$source"
} else {
    nuget sources add -name $name -source "$source"
}
