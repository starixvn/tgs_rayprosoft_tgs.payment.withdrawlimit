#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG BuildConfiguration=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Tgs.Payment.WithdrawLimit/Tgs.Payment.WithdrawLimit.csproj", "Tgs.Payment.WithdrawLimit/"]
COPY ["Payment.WithdrawLimit.Core/Payment.WithdrawLimit.Core.csproj", "Payment.WithdrawLimit.Core/"]
COPY ["Payment.WithdrawLimit.Common/Payment.WithdrawLimit.Common.csproj", "Payment.WithdrawLimit.Common/"]
COPY ["Payment.WithdrawLimit.Repositories/Payment.WithdrawLimit.Repositories.csproj", "Payment.WithdrawLimit.Repositories/"]
RUN dotnet restore "Tgs.Payment.WithdrawLimit/Tgs.Payment.WithdrawLimit.csproj"
COPY . .
WORKDIR "/src/Tgs.Payment.WithdrawLimit"
RUN dotnet build "Tgs.Payment.WithdrawLimit.csproj" -c $BuildConfiguration -o /app/build

FROM build AS publish
ARG BuildConfiguration
RUN dotnet publish "Tgs.Payment.WithdrawLimit.csproj" -c $BuildConfiguration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgs.Payment.WithdrawLimit.dll"]