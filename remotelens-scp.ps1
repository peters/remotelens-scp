param(
    [ValidateSet("x86", "x64")]
    [string]$platform = "x86",

    [string]$target = "Rebuild",
    
    [string]$verbosity = "Minimal",

    [string]$packageVersion = "0.0.0",

    [ValidateSet("debug", "ilrepack")]
    [string]$type = "debug"
)

# Initialization
$rootFolder = Split-Path -parent $script:MyInvocation.MyCommand.Path
. $rootFolder\myget.include.ps1

# Solution
$solutionName = "remotelens-scp"
$solutionFolder = Join-Path $rootFolder "src\remotelens-scp"
$outputFolder = Join-Path $rootFolder "bin"
$releasesDirectory = Join-Path $solutionFolder "Releases"

# MyGet
$env:CLI_BUILD = $true

# .NET
$targetFramework = "v4.5.1"
$MSBuildPath = "${env:ProgramFiles(x86)}\MSBuild\14.0\Bin\msbuild.exe"

# Make targets
function Build {

    $buildOutputFolder = Join-Path $outputFolder "$packageVersion\$config\$targetFramework"
 
    $constants = @(
        "DEPLOY"
    )

    $MSBuildCustomProperties = '/p:DefineConstants="{0}";SolutionDir="{1}\\"' -f (($constants -join ";"), $rootFolder)

    if($packageVersion -ne "0.0.0")  {
        MyGet-AssemblyVersion-Set -project $solutionFolder -version $packageVersion
    }

    $targetFrameworks = @($targetFramework)
    $platforms = @($platform)
	
	$projects = @(
		'src\remotelens-scp\remotelens-scp.csproj'
	)
	
    MyGet-Build-Solution -sln remotelens-scp.sln `
        -rootFolder $rootFolder `
		-projects $projects `
        -outputFolder $outputFolder `
        -version $packageVersion `
        -config $config `
        -target $target `
        -platforms $platforms `
        -targetFrameworks $targetFrameworks `
        -verbosity $verbosity `
		-excludeNupkgProjects $projects `
        -MSBuildCustomProperties $MSBuildCustomProperties `
		-MSBuildPath $MSBuildPath

}

switch ($type) {
    # Build in debug mode
    "debug" {
        $config = "Debug"
        Build
    }
    # Build installer
    "installer" {
        $config = "Release"
        Build
    }
}

MyGet-Build-Success