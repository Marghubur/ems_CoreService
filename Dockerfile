#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

ENV ASPNETCORE_ENVIRONMENT Production

COPY ["ems_CoreService/ems_CoreService.csproj", "ems_CoreService/"]
COPY ["ServiceLayer/ServiceLayer.csproj", "ServiceLayer/"]
COPY ["EMailService/EMailService.csproj", "EMailService/"]
RUN dotnet restore "ems_CoreService/ems_CoreService.csproj"
COPY . .
WORKDIR "/src/ems_CoreService"
RUN dotnet build "ems_CoreService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ems_CoreService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ems_CoreService.dll"]