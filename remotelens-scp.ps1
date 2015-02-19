param(
    [ValidateSet("AnyCPU")]
    [string]$platform = "AnyCPU",

    [string]$target = "Rebuild",
    
    [string]$verbosity = "Minimal",

    [string]$packageVersion = "0.0.0",

    [ValidateSet("debug", "release")]
    [string]$config = "release"
)

# Initialization
$rootFolder = Split-Path -parent $script:MyInvocation.MyCommand.Path
. $rootFolder\myget.include.ps1

# Solution
$solutionName = "remotelens-scp"
$solutionFolder = Join-Path $rootFolder "src\remotelens-scp"
$outputFolder = Join-Path $rootFolder "bin"
$releasesDirectory = Join-Path $solutionFolder "Releases"
$thirdpartyFolder = Join-Path $rootFolder "thirdparty"

# MyGet
$env:CLI_BUILD = $true

# .NET
$targetFramework = "v4.5.1"
$MSBuildPath = "${env:ProgramFiles(x86)}\MSBuild\14.0\Bin\msbuild.exe"

# Make targets
function Build {

	$buildOutputFolder = Join-Path $outputFolder "$packageVersion\$platform\$config\$targetFramework"

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

Build

MyGet-Build-Success