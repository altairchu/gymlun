FROM mcr.microsoft.com/dotnet/sdk:10.0.404@sha256:bef136a371963aaa46ec97882845ef6f54dec15728bb4b128b1c2a400ffbc9b4 AS backend
WORKDIR /app
COPY src/backend .
RUN dotnet publish -c Release -o publish src/FoodDiary.API/FoodDiary.API.csproj
RUN dotnet publish -c Release -o publish/migrator src/FoodDiary.Migrator/FoodDiary.Migrator.csproj

FROM node:24-alpine AS frontend
WORKDIR /app
COPY src/frontend .
RUN yarn install
RUN yarn build

FROM mcr.microsoft.com/dotnet/aspnet:10.0.15@sha256:12b1135f5f8b3c0a214f2a680ff2033dff4f684f445d6c697d9524ceeb303586
WORKDIR /app
COPY --from=backend app/publish .
COPY --from=frontend app/dist frontend/dist
EXPOSE 8080

CMD ["/bin/sh", "-c", "dotnet migrator/FoodDiary.Migrator.dll && exec dotnet FoodDiary.API.dll"]
