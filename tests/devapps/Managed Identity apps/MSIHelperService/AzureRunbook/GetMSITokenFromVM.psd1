<# 
	This PowerShell script was automatically converted to PowerShell Workflow so it can be run as a runbook.
	Specific changes that have been made are marked with a comment starting with “Converter:”
#>
workflow GetMSITokenFromVM {
	Param
    	(
        	[Parameter (Mandatory= $false)]
        	[object] $WebhookData,
        	[Parameter (Mandatory= $false)]
        	[String] $msi_uri = "http://169.254.169.254/metadata/identity/oauth2/token",
        	[Parameter (Mandatory= $false)]
        	[String] $msi_api_version = "2018-02-01",
        	[Parameter (Mandatory= $false)]
        	[String] $msi_resource = "https://management.azure.com"
    	
	)
	# Converter: Wrapping initial script in an InlineScript activity, and passing any parameters for use within the InlineScript
	# Converter: If you want this InlineScript to execute on another host rather than the Automation worker, simply add some combination of -PSComputerName, -PSCredential, -PSConnectionURI, or other workflow common parameters (https://learn.microsoft.com/powershell/module/psworkflow/about/about_workflowcommonparameters?view=powershell-5.1) as parameters of the InlineScript
	inlineScript {
		$WebhookData = $using:WebhookData
		$msi_uri = $using:msi_uri
		$msi_api_version = $using:msi_api_version
		$msi_resource = $using:msi_resource
		
		
    		#Get data from the Webhook Data Request Header
    		$RunbookHeaders = $WebhookData.RequestHeader
		
    		#ErrorActionPreference set to Stop
    		$ErrorActionPreference = "Stop"
		
    		#Uncomment the below line to print the MSAL URI formed my MI
    		#$RunbookHeaders.MSI_URI;
    		
    		$MSIResponse = "";
		
    		try 
    		{
        		#Form the URI 
        		$uri = $RunbookHeaders.MSI_URI;
        		$MSIResponse = Invoke-WebRequest -UseBasicParsing -Uri $uri -Method GET -Headers @{Metadata="TRUE"}
        		#$MSIResponse
        		$MSIContent = $MSIResponse.Content;
        		$MSIContent;
    		}
    		catch [System.Net.WebException] {
        		$ErrorMessage = $_.ErrorDetails
        		$ErrorMessage

    		}
	}
}
