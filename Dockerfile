FROM mcr.microsoft.com/dotnet/sdk:6.0

LABEL org.opencontainers.image.source=https://github.com/NordSecurity/uniffi-bindgen-cs

RUN curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh -s -- -y --default-toolchain=1.72

RUN apt-get update && apt-get install -y --no-install-recommends build-essential && apt-get clean

RUN dotnet tool install -g csharpier
RUN echo 'export PATH="$PATH:/root/.dotnet/tools"' >> /root/.bashrc
