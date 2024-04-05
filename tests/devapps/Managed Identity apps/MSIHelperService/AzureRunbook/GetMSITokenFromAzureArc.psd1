<# 
	This PowerShell script was automatically converted to PowerShell Workflow so it can be run as a runbook.
	Specific changes that have been made are marked with a comment starting with “Converter:”
#>
workflow GetMSITokenFromAzureArc {
	Param
    	(
        	[Parameter (Mandatory= $false)]
        	[object] $WebhookData,
        	[Parameter (Mandatory= $false)]
        	[String] $msi_uri = "http://localhost:40342/metadata/identity/oauth2/token",
        	[Parameter (Mandatory= $false)]
        	[String] $msi_api_version = "2020-06-01",
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
    		
            #Form the endpoint 
            $endpoint = "{0}?resource={1}&api-version={2}" -f $msi_uri,$msi_resource,$msi_api_version
            #$endpoint

    		$MSIResponse = "";
		
    		try 
    		{
        		Invoke-WebRequest -Method GET -Uri $endpoint -Headers @{Metadata='TRUE'} -UseBasicParsing
    		}
    		catch {

        		$wwwAuthHeader = $_.Exception.Response.Headers["WWW-Authenticate"]

                if ($wwwAuthHeader -match "Basic realm=.+")
                {
                    $secretFile = ($wwwAuthHeader -split "Basic realm=")[1]
                }

                $secret = cat -Raw $secretFile

                try
                {
                    $response = Invoke-WebRequest -Method GET -Uri $endpoint -Headers @{Metadata='True'; Authorization="Basic $secret"} -UseBasicParsing
                    
                    #$MSIResponse
                    $MSIContent = $response.Content;
                    $MSIContent;
                }
                catch [System.Net.WebException] {
                    $ErrorMessage = $_.ErrorDetails
                    $ErrorMessage
                }
    		}

            
	}
}
