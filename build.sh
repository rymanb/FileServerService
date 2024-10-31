#dotnet build ./FileServerService.sln /property:GenerateFullPaths=true /consoleloggerparameters:NoSummary;ForceNoAlign 
docker image build --pull --file './Dockerfile' --tag 'fileserverservice:latest' --label 'com.microsoft.created-by=visual-studio-code' --platform 'linux/amd64' './'
az acr login --name rbcs399registry
docker tag fileserverservice rbcs399registry.azurecr.io/fileserverservice:latest
docker push rbcs399registry.azurecr.io/fileserverservice:latest

