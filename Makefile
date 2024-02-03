.PHONY: all clean linux-x64 win-x64 linux-arm64 linux-arm

linux-x64:
	dotnet publish -c Release -r linux-x64 -p:PublishDir=bin/publish_dist/ -p:AssemblyName=ssdp-app-linux-x64 --self-contained true
win-x64:
	dotnet publish -c Release -r win-x64 -p:PublishDir=bin/publish_dist/ -p:AssemblyName=ssdp-app-win-x64 --self-contained true
linux-arm64:
	dotnet publish -c Release -r linux-arm64 -p:PublishDir=bin/publish_dist/ -p:AssemblyName=ssdp-app-linux-arm64 --self-contained true
linux-arm:
	dotnet publish -c Release -r linux-arm -p:PublishDir=bin/publish_dist/ -p:AssemblyName=ssdp-app-linux-arm --self-contained true

all: | linux-x64 win-x64 linux-arm64 linux-arm
