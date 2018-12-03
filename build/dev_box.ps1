function Get-VS2017-Installed-Components {
    # Install-Module works with Powershell 5 (Win10)
    # The VSSetup module provides an API for dev15+ installation, i.e. it does not cover VS 2015 and lower
    #$vsSetupModule = 
    if ((Get-Module -ListAvailable -Name "VSSetup") -eq $null) {
        Install-Module "VSSetup" -Scope CurrentUser
    }

    # Select the latest VS2017 installed
    $vs_2017_installation = Get-VSSetupInstance | Select-VSSetupInstance -Version '15.0' -Latest

    if ($vs_2017_installation -eq $null) {
        throw "Could not find a Visual Studio 2017 installation. Please install Visual Studio 2017 Community, Profession or Enterprise 
        with the following workloads: .Net Desktop Development, Azure Development, Mobile Development with .Net"
    }

    if ($vs_2017_installation.Count > 1) {
        throw "Multiple Visual Studio 2017 installations found. Configuration not supported by this script. "
    }

    # Get the package IDs - these can be cross referened with the official list at 
    # https://docs.microsoft.com/en-us/visualstudio/install/workload-component-id-vs-enterprise
    $vs_2017_installed_components = $vs_2017_installation.Packages | ForEach-Object { $_.Id }

    # Print the components to the console
    $vs_2017_installed_components
}


function Get-Required-Components {
    if ( (Test-Path -Path "vs2017_required_components.txt") -eq $false) {
        throw "Internal Error - could not find the list of required components (vs2017_required_components.txt)"
    }

    return  [System.IO.File]::ReadAllLines( ( Resolve-Path "vs2017_required_components.txt" ) )
}

function Get-Missing-VS2017-Components {
    $installed_components = Get-VS2017-Installed-Components
    $required_components = Get-Required-Components

    $missing_components = $required_components | Where {$installed_components -NotContains $_}
    return $missing_components
}

function Print-Missing-Components {
    $missing_components = Get-Missing-VS2017-Components

    if ($missing_components.Count -gt 0 ) {
        Write-Host "There are $($missing_components.Count) missing components:"
        $missing_components | ForEach-Object {Write-Host $_}
        Write-Host "See https://docs.microsoft.com/en-us/visualstudio/install/workload-component-id-vs-enterprise for a list with display names."
    }
    else {
        Write-Host "There are no missing components in your Visual Studio installation. If you still have problems building, see the build troubleshooting page on GitHub"
    }
}

Print-Missing-Components

