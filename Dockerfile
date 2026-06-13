FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Cms0053ClaimAttachmentDemo/Cms0053ClaimAttachmentDemo.csproj", "Cms0053ClaimAttachmentDemo/"]
RUN dotnet restore "Cms0053ClaimAttachmentDemo/Cms0053ClaimAttachmentDemo.csproj"
COPY . .
RUN dotnet publish "Cms0053ClaimAttachmentDemo/Cms0053ClaimAttachmentDemo.csproj" \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p wwwroot/uploads
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Cms0053ClaimAttachmentDemo.dll"]
