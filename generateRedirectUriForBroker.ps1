[Reflection.Assembly]::LoadWithPartialName("System.Web")
$client = new-object System.Net.WebClient 
$shell_app = new-object -com shell.application
$scriptpath = $MyInvocation.MyCommand.Path
# 
# Requirements
# Install and setup Java
# Install open ssl : https://code.google.com/p/openssl-for-windows/downloads/list
# Update openssl path below 
# Update to keystore folders for debug and release
# This will copy your debug.keystore to Desktop 
# taken from https://raw.githubusercontent.com/AzureAD/azure-activedirectory-library-for-android/dev/brokerRedirectPrint.ps1
$package_name = "com.your.app"
$javaFolder = "C:\Program Files (x86)\Java\jre7\bin"
$keytool = "$javaFolder\keytool.exe"
$openSSL = "C:\Program Files (x86)\Git\bin\openssl.exe"
$android_key_store = "C:\Users\$env:USERNAME\.android\debug.keystore"
$copy_store = "C:\Users\$env:USERNAME\Desktop\debug.keystore"


$release_alias = "your_release_alias"
$release_key_store_file = "C:\android_releases\release.keystore"
$release_pass="your_password"
$package_name_encoded = [System.Web.HttpUtility]::UrlEncode($package_name)  

# Android keystore file is saved with android password for debug
Write-Host "Debug key Store info"
if(Test-Path $android_key_store){
    # android debug keystore password is android that is set from ADT package
    Copy-Item $android_key_store -Destination $copy_store        
    printRedirect "android"  "androiddebugkey" $copy_store  
}else{
    Write-Host "Android debug.keystore file is not found. Please update the script" -ForegroundColor Red
}


# Release related redirectUri
Write-Host "Release key Store info"
printRedirect $release_pass $release_alias $release_key_store_file
 

Function printRedirect($pass, $alias, $key_store){
    Write-Host "Checking keystore for $key_store"
    if(Test-Path $key_store){               
        # use batch file since powershell invoke cmd returns differnt output
        cmd /c .\signature.cmd $keytool $pass $alias $key_store $openSSL
    
        # read tag
        $out = Get-Content  .\tag.txt
        Write-Host "TAG $out"
    
        #Url encode the tag
        $tag_encoded = [System.Web.HttpUtility]::UrlEncode($out)    
        Write-Host "RedirectURI: msauth://$package_name_encoded/$tag_encoded" -ForegroundColor Green    
    }else{
        Write-Host "Android $key_store file is not found. Please update the script" -ForegroundColor Red
    }
}
 