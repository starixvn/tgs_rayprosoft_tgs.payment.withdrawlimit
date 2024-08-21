FROM hub.lumigame.com/base/dotnet/core/sdk:8.0 AS base
USER app
WORKDIR /app


FROM hub.lumigame.com/base/dotnet/core/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Tgs.Payment.WithdrawLimit/Tgs.Payment.WithdrawLimit.csproj", "Tgs.Payment.WithdrawLimit/"]
COPY ["Payment.WithdrawLimit.Core/Payment.WithdrawLimit.Core.csproj", "Payment.WithdrawLimit.Core/"]
COPY ["Payment.WithdrawLimit.Common/Payment.WithdrawLimit.Common.csproj", "Payment.WithdrawLimit.Common/"]
COPY ["Payment.WithdrawLimit.Repositories/Payment.WithdrawLimit.Repositories.csproj", "Payment.WithdrawLimit.Repositories/"]
RUN dotnet restore "./Tgs.Payment.WithdrawLimit/./Tgs.Payment.WithdrawLimit.csproj"
COPY . .
WORKDIR "/src/Tgs.Payment.WithdrawLimit"
RUN dotnet build "./Tgs.Payment.WithdrawLimit.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Tgs.Payment.WithdrawLimit.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
EXPOSE 5000
COPY --from=publish /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Development
ENTRYPOINT ["dotnet", "Tgs.Payment.WithdrawLimit.dll"]