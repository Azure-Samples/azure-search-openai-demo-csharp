ARG IMAGE=dotnet
ARG VARIANT=bullseye
ARG VERSION=7.0
FROM mcr.microsoft.com/devcontainers/${IMAGE}:${VERSION}-${VARIANT}

ARG BUILDARCH

RUN export DEBIAN_FRONTEND=noninteractive && \
    apt-get update && \
    apt-get install -y xdg-utils && \
    curl -fsSL https://aka.ms/install-azd.sh | bash -s -- -a ${BUILDARCH} && \   
    apt-get clean -y && \
    rm -rf /var/lib/apt/lists/*