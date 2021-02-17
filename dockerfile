FROM mcr.microsoft.com/dotnet/core/sdk:3.1
WORKDIR /app/

COPY ./src/aws-xray-demo.Tests/*.csproj ./src/aws-xray-demo.Tests/
COPY ./src/aws-xray-demo/*.csproj ./src/aws-xray-demo/
COPY ./aws-xray-demo.sln ./
RUN dotnet restore

COPY ./src/ ./src/
ARG VERSION
RUN dotnet publish ./src/aws-xray-demo -c Release -o ./out/aws-xray-demo /p:Version=$VERSION

ENTRYPOINT ["dotnet", "test"]