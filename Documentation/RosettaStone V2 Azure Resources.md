# RosettaStone V2

## Steps to Create Azure Resources
1. Clone the [RosettaStone_V2 github repository](https://github.com/HISC/RosettaStone_V2):
    1. Open git bash
    2. Navigate to the root directory:
        1. "cd /c/"
        2. Create a repos directory
        3. "mkdir repos"
        4. Change your directory to the newly created repos directory
        5. "cd repos"
        6. Clone the repository from github
        7. "git clone https://github.com/HISC/RosettaStone_V2.git"
2. Submit a ticket to the PitCrew to elevate your permissions to have "Contributor" to the appropriate Azure Subscription.
    1. We will be running against the HISC-DEV, HISC-QA, and HISC-PROD Azure Subscriptions
3. For each Azure Subscription, once permissions have been elevated
    1. Using the Azure CLI:
        1. "az login" into your Azure account
        2. "az account list --output table" to see which Azure Subscription is the Default
            1. The Default subscription is where the Azure resources will be created when running terraform
        3. If you need to change the Default Azure Subscription:
            1. "az account set --subscription [Subscription GUID]"
            2. "az account list --output table" to verify that the Default is set to the desired Subscription
    2. Execute the terraform scripts using the relevant subscription directory tfvar file
        1. In git bash, navigate to the terraform directory
        2. It is HIGHLY recommended that you review the terraform script before running so that you have an understanding.
            1. run "terraform init" to initialize terraform
            2. run "terraform plan -var-file="secrets.tfvars" -var-file="hisc-[dev,qa,prod]/[development,qa,prod].tfvars"" to review. If you are happy with what will occur, continue.
            3. run "terraform apply -var-file="secrets.tfvars" -var-file="hisc-[dev,qa,prod]/[development,qa,prod].tfvars""
				1. take time to review the proposed changes, and then type "yes" when prompted to create the Azure resources
		3. If you are creating the resources for multiple Azure Subscriptions
			1. Remove the terraform.tfstate file between runs. This may not be needed but is what I have done.
4. After the resources have been created
    1. Login to Azure via a browser
    2. Navigate to the hisc-[Environment]-rosettav2-as App Service
        1. *note:* As of 2019-09-23, terraform does not have a way to set the following values and therefore we need to do this step manually
        2. Select the "Configuration" menu item under "Settings"
        3. Select the "General settings" tab
        4. Change the Stack to ".NET Core"
        5. Set the Major and Minor version to ".NET Core 3.0"
			1. Couple things to note:
				1. The .NET Core 3.0 Framework is not a Long Term Support (LTS) version
				2. .NET Core 3.1, IS a LTS version and our intent is to have the service moved to this as soon as it is available, however that is right now sometime [December 2019](https://dotnet.microsoft.com/platform/support/policy/dotnet-core)
				3. The Stack may need to be modified as you migrate the code to different versions in the future
		6. Under ARR affinity, Select Off.
        7. Click "Save"
5. Have PitCrew Create and Install a Self Renewing SSL Certificate for each environment.  As well as appropriate DNS pointing to the Public IP created from Terraform
6. Once they are done, we need to modify the Application Gateway Listener so that is accepts https requests on port 4567. Unfortunately, we are not able to edit the existing
   listener directly and have to create a dummy listener so that we can remove the originals usage of port 4567.
	1. Navigate to the hisc-[Environment]-rosettav2-ag Application Gateway in Azure
		1. Select the "Listeners" tab
		2. Click "+ Basic" button
		3. Set Listner name: temp-listner
		4. Set Frontend IP: Public
		5. Click "Add"
		6. Wait for it to Save.
		7. Select the "Rules" tab
		8. Click the hisc-[Environment]-rosettav2-vnet-rq-rt rule
		9. Click "Edit"
		10. Change the Listener to "temp-listener"
		11. Click "Save".
		12. Wait for it to Save.
		13. Select the "Listeners" tab
		14. Click the "..." on the hisc-[Environment]-rosettav2-vnet-http-lstn, Click "Delete"
		15. Wait for it to Delete.
		16. Create a managed identity called hisc-[Environment]-rosettav2-identity
		17. Navigate to the keyvault and under Access Policies
			1. Add the Managed Identiy giving it full access to keys, secrets and certs
		18. Now we need to update the Key Vault to enable Soft Delete as it is required to add the cert (The Azure UI informs us of this if it is not enabled later when creating our new listner)
			1. Open PowerShell
			2. Run the following in order, changing the values betwe [] as needed

Connect-AzureRmAccount -TenantId 43e5deba-2c54-43a4-9a10-c9f10b1c66a5 -SubscriptionId [Environment SubscriptionId]

($resource = Get-AzureRmResource -ResourceId (Get-AzureRmKeyVault -VaultName "hisc-[Environment]-rosettav2-kv").ResourceId).Properties | Add-Member -MemberType "NoteProperty" -Name "enableSoftDelete" -Value "true"

Set-AzureRmResource -resourceid $resource.ResourceId -Properties $resource.Properties

		19. Navigate to the hisc-[Environment]-rosettav2-ag Application Gateway in Azure
		20. Select Listeners
		21. Click "+ Basic" button
		22. Set Listner name: hisc-[Environment]-rosettav2-vnet-https-lstn
		23. Set Frontend IP: Public
		24. Select Protocol: HTTPS
		25. Change the Port: 4567
		26. Set "Choose a certificate": Create new
		27. Set "HTTPS"
		28. Under "HTTPS Certificate"
			1. Set "Choose a certificate" to: Choose a certificate from Key Vault
			2. Set Managed Identity to the identiy you created above
			3. Set Key Vault to hisc-[Environment]-rosettav2-kv
			4. Set Certificate to the only one availble
		29. Click Add.
		30. Wait for it to save
		31. Click the "..." on the temp-listener, Click "Delete"
		32. Wait for it to Delete.
	2. Once the above is done, you'll want to push a deployment to your environment and verify:
		1. The App Service is functional
		2. The App Gateway is functional


  
